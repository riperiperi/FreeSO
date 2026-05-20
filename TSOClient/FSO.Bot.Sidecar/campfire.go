/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"embed"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"sort"
	"strings"
	"time"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
	cfencoding "github.com/campfire-net/campfire/cf-protocol/encoding"
	"github.com/campfire-net/campfire/cf-protocol/protocol"
	"github.com/campfire-net/campfire/pkg/beacon"
)

// CampfireConfig parameterises campfire bringup.
type CampfireConfig struct {
	Home         string
	CampfireID   string // if non-empty, reuse rather than create
	Description  string
	Declarations embed.FS // root dir "conventions/"
}

// Campfire is the sidecar's handle to its private campfire.
type Campfire struct {
	Client       *protocol.Client
	ID           string
	PublicKeyHex string

	// Router is the single Subscribe + dispatch loop for all registered
	// convention handlers. Replaces the per-op convention.Server goroutines
	// that were saturating SQLite under load. See dispatcher.go.
	Router *Router

	declCount int

	// recorder, when non-nil, captures BroadcastEvent calls instead of sending
	// them to a real campfire. Test-only. See bridges_test.go.
	recorder broadcastRecorder
}

// broadcastRecorder is the hook that lets unit tests observe bridge output
// without spinning up a campfire store. Implementations live in _test.go.
type broadcastRecorder interface {
	record(kind string, payload []byte, simID string)
}

// StartCampfire initialises a cf identity, creates or resumes an invite-only
// campfire, publishes declarations, and prints the admission block on stdout.
// It returns a handle the bridges use to broadcast events.
func StartCampfire(ctx context.Context, cfg CampfireConfig) (*Campfire, error) {
	client, initRes, err := protocol.Init(cfg.Home)
	if err != nil {
		return nil, fmt.Errorf("protocol.Init(%s): %w", cfg.Home, err)
	}
	pk := client.PublicKeyHex()
	log.Printf("identity %s at %s", shortKey(pk), initRes.IdentityPath)

	id := cfg.CampfireID

	// Before creating a new campfire, check for a persisted body campfire ID.
	// This implements I0-5: cf $CF must survive sidecar restarts.
	// Reading is best-effort: if FSO_USER is unset or the file is absent, fall
	// through to create a fresh campfire as before.
	//
	// Stickiness (freesoexperiment-f6d): once a persisted ID is found, verify
	// the membership record still exists in the local campfire store before
	// reusing it. If the store was wiped (e.g. /tmp cleanup, BOT_DATA path
	// change) the membership is gone and we must create a fresh campfire rather
	// than attempting to use an orphaned ID. The new ID is written back to
	// body-cf.id so the next restart resumes from it.
	if id == "" {
		if persisted, rerr := ReadBodyCfID(); rerr != nil {
			log.Printf("read body-cf.id: %v (creating new campfire)", rerr)
		} else if persisted != "" {
			// Verify the cached campfire is still reachable in our local store.
			if reachErr := isCampfireReachable(client, persisted); reachErr != nil {
				log.Printf("cached body-cf.id %s is unreachable (%v) — creating new campfire", shortID(persisted), reachErr)
				// Fall through to create; old ID will be overwritten on success.
			} else {
				id = persisted
				log.Printf("resumed body campfire from persona state: %s", shortID(id))
			}
		}
	}

	if id == "" {
		transportDir := filepath.Join(cfg.Home, "campfires")
		if err := os.MkdirAll(transportDir, 0o700); err != nil {
			return nil, fmt.Errorf("mkdir transport dir: %w", err)
		}
		res, cerr := client.Create(protocol.CreateRequest{
			Description:  cfg.Description,
			JoinProtocol: "invite-only",
			Transport:    protocol.FilesystemTransport{Dir: transportDir},
		})
		if cerr != nil {
			return nil, fmt.Errorf("create %s: %w", cfg.Description, cerr)
		}
		id = res.CampfireID
		log.Printf("created invite-only campfire: %s", id)

		// Persist the new ID so subsequent restarts resume the same campfire.
		if werr := WriteBodyCfID(id); werr != nil {
			log.Printf("write body-cf.id: %v (non-fatal — next restart will create a new campfire)", werr)
		}

		// Write body-cf-beacon (automataisland-f3c): portable beacon string for
		// Legion jail auto_join provisioning. Written once at campfire creation;
		// subsequent boots resume the same campfire and find an existing beacon file.
		// Idempotency guard: writeBodyCFBeaconIfAbsent skips write if file exists.
		if res.Beacon != nil {
			if beaconStr, encErr := encodeBeaconString(res.Beacon); encErr != nil {
				log.Printf("encode body-cf-beacon: %v (non-fatal — beacon file absent)", encErr)
			} else if wErr := writeBodyCFBeaconIfAbsent(beaconStr); wErr != nil {
				log.Printf("write body-cf-beacon: %v (non-fatal — legion jail provisioning may need manual setup)", wErr)
			} else {
				log.Printf("body-cf-beacon written for persona (%s)", shortID(id))
			}
		} else {
			log.Printf("body-cf-beacon: create result has no Beacon struct — skipping (non-fatal)")
		}
	} else {
		log.Printf("reusing campfire: %s", id)

		// On resume, ensure body-cf-beacon exists. Written at first creation; on a
		// fresh install with an existing body-cf.id (e.g. migration or data move),
		// the beacon file may be absent. Attempt to recover it from the local beacon
		// directory so the Legion jail auto_join can be provisioned.
		if existing, readErr := ReadBodyCFBeacon(); readErr != nil {
			log.Printf("read body-cf-beacon on resume: %v (non-fatal)", readErr)
		} else if existing == "" {
			// File absent — try to reconstruct from the beacon directory.
			if beaconStr, scanErr := scanBeaconStringForCampfire(id, beacon.DefaultBeaconDir()); scanErr != nil {
				log.Printf("body-cf-beacon absent and scan failed: %v — beacon file will be missing (non-fatal)", scanErr)
			} else if wErr := WriteBodyCFBeacon(beaconStr); wErr != nil {
				log.Printf("write body-cf-beacon (resume recovery): %v (non-fatal)", wErr)
			} else {
				log.Printf("body-cf-beacon written (resume recovery) for persona (%s)", shortID(id))
			}
		} else {
			log.Printf("body-cf-beacon already exists for persona — reusing (idempotent)")
		}
	}

	cf := &Campfire{
		Client:       client,
		ID:           id,
		PublicKeyHex: pk,
		Router:       NewRouter(),
	}

	// Publish declarations — idempotently, using cf's canonical
	// (convention, operation, version) keying.
	//
	// The pattern mirrors `cf convention promote` (see cmd/cf/cmd/
	// convention_promote.go in the campfire repo): read what's already on
	// the campfire, parse each via convention.Parse to get the typed
	// (convention, operation, version), skip publication when our local
	// declaration has the same key. No content hashing, no disk ledger —
	// the campfire is the source of truth for what's published, and cf's
	// own Parse/key machinery is the dedup primitive.
	//
	// We scope the read by Sender = our pubkey so we only look at OUR
	// publication history (this sidecar identity, this campfire). Sender
	// is an indexed filter in the store, so even on a large campfire this
	// is a fast lookup, not a full scan.
	//
	// On first boot the filtered Read returns zero messages → everything
	// publishes. On subsequent boots, returns N declarations we've already
	// published; matching keys skip. Declarations whose version we've
	// bumped (or whose convention has changed) publish fresh and the new
	// publication appears under the new key.
	//
	// The publish itself remains a raw protocol.SendRequest because that
	// IS how a convention is registered — there's no "convention for
	// declaring a convention." This is the bootstrap.
	decls, err := LoadDeclarations(cfg.Declarations)
	if err != nil {
		return nil, fmt.Errorf("load declarations: %w", err)
	}
	log.Printf("loaded %d convention declarations", len(decls))

	publishedKeys, exErr := readMyPublishedDeclarations(client, id, pk)
	if exErr != nil {
		// Non-fatal: degrade to unconditional publish so a transient read
		// failure doesn't block bringup. A re-bringup later catches up.
		log.Printf("declaration discovery: filtered read failed (%v) — proceeding with empty existing set (will publish all)", exErr)
		publishedKeys = map[string]bool{}
	}

	published, skipped := 0, 0
	for _, d := range decls {
		key := declarationKey(d)
		if publishedKeys[key] {
			skipped++
			continue
		}
		data, merr := json.Marshal(d)
		if merr != nil {
			log.Printf("marshal decl %s: %v", d.Operation, merr)
			continue
		}
		_, serr := client.Send(protocol.SendRequest{
			CampfireID: id,
			Payload:    data,
			// convention:operation is cf's canonical discovery tag. The cf
			// binary's `cf $CF help` uses convention.ListOperations which
			// reads this tag. We omit the per-op "freeso:<op>" tag here
			// (that's for invocation; the Router subscribes on it) — a
			// declaration broadcast that also carried freeso:<op> would
			// trip the Router's own subscription as a phantom request.
			Tags: []string{
				"convention:operation",
			},
		})
		if serr != nil {
			log.Printf("publish decl %s: %v", d.Operation, serr)
			continue
		}
		publishedKeys[key] = true
		published++
	}
	log.Printf("declarations: %d new/changed, %d unchanged (skipped), %d total local → %s",
		published, skipped, len(decls), shortID(id))
	cf.declCount = published + skipped

	// Share beacon so the operator can hand it to the agent.
	beacon := ""
	if out, berr := shareBeacon(client, id); berr == nil {
		beacon = out
	} else {
		log.Printf("share beacon: %v (admission still possible via --campfire-id)", berr)
	}

	// Write a small info file the admit-agent.sh helper can pick up.
	infoPath := filepath.Join(cfg.Home, ".admit-info")
	infoContent := fmt.Sprintf("CAMPFIRE_ID=%s\nIDENTITY_PK=%s\nDECLARATIONS=%d\n", id, pk, cf.declCount)
	if werr := os.WriteFile(infoPath, []byte(infoContent), 0o600); werr != nil {
		log.Printf("write .admit-info: %v (non-fatal)", werr)
	}

	printAdmissionBlock(id, beacon, cfg.Home)

	return cf, nil
}

// Close releases the campfire client.
func (c *Campfire) Close() error {
	if c == nil || c.Client == nil {
		return nil
	}
	return c.Client.Close()
}

// readMyPublishedDeclarations asks the campfire — via the cf SDK — what
// declaration messages THIS sidecar identity has previously published.
// Returns the set of (convention, operation, version) keys that exist, in
// the canonical format cf itself uses (see cmd/cf/cmd/convention_promote.go
// loadExistingDeclarations).
//
// Why Sender-filtered: the campfire may carry declarations from other
// publishers (e.g. a future cf-mcp). We only want to know what WE'VE
// already sent so we don't republish our own work. Sender filter is
// indexed in the store, so this stays cheap even on a large campfire.
//
// Why convention.Parse: getting the typed declaration runs the same
// validation the cf binary applies — schema correctness, required fields,
// signing-ready shape. A malformed historical message gets skipped rather
// than poisoning the publish decision.
func readMyPublishedDeclarations(client *protocol.Client, campfireID, myPubkeyHex string) (map[string]bool, error) {
	resp, err := client.Read(protocol.ReadRequest{
		CampfireID: campfireID,
		Sender:     myPubkeyHex,
		Tags:       []string{"convention:operation"},
		// SkipSync: our own publications are already in our local store —
		// client.Send writes to both the filesystem transport AND the local
		// SQLite store. We do not need to re-sync from the filesystem
		// transport to see what WE'VE already sent. Without SkipSync, the
		// SDK's syncIfFilesystem walks every .cbor file in the campfire
		// directory (47k+ on our body-campfire), costing 10-30s per
		// bringup just to learn things we already know.
		SkipSync: true,
	})
	if err != nil {
		return nil, fmt.Errorf("read my published declarations: %w", err)
	}
	out := make(map[string]bool, len(resp.Messages))
	for _, msg := range resp.Messages {
		decl, _, perr := convention.Parse(msg.Tags, msg.Payload, msg.Sender, "", nil)
		if perr != nil {
			// Historical malformed publication — skip rather than failing
			// the whole bringup. Could log if it becomes noisy.
			continue
		}
		out[declarationKey(decl)] = true
	}
	return out, nil
}

// declarationKey produces the canonical (convention, operation, version) key
// used to identify a declaration across publications. Matches the format in
// cf's own convention_promote.go for cross-tool consistency.
func declarationKey(d *convention.Declaration) string {
	return d.Convention + ":" + d.Operation + "@" + d.Version
}

// BroadcastEvent sends a single event to the campfire with the given tags.
// Used by bridges to relay perception / dialog / system events.
func (c *Campfire) BroadcastEvent(kind string, payload []byte, simID string) error {
	if c.recorder != nil {
		c.recorder.record(kind, payload, simID)
		return nil
	}
	tags := []string{"freeso:" + kind}
	if simID != "" {
		tags = append(tags, "sim:"+simID)
	}
	_, err := c.Client.Send(protocol.SendRequest{
		CampfireID: c.ID,
		Payload:    payload,
		Tags:       tags,
	})
	return err
}

// readSyntheticTick reads recent freeso:perception messages from the local
// campfire store and checks whether any of them contain the given corrToken.
// Used by the verify-perception handler to confirm the synthetic tick we just
// broadcast is visible in the store.
//
// afterTime is the lower-bound wall-clock: only messages sent at or after
// this time are considered. We subtract 1s to absorb clock skew between the
// broadcast and the store write.
func (c *Campfire) readSyntheticTick(corrToken string, afterTime time.Time) (bool, error) {
	if c.Client == nil {
		// No campfire client — broadcast path is broken. tick_seen will be false.
		return false, fmt.Errorf("campfire client is nil (broadcast path broken)")
	}
	afterNano := (afterTime.UnixNano() - int64(time.Second)) // 1s margin
	resp, err := c.Client.Read(protocol.ReadRequest{
		CampfireID:     c.ID,
		Tags:           []string{"freeso:perception"},
		AfterTimestamp: afterNano,
		SkipSync:       false, // must sync to see our own send
	})
	if err != nil {
		return false, fmt.Errorf("read synthetic tick: %w", err)
	}
	for _, msg := range resp.Messages {
		// Fast path: check for corrToken string before full JSON parse.
		if len(msg.Payload) == 0 {
			continue
		}
		var env struct {
			Token     string `json:"token"`
			Synthetic bool   `json:"synthetic"`
		}
		if jerr := json.Unmarshal(msg.Payload, &env); jerr != nil {
			continue
		}
		if env.Synthetic && env.Token == corrToken {
			return true, nil
		}
	}
	return false, nil
}

// LoadDeclarations reads every conventions/*.json into a convention.Declaration.
// Order is stable (sorted by filename). Ignores files starting with "_" so the
// generator script lives alongside the json without being parsed.
func LoadDeclarations(efs embed.FS) ([]*convention.Declaration, error) {
	entries, err := efs.ReadDir("conventions")
	if err != nil {
		return nil, fmt.Errorf("read embedded conventions: %w", err)
	}
	names := make([]string, 0, len(entries))
	for _, e := range entries {
		if e.IsDir() {
			continue
		}
		if filepath.Ext(e.Name()) != ".json" {
			continue
		}
		if strings.HasPrefix(e.Name(), "_") {
			continue
		}
		names = append(names, e.Name())
	}
	sort.Strings(names)

	var decls []*convention.Declaration
	for _, name := range names {
		data, rerr := efs.ReadFile("conventions/" + name)
		if rerr != nil {
			return nil, fmt.Errorf("read %s: %w", name, rerr)
		}
		var d convention.Declaration
		if jerr := json.Unmarshal(data, &d); jerr != nil {
			return nil, fmt.Errorf("parse %s: %w", name, jerr)
		}
		if d.Operation == "" {
			return nil, fmt.Errorf("parse %s: empty operation", name)
		}
		// Fill defaults the Parse() path would normally set.
		if d.Signing == "" {
			d.Signing = "member_key"
		}
		if d.Response == "" {
			d.Response = "sync"
		}
		decls = append(decls, &d)
	}
	return decls, nil
}

// conventionOpNames loads all embedded convention declarations and returns a
// slice of their operation names. Used to seed the smoke test's skill
// referential integrity audit.
//
// On load error (malformed JSON) the individual declaration is skipped and the
// error is logged; the returned slice contains only successfully-parsed ops.
// A completely empty slice causes the smoke test's count check to compare
// handler_count against 0, which will (correctly) fail if any handlers are
// registered — but this is an extremely unlikely path given the embedded files
// are statically compiled in.
func conventionOpNames(efs embed.FS) []string {
	decls, err := LoadDeclarations(efs)
	if err != nil {
		log.Printf("conventionOpNames: load declarations: %v — skill integrity audit will have empty reference set", err)
		return nil
	}
	names := make([]string, 0, len(decls))
	for _, d := range decls {
		if d.Operation != "" {
			names = append(names, d.Operation)
		}
	}
	return names
}

// printAdmissionBlock writes a paste-ready block to stdout so the operator can
// admit an agent identity with a single copy-paste. The format is fixed so
// scripts and humans can both consume it:
//
//	=== Agent admission (freeso-sidecar) ===
//	Campfire: <id>
//	Beacon:   <beacon or "unavailable">
//	Sidecar identity: <hex>
//	CF_HOME for this sidecar: <absolute path>
//
//	Admit a new agent (paste this on the operator machine):
//	  cf init                                         # if the agent has no identity yet
//	  AGENT_PK=$(cf id)                               # capture public key hex
//	  cf admit <id> $AGENT_PK                         # run on this sidecar host
//	  cf join <beacon>                                # run on the agent host
//
//	=== / Agent admission ===
//
// Operators can run the admit line as-is; the block is deliberately verbose
// so nothing has to be assembled by hand.
func printAdmissionBlock(id, beacon, cfHome string) {
	beaconStr := beacon
	if beaconStr == "" {
		beaconStr = "unavailable (use campfire id directly)"
	}
	lines := []string{
		"",
		"=== Agent admission (freeso-sidecar) ===",
		fmt.Sprintf("Campfire: %s", id),
		fmt.Sprintf("Beacon:   %s", beaconStr),
		fmt.Sprintf("Sidecar identity: %s", "(see logs at --cf-home)"),
		fmt.Sprintf("CF_HOME for this sidecar: %s", cfHome),
		"",
		"Admit a new agent (paste this on the operator machine):",
		"  # 1) On the agent machine, create an identity and capture its public key:",
		"  cf init",
		"  AGENT_PK=$(cf id)",
		"",
		fmt.Sprintf("  # 2) On this sidecar host (CF_HOME=%s), admit the agent:", cfHome),
		fmt.Sprintf("  CF_HOME=%s cf admit %s $AGENT_PK", cfHome, id),
		"",
		"  # 3) On the agent machine, join the campfire:",
	}
	if beacon != "" {
		lines = append(lines, fmt.Sprintf("  cf join %s", beacon))
	} else {
		lines = append(lines, fmt.Sprintf("  cf join %s", id))
	}
	lines = append(lines,
		"",
		"Or use the helper: scripts/admit-agent.sh <agent-name> (see README.md).",
		"=== / Agent admission ===",
		"",
	)
	for _, ln := range lines {
		fmt.Println(ln)
	}
}

// shareBeacon produces a portable beacon string for campfireID by scanning the
// default beacon directory. Returns the "beacon:<base64>" string on success.
//
// The beacon is published to the default beacon directory by protocol.Client.Create()
// during campfire creation. shareBeacon reads it back from disk so the admission
// block can display it without re-generating the beacon.
func shareBeacon(client *protocol.Client, campfireID string) (string, error) {
	return scanBeaconStringForCampfire(campfireID, beacon.DefaultBeaconDir())
}

// encodeBeaconString encodes a campfire beacon.Beacon struct as the canonical
// portable string: "beacon:" + base64.StdEncoding(CBOR(beacon)).
// This matches the format produced by `cf share` (cmd/cf/cmd/share.go).
func encodeBeaconString(b *beacon.Beacon) (string, error) {
	data, err := cfencoding.Marshal(b)
	if err != nil {
		return "", fmt.Errorf("encode beacon CBOR: %w", err)
	}
	return "beacon:" + base64.StdEncoding.EncodeToString(data), nil
}

// scanBeaconStringForCampfire scans beaconDir for a beacon whose campfire ID
// matches campfireID (exact hex match) and returns the encoded "beacon:<base64>"
// string. Returns an error if no matching beacon is found or encoding fails.
func scanBeaconStringForCampfire(campfireID, beaconDir string) (string, error) {
	beacons, err := beacon.Scan(beaconDir)
	if err != nil {
		return "", fmt.Errorf("scan beacons in %s: %w", beaconDir, err)
	}
	for i := range beacons {
		b := beacons[i]
		if b.CampfireIDHex() == campfireID {
			return encodeBeaconString(&b)
		}
	}
	return "", fmt.Errorf("no beacon found for campfire %s in %s", shortID(campfireID), beaconDir)
}

// writeBodyCFBeaconIfAbsent writes the body-cf-beacon file only if it does not
// already exist. This is the first-write-wins idempotency guard for the creation
// path: once a beacon is written, subsequent sidecar restarts preserve the same
// body cf and skip this write entirely.
func writeBodyCFBeaconIfAbsent(beaconStr string) error {
	existing, err := ReadBodyCFBeacon()
	if err != nil {
		// Treat read errors (other than not-exist) as "absent" and write anyway;
		// a corrupt file is worse than a fresh one.
		log.Printf("writeBodyCFBeaconIfAbsent: read check failed: %v — overwriting", err)
		return WriteBodyCFBeacon(beaconStr)
	}
	if existing != "" {
		log.Printf("writeBodyCFBeaconIfAbsent: beacon already exists — idempotent, skipping write")
		return nil
	}
	return WriteBodyCFBeacon(beaconStr)
}

// isCampfireReachable checks whether the sidecar's local campfire store holds
// a valid membership record for campfireID. If the store was wiped (e.g. the
// bot-data directory was deleted or /tmp was flushed after a reboot), the
// membership record will be absent and this returns an error — the caller
// should create a fresh campfire and overwrite body-cf.id.
//
// This is a local store query (no network) and is fast.
func isCampfireReachable(client *protocol.Client, campfireID string) error {
	membership, err := client.GetMembership(campfireID)
	if err != nil {
		return fmt.Errorf("GetMembership: %w", err)
	}
	if membership == nil {
		return fmt.Errorf("no membership record for %s", shortID(campfireID))
	}
	return nil
}

func shortID(id string) string {
	if len(id) <= 12 {
		return id
	}
	return id[:12] + "…"
}

func shortKey(k string) string {
	if len(k) <= 16 {
		return k
	}
	return k[:8] + "…" + k[len(k)-4:]
}

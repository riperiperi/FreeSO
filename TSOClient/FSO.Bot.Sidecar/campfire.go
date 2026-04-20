/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"embed"
	"encoding/json"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"sort"
	"strings"

	"github.com/campfire-net/campfire/pkg/convention"
	"github.com/campfire-net/campfire/pkg/protocol"
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
	} else {
		log.Printf("reusing campfire: %s", id)
	}

	cf := &Campfire{
		Client:       client,
		ID:           id,
		PublicKeyHex: pk,
		Router:       NewRouter(),
	}

	// Publish declarations.
	decls, err := LoadDeclarations(cfg.Declarations)
	if err != nil {
		return nil, fmt.Errorf("load declarations: %w", err)
	}
	log.Printf("loaded %d convention declarations", len(decls))

	published := 0
	for _, d := range decls {
		data, merr := json.Marshal(d)
		if merr != nil {
			log.Printf("marshal decl %s: %v", d.Operation, merr)
			continue
		}
		_, serr := client.Send(protocol.SendRequest{
			CampfireID: id,
			Payload:    data,
			// convention:operation lets cf clients discover ops via `cf read --tag convention:operation`.
			// We deliberately OMIT the per-op "freeso:<op>" tag here: convention handlers subscribe on
			// that tag, so including it would make each handler re-ingest its own declaration as a
			// phantom request. Discovery uses the broad tag; dispatch uses the narrow one.
			Tags: []string{
				"convention:operation",
			},
		})
		if serr != nil {
			log.Printf("publish decl %s: %v", d.Operation, serr)
			continue
		}
		published++
	}
	log.Printf("published %d/%d declarations to %s", published, len(decls), shortID(id))
	cf.declCount = published

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

// shareBeacon invokes the campfire library to produce a portable beacon. The
// beacon format lets `cf join <beacon>` bootstrap address + peers.
func shareBeacon(client *protocol.Client, campfireID string) (string, error) {
	// protocol.Client does not currently expose a public "Share" helper; the
	// `cf share` CLI does this via the beacon package. For scaffold stage we
	// return the raw id, which `cf join <id>` accepts when there is a local
	// config pointing at the transport. A follow-up item will promote this to
	// a proper beacon once we wire p2p-http transport. Track in rd.
	return "", fmt.Errorf("beacon generation deferred (use campfire-id directly)")
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

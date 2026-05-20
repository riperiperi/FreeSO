/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

// lot_cf.go — Lot-as-campfire side effect (automataisland-9b4).
//
// When purchase-lot succeeds, the sidecar auto-creates a per-lot campfire.
// The campfire is created asynchronously (in a goroutine) so the purchase-lot
// response returns immediately. On failure, the error is logged and a .err
// recovery hint file is written alongside the beacon path — recoverable via
// the ensure-lot-cf admin op.
//
// # Campfire ID
//
// The campfire ID is assigned by protocol.Client.Create() and is NOT
// deterministic from lot_id. The campfire library generates its own random
// ed25519 keypair on Create(). The campfire ID is stored in the beacon file
// at beaconDir/<lot_id>.beacon and is stable across sidecar restarts.
//
// DeriveLotCFIDs() provides a deterministic ed25519 keypair from lot_id for
// use as a lot-campfire signing identity (e.g. for message producers that need
// a stable identity tied to a lot). It is NOT the campfire ID.
//
// # Idempotency
//
// The beacon file is the idempotency guard. If beaconDir/<lot_id>.beacon exists
// and is non-empty, EnsureLotCF returns its content (the campfire ID) immediately
// without creating a new campfire. The beacon is written atomically (rename) as
// the final step of successful creation. Re-running on an already-created lot is
// a no-op.
//
// # Beacon publication
//
// Beacon files are written to /var/freeso/lot-beacons/<lot_id>.beacon. The file
// contains the campfire ID hex string + "\n". The founding-circle convention
// (Wave 5 build-coordination) reads from this directory to discover newly
// available lot campfires. Operators can override the beacon directory by
// setting LOT_CF_BEACON_DIR.
//
// # Recovery
//
// If lot-cf creation fails asynchronously, a .err file is written:
//
//	/var/freeso/lot-beacons/<lot_id>.beacon.err
//
// The ensure-lot-cf admin op re-tries creation for a given lot_id and is safe
// to call repeatedly (idempotent).

import (
	"context"
	"crypto/ed25519"
	"crypto/sha256"
	"encoding/hex"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"strconv"
	"strings"
	"time"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
	"github.com/campfire-net/campfire/cf-protocol/protocol"
)

const (
	// lotCFBeaconDirDefault is the default directory for lot beacon files.
	// Overridable via LOT_CF_BEACON_DIR environment variable.
	lotCFBeaconDirDefault = "/var/freeso/lot-beacons"

	// lotCFPrefix is the derivation prefix for lot campfire seeds.
	// Changing this prefix invalidates all previously derived lot-cf IDs.
	lotCFPrefix = "lot-cf:"
)

// LotCFConfig holds the parameters needed to create or ensure a lot campfire.
// Constructed in the purchase-lot success path and passed to SpawnLotCFAsync.
type LotCFConfig struct {
	// CfHome is the sidecar's campfire home directory (--cf-home).
	// The lot campfire transport lives at <CfHome>/lot-campfires/<lotID>/.
	CfHome string

	// LotID is the server-assigned lot ID from the purchase-lot response.
	LotID int64

	// OwnerPubKeyHex is the hex-encoded Ed25519 public key of the sidecar's
	// identity (the lot owner). Admitted as creator on the new lot campfire.
	OwnerPubKeyHex string

	// BeaconDir overrides the beacon directory when non-empty.
	// Default: LOT_CF_BEACON_DIR env var or /var/freeso/lot-beacons.
	BeaconDir string
}

// LotCFIDs holds the derived identifiers for a lot campfire.
type LotCFIDs struct {
	// CampfireID is the hex-encoded campfire public key (64 chars).
	CampfireID string
	// PrivateKey is the ed25519 private key for the lot campfire.
	PrivateKey ed25519.PrivateKey
	// PublicKey is the ed25519 public key for the lot campfire.
	PublicKey ed25519.PublicKey
}

// DeriveLotCFIDs deterministically derives a campfire-style ed25519 keypair
// from lot_id. This keypair is intended as a stable SIGNING identity for
// producers that write to the lot campfire — NOT as the lot campfire's id.
//
// IMPORTANT: The actual lot campfire ID is the one returned by EnsureLotCF
// and stored in beaconDir/<lot_id>.beacon. It is assigned randomly by
// protocol.Client.Create() and is NOT recoverable from this function. Wave 5
// build-coordination consumers must read the beacon file (or subscribe to the
// founding-circle beacon projection) to discover the actual campfire ID — do
// not call DeriveLotCFIDs(lotID).CampfireID expecting it to match.
//
// Derivation: seed = SHA256(lotCFPrefix + strconv.FormatInt(lotID, 10))
// The 32-byte SHA256 output is used as the ed25519 private key seed.
// The resulting "campfire ID" (hex-encoded public key, 64 chars) is stable
// across sidecar restarts for the same lot_id.
func DeriveLotCFIDs(lotID int64) LotCFIDs {
	input := lotCFPrefix + strconv.FormatInt(lotID, 10)
	seedBytes := sha256.Sum256([]byte(input))
	priv := ed25519.NewKeyFromSeed(seedBytes[:])
	pub := priv.Public().(ed25519.PublicKey)
	return LotCFIDs{
		CampfireID: hex.EncodeToString(pub),
		PrivateKey: priv,
		PublicKey:  pub,
	}
}

// effectiveBeaconDir returns the beacon directory in priority order:
// cfg.BeaconDir > LOT_CF_BEACON_DIR env > lotCFBeaconDirDefault.
func effectiveBeaconDir(cfg LotCFConfig) string {
	if cfg.BeaconDir != "" {
		return cfg.BeaconDir
	}
	if env := os.Getenv("LOT_CF_BEACON_DIR"); env != "" {
		return env
	}
	return lotCFBeaconDirDefault
}

// lotTransportDir returns the filesystem transport directory for a lot campfire.
// Layout: <cfHome>/lot-campfires/<lotID>/
func lotTransportDir(cfHome string, lotID int64) string {
	return filepath.Join(cfHome, "lot-campfires", strconv.FormatInt(lotID, 10))
}

// EnsureLotCF creates the lot campfire for lotID if it does not already exist.
// This function is idempotent: calling it for a lot_id that already has a campfire
// is a no-op (the existing campfire is returned unchanged).
//
// Idempotency mechanism: the beacon file at beaconDir/<lotID>.beacon is the
// authoritative record of whether the lot campfire has been created. If the file
// exists and is readable, its content (a 64-char hex campfire ID) is returned
// without creating a new campfire. This is safe because the beacon file is written
// atomically (rename) as the last step of a successful creation.
//
// Note on campfire ID: protocol.Client.Create() generates its own campfire keypair
// internally and does not accept a pre-supplied keypair. The returned campfire ID
// is random (not deterministic from lot_id). DeriveLotCFIDs() is retained for
// signing key derivation and is available for use by lot campfire message producers.
//
// Steps:
//  1. Check beacon file → if exists, return stored campfire ID (idempotent).
//  2. Create the campfire (filesystem transport, invite-only).
//  3. Admit OwnerPubKeyHex as creator (role=full).
//  4. Write beacon file to beaconDir/<lotID>.beacon.
//
// Returns the campfire ID on success, even if already existed.
func EnsureLotCF(cfg LotCFConfig) (campfireID string, err error) {
	transportDir := lotTransportDir(cfg.CfHome, cfg.LotID)
	beaconDir := effectiveBeaconDir(cfg)
	beaconPath := filepath.Join(beaconDir, strconv.FormatInt(cfg.LotID, 10)+".beacon")

	// Idempotency check: if beacon file exists, campfire was already created.
	if existing, readErr := os.ReadFile(beaconPath); readErr == nil {
		existingID := strings.TrimSpace(string(existing))
		if existingID != "" {
			log.Printf("lot-cf: lot %d campfire %s already exists (beacon found) — skipping creation",
				cfg.LotID, shortID(existingID))
			return existingID, nil
		}
	}

	// Open the sidecar's protocol client using the sidecar's own identity.
	client, _, initErr := protocol.Init(cfg.CfHome)
	if initErr != nil {
		return "", fmt.Errorf("EnsureLotCF lot %d: protocol.Init(%s): %w", cfg.LotID, cfg.CfHome, initErr)
	}
	defer client.Close()

	// Create the campfire with filesystem transport.
	// protocol.Client.Create() generates its own random campfire keypair.
	if mkErr := os.MkdirAll(transportDir, 0o700); mkErr != nil {
		return "", fmt.Errorf("EnsureLotCF lot %d: mkdir transport dir: %w", cfg.LotID, mkErr)
	}
	createResult, createErr := client.Create(protocol.CreateRequest{
		Description:  fmt.Sprintf("lot:%d", cfg.LotID),
		JoinProtocol: "invite-only",
		Transport:    protocol.FilesystemTransport{Dir: transportDir},
	})
	if createErr != nil {
		return "", fmt.Errorf("EnsureLotCF lot %d: create campfire: %w", cfg.LotID, createErr)
	}
	campfireID = createResult.CampfireID
	log.Printf("lot-cf: created campfire %s for lot %d", shortID(campfireID), cfg.LotID)

	// Admit owner as creator (role=full).
	// protocol.Client.Create() admits the sidecar's own identity as the first
	// member. We call Admit again so the owner's pubkey is explicitly recorded
	// in the campfire's filesystem transport (founding-circle Wave 5 reads it).
	// If cfg.OwnerPubKeyHex matches the sidecar's own identity, this is a no-op
	// at the protocol level but is still called for explicit intent.
	if cfg.OwnerPubKeyHex != "" {
		admitErr := client.Admit(protocol.AdmitRequest{
			CampfireID:      campfireID,
			MemberPubKeyHex: cfg.OwnerPubKeyHex,
			Role:            "full",
		})
		if admitErr != nil {
			// Non-fatal: campfire exists; admission failure is not a blocking error.
			// The sidecar's own identity is already a member via Create().
			log.Printf("lot-cf: lot %d admit owner %s: %v (non-fatal)", cfg.LotID, shortKey(cfg.OwnerPubKeyHex), admitErr)
		} else {
			log.Printf("lot-cf: lot %d admitted owner %s (role=full)", cfg.LotID, shortKey(cfg.OwnerPubKeyHex))
		}
	}

	// Write beacon file.
	if bErr := writeLotBeacon(cfg, campfireID); bErr != nil {
		// Non-fatal: campfire created, owner admitted. Beacon re-writable via ensure-lot-cf.
		log.Printf("lot-cf: lot %d write beacon: %v (non-fatal — campfire created)", cfg.LotID, bErr)
		return campfireID, nil
	}

	log.Printf("lot-cf: lot %d setup complete campfire_id=%s beacon_dir=%s", cfg.LotID, shortID(campfireID), effectiveBeaconDir(cfg))
	return campfireID, nil
}

// writeLotBeacon atomically writes the lot campfire beacon file.
// Content: campfire ID hex string + newline.
// Creates the beacon directory if it does not exist.
func writeLotBeacon(cfg LotCFConfig, campfireID string) error {
	beaconDir := effectiveBeaconDir(cfg)
	if err := os.MkdirAll(beaconDir, 0o755); err != nil {
		return fmt.Errorf("mkdir beacon dir %s: %w", beaconDir, err)
	}
	beaconPath := filepath.Join(beaconDir, strconv.FormatInt(cfg.LotID, 10)+".beacon")
	content := campfireID + "\n"
	tmp := beaconPath + ".tmp"
	if err := os.WriteFile(tmp, []byte(content), 0o644); err != nil {
		return fmt.Errorf("write beacon tmp: %w", err)
	}
	if err := os.Rename(tmp, beaconPath); err != nil {
		os.Remove(tmp) //nolint:errcheck
		return fmt.Errorf("rename beacon: %w", err)
	}
	return nil
}

// writeLotBeaconErr writes a recovery hint file alongside the (missing) beacon.
// Content: RFC3339 timestamp + lot_id + error message.
// Used so operators can discover failed lot-cf creations and run ensure-lot-cf.
func writeLotBeaconErr(cfg LotCFConfig, reason error) {
	beaconDir := effectiveBeaconDir(cfg)
	if mkErr := os.MkdirAll(beaconDir, 0o755); mkErr != nil {
		log.Printf("lot-cf: lot %d write .beacon.err: mkdir: %v", cfg.LotID, mkErr)
		return
	}
	errPath := filepath.Join(beaconDir, strconv.FormatInt(cfg.LotID, 10)+".beacon.err")
	content := fmt.Sprintf("%s lot_id=%d error=%v\n", time.Now().UTC().Format(time.RFC3339), cfg.LotID, reason)
	if wErr := os.WriteFile(errPath, []byte(content), 0o644); wErr != nil {
		log.Printf("lot-cf: lot %d write .beacon.err: %v", cfg.LotID, wErr)
	}
}

// SpawnLotCFAsync starts a background goroutine that calls EnsureLotCF.
// Called immediately after purchase-lot returns ok=true so the HTTP response
// is not delayed by campfire creation I/O.
// Failures are logged and written to a .beacon.err file for operator recovery.
func SpawnLotCFAsync(cfg LotCFConfig) {
	go func() {
		if _, ensureErr := EnsureLotCF(cfg); ensureErr != nil {
			log.Printf("lot-cf: lot %d async creation FAILED: %v — run ensure-lot-cf --lot_id %d to recover",
				cfg.LotID, ensureErr, cfg.LotID)
			writeLotBeaconErr(cfg, ensureErr)
		}
	}()
}

// RegisterEnsureLotCFHandler wires the ensure-lot-cf admin op.
//
// ensure-lot-cf is the one-shot recovery path for failed async lot-cf creation.
// Operators call it after discovering a .beacon.err file or when a lot campfire
// is expected but absent. Safe to call multiple times (idempotent).
//
// Required args: lot_id (int64 or float64 from JSON)
// Optional args: (none)
//
// Response: {ok: true, campfire_id: "<hex>", lot_id: <int>} on success.
//
// The cfHome parameter must be the same --cf-home path used when the sidecar
// was launched so the protocol.Client uses the sidecar's identity.
func RegisterEnsureLotCFHandler(ctx context.Context, cf *Campfire, cfHome string) (int, error) {
	handler := buildEnsureLotCFHandler(cf, cfHome)

	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		return 0, fmt.Errorf("load declarations: %w", err)
	}
	for _, d := range decls {
		if d.Operation == "ensure-lot-cf" {
			cf.Router.Register(d, handler)
			return 1, nil
		}
	}
	return 0, fmt.Errorf("declaration for op %q missing (expected in conventions/ensure-lot-cf.json)", "ensure-lot-cf")
}

// buildEnsureLotCFHandler returns the convention.HandlerFunc for ensure-lot-cf.
func buildEnsureLotCFHandler(bodyCF *Campfire, cfHome string) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		// Extract lot_id from args.
		rawLotID, ok := req.Args["lot_id"]
		if !ok || rawLotID == nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": "lot_id required (int64)"},
			}, nil
		}
		lotID, lotIDOk := coerceInt64(rawLotID)
		if !lotIDOk {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": fmt.Sprintf("lot_id: cannot convert %T to int64", rawLotID)},
			}, nil
		}
		if lotID <= 0 {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": "lot_id must be > 0"},
			}, nil
		}

		cfg := LotCFConfig{
			CfHome:         cfHome,
			LotID:          lotID,
			OwnerPubKeyHex: bodyCF.PublicKeyHex,
		}
		campfireID, ensureErr := EnsureLotCF(cfg)
		if ensureErr != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":     false,
					"error":  ensureErr.Error(),
					"lot_id": lotID,
				},
			}, nil
		}
		return &convention.Response{
			Payload: map[string]any{
				"ok":          true,
				"lot_id":      lotID,
				"campfire_id": campfireID,
			},
		}, nil
	}
}

// Note: coerceInt64 is defined in cross_level.go (returns int64, bool).

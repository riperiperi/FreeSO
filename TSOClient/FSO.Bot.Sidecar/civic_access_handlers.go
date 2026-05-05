/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"sync"

	"github.com/campfire-net/campfire/pkg/convention"
)

// RegisterCivicAccessHandlers (freesoexperiment-409) wires the grant-community-access
// civic op.
//
// Design: community lot access is a machine-global shared gate. The sidecar
// maintains community-access.json in the shared state directory (NOT keyed by
// persona/FSO_USER). This is critical for multi-persona deployments: baron's
// sidecar (FSO_USER=baron) grants access, ellis's sidecar (FSO_USER=ellis)
// reads the same file and sees the grant.
//
// Storage: SharedCommunityAccessDir() → XDG_DATA_HOME/freeso-souls-shared/
// (falls back to ~/.local/share/freeso-souls-shared/). All sidecars on the
// same machine share this directory.
//
// Authorization: mayor-only (freesoexperiment-ea0). Mayor status is read from
// the latest perception tick cached in augmentor.LatestMayorStatus(). Fails
// closed when augmentor is nil or has no cached mayor_status.
//
// Persistence: community-access.json survives sidecar restarts. Format: array
// of CommunityAccessGrant.
func RegisterCivicAccessHandlers(ctx context.Context, cf *Campfire, augmentor *PerceptionAugmentor) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"grant-community-access": grantCommunityAccessHandler(augmentor),
	}

	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		return 0, fmt.Errorf("load declarations: %w", err)
	}
	byOp := make(map[string]*convention.Declaration, len(decls))
	for _, d := range decls {
		byOp[d.Operation] = d
	}

	started := 0
	for op, handler := range ops {
		decl, ok := byOp[op]
		if !ok {
			return started, fmt.Errorf("declaration for op %q missing (expected in conventions/%s.json)", op, op)
		}
		cf.Router.Register(decl, handler)
		started++
	}
	return started, nil
}

// CommunityAccessGrant is one entry in community-access.json.
type CommunityAccessGrant struct {
	LotID       uint32 `json:"lot_id"`
	PersonaName string `json:"persona_name"` // lowercase; "*" means all
	GrantedAt   int64  `json:"granted_at"`   // unix ms
}

// SharedCommunityAccessDir returns the machine-global directory for community-access
// state. This directory is NOT keyed by persona/FSO_USER — it is shared across all
// sidecars on the same machine so that a grant written by baron's sidecar is
// visible to ellis's sidecar.
//
// Path resolution:
//  1. XDG_DATA_HOME/freeso-souls-shared/ (if XDG_DATA_HOME is set).
//  2. HOME/.local/share/freeso-souls-shared/ (POSIX default).
//  3. /tmp/freeso-souls-shared/ (last resort when HOME is also unset).
//
// Tests override this via XDG_DATA_HOME to get a per-test temp directory.
func SharedCommunityAccessDir() string {
	if xdg := strings.TrimSpace(os.Getenv("XDG_DATA_HOME")); xdg != "" {
		return filepath.Join(xdg, "freeso-souls-shared")
	}
	if home, err := os.UserHomeDir(); err == nil && home != "" {
		return filepath.Join(home, ".local", "share", "freeso-souls-shared")
	}
	return "/tmp/freeso-souls-shared"
}

// communityAccessMu guards concurrent writes to community-access.json.
// Multiple sidecars on the same machine write to the same shared directory —
// the mutex serialises in-process accesses, while the atomic tmp-rename
// provides cross-process safety.
var communityAccessMu sync.Mutex

// grantCommunityAccessHandler implements the grant-community-access convention.
//
// Flow:
//  1. Validate lot_id and persona_name.
//  2. Verify caller is mayor via augmentor.LatestMayorStatus().
//  3. Append grant to community-access.json under PersonaStateDir().
//  4. Return {ok, lot_id, persona_name, grant_count}.
func grantCommunityAccessHandler(augmentor *PerceptionAugmentor) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := req.Args

		// --- Step 1: validate lot_id ---
		rawLotID, ok := args["lot_id"]
		if !ok || rawLotID == nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": "lot_id required", "reason": "INVALID_LOT_ID"},
			}, nil
		}
		lotID64, err := coerceUint64(rawLotID)
		if err != nil || lotID64 == 0 {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": "INVALID_LOT_ID", "reason": "INVALID_LOT_ID"},
			}, nil
		}
		lotID := uint32(lotID64)

		// --- Step 2: validate persona_name ---
		personaName, _ := args["persona_name"].(string)
		personaName = strings.TrimSpace(personaName)
		if personaName == "" {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": "persona_name required", "reason": "INVALID_PERSONA"},
			}, nil
		}
		// Normalise to lowercase (matches FSO_USER convention in PersonaStateDir).
		// Wildcard "*" is preserved as-is.
		if personaName != "*" {
			personaName = strings.ToLower(personaName)
		}

		// --- Step 3: mayor authorization ---
		// Read from latest perception tick (freesoexperiment-ea0).
		isMayor, authErr := checkMayorAuthorization(augmentor)
		if authErr != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("mayor check failed: %v", authErr),
				},
			}, nil
		}
		if !isMayor {
			return &convention.Response{
				Payload: map[string]any{
					"ok":     false,
					"error":  "NO_AUTH",
					"reason": "NO_AUTH",
					"hint":   "Only the neighborhood mayor can grant community lot access. Your avatar must have mayor_nhood > 0.",
				},
			}, nil
		}

		// --- Step 4: append grant to community-access.json ---
		communityAccessMu.Lock()
		defer communityAccessMu.Unlock()

		grants, readErr := readCommunityAccess()
		if readErr != nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": fmt.Sprintf("read community-access.json: %v", readErr)},
			}, nil
		}

		// Idempotent: if the exact (lot_id, persona_name) pair already exists, update granted_at.
		found := false
		nowMs := nowUnixMs()
		for i, g := range grants {
			if g.LotID == lotID && g.PersonaName == personaName {
				grants[i].GrantedAt = nowMs
				found = true
				break
			}
		}
		if !found {
			grants = append(grants, CommunityAccessGrant{
				LotID:       lotID,
				PersonaName: personaName,
				GrantedAt:   nowMs,
			})
		}

		if writeErr := writeCommunityAccess(grants); writeErr != nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": fmt.Sprintf("write community-access.json: %v", writeErr)},
			}, nil
		}

		return &convention.Response{
			Payload: map[string]any{
				"ok":           true,
				"lot_id":       lotID,
				"persona_name": personaName,
				"grant_count":  len(grants),
				"idempotent":   found,
			},
		}, nil
	}
}

// checkMayorAuthorization returns true if the current avatar is the neighborhood
// mayor. It reads from the latest perception tick cached in augmentor
// (freesoexperiment-ea0). Fails closed when augmentor is nil or has no cached
// mayor_status — returns (false, error) so callers surface "perception not yet
// established" rather than silently denying or allowing.
func checkMayorAuthorization(augmentor *PerceptionAugmentor) (bool, error) {
	if augmentor == nil {
		return false, fmt.Errorf("perception not yet established (augmentor is nil)")
	}
	ms := augmentor.LatestMayorStatus()
	return ms.IsMayor, nil
}

// readCommunityAccess reads the machine-global community-access.json file.
// Returns an empty slice when the file does not exist.
// Uses SharedCommunityAccessDir() — NOT PersonaStateDir() — so grants written
// by any sidecar on this machine are visible to all other sidecars.
func readCommunityAccess() ([]CommunityAccessGrant, error) {
	dir := SharedCommunityAccessDir()
	path := filepath.Join(dir, "community-access.json")
	data, err := os.ReadFile(path)
	if err != nil {
		if os.IsNotExist(err) {
			return []CommunityAccessGrant{}, nil
		}
		return nil, fmt.Errorf("read community-access.json: %w", err)
	}
	var grants []CommunityAccessGrant
	if err := json.Unmarshal(data, &grants); err != nil {
		return nil, fmt.Errorf("parse community-access.json: %w", err)
	}
	return grants, nil
}

// writeCommunityAccess atomically writes grants to the machine-global
// community-access.json. Uses SharedCommunityAccessDir() — NOT PersonaStateDir()
// — so the file is readable by any sidecar on this machine regardless of FSO_USER.
func writeCommunityAccess(grants []CommunityAccessGrant) error {
	dir := SharedCommunityAccessDir()
	if err := os.MkdirAll(dir, 0o755); err != nil {
		return fmt.Errorf("mkdir shared community-access dir: %w", err)
	}
	path := filepath.Join(dir, "community-access.json")
	data, err := json.MarshalIndent(grants, "", "  ")
	if err != nil {
		return fmt.Errorf("marshal community-access: %w", err)
	}
	tmp := path + ".tmp"
	if err := os.WriteFile(tmp, data, 0o644); err != nil {
		return fmt.Errorf("write community-access.json tmp: %w", err)
	}
	if err := os.Rename(tmp, path); err != nil {
		return fmt.Errorf("rename community-access.json: %w", err)
	}
	return nil
}

// HasCommunityAccess returns true if the given persona (FSO_USER value) has
// been granted access to the given lot_id. Called by the visit-lot handler
// before allowing a cross-lot transition to a community lot.
//
// Returns true when:
//   - A grant exists with PersonaName == "*" (open to all) for this lot_id.
//   - A grant exists with PersonaName == personaName (case-insensitive) for this lot_id.
//
// Returns false when no matching grant exists (CLOSED gate).
func HasCommunityAccess(lotID uint32, personaName string) bool {
	communityAccessMu.Lock()
	defer communityAccessMu.Unlock()

	grants, err := readCommunityAccess()
	if err != nil {
		return false
	}
	target := strings.ToLower(strings.TrimSpace(personaName))
	for _, g := range grants {
		if g.LotID != lotID {
			continue
		}
		if g.PersonaName == "*" || g.PersonaName == target {
			return true
		}
	}
	return false
}

// IsCommunityGated returns true if the given lot_id has any grants registered
// in community-access.json. A lot is "community-gated" once a mayor has issued
// at least one grant-community-access for it — even if that grant covers only
// specific personas or the wildcard "*". The visit-lot handler calls this to
// decide whether to enforce the HasCommunityAccess check; lots without any
// grants are ordinary residential lots and pass through unconditionally.
func IsCommunityGated(lotID uint32) bool {
	communityAccessMu.Lock()
	defer communityAccessMu.Unlock()

	grants, err := readCommunityAccess()
	if err != nil {
		return false
	}
	for _, g := range grants {
		if g.LotID == lotID {
			return true
		}
	}
	return false
}

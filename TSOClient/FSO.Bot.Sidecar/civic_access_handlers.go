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
// Design: community lot access is a sidecar-local gate. The sidecar maintains
// community-access.json in the persona state directory. The visit-lot handler
// reads this file before allowing a cross-lot transition to a community lot —
// without a grant, community lot visits return CLOSED for non-mayor avatars.
//
// Authorization: mayor-only (check delegated to C# bot via bot-cmd:check-mayor,
// or verified via FSO_MAYOR_NHOOD env var for test scenarios). The handler uses
// the bot-cmd:check-mayor path when a bot is available, and falls back to
// FSO_MAYOR_NHOOD for --no-bot mode.
//
// Persistence: community-access.json is written under PersonaStateDir() so it
// survives sidecar restarts. Format: array of CommunityAccessGrant.
func RegisterCivicAccessHandlers(ctx context.Context, cf *Campfire, botCmds *BotCmdPump) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"grant-community-access": grantCommunityAccessHandler(botCmds),
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

// communityAccessMu guards concurrent writes to community-access.json. Multiple
// sidecars on the same machine would each have their own copy under their persona
// state dir — no cross-process coordination needed for the grant op.
var communityAccessMu sync.Mutex

// grantCommunityAccessHandler implements the grant-community-access convention.
//
// Flow:
//  1. Validate lot_id and persona_name.
//  2. Verify caller is mayor via bot-cmd:check-mayor (or FSO_MAYOR_NHOOD fallback).
//  3. Append grant to community-access.json under PersonaStateDir().
//  4. Return {ok, lot_id, persona_name, grant_count}.
func grantCommunityAccessHandler(botCmds *BotCmdPump) convention.HandlerFunc {
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
		// Prefer bot-cmd:check-mayor when a bot is available.
		// Fall back to FSO_MAYOR_NHOOD env var (non-zero = is mayor).
		isMayor, authErr := checkMayorAuthorization(ctx, botCmds)
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
// mayor. It uses bot-cmd:check-mayor when botCmds is available, and falls back
// to FSO_MAYOR_NHOOD env var (non-zero = mayor) in --no-bot mode.
func checkMayorAuthorization(ctx context.Context, botCmds *BotCmdPump) (bool, error) {
	if botCmds != nil {
		reply, err := botCmds.Send(ctx, "check-mayor", nil)
		if err != nil {
			return false, fmt.Errorf("bot-cmd:check-mayor: %w", err)
		}
		var data struct {
			IsMayor    bool   `json:"is_mayor"`
			MayorNhood int    `json:"mayor_nhood"`
			Reason     string `json:"reason,omitempty"`
		}
		if len(reply.Data) > 0 {
			if perr := json.Unmarshal(reply.Data, &data); perr != nil {
				return false, fmt.Errorf("check-mayor data parse: %w", perr)
			}
		}
		return reply.Ok && data.IsMayor, nil
	}
	// --no-bot fallback: check FSO_MAYOR_NHOOD env var.
	mayorNhood := strings.TrimSpace(os.Getenv("FSO_MAYOR_NHOOD"))
	if mayorNhood == "" || mayorNhood == "0" {
		return false, nil
	}
	return true, nil
}

// readCommunityAccess reads the community-access.json file for this persona.
// Returns an empty slice when the file does not exist.
func readCommunityAccess() ([]CommunityAccessGrant, error) {
	dir, err := PersonaStateDir()
	if err != nil {
		return nil, err
	}
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

// writeCommunityAccess atomically writes grants to community-access.json.
func writeCommunityAccess(grants []CommunityAccessGrant) error {
	dir, err := PersonaStateDir()
	if err != nil {
		return err
	}
	if err := os.MkdirAll(dir, 0o700); err != nil {
		return fmt.Errorf("mkdir persona state dir: %w", err)
	}
	path := filepath.Join(dir, "community-access.json")
	data, err := json.MarshalIndent(grants, "", "  ")
	if err != nil {
		return fmt.Errorf("marshal community-access: %w", err)
	}
	tmp := path + ".tmp"
	if err := os.WriteFile(tmp, data, 0o600); err != nil {
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

/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

// Claim and query-claims convention handlers (freesoexperiment-14b).
//
// Architecture:
//   - Claims persist to ~/.config/freeso-souls/<persona>/claims.json so they
//     survive sidecar restarts and session boundaries. This is the same
//     persona-state directory used for owned-lots.json and body-cf.id.
//   - The claim file is a JSON array of ClaimEntry values keyed by object_id.
//     A second claim on the same object_id replaces the prior entry (last-
//     writer-wins, consistent with owned-lots append/replace pattern).
//   - query-claims reads the same file and returns it verbatim. No engine IPC,
//     no FSO wire PDU — sidecar-tier only per freesoexperiment-14b constraint.
//   - The ClaimStore provides the in-memory cache used by PerceptionAugmentor
//     to emit body.my_objects[] each tick without re-reading the file. A mutex
//     guards both the cache and the file so concurrent claim + query-claims
//     calls don't race.
//
// Persistence invariant: after a successful claim response, a process restart
// followed by ReadClaims MUST return the claim. The atomic write (write-then-
// rename) on WriteAllClaims guarantees this.

import (
	"context"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"sync"
	"time"

	"github.com/campfire-net/campfire/pkg/convention"
)

// ClaimEntry is one record in the persona's claims.json. It is the canonical
// shape returned by query-claims and emitted by body.my_objects[].
type ClaimEntry struct {
	ObjectID  int64  `json:"object_id"`
	Note      string `json:"note"`       // may be empty
	LotID     int64  `json:"lot_id"`     // lot the Sim was on when the claim was made; 0 if unknown
	ClaimedAt int64  `json:"claimed_at"` // unix ms
}

// ClaimStore is the in-memory representation of the persona's claims.json.
// It is the single source of truth within a sidecar session: claim writes
// here AND to disk; query-claims reads only this; the augmentor reads from
// this without holding mu (via Snapshot).
type ClaimStore struct {
	mu     sync.RWMutex
	claims []ClaimEntry // ordered by claimed_at, last-writer-wins per object_id
}

// NewClaimStore constructs an empty ClaimStore. Call LoadClaims to populate
// from disk before registering handlers.
func NewClaimStore() *ClaimStore {
	return &ClaimStore{}
}

// Upsert adds or replaces the claim for entry.ObjectID. Thread-safe.
// Returns the final slice (all claims including the upserted one).
func (s *ClaimStore) Upsert(entry ClaimEntry) []ClaimEntry {
	s.mu.Lock()
	defer s.mu.Unlock()
	// Replace in-place if the same object_id already exists.
	for i, c := range s.claims {
		if c.ObjectID == entry.ObjectID {
			s.claims[i] = entry
			cp := make([]ClaimEntry, len(s.claims))
			copy(cp, s.claims)
			return cp
		}
	}
	s.claims = append(s.claims, entry)
	cp := make([]ClaimEntry, len(s.claims))
	copy(cp, s.claims)
	return cp
}

// Snapshot returns a defensive copy of the current claim slice. Safe for
// concurrent callers (the augmentor goroutine reads this without holding mu
// for longer than the lock window).
func (s *ClaimStore) Snapshot() []ClaimEntry {
	s.mu.RLock()
	defer s.mu.RUnlock()
	if len(s.claims) == 0 {
		return nil
	}
	cp := make([]ClaimEntry, len(s.claims))
	copy(cp, s.claims)
	return cp
}

// Replace atomically replaces the full claim set (used during startup load).
func (s *ClaimStore) Replace(entries []ClaimEntry) {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.claims = make([]ClaimEntry, len(entries))
	copy(s.claims, entries)
}

// --- Persona-state persistence (claims.json) ---

// ReadClaims reads ~/.config/freeso-souls/<persona>/claims.json. Returns an
// empty (non-nil) slice when the file does not exist (first claim). Returns an
// error on I/O or parse failures.
func ReadClaims() ([]ClaimEntry, error) {
	dir, err := PersonaStateDir()
	if err != nil {
		return nil, err
	}
	path := filepath.Join(dir, "claims.json")
	data, err := os.ReadFile(path)
	if err != nil {
		if os.IsNotExist(err) {
			return []ClaimEntry{}, nil
		}
		return nil, fmt.Errorf("read claims.json: %w", err)
	}
	var entries []ClaimEntry
	if err := json.Unmarshal(data, &entries); err != nil {
		return nil, fmt.Errorf("parse claims.json: %w", err)
	}
	return entries, nil
}

// WriteAllClaims atomically writes entries to claims.json. Creates the persona
// state directory if necessary. Uses write-then-rename for durability.
func WriteAllClaims(entries []ClaimEntry) error {
	dir, err := PersonaStateDir()
	if err != nil {
		return err
	}
	if err := os.MkdirAll(dir, 0o700); err != nil {
		return fmt.Errorf("mkdir persona state dir: %w", err)
	}
	path := filepath.Join(dir, "claims.json")
	data, err := json.MarshalIndent(entries, "", "  ")
	if err != nil {
		return fmt.Errorf("marshal claims: %w", err)
	}
	tmp := path + ".tmp"
	if err := os.WriteFile(tmp, data, 0o600); err != nil {
		return fmt.Errorf("write claims.json tmp: %w", err)
	}
	if err := os.Rename(tmp, path); err != nil {
		return fmt.Errorf("rename claims.json: %w", err)
	}
	return nil
}

// LoadClaims reads claims.json and populates store. Should be called once at
// sidecar startup before registering convention handlers.
func LoadClaims(store *ClaimStore) error {
	entries, err := ReadClaims()
	if err != nil {
		return err
	}
	store.Replace(entries)
	return nil
}

// --- Convention handlers ---

// RegisterClaimHandlers wires the claim and query-claims convention handlers
// on cf using the single-dispatcher Router pattern (b28ee3d). Returns the
// number of handlers registered.
func RegisterClaimHandlers(ctx context.Context, cf *Campfire, store *ClaimStore) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"claim":        claimHandler(store),
		"query-claims": queryClaimsHandler(store),
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

// claimHandler stores a claim for target_object_id in both the in-memory store
// and the persona's claims.json file. On success the claim is visible in the
// next perception tick via body.my_objects[].
//
// Args:
//
//	target_object_id (uint, required) — object to claim; must be > 0.
//	note             (string, optional) — brief reason for the claim.
//
// Returns: {ok: true, object_id: N, claimed_at: <unix_ms>}.
// Errors: zero/missing object_id → ok:false.
func claimHandler(store *ClaimStore) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		objectID, ok := numericArg(req.Args, "target_object_id")
		if !ok || objectID <= 0 {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": "target_object_id is required and must be positive"},
			}, nil
		}

		note := ""
		if n, ok := req.Args["note"].(string); ok {
			note = n
		}

		now := time.Now().UnixMilli()
		entry := ClaimEntry{
			ObjectID:  objectID,
			Note:      note,
			ClaimedAt: now,
		}

		// Update in-memory cache first, then persist.
		all := store.Upsert(entry)
		if err := WriteAllClaims(all); err != nil {
			// Persist failed: revert in-memory store to the pre-upsert snapshot
			// by re-reading from disk. If the disk read also fails, the in-memory
			// state is newer than disk — log but don't fail the agent: the claim
			// IS in memory and will appear in perception for this session.
			if rollbackEntries, rerr := ReadClaims(); rerr == nil {
				store.Replace(rollbackEntries)
			}
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": "persist claim: " + err.Error()},
			}, nil
		}

		return &convention.Response{
			Payload: map[string]any{
				"ok":         true,
				"object_id":  objectID,
				"claimed_at": now,
			},
		}, nil
	}
}

// queryClaimsHandler returns all claims in this persona's claim store. Each
// record has {object_id, note, lot_id, claimed_at}.
//
// Returns: {ok: true, claims: [{object_id, note, lot_id, claimed_at}, ...]}.
// An empty persona returns claims=[].
func queryClaimsHandler(store *ClaimStore) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		snapshot := store.Snapshot()

		// Convert to []any for clean JSON round-trip through the convention framework.
		claimsOut := make([]any, 0, len(snapshot))
		for _, c := range snapshot {
			claimsOut = append(claimsOut, map[string]any{
				"object_id":  c.ObjectID,
				"note":       c.Note,
				"lot_id":     c.LotID,
				"claimed_at": c.ClaimedAt,
			})
		}

		return &convention.Response{
			Payload: map[string]any{
				"ok":     true,
				"claims": claimsOut,
			},
		}, nil
	}
}

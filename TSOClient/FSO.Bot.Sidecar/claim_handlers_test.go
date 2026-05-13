/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

// Tests for claim / query-claims handlers and ClaimStore (freesoexperiment-14b).
//
// Coverage:
//   1. ClaimStore round-trip: Upsert + Snapshot returns the entry.
//   2. ClaimStore upsert replaces prior entry on same object_id.
//   3. claimHandler stores and returns ok=true with object_id + claimed_at.
//   4. claimHandler rejects zero/missing target_object_id.
//   5. claimHandler persists to disk (simulated session boundary).
//   6. queryClaimsHandler returns all claims in the store.
//   7. queryClaimsHandler returns empty array (not null) when store is empty.
//   8. Cross-sim isolation: two separate ClaimStores don't share data.
//   9. Session-boundary simulation: load from disk round-trips correctly.
//   10. PerceptionAugmentor emits body.my_objects[] from ClaimStore.
//   11. PerceptionAugmentor emits empty body.my_objects[] when store has no claims.

import (
	"context"
	"encoding/json"
	"os"
	"path/filepath"
	"testing"
	"time"

	"github.com/campfire-net/campfire/pkg/convention"
)

// setupClaimTestEnv sets FSO_USER to an isolated per-test persona derived from
// t.Name(), pointing at a temp dir. Returns a cleanup function.
func setupClaimTestEnv(t *testing.T) func() {
	t.Helper()
	tmp := t.TempDir()
	t.Setenv("XDG_CONFIG_HOME", tmp)
	persona := "claimtest_" + sanitizeTestName(t.Name())
	t.Setenv("FSO_USER", persona)
	// Pre-create persona state dir.
	dir := filepath.Join(tmp, "freeso-souls", persona)
	if err := os.MkdirAll(dir, 0o700); err != nil {
		t.Fatalf("mkdir persona dir: %v", err)
	}
	return func() { os.RemoveAll(dir) }
}

// --- ClaimStore unit tests ---

func TestClaimStore_UpsertSnapshot_RoundTrip(t *testing.T) {
	s := NewClaimStore()
	s.Upsert(ClaimEntry{ObjectID: 100, Note: "my chair", ClaimedAt: 1000})

	snap := s.Snapshot()
	if len(snap) != 1 {
		t.Fatalf("expected 1 claim, got %d", len(snap))
	}
	if snap[0].ObjectID != 100 {
		t.Errorf("object_id: want 100, got %d", snap[0].ObjectID)
	}
	if snap[0].Note != "my chair" {
		t.Errorf("note: want 'my chair', got %q", snap[0].Note)
	}
}

func TestClaimStore_UpsertReplacesSameObjectID(t *testing.T) {
	s := NewClaimStore()
	s.Upsert(ClaimEntry{ObjectID: 200, Note: "first note", ClaimedAt: 1000})
	s.Upsert(ClaimEntry{ObjectID: 200, Note: "updated note", ClaimedAt: 2000})

	snap := s.Snapshot()
	if len(snap) != 1 {
		t.Fatalf("expected 1 claim (upsert), got %d", len(snap))
	}
	if snap[0].Note != "updated note" {
		t.Errorf("expected updated note, got %q", snap[0].Note)
	}
	if snap[0].ClaimedAt != 2000 {
		t.Errorf("expected updated claimed_at=2000, got %d", snap[0].ClaimedAt)
	}
}

func TestClaimStore_MultipleDistinctObjects(t *testing.T) {
	s := NewClaimStore()
	s.Upsert(ClaimEntry{ObjectID: 10, Note: "a", ClaimedAt: 1})
	s.Upsert(ClaimEntry{ObjectID: 20, Note: "b", ClaimedAt: 2})
	s.Upsert(ClaimEntry{ObjectID: 30, Note: "c", ClaimedAt: 3})

	snap := s.Snapshot()
	if len(snap) != 3 {
		t.Fatalf("expected 3 claims, got %d", len(snap))
	}
}

func TestClaimStore_SnapshotEmpty(t *testing.T) {
	s := NewClaimStore()
	snap := s.Snapshot()
	if snap != nil {
		t.Errorf("expected nil snapshot for empty store, got %v", snap)
	}
}

// --- Handler tests ---

func TestClaimHandler_StoresAndReturnsOK(t *testing.T) {
	cleanup := setupClaimTestEnv(t)
	defer cleanup()

	store := NewClaimStore()
	handler := claimHandler(store)

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"target_object_id": int64(352),
			"note":             "my bed",
		},
	})
	if err != nil {
		t.Fatalf("claimHandler: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil || payload["ok"] != true {
		t.Fatalf("expected ok=true, got %v", resp.Payload)
	}
	// object_id echoed back. The handler stores int64 natively (pre-JSON),
	// so the value may be int64 or float64 depending on whether it passed
	// through JSON round-trip. Accept both.
	objIDRaw := payload["object_id"]
	var objID int64
	switch v := objIDRaw.(type) {
	case int64:
		objID = v
	case float64:
		objID = int64(v)
	default:
		t.Fatalf("unexpected type for object_id: %T %v", objIDRaw, objIDRaw)
	}
	if objID != 352 {
		t.Errorf("expected object_id=352, got %d", objID)
	}
	// claimed_at present and plausible. Same type flexibility.
	caRaw := payload["claimed_at"]
	var ca int64
	switch v := caRaw.(type) {
	case int64:
		ca = v
	case float64:
		ca = int64(v)
	default:
		t.Fatalf("unexpected type for claimed_at: %T %v", caRaw, caRaw)
	}
	if ca <= 0 {
		t.Errorf("expected positive claimed_at, got %v", ca)
	}

	// Verify it's in the store.
	snap := store.Snapshot()
	if len(snap) != 1 || snap[0].ObjectID != 352 || snap[0].Note != "my bed" {
		t.Fatalf("claim not in store: %+v", snap)
	}
}

func TestClaimHandler_RejectsZeroObjectID(t *testing.T) {
	cleanup := setupClaimTestEnv(t)
	defer cleanup()

	store := NewClaimStore()
	handler := claimHandler(store)
	ctx := context.Background()

	for _, tc := range []struct {
		name string
		args map[string]any
	}{
		{"zero id", map[string]any{"target_object_id": int64(0)}},
		{"missing id", map[string]any{}},
		{"negative id", map[string]any{"target_object_id": int64(-5)}},
	} {
		t.Run(tc.name, func(t *testing.T) {
			resp, err := handler(ctx, &convention.Request{Args: tc.args})
			if err != nil {
				t.Fatalf("handler: %v", err)
			}
			payload, _ := resp.Payload.(map[string]any)
			if payload == nil || payload["ok"] != false {
				t.Fatalf("expected ok=false for %s, got %v", tc.name, payload)
			}
			if _, hasErr := payload["error"]; !hasErr {
				t.Fatalf("expected error field for %s, got %v", tc.name, payload)
			}
		})
	}
}

func TestClaimHandler_PersistsToDisk(t *testing.T) {
	cleanup := setupClaimTestEnv(t)
	defer cleanup()

	store := NewClaimStore()
	handler := claimHandler(store)
	ctx := context.Background()

	_, err := handler(ctx, &convention.Request{
		Args: map[string]any{"target_object_id": int64(352), "note": "my bed"},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}

	// Simulate session boundary: read from disk into a fresh store.
	freshStore := NewClaimStore()
	if err := LoadClaims(freshStore); err != nil {
		t.Fatalf("LoadClaims: %v", err)
	}
	snap := freshStore.Snapshot()
	if len(snap) != 1 || snap[0].ObjectID != 352 || snap[0].Note != "my bed" {
		t.Fatalf("claim not persisted correctly: %+v", snap)
	}
}

func TestQueryClaimsHandler_ReturnsAllClaims(t *testing.T) {
	cleanup := setupClaimTestEnv(t)
	defer cleanup()

	store := NewClaimStore()
	store.Upsert(ClaimEntry{ObjectID: 100, Note: "a", ClaimedAt: 1000})
	store.Upsert(ClaimEntry{ObjectID: 200, Note: "b", ClaimedAt: 2000})

	handler := queryClaimsHandler(store)
	ctx := context.Background()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{}})
	if err != nil {
		t.Fatalf("queryClaimsHandler: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil || payload["ok"] != true {
		t.Fatalf("expected ok=true, got %v", resp.Payload)
	}
	claims, _ := payload["claims"].([]any)
	if len(claims) != 2 {
		t.Fatalf("expected 2 claims, got %d: %v", len(claims), claims)
	}
}

func TestQueryClaimsHandler_EmptyIsNotNull(t *testing.T) {
	cleanup := setupClaimTestEnv(t)
	defer cleanup()

	store := NewClaimStore()
	handler := queryClaimsHandler(store)
	ctx := context.Background()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{}})
	if err != nil {
		t.Fatalf("queryClaimsHandler: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil || payload["ok"] != true {
		t.Fatalf("expected ok=true, got %v", resp.Payload)
	}
	// claims must be a non-nil slice (empty), not nil/absent.
	claims, ok := payload["claims"]
	if !ok {
		t.Fatal("claims field absent from response")
	}
	claimsSlice, _ := claims.([]any)
	if claimsSlice == nil {
		t.Fatal("claims field is nil, expected empty slice")
	}
	if len(claimsSlice) != 0 {
		t.Fatalf("expected empty claims, got %d", len(claimsSlice))
	}
}

// TestCrossSimIsolation verifies that two separate ClaimStores (representing
// two different Sims) do not share data — each reads its own persona dir.
func TestCrossSimIsolation(t *testing.T) {
	tmp := t.TempDir()

	// Marlo's store.
	t.Setenv("XDG_CONFIG_HOME", tmp)
	t.Setenv("FSO_USER", "marlo")
	marloDir := filepath.Join(tmp, "freeso-souls", "marlo")
	if err := os.MkdirAll(marloDir, 0o700); err != nil {
		t.Fatalf("mkdir marlo dir: %v", err)
	}
	marloStore := NewClaimStore()
	marloHandler := claimHandler(marloStore)
	ctx := context.Background()
	_, err := marloHandler(ctx, &convention.Request{
		Args: map[string]any{"target_object_id": int64(352), "note": "my bed"},
	})
	if err != nil {
		t.Fatalf("marlo claim: %v", err)
	}

	// Cass's store (different persona dir).
	t.Setenv("FSO_USER", "cass")
	cassDir := filepath.Join(tmp, "freeso-souls", "cass")
	if err := os.MkdirAll(cassDir, 0o700); err != nil {
		t.Fatalf("mkdir cass dir: %v", err)
	}
	cassStore := NewClaimStore()
	if err := LoadClaims(cassStore); err != nil {
		t.Fatalf("LoadClaims cass: %v", err)
	}
	cassSnap := cassStore.Snapshot()
	if len(cassSnap) != 0 {
		t.Fatalf("cass should have no claims, but got %v", cassSnap)
	}

	// Marlo's store is unaffected by Cass's empty load.
	marloSnap := marloStore.Snapshot()
	if len(marloSnap) != 1 || marloSnap[0].ObjectID != 352 {
		t.Fatalf("marlo's claims unexpectedly changed: %v", marloSnap)
	}
}

// TestReadWriteClaimsRoundTrip exercises the persona-state persistence layer
// directly (ReadClaims / WriteAllClaims).
func TestReadWriteClaimsRoundTrip(t *testing.T) {
	cleanup := setupClaimTestEnv(t)
	defer cleanup()

	entries := []ClaimEntry{
		{ObjectID: 1, Note: "a", LotID: 2, ClaimedAt: 1000},
		{ObjectID: 2, Note: "b", LotID: 0, ClaimedAt: 2000},
	}
	if err := WriteAllClaims(entries); err != nil {
		t.Fatalf("WriteAllClaims: %v", err)
	}

	got, err := ReadClaims()
	if err != nil {
		t.Fatalf("ReadClaims: %v", err)
	}
	if len(got) != 2 {
		t.Fatalf("expected 2 entries, got %d", len(got))
	}
	if got[0].ObjectID != 1 || got[1].ObjectID != 2 {
		t.Fatalf("round-trip mismatch: %+v", got)
	}
	if got[0].Note != "a" || got[1].LotID != 0 {
		t.Fatalf("round-trip field mismatch: %+v", got)
	}
}

// TestReadClaimsMissingFile verifies that ReadClaims returns an empty (non-nil)
// slice when claims.json does not exist (first boot / never claimed).
func TestReadClaimsMissingFile(t *testing.T) {
	cleanup := setupClaimTestEnv(t)
	defer cleanup()

	entries, err := ReadClaims()
	if err != nil {
		t.Fatalf("ReadClaims on missing file: want nil error, got %v", err)
	}
	if entries == nil {
		t.Fatal("ReadClaims: expected non-nil empty slice for missing file")
	}
	if len(entries) != 0 {
		t.Fatalf("expected 0 entries, got %d", len(entries))
	}
}

// --- PerceptionAugmentor + claim integration ---

// TestAugmentor_MyObjectsEmitted verifies that AugmentPerception injects
// body.my_objects[] when a ClaimStore with entries is provided.
func TestAugmentor_MyObjectsEmitted(t *testing.T) {
	cleanup := setupClaimTestEnv(t)
	defer cleanup()

	store := NewClaimStore()
	store.Upsert(ClaimEntry{ObjectID: 352, Note: "my bed", ClaimedAt: 9999})

	a := NewPerceptionAugmentor(store, false)
	tick := makeAugmentorPerceptionLine(2, true, "Test Lot")
	out := a.AugmentPerception(tick)

	var m map[string]json.RawMessage
	if err := json.Unmarshal(out, &m); err != nil {
		t.Fatalf("unmarshal augmented tick: %v", err)
	}

	bodyRaw, ok := m["body"]
	if !ok {
		t.Fatal("augmented tick missing 'body' key")
	}
	var body map[string]json.RawMessage
	if err := json.Unmarshal(bodyRaw, &body); err != nil {
		t.Fatalf("unmarshal body: %v", err)
	}
	myObjectsRaw, ok := body["my_objects"]
	if !ok {
		t.Fatal("body missing 'my_objects' key")
	}
	var myObjects []map[string]any
	if err := json.Unmarshal(myObjectsRaw, &myObjects); err != nil {
		t.Fatalf("unmarshal my_objects: %v", err)
	}
	if len(myObjects) != 1 {
		t.Fatalf("expected 1 my_object, got %d: %v", len(myObjects), myObjects)
	}
	objIDRaw := myObjects[0]["object_id"]
	var objID int64
	switch v := objIDRaw.(type) {
	case int64:
		objID = v
	case float64:
		objID = int64(v)
	default:
		t.Fatalf("unexpected type for my_objects[0].object_id: %T %v", objIDRaw, objIDRaw)
	}
	if objID != 352 {
		t.Errorf("expected object_id=352, got %d", objID)
	}
	if myObjects[0]["note"] != "my bed" {
		t.Errorf("expected note='my bed', got %v", myObjects[0]["note"])
	}
}

// TestAugmentor_MyObjectsEmptyWhenNoClaims verifies that body.my_objects is an
// empty JSON array (never null or absent) when the store has no claims.
func TestAugmentor_MyObjectsEmptyWhenNoClaims(t *testing.T) {
	cleanup := setupClaimTestEnv(t)
	defer cleanup()

	store := NewClaimStore() // empty
	a := NewPerceptionAugmentor(store, false)
	tick := makeAugmentorPerceptionLine(2, true, "Test Lot")
	out := a.AugmentPerception(tick)

	var m map[string]json.RawMessage
	if err := json.Unmarshal(out, &m); err != nil {
		t.Fatalf("unmarshal augmented tick: %v", err)
	}

	bodyRaw, ok := m["body"]
	if !ok {
		t.Fatal("augmented tick missing 'body' key")
	}
	var body map[string]json.RawMessage
	if err := json.Unmarshal(bodyRaw, &body); err != nil {
		t.Fatalf("unmarshal body: %v", err)
	}
	myObjectsRaw, ok := body["my_objects"]
	if !ok {
		t.Fatal("body missing 'my_objects' key")
	}
	// Must decode to an empty array, not null.
	var myObjects []any
	if err := json.Unmarshal(myObjectsRaw, &myObjects); err != nil {
		t.Fatalf("unmarshal my_objects: %v", err)
	}
	if myObjects == nil {
		t.Fatal("my_objects is null, expected empty array []")
	}
	if len(myObjects) != 0 {
		t.Fatalf("expected empty array, got %d entries", len(myObjects))
	}
}

// TestAugmentor_NilClaimStore_NoMyObjects verifies that when claimStore is nil
// (claims feature disabled), body.my_objects is absent from the tick — existing
// tests that pass nil don't regress.
func TestAugmentor_NilClaimStore_NoMyObjects(t *testing.T) {
	cleanup := setupClaimTestEnv(t)
	defer cleanup()

	a := NewPerceptionAugmentor(nil, false)
	tick := makeAugmentorPerceptionLine(2, true, "Test Lot")
	out := a.AugmentPerception(tick)

	var m map[string]json.RawMessage
	if err := json.Unmarshal(out, &m); err != nil {
		t.Fatalf("unmarshal augmented tick: %v", err)
	}

	// If body key exists, my_objects must not be in it.
	if bodyRaw, ok := m["body"]; ok {
		var body map[string]json.RawMessage
		if err := json.Unmarshal(bodyRaw, &body); err != nil {
			t.Fatalf("unmarshal body: %v", err)
		}
		if _, hasMyObjects := body["my_objects"]; hasMyObjects {
			t.Fatal("body.my_objects should be absent when claimStore is nil")
		}
	}
	// body may not exist at all — that's also acceptable.
}

// TestSessionBoundaryPersistence simulates a full session boundary:
// 1. Sim claims an object.
// 2. Sidecar "restarts" (new store, LoadClaims from disk).
// 3. query-claims returns the same claim.
func TestSessionBoundaryPersistence(t *testing.T) {
	cleanup := setupClaimTestEnv(t)
	defer cleanup()

	ctx := context.Background()

	// Session 1: claim object 352.
	store1 := NewClaimStore()
	if err := LoadClaims(store1); err != nil {
		t.Fatalf("LoadClaims session1: %v", err)
	}
	claimH := claimHandler(store1)
	resp, err := claimH(ctx, &convention.Request{
		Args: map[string]any{"target_object_id": int64(352), "note": "my bed"},
	})
	if err != nil || resp.Payload.(map[string]any)["ok"] != true {
		t.Fatalf("claim: %v / %v", err, resp.Payload)
	}

	// Session boundary: simulate sidecar restart with a fresh store.
	store2 := NewClaimStore()
	if err := LoadClaims(store2); err != nil {
		t.Fatalf("LoadClaims session2: %v", err)
	}

	queryH := queryClaimsHandler(store2)
	resp, err = queryH(ctx, &convention.Request{Args: map[string]any{}})
	if err != nil {
		t.Fatalf("query-claims: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != true {
		t.Fatalf("query-claims not ok: %v", payload)
	}
	claims, _ := payload["claims"].([]any)
	if len(claims) != 1 {
		t.Fatalf("expected 1 claim after session boundary, got %d: %v", len(claims), claims)
	}
	c0, _ := claims[0].(map[string]any)
	objIDRaw := c0["object_id"]
	var objID int64
	switch v := objIDRaw.(type) {
	case int64:
		objID = v
	case float64:
		objID = int64(v)
	default:
		t.Fatalf("unexpected type for object_id in claim: %T %v", objIDRaw, objIDRaw)
	}
	if objID != 352 {
		t.Errorf("wrong object_id after session boundary: got %d, want 352", objID)
	}
	if c0["note"] != "my bed" {
		t.Errorf("wrong note after session boundary: %v", c0)
	}
}

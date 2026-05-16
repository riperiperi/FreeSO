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
	"sync"
	"sync/atomic"
	"testing"
	"time"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// TestRememberHandlerStoresValue asserts that a remember invocation stores the
// value and returns ok=true, and a subsequent recall returns the same value.
// This is the ground-truth of the memory family: remember → recall round-trip
// preserves the structured value verbatim.
func TestRememberHandlerStoresValue(t *testing.T) {
	store := NewMemoryStore()
	remember := rememberHandler(store)
	recall := recallHandler(store)

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	// 1) remember(key="test-1", value={"text":"hello"}).
	resp, err := remember(ctx, &convention.Request{
		Args: map[string]any{
			"key":   "test-1",
			"value": map[string]any{"text": "hello"},
		},
	})
	if err != nil {
		t.Fatalf("remember: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil || payload["ok"] != true {
		t.Fatalf("remember: expected ok=true, got %v", resp.Payload)
	}

	// 2) recall(key="test-1") — found=true, value matches.
	resp, err = recall(ctx, &convention.Request{Args: map[string]any{"key": "test-1"}})
	if err != nil {
		t.Fatalf("recall: %v", err)
	}
	payload, _ = resp.Payload.(map[string]any)
	if payload == nil || payload["ok"] != true {
		t.Fatalf("recall: expected ok=true, got %v", resp.Payload)
	}
	if payload["found"] != true {
		t.Fatalf("recall: expected found=true, got %v", payload["found"])
	}
	value, _ := payload["value"].(map[string]any)
	if value == nil || value["text"] != "hello" {
		t.Fatalf("recall: value shape mismatch: %v", payload["value"])
	}
}

// TestRecallMissingKey asserts that recall on a never-stored key returns
// ok=true with found=false. The brief requires this exact shape — agents
// must be able to distinguish "stored null" from "never set".
func TestRecallMissingKey(t *testing.T) {
	store := NewMemoryStore()
	recall := recallHandler(store)

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := recall(ctx, &convention.Request{Args: map[string]any{"key": "never-set"}})
	if err != nil {
		t.Fatalf("recall: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil {
		t.Fatalf("recall: nil payload")
	}
	if payload["ok"] != true {
		t.Fatalf("recall: expected ok=true (the op itself did not fail), got %v", payload["ok"])
	}
	if payload["found"] != false {
		t.Fatalf("recall: expected found=false for missing key, got %v", payload["found"])
	}
	if _, present := payload["value"]; present {
		t.Fatalf("recall: expected no value field for missing key, got %v", payload["value"])
	}
}

// TestRememberRequiresKey asserts that remember refuses empty/missing key.
func TestRememberRequiresKey(t *testing.T) {
	store := NewMemoryStore()
	remember := rememberHandler(store)
	ctx := context.Background()

	for _, tc := range []struct {
		name string
		args map[string]any
	}{
		{"missing key", map[string]any{"value": 1}},
		{"empty key", map[string]any{"key": "", "value": 1}},
	} {
		t.Run(tc.name, func(t *testing.T) {
			resp, err := remember(ctx, &convention.Request{Args: tc.args})
			if err != nil {
				t.Fatalf("remember: %v", err)
			}
			payload, _ := resp.Payload.(map[string]any)
			if payload == nil || payload["ok"] != false {
				t.Fatalf("expected ok=false, got %v", resp.Payload)
			}
			if _, hasErr := payload["error"]; !hasErr {
				t.Fatalf("expected error field on failure payload, got %v", resp.Payload)
			}
		})
	}
}

// TestRememberRequiresValue asserts that remember with a missing value arg
// returns ok=false. (nil / explicit null is allowed — see the "null value"
// test — but an absent arg is not.)
func TestRememberRequiresValue(t *testing.T) {
	store := NewMemoryStore()
	remember := rememberHandler(store)
	ctx := context.Background()

	resp, err := remember(ctx, &convention.Request{Args: map[string]any{"key": "k"}})
	if err != nil {
		t.Fatalf("remember: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil || payload["ok"] != false {
		t.Fatalf("expected ok=false, got %v", resp.Payload)
	}
}

// TestRememberAcceptsJSONStringValue asserts the primary convention-framework
// delivery shape: the `json` arg type arrives as a validated JSON string (see
// campfire/pkg/convention/executor.go). The handler must parse it and store
// the native shape so recall returns the un-escaped value.
func TestRememberAcceptsJSONStringValue(t *testing.T) {
	store := NewMemoryStore()
	remember := rememberHandler(store)
	recall := recallHandler(store)
	ctx := context.Background()

	cases := []struct {
		name       string
		jsonStr    string
		expectType string
	}{
		{"object", `{"text":"hello","n":1}`, "object"},
		{"array", `[1,2,3]`, "array"},
		{"string", `"hello"`, "string"},
		{"number", `42`, "number"},
		{"bool", `true`, "bool"},
		{"null", `null`, "null"},
	}
	for _, tc := range cases {
		t.Run(tc.name, func(t *testing.T) {
			resp, err := remember(ctx, &convention.Request{Args: map[string]any{"key": tc.name, "value": tc.jsonStr}})
			if err != nil {
				t.Fatalf("remember: %v", err)
			}
			p, _ := resp.Payload.(map[string]any)
			if p["ok"] != true {
				t.Fatalf("remember: expected ok=true, got %v", p)
			}

			resp, err = recall(ctx, &convention.Request{Args: map[string]any{"key": tc.name}})
			if err != nil {
				t.Fatalf("recall: %v", err)
			}
			p, _ = resp.Payload.(map[string]any)
			if p["found"] != true {
				t.Fatalf("recall: expected found=true, got %v", p)
			}
		})
	}
}

// TestRememberRejectsInvalidJSONString asserts that a string arg that isn't
// valid JSON is refused. This is the same contract the convention framework
// enforces for `json`-typed args at the executor layer, re-asserted in-handler
// so direct callers (and legacy clients) see the same refusal.
func TestRememberRejectsInvalidJSONString(t *testing.T) {
	store := NewMemoryStore()
	remember := rememberHandler(store)
	ctx := context.Background()

	resp, err := remember(ctx, &convention.Request{Args: map[string]any{"key": "k", "value": "not json"}})
	if err != nil {
		t.Fatalf("remember: %v", err)
	}
	p, _ := resp.Payload.(map[string]any)
	if p["ok"] != false {
		t.Fatalf("expected ok=false for invalid JSON, got %v", p)
	}
}

// TestRememberAllowsNullValue asserts that remember(key=k, value=nil) is
// legal — explicit null is a valid value, and recall should return it with
// found=true.
func TestRememberAllowsNullValue(t *testing.T) {
	store := NewMemoryStore()
	remember := rememberHandler(store)
	recall := recallHandler(store)
	ctx := context.Background()

	resp, err := remember(ctx, &convention.Request{Args: map[string]any{"key": "nullkey", "value": nil}})
	if err != nil {
		t.Fatalf("remember: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil || payload["ok"] != true {
		t.Fatalf("remember: expected ok=true for null value, got %v", resp.Payload)
	}

	resp, err = recall(ctx, &convention.Request{Args: map[string]any{"key": "nullkey"}})
	if err != nil {
		t.Fatalf("recall: %v", err)
	}
	payload, _ = resp.Payload.(map[string]any)
	if payload["found"] != true {
		t.Fatalf("recall: expected found=true for stored-null, got %v", payload)
	}
	// The Go json decoding of "null" yields nil. Confirm the key is present
	// in the map (distinguishes "stored null" from "never set").
	v, present := payload["value"]
	if !present {
		t.Fatalf("recall: expected value key present for stored null, got %v", payload)
	}
	if v != nil {
		t.Fatalf("recall: expected value=nil for stored null, got %v", v)
	}
}

// TestRememberOverwrites asserts a second remember on the same key replaces
// the value.
func TestRememberOverwrites(t *testing.T) {
	store := NewMemoryStore()
	remember := rememberHandler(store)
	recall := recallHandler(store)
	ctx := context.Background()

	// Values are JSON-encoded strings to mirror the convention framework's
	// delivery shape (arg type "json" — see executor.go). An agent sending a
	// plain string would JSON-encode it first (`"first"` not `first`).
	for _, val := range []string{`"first"`, `"second"`, `"third"`} {
		_, err := remember(ctx, &convention.Request{Args: map[string]any{"key": "k", "value": val}})
		if err != nil {
			t.Fatalf("remember: %v", err)
		}
	}

	resp, err := recall(ctx, &convention.Request{Args: map[string]any{"key": "k"}})
	if err != nil {
		t.Fatalf("recall: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["value"] != "third" {
		t.Fatalf("recall: expected final overwrite value 'third', got %v", payload["value"])
	}
}

// TestConcurrentRememberRecall runs the brief's required stress test:
// 100 concurrent remember+recall against the same key. Asserts that recall
// always returns either (a) a valid stored value (never a corrupted partial)
// or (b) not-found (if the first remember hasn't landed yet on a given
// schedule), and that no race detector triggers fire. Run with `go test -race`
// to fully validate.
//
// Each writer stores an integer value (its own worker id), each reader
// asserts the returned value is a valid integer from the set of writer ids
// (no torn writes, no garbage bytes). sync.Map + json.RawMessage guarantees
// atomic per-key swaps — this test is the ground-source verification of that
// invariant against the actual handlers we ship.
func TestConcurrentRememberRecall(t *testing.T) {
	const writers = 100
	const readers = 100
	const iterations = 50

	store := NewMemoryStore()
	remember := rememberHandler(store)
	recall := recallHandler(store)
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	validValues := make(map[int]struct{}, writers)
	for i := 0; i < writers; i++ {
		validValues[i] = struct{}{}
	}

	var wg sync.WaitGroup
	var corruptedReads, missingReads, totalReads atomic.Int64
	errCh := make(chan error, writers+readers)

	// Writers: each iteration stores its worker id against the shared key.
	for w := 0; w < writers; w++ {
		wg.Add(1)
		go func(w int) {
			defer wg.Done()
			for i := 0; i < iterations; i++ {
				resp, err := remember(ctx, &convention.Request{
					Args: map[string]any{"key": "shared", "value": w},
				})
				if err != nil {
					errCh <- fmt.Errorf("writer %d: remember err: %w", w, err)
					return
				}
				payload, _ := resp.Payload.(map[string]any)
				if payload["ok"] != true {
					errCh <- fmt.Errorf("writer %d: remember not ok: %v", w, payload)
					return
				}
			}
		}(w)
	}

	// Readers: each iteration recalls the shared key, asserts the value is
	// one of the writer ids OR not-yet-found. Any other outcome (torn write,
	// corrupted JSON, different key's value) is a corruption and FAILS.
	for r := 0; r < readers; r++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for i := 0; i < iterations; i++ {
				resp, err := recall(ctx, &convention.Request{
					Args: map[string]any{"key": "shared"},
				})
				if err != nil {
					errCh <- fmt.Errorf("reader: recall err: %w", err)
					return
				}
				totalReads.Add(1)
				payload, _ := resp.Payload.(map[string]any)
				if payload["ok"] != true {
					errCh <- fmt.Errorf("reader: recall not ok: %v", payload)
					return
				}
				if payload["found"] != true {
					missingReads.Add(1)
					continue
				}
				// JSON unmarshal of an int produces float64. Normalise.
				f, ok := payload["value"].(float64)
				if !ok {
					corruptedReads.Add(1)
					errCh <- fmt.Errorf("reader: value not float64 (corruption?): %T %v", payload["value"], payload["value"])
					return
				}
				iv := int(f)
				if _, valid := validValues[iv]; !valid {
					corruptedReads.Add(1)
					errCh <- fmt.Errorf("reader: value %d not in writer id set (torn write?)", iv)
					return
				}
			}
		}()
	}

	wg.Wait()
	close(errCh)

	for err := range errCh {
		if err != nil {
			t.Fatalf("concurrent stress: %v", err)
		}
	}

	// After everyone finishes, recall must return the last-writer-wins value —
	// which is *some* writer id; we can't predict which, but we can prove it's
	// present and valid.
	resp, err := recall(ctx, &convention.Request{Args: map[string]any{"key": "shared"}})
	if err != nil {
		t.Fatalf("final recall: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["found"] != true {
		t.Fatalf("final recall: expected found=true after %d writes, got %v", writers*iterations, payload)
	}
	finalVal := int(payload["value"].(float64))
	if _, valid := validValues[finalVal]; !valid {
		t.Fatalf("final recall: value %d not a valid writer id", finalVal)
	}

	t.Logf("stress: total_reads=%d missing_reads=%d corrupted=%d final=%d",
		totalReads.Load(), missingReads.Load(), corruptedReads.Load(), finalVal)
}

// TestConcurrentDistinctKeys asserts that N writers on N distinct keys produce
// N distinct recall results with no cross-contamination.
func TestConcurrentDistinctKeys(t *testing.T) {
	const n = 200

	store := NewMemoryStore()
	remember := rememberHandler(store)
	recall := recallHandler(store)
	ctx := context.Background()

	var wg sync.WaitGroup
	for i := 0; i < n; i++ {
		wg.Add(1)
		go func(i int) {
			defer wg.Done()
			key := fmt.Sprintf("k-%d", i)
			val := map[string]any{"id": i, "payload": fmt.Sprintf("v-%d", i)}
			_, err := remember(ctx, &convention.Request{Args: map[string]any{"key": key, "value": val}})
			if err != nil {
				t.Errorf("remember k-%d: %v", i, err)
			}
		}(i)
	}
	wg.Wait()

	for i := 0; i < n; i++ {
		resp, err := recall(ctx, &convention.Request{Args: map[string]any{"key": fmt.Sprintf("k-%d", i)}})
		if err != nil {
			t.Fatalf("recall k-%d: %v", i, err)
		}
		payload, _ := resp.Payload.(map[string]any)
		if payload["found"] != true {
			t.Fatalf("k-%d: expected found, got %v", i, payload)
		}
		value, _ := payload["value"].(map[string]any)
		if value == nil {
			t.Fatalf("k-%d: value not a map: %v", i, payload["value"])
		}
		gotID, _ := value["id"].(float64)
		if int(gotID) != i {
			t.Fatalf("k-%d: cross-contamination — got id=%v", i, value["id"])
		}
		if value["payload"] != fmt.Sprintf("v-%d", i) {
			t.Fatalf("k-%d: payload mismatch: %v", i, value["payload"])
		}
	}
}

// TestStoreDefensiveCopy asserts that the store does not alias the caller's
// slice — mutating the input after Store must not change the stored value.
func TestStoreDefensiveCopy(t *testing.T) {
	store := NewMemoryStore()
	raw := json.RawMessage(`{"text":"original"}`)
	store.Store("k", raw)
	// Mutate the caller's slice.
	for i := range raw {
		raw[i] = 'X'
	}
	got, found := store.Load("k")
	if !found {
		t.Fatal("missing key")
	}
	if string(got) != `{"text":"original"}` {
		t.Fatalf("store aliased caller slice: got %s", string(got))
	}
}

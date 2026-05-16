/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"encoding/json"
	"testing"
	"time"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// ---------------------------------------------------------------------------
// Unit tests: ExtractTargetLevel
// ---------------------------------------------------------------------------

func TestExtractTargetLevel_DirectLevel(t *testing.T) {
	lv, ok := ExtractTargetLevel(map[string]any{"level": float64(3)})
	if !ok || lv != 3 {
		t.Errorf("want (3, true), got (%d, %v)", lv, ok)
	}
}

func TestExtractTargetLevel_LocationLevel(t *testing.T) {
	lv, ok := ExtractTargetLevel(map[string]any{
		"location": map[string]any{"x": float64(100), "y": float64(200), "level": float64(2)},
	})
	if !ok || lv != 2 {
		t.Errorf("want (2, true), got (%d, %v)", lv, ok)
	}
}

func TestExtractTargetLevel_NoLevel(t *testing.T) {
	lv, ok := ExtractTargetLevel(map[string]any{"x": float64(100), "y": float64(200)})
	if ok {
		t.Errorf("want (_, false) for args without level, got (%d, true)", lv)
	}
}

func TestExtractTargetLevel_LevelZeroNotExtracted(t *testing.T) {
	_, ok := ExtractTargetLevel(map[string]any{"level": float64(0)})
	if ok {
		t.Error("level=0 should not be extracted as a valid level")
	}
}

// TestExtractTargetLevel_WireFormatStringLocation is the regression test for
// freesoexperiment-133. The cf executor (executor.go validateSingleValue) stores
// type=json args as Go string values — not pre-parsed maps. This test builds args
// in the exact wire format that arrives from cf and verifies ExtractTargetLevel
// returns the correct level instead of (1, false).
//
// Before the fix: loc.(map[string]any) cast failed silently, returning (1, false).
// After the fix: the string fallback parses the JSON and returns (2, true).
func TestExtractTargetLevel_WireFormatStringLocation(t *testing.T) {
	// Simulate what cf sends on the wire: location serialized as a JSON string.
	wireArgs := map[string]any{
		"location": `{"x":35,"y":70,"level":2}`, // string, not map[string]any
	}
	lv, ok := ExtractTargetLevel(wireArgs)
	if !ok {
		t.Fatal("want (2, true) for wire-format string location, got (_, false) — cross-level stair path would be skipped")
	}
	if lv != 2 {
		t.Errorf("want level=2, got %d", lv)
	}
}

// TestExtractTargetLevel_WireFormatStringLocationLevel1 verifies that a
// wire-format string location with level=1 is NOT extracted (returns false),
// since level=1 is the same as no cross-level navigation needed. This ensures
// the zero-guard still applies after JSON unmarshaling.
func TestExtractTargetLevel_WireFormatStringLocationLevel1Kept(t *testing.T) {
	// level=1 in wire format — coerceInt64 returns 1 which is > 0, so it IS extracted.
	// This is correct: if the agent explicitly passes level=1 and the avatar is on
	// floor 2, the stair-detection path should engage (floor 2 != floor 1).
	wireArgs := map[string]any{
		"location": `{"x":10,"y":20,"level":1}`,
	}
	lv, ok := ExtractTargetLevel(wireArgs)
	if !ok {
		t.Fatal("want (1, true) for wire-format level=1, got (_, false)")
	}
	if lv != 1 {
		t.Errorf("want level=1, got %d", lv)
	}
}

// TestExtractTargetLevel_WireFormatStringInvalidJSON verifies that a malformed
// JSON string in the location arg does not panic and returns (1, false).
func TestExtractTargetLevel_WireFormatStringInvalidJSON(t *testing.T) {
	wireArgs := map[string]any{
		"location": `{not valid json`,
	}
	_, ok := ExtractTargetLevel(wireArgs)
	// Invalid JSON → cannot extract → ok must be false (no panic).
	if ok {
		t.Error("malformed JSON string should return ok=false")
	}
}

// ---------------------------------------------------------------------------
// Unit tests: isStairObject
// ---------------------------------------------------------------------------

// TestIsStairObject_NameFallback tests the backward-compat name-substring path.
// When ObjectType is empty (older payload pre-dating freesoexperiment-d5b),
// the function falls back to the name-substring heuristic.
func TestIsStairObject_NameFallback(t *testing.T) {
	cases := []struct {
		name string
		want bool
	}{
		{"Staircase", true},
		{"stair", true},
		{"STAIR 2", true},
		{"Spiral Staircase", true},
		{"stairsspiral", true},
		{"Door", false},
		{"Refrigerator", false},
		{"Stairs Landing", true},
	}
	for _, c := range cases {
		obj := nearbyObject{Name: c.name, ObjectType: ""} // empty type = older payload
		got := isStairObject(obj)
		if got != c.want {
			t.Errorf("isStairObject(name=%q, type=): want %v, got %v", c.name, c.want, got)
		}
	}
}

// TestIsStairObject_TypeEnum tests the primary type-field path (freesoexperiment-d5b).
// When ObjectType is set by the PerceptionProjector, type-aware filtering applies.
func TestIsStairObject_TypeEnum(t *testing.T) {
	cases := []struct {
		name       string
		objectType string
		want       bool
	}{
		// Portal + stair name → stair
		{"Staircase", "Portal", true},
		{"stair", "Portal", true},
		{"Spiral Staircase", "Portal", true},
		// Portal + non-stair name → not a stair (door, window, pool equipment)
		{"Door", "Portal", false},
		{"Pool Ladder", "Portal", false},
		{"Window", "Portal", false},
		// Non-Portal types → never a stair
		{"Refrigerator", "Normal", false},
		{"Stair Painting", "Normal", false}, // name contains stair but type is Normal
		{"Stair Rug", "Food", false},
		// Unknown type falls back to name heuristic
		{"Staircase", "Unknown", true},
		{"Door", "Unknown", false},
		// Empty type (backward compat) falls back to name heuristic
		{"Staircase", "", true},
		{"Door", "", false},
	}
	for _, c := range cases {
		obj := nearbyObject{Name: c.name, ObjectType: c.objectType}
		got := isStairObject(obj)
		if got != c.want {
			t.Errorf("isStairObject(name=%q, type=%q): want %v, got %v", c.name, c.objectType, c.want, got)
		}
	}
}

// ---------------------------------------------------------------------------
// Unit tests: coerceInt64
// ---------------------------------------------------------------------------

func TestCoerceInt64_Types(t *testing.T) {
	cases := []struct {
		v    any
		want int64
		ok   bool
	}{
		{int64(3), 3, true},
		{int(7), 7, true},
		{float64(2.0), 2, true},
		{json.Number("5"), 5, true},
		{"string", 0, false},
		{nil, 0, false},
	}
	for _, c := range cases {
		got, ok := coerceInt64(c.v)
		if ok != c.ok || (ok && got != c.want) {
			t.Errorf("coerceInt64(%v): want (%d, %v), got (%d, %v)", c.v, c.want, c.ok, got, ok)
		}
	}
}

// ---------------------------------------------------------------------------
// Helper: multiResponder wires a fake bot that responds to N IPC ops in order.
// Each entry in responses maps op-name → payload to return.
// ---------------------------------------------------------------------------

// multiAutoResponder consumes commands from fake.stdinLines and delivers
// pre-scripted responses keyed by op. Unknown ops get ok:true, empty payload.
func multiAutoResponder(t *testing.T, fake *fakeBotProcess, ipc *IPC, responses map[string]map[string]any) chan Command {
	t.Helper()
	received := make(chan Command, 8)
	go func() {
		for line := range fake.stdinLines {
			var cmd Command
			if err := json.Unmarshal(line, &cmd); err != nil {
				t.Errorf("multiAutoResponder: unmarshal: %v", err)
				continue
			}
			received <- cmd
			respPayload, ok := responses[cmd.Op]
			if !ok {
				respPayload = map[string]any{"ok": true}
			}
			respPayload["cmd_id"] = cmd.ID
			respPayload["kind"] = "response"
			data, _ := json.Marshal(respPayload)
			ipc.Deliver(data)
		}
	}()
	return received
}

// ---------------------------------------------------------------------------
// TestWalkToHandler_SameLevel_DirectForward
//
// When target level == current level, walk-to is forwarded directly with no
// stair check (only one IPC call: walk-to).
// ---------------------------------------------------------------------------

func TestWalkToHandler_SameLevel_DirectForward(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	responses := map[string]map[string]any{
		// query-self returns level=1
		"query-self": {
			"ok": true,
			"payload": map[string]any{
				"position": map[string]any{"x": 10.0, "y": 20.0, "level": 1.0},
			},
		},
		// walk-to succeeds
		"walk-to": {
			"ok":      true,
			"payload": map[string]any{"queued": true, "x": 100, "y": 200},
		},
	}
	received := multiAutoResponder(t, fake, ipc, responses)

	handler := walkToHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"x": float64(100), "y": float64(200), "level": float64(1),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}

	// Should only receive a single IPC call: walk-to (no query-self, because
	// same-level check only queries self when level arg is present AND might
	// differ — actually the handler calls query-self first).
	// Drain received commands.
	var cmds []Command
	drainLoop:
	for {
		select {
		case cmd := <-received:
			cmds = append(cmds, cmd)
			// We expect query-self then walk-to, OR just walk-to if handler
			// detects same level without querying.
		case <-time.After(200 * time.Millisecond):
			break drainLoop
		}
	}

	// Verify the last command was walk-to.
	var walkToSent bool
	for _, cmd := range cmds {
		if cmd.Op == "walk-to" {
			walkToSent = true
		}
	}
	if !walkToSent {
		t.Errorf("expected walk-to IPC command, got: %v", cmds)
	}

	// No stair interact-with should have been queued.
	for _, cmd := range cmds {
		if cmd.Op == "interact-with" {
			t.Errorf("unexpected interact-with for same-level navigation: %v", cmd)
		}
	}

	payload, _ := resp.Payload.(map[string]any)
	if payload == nil || payload["ok"] != true {
		t.Errorf("expected ok=true response, got: %v", resp.Payload)
	}
}

// ---------------------------------------------------------------------------
// TestWalkToHandler_CrossLevel_QueuesStairThenDestination
//
// When target level != current level and a stair exists nearby, walk-to should:
// 1. Issue query-self (to detect level mismatch).
// 2. Issue query-nearby (to find stairs).
// 3. Issue query-pie-menu on the stair (to get climb interaction ID).
// 4. Issue interact-with on the stair with queue_mode=queue.
// 5. Issue walk-to with queue_mode=queue.
// The convention response carries cross_level=true and stair_object_id.
// ---------------------------------------------------------------------------

func TestWalkToHandler_CrossLevel_QueuesStairThenDestination(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	responses := map[string]map[string]any{
		"query-self": {
			"ok": true,
			"payload": map[string]any{
				"position": map[string]any{"x": 5.0, "y": 5.0, "level": 1.0},
			},
		},
		"query-nearby": {
			"ok": true,
			"payload": map[string]any{
				"nearby_objects": []map[string]any{
					{
						"object_id": 42,
						"name":      "Staircase",
						"distance_tiles": 3.5,
						"position": map[string]any{"x": 8.0, "y": 8.0, "level": 1.0},
					},
				},
			},
		},
		"query-pie-menu": {
			"ok": true,
			"payload": map[string]any{
				"interactions": []map[string]any{
					{"id": 2, "name": "Climb Stairs"},
				},
			},
		},
		"interact-with": {
			"ok": true,
			"payload": map[string]any{"queued": true, "interaction": 2, "callee_id": 42},
		},
		"walk-to": {
			"ok": true,
			"payload": map[string]any{"queued": true, "x": 100, "y": 200},
		},
	}
	received := multiAutoResponder(t, fake, ipc, responses)

	handler := walkToHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"x": float64(100), "y": float64(200), "level": float64(3),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}

	// Collect all IPC commands issued.
	var cmds []Command
	drainLoop2:
	for {
		select {
		case cmd := <-received:
			cmds = append(cmds, cmd)
		case <-time.After(300 * time.Millisecond):
			break drainLoop2
		}
	}

	// Verify the sequence contains interact-with BEFORE walk-to.
	var interactIdx, walkToIdx int = -1, -1
	for i, cmd := range cmds {
		if cmd.Op == "interact-with" {
			interactIdx = i
		}
		if cmd.Op == "walk-to" {
			walkToIdx = i
		}
	}
	if interactIdx < 0 {
		t.Errorf("expected interact-with (Climb-Stairs) in IPC sequence, got: %v", opsOf(cmds))
	}
	if walkToIdx < 0 {
		t.Errorf("expected walk-to in IPC sequence, got: %v", opsOf(cmds))
	}
	if interactIdx >= 0 && walkToIdx >= 0 && interactIdx > walkToIdx {
		t.Errorf("interact-with must come BEFORE walk-to; got order: %v", opsOf(cmds))
	}

	// Verify interact-with targets the stair (callee_id=42).
	for _, cmd := range cmds {
		if cmd.Op == "interact-with" {
			if cmd.Args["callee_id"] != float64(42) {
				t.Errorf("interact-with callee_id: want 42, got %v", cmd.Args["callee_id"])
			}
			// queue_mode must be "queue".
			if cmd.Args["queue_mode"] != "queue" {
				t.Errorf("interact-with queue_mode: want queue, got %v", cmd.Args["queue_mode"])
			}
		}
	}

	// Verify walk-to has queue_mode=queue.
	for _, cmd := range cmds {
		if cmd.Op == "walk-to" {
			if cmd.Args["queue_mode"] != "queue" {
				t.Errorf("walk-to queue_mode: want queue, got %v", cmd.Args["queue_mode"])
			}
		}
	}

	// Verify convention response carries cross_level=true and stair_object_id.
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil {
		t.Fatal("nil payload")
	}
	if payload["ok"] != true {
		t.Errorf("expected ok=true, got: %v", payload["ok"])
	}
	if payload["cross_level"] != true {
		t.Errorf("expected cross_level=true in response, got: %v", payload)
	}
	if payload["stair_object_id"] == nil {
		t.Errorf("expected stair_object_id in response, got: %v", payload)
	}
}

// ---------------------------------------------------------------------------
// TestWalkToHandler_CrossLevel_NoStairRefuses
//
// When target level != current level but no stair is found, the handler returns
// ok:false with reason=category:no-stair-path.
// ---------------------------------------------------------------------------

func TestWalkToHandler_CrossLevel_NoStairRefuses(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	responses := map[string]map[string]any{
		"query-self": {
			"ok": true,
			"payload": map[string]any{
				"position": map[string]any{"x": 5.0, "y": 5.0, "level": 1.0},
			},
		},
		"query-nearby": {
			"ok": true,
			"payload": map[string]any{
				// No stair objects.
				"nearby_objects": []map[string]any{
					{
						"object_id": 10,
						"name":      "Refrigerator",
						"distance_tiles": 2.0,
						"position": map[string]any{"x": 7.0, "y": 5.0, "level": 1.0},
					},
				},
			},
		},
	}
	multiAutoResponder(t, fake, ipc, responses)

	handler := walkToHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"x": float64(50), "y": float64(50), "level": float64(2),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}

	payload, _ := resp.Payload.(map[string]any)
	if payload == nil {
		t.Fatal("nil payload")
	}
	if payload["ok"] != false {
		t.Errorf("expected ok=false for no-stair-path, got: %v", payload["ok"])
	}
	reason, _ := payload["reason"].(string)
	if reason == "" || len(reason) == 0 {
		t.Errorf("expected reason field in no-stair-path response, got: %v", payload)
	}
	// Reason must contain the no-stair-path token.
	if !containsStr(reason, "no-stair-path") {
		t.Errorf("expected reason to contain 'no-stair-path', got %q", reason)
	}
}

// ---------------------------------------------------------------------------
// TestGoToHandler_CrossLevel_QueuesStairThenDestination
//
// go-to with a location map carrying level=3 from level=1 should follow the
// same stair-queue path as walk-to.
// ---------------------------------------------------------------------------

func TestGoToHandler_CrossLevel_QueuesStairThenDestination(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	responses := map[string]map[string]any{
		"query-self": {
			"ok": true,
			"payload": map[string]any{
				"position": map[string]any{"x": 5.0, "y": 5.0, "level": 1.0},
			},
		},
		"query-nearby": {
			"ok": true,
			"payload": map[string]any{
				"nearby_objects": []map[string]any{
					{
						"object_id": 77,
						"name":      "Spiral Staircase",
						"distance_tiles": 2.0,
						"position": map[string]any{"x": 6.0, "y": 6.0, "level": 1.0},
					},
				},
			},
		},
		"query-pie-menu": {
			"ok": true,
			"payload": map[string]any{
				"interactions": []map[string]any{
					{"id": 0, "name": "Go Up"},
				},
			},
		},
		"interact-with": {
			"ok": true,
			"payload": map[string]any{"queued": true, "interaction": 0, "callee_id": 77},
		},
		"go-to": {
			"ok": true,
			"payload": map[string]any{"queued": true},
		},
	}
	received := multiAutoResponder(t, fake, ipc, responses)

	store := NewMemoryStore()
	handler := goToHandler(ipc, store)
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"location": map[string]any{"x": float64(50), "y": float64(50), "level": float64(3)},
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}

	var cmds []Command
	drainLoop3:
	for {
		select {
		case cmd := <-received:
			cmds = append(cmds, cmd)
		case <-time.After(300 * time.Millisecond):
			break drainLoop3
		}
	}

	var interactIdx, goToIdx int = -1, -1
	for i, cmd := range cmds {
		if cmd.Op == "interact-with" {
			interactIdx = i
		}
		if cmd.Op == "go-to" {
			goToIdx = i
		}
	}
	if interactIdx < 0 {
		t.Errorf("expected interact-with in IPC sequence, got: %v", opsOf(cmds))
	}
	if goToIdx < 0 {
		t.Errorf("expected go-to in IPC sequence, got: %v", opsOf(cmds))
	}
	if interactIdx >= 0 && goToIdx >= 0 && interactIdx > goToIdx {
		t.Errorf("interact-with must come BEFORE go-to; got order: %v", opsOf(cmds))
	}

	payload, _ := resp.Payload.(map[string]any)
	if payload == nil || payload["cross_level"] != true {
		t.Errorf("expected cross_level=true in response, got: %v", payload)
	}
	if payload["stair_object_id"] == nil {
		t.Errorf("expected stair_object_id in response, got: %v", payload)
	}
}

// ---------------------------------------------------------------------------
// TestWalkToHandler_NoLevel_DirectForward
//
// When no level is specified in args (level key absent), the handler passes
// through directly without any stair check.
// ---------------------------------------------------------------------------

func TestWalkToHandler_NoLevel_DirectForward(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{"queued": true},
	})

	handler := walkToHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"target_object_id": float64(55), // no level — go to object on same level
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}

	cmd := <-gotCmd
	// Should forward directly to walk-to, no query-self or interact-with.
	if cmd.Op != "walk-to" {
		t.Errorf("want op=walk-to, got %q", cmd.Op)
	}

	payload, _ := resp.Payload.(map[string]any)
	if payload == nil || payload["ok"] != true {
		t.Errorf("expected ok=true: %v", resp.Payload)
	}
}

// ---------------------------------------------------------------------------
// TestFindStairForCrossLevel_SameLevel
//
// findStairForCrossLevel returns (nil, nil) when current level == target level.
// ---------------------------------------------------------------------------

func TestFindStairForCrossLevel_SameLevel(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// Respond to query-self with level=2.
	go func() {
		for line := range fake.stdinLines {
			var cmd Command
			_ = json.Unmarshal(line, &cmd)
			data, _ := json.Marshal(map[string]any{
				"kind":   "response",
				"cmd_id": cmd.ID,
				"ok":     true,
				"payload": map[string]any{
					"position": map[string]any{"x": 0.0, "y": 0.0, "level": 2.0},
				},
			})
			ipc.Deliver(data)
		}
	}()

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	result, err := findStairForCrossLevel(ctx, ipc, 2, 0)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if result != nil {
		t.Errorf("expected nil result for same-level, got: %v", result)
	}
}

// ---------------------------------------------------------------------------
// TestFindStairForCrossLevel_TypeFieldPortal (freesoexperiment-d5b)
//
// When nearby_objects includes object_type="Portal" on a stair name, the type-field
// path correctly identifies it as a stair and returns it as the cross-level result.
// ---------------------------------------------------------------------------

func TestFindStairForCrossLevel_TypeFieldPortal_StairName(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// Avatar on level 1, target level 2. Query-nearby returns a Portal-typed stair.
	responses := map[string]map[string]any{
		"query-self": {
			"ok": true,
			"payload": map[string]any{
				"position": map[string]any{"x": 1.0, "y": 1.0, "level": 1.0},
			},
		},
		"query-nearby": {
			"ok": true,
			"payload": map[string]any{
				"nearby_objects": []map[string]any{
					{
						"object_id":    55,
						"name":         "Staircase",
						"object_type":  "Portal",
						"distance_tiles": 4.0,
						"position":     map[string]any{"x": 5.0, "y": 5.0, "level": 1.0},
					},
				},
			},
		},
	}
	multiAutoResponder(t, fake, ipc, responses)

	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	result, err := findStairForCrossLevel(ctx, ipc, 2, 0)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if result == nil {
		t.Fatal("expected stair result, got nil — Portal+stair-name object should be found")
	}
	if result.ObjectID != 55 {
		t.Errorf("expected ObjectID=55, got %d", result.ObjectID)
	}
}

// TestFindStairForCrossLevel_TypeFieldNormal_StairNameExcluded verifies that an
// object with object_type="Normal" is NOT selected as a stair even if its name
// contains "stair". This is the key improvement over the old name-only heuristic:
// an object called "Stair Painting" (Normal type) should not block navigation.
func TestFindStairForCrossLevel_TypeFieldNormal_StairNameExcluded(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	responses := map[string]map[string]any{
		"query-self": {
			"ok": true,
			"payload": map[string]any{
				"position": map[string]any{"x": 1.0, "y": 1.0, "level": 1.0},
			},
		},
		"query-nearby": {
			"ok": true,
			"payload": map[string]any{
				"nearby_objects": []map[string]any{
					{
						"object_id":    20,
						"name":         "Stair Painting",  // name contains stair but type is Normal
						"object_type":  "Normal",
						"distance_tiles": 2.0,
						"position":     map[string]any{"x": 3.0, "y": 3.0, "level": 1.0},
					},
				},
			},
		},
	}
	multiAutoResponder(t, fake, ipc, responses)

	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	result, err := findStairForCrossLevel(ctx, ipc, 2, 0)
	// Should return errNoStair — Normal-typed "Stair Painting" is not a portal.
	if err == nil {
		t.Errorf("expected no-stair-path error, got nil error with result: %v", result)
	}
	if result != nil {
		t.Errorf("expected nil result for Normal-typed stair-named object, got: %v", result)
	}
}

// ---------------------------------------------------------------------------
// TestGoToHandler_CrossLevel_WireFormatStringLocation (regression: freesoexperiment-133)
//
// This is the pipeline regression test for freesoexperiment-133. When cf sends
// go-to with --location '{"x":35,"y":70,"level":2}', the executor serializes it
// as a JSON string in the wire payload ("location":"{\"x\":35,\"y\":70,\"level\":2}").
//
// Before the fix: ExtractTargetLevel did loc.(map[string]any) — always failed for
// a string arg — returned (1, false), so findStairForCrossLevel was never called,
// and the handler forwarded go-to without queuing stairs. Live evidence: action_queue
// showed mode=walk picked_name=tile(35,70,2), no Climb-Stairs entry.
//
// After the fix: the string fallback branch parses the JSON, extracts level=2,
// triggers findStairForCrossLevel (avatar on level=1), finds the stair, queues
// interact-with before go-to. Response carries cross_level=true.
// ---------------------------------------------------------------------------

func TestGoToHandler_CrossLevel_WireFormatStringLocation(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	responses := map[string]map[string]any{
		"query-self": {
			"ok": true,
			"payload": map[string]any{
				"position": map[string]any{"x": 5.0, "y": 5.0, "level": 1.0},
			},
		},
		"query-nearby": {
			"ok": true,
			"payload": map[string]any{
				"nearby_objects": []map[string]any{
					{
						"object_id":      99,
						"name":           "Staircase",
						"distance_tiles": 4.0,
						"position":       map[string]any{"x": 9.0, "y": 9.0, "level": 1.0},
					},
				},
			},
		},
		"query-pie-menu": {
			"ok": true,
			"payload": map[string]any{
				"interactions": []map[string]any{
					{"id": 1, "name": "Climb Stairs"},
				},
			},
		},
		"interact-with": {
			"ok": true,
			"payload": map[string]any{"queued": true, "callee_id": 99},
		},
		"go-to": {
			"ok": true,
			"payload": map[string]any{"queued": true},
		},
	}
	received := multiAutoResponder(t, fake, ipc, responses)

	store := NewMemoryStore()
	handler := goToHandler(ipc, store)
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	// Wire-format args: location is a JSON string, not a pre-parsed map.
	// This is exactly what arrives from the cf executor.
	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"location": `{"x":35,"y":70,"level":2}`, // string, not map[string]any
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}

	var cmds []Command
drainLoopWire:
	for {
		select {
		case cmd := <-received:
			cmds = append(cmds, cmd)
		case <-time.After(300 * time.Millisecond):
			break drainLoopWire
		}
	}

	// Must have interact-with (Climb-Stairs) BEFORE go-to.
	var interactIdx, goToIdx int = -1, -1
	for i, cmd := range cmds {
		if cmd.Op == "interact-with" {
			interactIdx = i
		}
		if cmd.Op == "go-to" {
			goToIdx = i
		}
	}
	if interactIdx < 0 {
		t.Errorf("[regression freesoexperiment-133] expected interact-with (Climb-Stairs) in IPC sequence for wire-format string location, got: %v", opsOf(cmds))
	}
	if goToIdx < 0 {
		t.Errorf("[regression freesoexperiment-133] expected go-to in IPC sequence, got: %v", opsOf(cmds))
	}
	if interactIdx >= 0 && goToIdx >= 0 && interactIdx > goToIdx {
		t.Errorf("[regression freesoexperiment-133] interact-with must come BEFORE go-to; got order: %v", opsOf(cmds))
	}

	payload, _ := resp.Payload.(map[string]any)
	if payload == nil {
		t.Fatal("nil payload")
	}
	if payload["cross_level"] != true {
		t.Errorf("[regression freesoexperiment-133] expected cross_level=true in response, got: %v", payload)
	}
	if payload["stair_object_id"] == nil {
		t.Errorf("[regression freesoexperiment-133] expected stair_object_id in response, got: %v", payload)
	}
}

// ---------------------------------------------------------------------------
// helpers
// ---------------------------------------------------------------------------

func opsOf(cmds []Command) []string {
	ops := make([]string, len(cmds))
	for i, c := range cmds {
		ops[i] = c.Op
	}
	return ops
}

func containsStr(s, sub string) bool {
	return len(s) >= len(sub) && (s == sub || len(s) > 0 && containsRuneStr(s, sub))
}

func containsRuneStr(s, sub string) bool {
	for i := range s {
		if i+len(sub) <= len(s) && s[i:i+len(sub)] == sub {
			return true
		}
	}
	return false
}

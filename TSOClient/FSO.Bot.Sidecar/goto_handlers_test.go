/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"bytes"
	"context"
	"log"
	"strings"
	"sync"
	"testing"
	"time"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// goToHandlerFast returns a goToHandler wired with the fast test config for the
// verifying path so unit tests don't burn 1500ms settle per call.
// The handler is returned as the inner convention.HandlerFunc (no ctx/declaration wrap)
// so callers can invoke it directly as a black box.
func goToHandlerFast(ipc *IPC, store *MemoryStore) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := map[string]any{}
		for _, k := range []string{"target_object_id", "target_sim_id", "object_name", "location", "interaction", "queue_mode", "max_distance_tiles"} {
			if v, ok := req.Args[k]; ok {
				args[k] = v
			}
		}
		// Skip name resolution and cross-level check for unit tests (no real bot).
		if _, hasInteraction := args["interaction"]; hasInteraction {
			h := verifyingHandlerWithExpect(
				ipc,
				"go-to",
				[]string{"target_object_id", "target_sim_id", "object_name", "location", "interaction", "queue_mode", "max_distance_tiles"},
				0, "", nil,
				gotoInteractionExpect,
				fastTestConfig(),
			)
			return h(ctx, req)
		}
		return forwardIPC(ctx, ipc, "go-to", args)
	}
}

// buildSnapshotScript is a helper that builds a scriptedResponder script with
// the standard pre/post query-self and query-lot-objects entries, plus the
// named op's entries. preQueue and postQueue are the action_queue entries for
// the pre- and post-snapshots of query-self.
func buildSnapshotScript(op string, opEntries []map[string]any, preQueue, postQueue []any) map[string][]map[string]any {
	return map[string][]map[string]any{
		"query-self": {
			{"ok": true, "payload": map[string]any{"balance": 5000, "action_queue": preQueue}},
			{"ok": true, "payload": map[string]any{"balance": 5000, "action_queue": postQueue}},
		},
		"query-lot-objects": {
			{"ok": true, "payload": map[string]any{"objects": []any{}}},
			{"ok": true, "payload": map[string]any{"objects": []any{}}},
		},
		op: opEntries,
	}
}

// TestGoToWithInteraction_Queued: go-to --interaction dispatches the IPC and
// gets verdict=queued when the post-snapshot shows a new action_queue entry
// for the picked object.
func TestGoToWithInteraction_Queued(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	postQueue := []any{
		map[string]any{"interaction_id": 77, "name": "Read", "target_object_id": float64(55), "status": "queued"},
	}
	script := buildSnapshotScript("go-to", []map[string]any{
		{"ok": true, "payload": map[string]any{
			"mode":               "walk-and-do",
			"picked_object_id":   55,
			"picked_interaction_id": 9,
			"picked_name":        "Newspaper",
		}},
	}, []any{}, postQueue)
	_ = newScriptedResponder(t, fake, ipc, script)

	store := NewMemoryStore()
	handler := goToHandlerFast(ipc, store)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"target_object_id": float64(55),
			"interaction":      "Read",
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	pay, _ := resp.Payload.(map[string]any)
	if pay == nil {
		t.Fatal("nil payload")
	}
	verdict, _ := pay["verdict"].(string)
	if verdict != "queued" && verdict != "interaction-started" {
		t.Errorf("want verdict=queued or interaction-started, got %q (payload=%v)", verdict, pay)
	}
	if ok, _ := pay["ok"].(bool); !ok {
		t.Errorf("want ok=true, got %v", pay["ok"])
	}
}

// TestGoToWithInteraction_SilentDrop: go-to --interaction IPC returns ok:true
// but no new action_queue entry appears. Verdict must be silent-drop with
// "unavailable-interaction-no-event" hint.
func TestGoToWithInteraction_SilentDrop(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// Post queue is identical to pre (empty) — no new entry.
	script := buildSnapshotScript("go-to", []map[string]any{
		{"ok": true, "payload": map[string]any{
			"mode":             "walk-and-do",
			"picked_object_id": 55,
			"picked_name":      "Newspaper",
		}},
	}, []any{}, []any{})
	_ = newScriptedResponder(t, fake, ipc, script)

	store := NewMemoryStore()
	handler := goToHandlerFast(ipc, store)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, _ := handler(ctx, &convention.Request{
		Args: map[string]any{
			"target_object_id": float64(55),
			"interaction":      "Read",
		},
	})
	pay, _ := resp.Payload.(map[string]any)
	if pay == nil {
		t.Fatal("nil payload")
	}
	if verdict := pay["verdict"]; verdict != "silent-drop" {
		t.Errorf("want verdict=silent-drop, got %q", verdict)
	}
	hints, _ := pay["hints"].([]string)
	if !containsHint(hints, "unavailable-interaction-no-event") {
		t.Errorf("want hint 'unavailable-interaction-no-event', got %v", hints)
	}
	// target-out-of-range should also fire: picked_object_id=55 not in either
	// snapshot's object list.
	if !containsHint(hints, "target-out-of-range") {
		t.Errorf("want hint 'target-out-of-range' (callee not in snapshot objects), got %v", hints)
	}
}

// TestGoToPlain_FastPath: go-to WITHOUT --interaction must NOT go through the
// verifier. The response must be the raw IPC ack (no "verdict" field), proving
// plain locomotion stays on forwardIPC.
func TestGoToPlain_FastPath(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// Only one IPC call expected (go-to itself) — no snapshot calls.
	rawAck := map[string]any{
		"mode":              "walk",
		"picked_object_id":  55,
		"picked_name":       "Newspaper",
		"queue_mode":        "queue",
		"cancelled":         0,
	}
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true, "payload": rawAck,
	})

	store := NewMemoryStore()
	handler := goToHandlerFast(ipc, store)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"target_object_id": float64(55),
			// No "interaction" key — plain locomotion path.
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	cmd := <-gotCmd
	if cmd.Op != "go-to" {
		t.Errorf("want op=go-to, got %q", cmd.Op)
	}
	pay, _ := resp.Payload.(map[string]any)
	if pay == nil {
		t.Fatal("nil payload")
	}
	// Critical: plain locomotion response must NOT have a "verdict" field.
	// If "verdict" is present, the verifier path fired — that's the bug.
	if _, hasVerdict := pay["verdict"]; hasVerdict {
		t.Errorf("plain go-to (no --interaction) must NOT return a verdict field; got payload=%v", pay)
	}
}

// TestGotoInteractionExpect_IgnoresPreExistingEntries: entries that existed in
// pre must be ignored — only NEW entries count.
func TestGotoInteractionExpect_IgnoresPreExistingEntries(t *testing.T) {
	pre := lotSnapshot{
		actionQueue: []ActionQueueEntry{
			{InteractionID: 10, Name: "Sleep", TargetObjectID: 55, Status: "running"},
		},
		objectsByPID: map[uint64]objectRef{},
	}
	// Post adds the same entry (still running) — no NEW entry.
	post := lotSnapshot{
		actionQueue: []ActionQueueEntry{
			{InteractionID: 10, Name: "Sleep", TargetObjectID: 55, Status: "running"},
		},
		objectsByPID: map[uint64]objectRef{},
	}
	ipcResp := &Response{Ok: true, Payload: []byte(`{"mode":"walk-and-do","picked_object_id":55}`)}
	verdict, hints, _, ok := gotoInteractionExpect(pre, post, nil, ipcResp)
	if verdict != "silent-drop" {
		t.Errorf("want silent-drop (pre entry not new), got %q", verdict)
	}
	if ok {
		t.Error("ok must be false for silent-drop")
	}
	if !containsHint(hints, "unavailable-interaction-no-event") {
		t.Errorf("want unavailable-interaction-no-event hint, got %v", hints)
	}
}

// TestGotoInteractionExpect_WrongTargetIgnored: new entry exists but targets a
// different object — must not match.
func TestGotoInteractionExpect_WrongTargetIgnored(t *testing.T) {
	pre := lotSnapshot{actionQueue: []ActionQueueEntry{}, objectsByPID: map[uint64]objectRef{}}
	post := lotSnapshot{
		actionQueue: []ActionQueueEntry{
			{InteractionID: 20, Name: "Sit", TargetObjectID: 99, Status: "queued"},
		},
		objectsByPID: map[uint64]objectRef{},
	}
	// IPC ack says picked_object_id=55 — different from the new entry's target (99).
	ipcResp := &Response{Ok: true, Payload: []byte(`{"mode":"walk-and-do","picked_object_id":55}`)}
	verdict, _, _, _ := gotoInteractionExpect(pre, post, nil, ipcResp)
	if verdict != "silent-drop" {
		t.Errorf("want silent-drop (new entry targets wrong object), got %q", verdict)
	}
}

// TestGoToDeprecationOnceEmission asserts that the objectNameDeprecationOnce guard
// emits the deprecation warning exactly once, even when the handler is called
// multiple times with object_name.
//
// Strategy: capture log output with bytes.Buffer/log.SetOutput, reset the sync.Once
// via reassignment in test scope, call goToHandler twice with object_name, and verify
// the DEPRECATED message appears exactly once in the captured output.
func TestGoToDeprecationOnceEmission(t *testing.T) {
	// Save original stdout and log output.
	originalOut := log.Default().Writer()
	defer log.SetOutput(originalOut)

	// Capture log output to a buffer.
	var logBuf bytes.Buffer
	log.SetOutput(&logBuf)

	// CRITICAL: Reset the package-level Once for this test.
	// This must run BEFORE we call the handler, since Once.Do fires only once
	// per process lifetime. By reassigning in test scope, the handler will see
	// a fresh Once that has never fired.
	objectNameDeprecationOnce = sync.Once{}

	// Set up a fake bot so goToHandler can forward to IPC.
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// Drain stdin in the background so IPC.Send() doesn't block.
	go func() {
		for range fake.stdinLines {
		}
	}()

	// Create an empty MemoryStore (no name resolution needed for this test).
	store := NewMemoryStore()

	// Create the handler.
	handler := goToHandler(ipc, store)

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	// First call with object_name: should emit the deprecation message.
	resp1, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"object_name": "my_object",
		},
	})
	if err != nil {
		t.Fatalf("first handler call: %v", err)
	}
	if resp1 == nil {
		t.Fatal("first call: nil response")
	}

	// Small delay to allow any goroutine logging to flush.
	time.Sleep(10 * time.Millisecond)

	// Second call with object_name: should NOT emit the message again.
	resp2, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"object_name": "another_object",
		},
	})
	if err != nil {
		t.Fatalf("second handler call: %v", err)
	}
	if resp2 == nil {
		t.Fatal("second call: nil response")
	}

	// Check the captured output.
	captured := logBuf.String()
	count := strings.Count(captured, "DEPRECATED:")
	if count != 1 {
		t.Errorf("want exactly 1 DEPRECATED message, got %d\nCaptured:\n%s", count, captured)
	}

	// Verify the message contains the expected text.
	if !strings.Contains(captured, "object_name is deprecated") {
		t.Errorf("deprecation message doesn't contain expected text. Captured:\n%s", captured)
	}
}

// TestGoToDeclarationPresent asserts the go-to declaration loads and carries
// a galtrader-style description.
func TestGoToDeclarationPresent(t *testing.T) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}
	var d *convention.Declaration
	for _, x := range decls {
		if x.Operation == "go-to" {
			d = x
			break
		}
	}
	if d == nil {
		t.Fatal("declaration for go-to missing")
	}
	if d.Convention != "freeso-embodiment" {
		t.Errorf("convention=%q, want freeso-embodiment", d.Convention)
	}
	if d.Description == "" {
		t.Fatal("empty description")
	}
}

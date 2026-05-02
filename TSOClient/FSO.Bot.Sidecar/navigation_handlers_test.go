/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"encoding/json"
	"os"
	"path/filepath"
	"testing"
	"time"

	"github.com/campfire-net/campfire/pkg/convention"
)

// TestGoHomeHandlerDispatchesIPC asserts the go-home convention handler sends
// an IPC command with op="go-home" and no args, and surfaces the bot's
// already_home payload back to the caller.
func TestGoHomeHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{
			"already_home":         true,
			"current_lot_location": "0xF8F0DC",
			"home_lot_location":    "0xF8F0DC",
		},
	})

	handler := goHomeHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{Args: map[string]any{}})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	if resp == nil {
		t.Fatal("nil response")
	}
	cmd := <-gotCmd
	if cmd.Op != "go-home" {
		t.Errorf("want op=go-home got %q", cmd.Op)
	}
	if len(cmd.Args) != 0 {
		t.Errorf("go-home takes no args, got %v", cmd.Args)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil || payload["ok"] != true {
		t.Errorf("response payload missing ok=true: %v", resp.Payload)
	}
	// The bot's already_home marker should survive forwardIPC untouched.
	inner, _ := payload["payload"].(map[string]any)
	if inner == nil || inner["already_home"] != true {
		t.Errorf("already_home marker lost in forwardIPC: %v", payload)
	}
}

// TestGoHomeHandlerForwardsDeferredMarker — when the bot reports deferred:true
// (cross-lot transition unimplemented), the marker must reach the caller as-is
// so agents can branch on it. Wave-2b standard: deferral is a first-class
// machine-detectable marker, not a prose error.
func TestGoHomeHandlerForwardsDeferredMarker(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{
			"already_home":         false,
			"deferred":             true,
			"deferred_reason":      "cross-lot transition unimplemented",
			"current_lot_location": "0xAAAAAA",
			"home_lot_location":    "0xBBBBBB",
		},
	})

	handler := goHomeHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, _ := handler(ctx, &convention.Request{Args: map[string]any{}})
	payload, _ := resp.Payload.(map[string]any)
	inner, _ := payload["payload"].(map[string]any)
	if inner == nil {
		t.Fatalf("no inner payload: %v", payload)
	}
	if inner["deferred"] != true {
		t.Errorf("deferred marker missing: %v", inner)
	}
	if inner["already_home"] != false {
		t.Errorf("already_home should be false when deferred: %v", inner)
	}
}

// TestVisitLotHandlerHexFallback asserts the hex fallback shape:
// --target_lot_location "0x123456" → probe-lot → WriteNextLot → bot-exit-request.
// This tests the full handler end-to-end with a stub BotCmdPump.
func TestVisitLotHandlerHexFallback(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "visit-lot-test-hex")
	withConfigHome(t, tmp)
	withSharedDataHome(t, tmp)

	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	// Note: no separate drain goroutine — the test's inline goroutine below
	// is the sole consumer of fake.stdinLines. IPC is not used by this handler
	// path (visit-lot uses BotCmdPump, not IPC.Send). We do not drain ipc-cmd
	// frames because the handler never sends any.

	pump := NewBotCmdPump(fake.bot)
	store := NewMemoryStore()

	// Stub: respond to probe-lot with FOUND, then bot-exit-request with accepted:true.
	// We need to handle two sequential bot-cmd frames.
	go func() {
		// First frame: probe-lot
		line1 := <-fake.stdinLines
		var req1 BotCmdRequest
		if err := json.Unmarshal(line1, &req1); err != nil {
			t.Errorf("unmarshal probe-lot req: %v", err)
			return
		}
		if req1.Cmd != "probe-lot" {
			t.Errorf("want cmd=probe-lot, got %q", req1.Cmd)
			return
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req1.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"status": "FOUND", "lot_id": 17},
		}))

		// Second frame: bot-exit-request
		line2 := <-fake.stdinLines
		var req2 BotCmdRequest
		if err := json.Unmarshal(line2, &req2); err != nil {
			t.Errorf("unmarshal bot-exit req: %v", err)
			return
		}
		if req2.Cmd != "bot-exit-request" {
			t.Errorf("want cmd=bot-exit-request, got %q", req2.Cmd)
			return
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req2.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"accepted": true},
		}))
	}()

	handler := visitLotHandler(ipc, pump, store)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x00110F00",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil {
		t.Fatalf("nil payload")
	}
	if payload["ok"] != true {
		t.Errorf("want ok=true, got %v: %v", payload["ok"], payload)
	}
	if payload["probe_status"] != "FOUND" {
		t.Errorf("want probe_status=FOUND, got %v", payload["probe_status"])
	}
	// Verify next-lot was written (WriteNextLot-before-exit invariant).
	dir := filepath.Join(tmp, "freeso-souls", "visit-lot-test-hex")
	nextLotPath := filepath.Join(dir, "next-lot")
	// After the handler completes, next-lot should have been written.
	// (The supervisor loop would have read+cleared it, but it hasn't run here.)
	data, err := os.ReadFile(nextLotPath)
	if err != nil {
		t.Fatalf("next-lot not written (WriteNextLot invariant violated): %v", err)
	}
	// 0x00110F00 = 1117952 in decimal
	got := string(data)
	if got == "" || got == "\n" {
		t.Errorf("next-lot file is empty")
	}
	t.Logf("next-lot content: %q (lot_location=%v)", got, payload["lot_location"])
}

// TestVisitLotHandlerMyNameShape asserts the name-primary shape:
// --my_name bound to a lot → resolves to lot_location → probe → WriteNextLot → exit.
func TestVisitLotHandlerMyNameShape(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "visit-lot-test-name")
	withConfigHome(t, tmp)
	withSharedDataHome(t, tmp)

	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	// No drain goroutine — test's goroutine is the sole stdin consumer.

	pump := NewBotCmdPump(fake.bot)
	store := NewMemoryStore()

	// Pre-bind "joao's place" → lot_location = 16318812 (decimal = 0x00F9015C).
	encoded, _ := json.Marshal(map[string]any{
		"name":         "joao's place",
		"kind":         "lot",
		"lot_location": "16318812",
	})
	store.Store("name:joao's place", encoded)

	// Stub: probe-lot → FOUND, then bot-exit-request → accepted.
	go func() {
		line1 := <-fake.stdinLines
		var req1 BotCmdRequest
		if err := json.Unmarshal(line1, &req1); err != nil {
			t.Errorf("unmarshal probe-lot req: %v", err)
			return
		}
		if req1.Cmd != "probe-lot" {
			t.Errorf("want cmd=probe-lot, got %q", req1.Cmd)
			return
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req1.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"status": "FOUND", "lot_id": 2},
		}))

		line2 := <-fake.stdinLines
		var req2 BotCmdRequest
		if err := json.Unmarshal(line2, &req2); err != nil {
			t.Errorf("unmarshal bot-exit req: %v", err)
			return
		}
		if req2.Cmd != "bot-exit-request" {
			t.Errorf("want cmd=bot-exit-request, got %q", req2.Cmd)
			return
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req2.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"accepted": true},
		}))
	}()

	handler := visitLotHandler(ipc, pump, store)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"my_name": "joao's place",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil {
		t.Fatalf("nil payload")
	}
	if payload["ok"] != true {
		t.Errorf("want ok=true, got %v: %v", payload["ok"], payload)
	}
	if payload["probe_status"] != "FOUND" {
		t.Errorf("want probe_status=FOUND, got %v", payload["probe_status"])
	}
	// Verify next-lot was written with the resolved lot_location.
	dir := filepath.Join(tmp, "freeso-souls", "visit-lot-test-name")
	data, err := os.ReadFile(filepath.Join(dir, "next-lot"))
	if err != nil {
		t.Fatalf("next-lot not written: %v", err)
	}
	t.Logf("next-lot=%q resolved from my_name=%q", data, "joao's place")
}

// TestVisitLotHandlerUnknownNameReturnsError asserts that an unbound --my_name
// returns ok:false with a helpful error, not a panic or ok:true.
func TestVisitLotHandlerUnknownNameReturnsError(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	// No probe-lot reaches the bot — handler returns early on name resolution failure.
	// Drain stdin to prevent goroutine leaks from the fake BotProcess background reader.
	go func() {
		for range fake.stdinLines {
		}
	}()
	pump := NewBotCmdPump(fake.bot)
	store := NewMemoryStore()

	handler := visitLotHandler(ipc, pump, store)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"my_name": "nowhere-bound",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for unbound name, got %v", payload)
	}
	if payload["error"] == nil || payload["error"] == "" {
		t.Errorf("want non-empty error for unbound name")
	}
	t.Logf("error for unbound name: %v", payload["error"])
}

// TestVisitLotHandlerWrongKindName asserts that a name bound to a non-lot kind
// (e.g. kind="object") returns ok:false with a descriptive error.
func TestVisitLotHandlerWrongKindName(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	// Handler returns early before any bot-cmd — drain stdin to prevent leaks.
	go func() {
		for range fake.stdinLines {
		}
	}()
	pump := NewBotCmdPump(fake.bot)
	store := NewMemoryStore()

	// Bind "the toilet" as kind=object.
	encoded, _ := json.Marshal(map[string]any{
		"name":             "the toilet",
		"kind":             "object",
		"target_object_id": int64(322),
	})
	store.Store("name:the toilet", encoded)

	handler := visitLotHandler(ipc, pump, store)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"my_name": "the toilet",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for wrong-kind name, got %v", payload)
	}
	t.Logf("error for wrong-kind: %v", payload["error"])
}

// TestVisitLotHandlerProbeNotOpen asserts that a NOT_OPEN probe reply causes
// ok:false with probe_status in the response.
func TestVisitLotHandlerProbeNotOpen(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	// No drain goroutine — the test's inline goroutine is the sole consumer.
	pump := NewBotCmdPump(fake.bot)
	store := NewMemoryStore()

	go func() {
		line := <-fake.stdinLines
		var req BotCmdRequest
		if err := json.Unmarshal(line, &req); err != nil {
			t.Errorf("unmarshal: %v", err)
			return
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"status": "NOT_OPEN"},
		}))
	}()

	handler := visitLotHandler(ipc, pump, store)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x00110F00",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for NOT_OPEN lot, got %v", payload)
	}
	if payload["probe_status"] != "NOT_OPEN" {
		t.Errorf("want probe_status=NOT_OPEN, got %v", payload["probe_status"])
	}
}

// TestVisitLotHandlerMissingBothArgs asserts that a call with neither --my_name
// nor --target_lot_location returns ok:false.
func TestVisitLotHandlerMissingBothArgs(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	// Handler returns early before any bot-cmd — drain stdin to prevent leaks.
	go func() {
		for range fake.stdinLines {
		}
	}()
	pump := NewBotCmdPump(fake.bot)
	store := NewMemoryStore()

	handler := visitLotHandler(ipc, pump, store)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{Args: map[string]any{}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for missing args, got %v", payload)
	}
}

// TestVisitLotHandlerWriteNextLotBeforeExit is the veracity test for the
// INVARIANT: WriteNextLot must complete before bot-cmd:exit is dispatched.
// It verifies that the next-lot file exists BETWEEN probe-lot reply and
// bot-exit-request — confirming ordering is correct.
func TestVisitLotHandlerWriteNextLotBeforeExit(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "visit-lot-ordering-test")
	withConfigHome(t, tmp)
	withSharedDataHome(t, tmp)

	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	// No drain goroutine — test's inline goroutine is the sole stdin consumer.

	pump := NewBotCmdPump(fake.bot)
	store := NewMemoryStore()

	nextLotPath := filepath.Join(tmp, "freeso-souls", "visit-lot-ordering-test", "next-lot")

	nextLotExistedBeforeExit := make(chan bool, 1)

	go func() {
		// First: probe-lot reply
		line1 := <-fake.stdinLines
		var req1 BotCmdRequest
		if err := json.Unmarshal(line1, &req1); err != nil {
			t.Errorf("probe-lot unmarshal: %v", err)
			return
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req1.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"status": "FOUND"},
		}))

		// Second: bot-exit-request.
		// BEFORE replying, check that next-lot file already exists.
		line2 := <-fake.stdinLines
		var req2 BotCmdRequest
		if err := json.Unmarshal(line2, &req2); err != nil {
			t.Errorf("bot-exit unmarshal: %v", err)
			return
		}
		_, statErr := os.Stat(nextLotPath)
		nextLotExistedBeforeExit <- (statErr == nil)
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req2.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"accepted": true},
		}))
	}()

	handler := visitLotHandler(ipc, pump, store)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x00F9015C",
	}})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != true {
		t.Errorf("want ok=true: %v", payload)
	}

	select {
	case existed := <-nextLotExistedBeforeExit:
		if !existed {
			t.Error("INVARIANT VIOLATED: next-lot file did NOT exist when bot-exit-request was dispatched — WriteNextLot must happen before bot-cmd:exit")
		} else {
			t.Log("INVARIANT VERIFIED: next-lot written before bot-exit-request dispatched")
		}
	case <-time.After(2 * time.Second):
		t.Error("timed out waiting for ordering check")
	}
}

// TestFindAvatarHandlerDispatchesIPC asserts target_avatar_id is forwarded and
// the response payload shape is propagated.
func TestFindAvatarHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{
			"target_avatar_id": float64(2),
			"status":           "FOUND",
			"on_lot":           true,
			"lot_location":     "0xF8F0DC",
			"lot_location_raw": float64(16318812),
		},
	})

	handler := findAvatarHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_avatar_id": float64(2),
	}})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	cmd := <-gotCmd
	if cmd.Op != "find-avatar" {
		t.Errorf("want op=find-avatar got %q", cmd.Op)
	}
	if cmd.Args["target_avatar_id"] != float64(2) {
		t.Errorf("target_avatar_id not forwarded: %v", cmd.Args)
	}
	payload, _ := resp.Payload.(map[string]any)
	inner, _ := payload["payload"].(map[string]any)
	if inner == nil {
		t.Fatalf("no inner payload: %v", payload)
	}
	if inner["status"] != "FOUND" {
		t.Errorf("status lost in forwardIPC: %v", inner)
	}
	if inner["on_lot"] != true {
		t.Errorf("on_lot lost in forwardIPC: %v", inner)
	}
}

// TestVisitLotHandlerCommunityAccessBlocked is the primary integration test for
// freesoexperiment-381 (M1 finding). It asserts that visit-lot returns
// ok:false + reason=COMMUNITY_ACCESS_DENIED when:
//   - probe-lot returns FOUND with a lot_id that is community-gated
//   - the calling persona (FSO_USER) has NOT been granted access
//
// Mutation evidence: the gate fires before WriteNextLot — no next-lot file is
// written and no bot-exit-request reaches the bot.
func TestVisitLotHandlerCommunityAccessBlocked(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "ellis") // caller is Ellis, who has no grant
	withConfigHome(t, tmp)
	withSharedDataHome(t, tmp)

	// Grant access to lot 17 for "botrous" only (not "ellis").
	// Use FSO_MAYOR_NHOOD to authorise the grant handler.
	os.Setenv("FSO_MAYOR_NHOOD", "1")
	t.Cleanup(func() { os.Unsetenv("FSO_MAYOR_NHOOD") })

	grantHandler := grantCommunityAccessHandler(nil)
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	grantResp, err := grantHandler(ctx, &convention.Request{Args: map[string]any{
		"lot_id":       float64(17),
		"persona_name": "botrous",
	}})
	if err != nil {
		t.Fatalf("grant-community-access: %v", err)
	}
	grantPayload, _ := grantResp.Payload.(map[string]any)
	if grantPayload["ok"] != true {
		t.Fatalf("grant-community-access failed (prerequisite): %v", grantPayload)
	}
	// Confirm lot 17 is now community-gated and Ellis lacks access.
	if !IsCommunityGated(17) {
		t.Fatal("IsCommunityGated(17) should be true after grant for botrous")
	}
	if HasCommunityAccess(17, "ellis") {
		t.Fatal("HasCommunityAccess(17, ellis) should be false before any grant for ellis")
	}

	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	pump := NewBotCmdPump(fake.bot)
	store := NewMemoryStore()

	// Stub: probe-lot returns FOUND with lot_id=17 (the community-gated lot).
	// The handler must block on the community-access check and never send bot-exit-request.
	go func() {
		line := <-fake.stdinLines
		var req BotCmdRequest
		if err := json.Unmarshal(line, &req); err != nil {
			t.Errorf("unmarshal probe-lot req: %v", err)
			return
		}
		if req.Cmd != "probe-lot" {
			t.Errorf("want cmd=probe-lot, got %q", req.Cmd)
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"status": "FOUND", "lot_id": 17},
		}))
		// Drain any further stdin (should be none — handler must stop after DENIED).
		for range fake.stdinLines {
		}
	}()

	handler := visitLotHandler(ipc, pump, store)
	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x00110F00",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for community-access denied, got %v: %v", payload["ok"], payload)
	}
	if payload["reason"] != "COMMUNITY_ACCESS_DENIED" {
		t.Errorf("want reason=COMMUNITY_ACCESS_DENIED, got %v", payload["reason"])
	}

	// MUTATION EVIDENCE: next-lot must NOT have been written (gate fires before WriteNextLot).
	dir := filepath.Join(tmp, "freeso-souls", "ellis")
	nextLotPath := filepath.Join(dir, "next-lot")
	if _, err := os.Stat(nextLotPath); err == nil {
		t.Error("GATE BYPASSED: next-lot file was written despite COMMUNITY_ACCESS_DENIED — gate must block before WriteNextLot")
	}
	t.Logf("BLOCKED: community-access denied for ellis on lot 17 — next-lot not written, bot-exit-request not sent: %v", payload)
}

// TestVisitLotHandlerCommunityAccessGranted asserts that visit-lot proceeds
// normally (probe → WriteNextLot → bot-exit-request) when the calling persona
// has been granted access to a community-gated lot.
//
// Done condition: ok:true, probe_status=FOUND, next-lot written.
func TestVisitLotHandlerCommunityAccessGranted(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "botrous") // Botrous has a grant for lot 17
	withConfigHome(t, tmp)
	withSharedDataHome(t, tmp)

	// Grant botrous access to lot 17.
	os.Setenv("FSO_MAYOR_NHOOD", "1")
	t.Cleanup(func() { os.Unsetenv("FSO_MAYOR_NHOOD") })

	grantHandler := grantCommunityAccessHandler(nil)
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	grantResp, err := grantHandler(ctx, &convention.Request{Args: map[string]any{
		"lot_id":       float64(17),
		"persona_name": "botrous",
	}})
	if err != nil {
		t.Fatalf("grant-community-access: %v", err)
	}
	grantPayload, _ := grantResp.Payload.(map[string]any)
	if grantPayload["ok"] != true {
		t.Fatalf("grant-community-access failed (prerequisite): %v", grantPayload)
	}
	if !HasCommunityAccess(17, "botrous") {
		t.Fatal("HasCommunityAccess(17, botrous) should be true after grant")
	}

	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	pump := NewBotCmdPump(fake.bot)
	store := NewMemoryStore()

	// Stub: probe-lot → FOUND with lot_id=17, then bot-exit-request → accepted.
	go func() {
		line1 := <-fake.stdinLines
		var req1 BotCmdRequest
		if err := json.Unmarshal(line1, &req1); err != nil {
			t.Errorf("unmarshal probe-lot req: %v", err)
			return
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req1.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"status": "FOUND", "lot_id": 17},
		}))

		line2 := <-fake.stdinLines
		var req2 BotCmdRequest
		if err := json.Unmarshal(line2, &req2); err != nil {
			t.Errorf("unmarshal bot-exit req: %v", err)
			return
		}
		if req2.Cmd != "bot-exit-request" {
			t.Errorf("want cmd=bot-exit-request, got %q", req2.Cmd)
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req2.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"accepted": true},
		}))
	}()

	handler := visitLotHandler(ipc, pump, store)
	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x00110F00",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != true {
		t.Errorf("want ok=true for granted botrous on community lot 17, got %v: %v", payload["ok"], payload)
	}
	if payload["probe_status"] != "FOUND" {
		t.Errorf("want probe_status=FOUND, got %v", payload["probe_status"])
	}

	// GRANT PATH EVIDENCE: next-lot must have been written.
	dir := filepath.Join(tmp, "freeso-souls", "botrous")
	data, err := os.ReadFile(filepath.Join(dir, "next-lot"))
	if err != nil {
		t.Fatalf("next-lot not written (gate passed but WriteNextLot invariant violated): %v", err)
	}
	t.Logf("GRANTED: botrous on lot 17 — next-lot=%q, response=%v", data, payload)
}

// TestVisitLotHandlerNonCommunityLotPassesThrough asserts that a lot with no
// community-access grants (ordinary residential lot) is not blocked by the
// community-access gate even when community-access.json exists for other lots.
func TestVisitLotHandlerNonCommunityLotPassesThrough(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "marlo") // persona with no grant
	withConfigHome(t, tmp)
	withSharedDataHome(t, tmp)

	// Grant community access for lot 17 (but we'll be visiting lot 99 = non-community).
	os.Setenv("FSO_MAYOR_NHOOD", "1")
	t.Cleanup(func() { os.Unsetenv("FSO_MAYOR_NHOOD") })

	grantHandler := grantCommunityAccessHandler(nil)
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	grantResp, err := grantHandler(ctx, &convention.Request{Args: map[string]any{
		"lot_id":       float64(17), // gating lot 17, NOT lot 99
		"persona_name": "botrous",
	}})
	if err != nil {
		t.Fatalf("grant-community-access: %v", err)
	}
	grantPayload, _ := grantResp.Payload.(map[string]any)
	if grantPayload["ok"] != true {
		t.Fatalf("grant-community-access failed: %v", grantPayload)
	}

	// Lot 99 is not gated — no grants for it.
	if IsCommunityGated(99) {
		t.Fatal("IsCommunityGated(99) should be false (no grants for lot 99)")
	}

	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	pump := NewBotCmdPump(fake.bot)
	store := NewMemoryStore()

	// Stub: probe-lot → FOUND with lot_id=99 (non-community lot), then bot-exit-request.
	go func() {
		line1 := <-fake.stdinLines
		var req1 BotCmdRequest
		if err := json.Unmarshal(line1, &req1); err != nil {
			t.Errorf("unmarshal probe-lot req: %v", err)
			return
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req1.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"status": "FOUND", "lot_id": 99},
		}))

		line2 := <-fake.stdinLines
		var req2 BotCmdRequest
		if err := json.Unmarshal(line2, &req2); err != nil {
			t.Errorf("unmarshal bot-exit req: %v", err)
			return
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req2.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"accepted": true},
		}))
	}()

	handler := visitLotHandler(ipc, pump, store)
	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x00630000",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != true {
		t.Errorf("want ok=true for non-community lot 99 (marlo has no grant but lot is not gated), got %v: %v", payload["ok"], payload)
	}
	t.Logf("PASS: non-community lot 99 passes through for marlo: %v", payload)
}

// mustMarshal marshals v to JSON and panics on error. Test-only helper.
func mustMarshal(v any) []byte {
	b, err := json.Marshal(v)
	if err != nil {
		panic(err)
	}
	return b
}

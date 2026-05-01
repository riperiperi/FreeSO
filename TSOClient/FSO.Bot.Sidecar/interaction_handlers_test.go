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
	"testing"
	"time"

	"github.com/campfire-net/campfire/pkg/convention"
)

// TestInteractWithHandlerDispatchesIPC asserts that an interact-with convention
// invocation produces an IPC command with op="interact-with" carrying the
// declared args. The bot-side dispatcher (InteractionHandlers.InteractWith)
// then turns that into a VMNetInteractionCmd — the golden-byte test in
// MovementCommandEncodingTests.Interaction_EncodesExpectedBytes pins the PDU.
func TestInteractWithHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{
			"queued": true, "interaction": 3, "callee_id": 17, "param0": 0, "global": false,
		},
	})

	// botCmds nil: bulletin_board path not exercised in this test.
	handler := interactWithHandler(ipc, nil)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"interaction": float64(3),
			"callee_id":   float64(17),
			"param0":      float64(0),
			"global":      false,
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	if resp == nil {
		t.Fatal("nil response")
	}
	cmd := <-gotCmd
	if cmd.Op != "interact-with" {
		t.Errorf("want op=interact-with got %q", cmd.Op)
	}
	if cmd.Args["interaction"] != float64(3) || cmd.Args["callee_id"] != float64(17) {
		t.Errorf("args not forwarded: %v", cmd.Args)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil || payload["ok"] != true {
		t.Errorf("response payload missing ok=true: %v", resp.Payload)
	}
}

// TestCancelInteractionHandlerDispatchesIPC asserts the scoped cancel forwards
// action_uid to the bot. Validation of required action_uid lives bot-side
// (tested via integration test) — the Go handler is a pure forward.
func TestCancelInteractionHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{"cancelled": 1, "action_uid": 42},
	})

	handler := cancelInteractionHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	_, err := handler(ctx, &convention.Request{Args: map[string]any{"action_uid": float64(42)}})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	cmd := <-gotCmd
	if cmd.Op != "cancel-interaction" {
		t.Errorf("want op=cancel-interaction got %q", cmd.Op)
	}
	if cmd.Args["action_uid"] != float64(42) {
		t.Errorf("action_uid not forwarded: %v", cmd.Args)
	}
}

// TestQueryPieMenuHandlerDispatchesIPC asserts target_object_id + flags are
// forwarded. query-pie-menu is the one interaction-family op with no outbound
// wire PDU — it runs entirely in the bot's tick-locked VM — so the Go side
// only needs to prove the forward-and-await contract.
func TestQueryPieMenuHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{
			"target_object_id": 17,
			"interactions":     []any{map[string]any{"id": 3, "name": "Sit", "param0": 0, "global": false, "score": 0.0}},
		},
	})

	handler := queryPieMenuHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"target_object_id": float64(17),
			"include_hidden":   false,
			"include_global":   true,
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	cmd := <-gotCmd
	if cmd.Op != "query-pie-menu" {
		t.Errorf("want op=query-pie-menu got %q", cmd.Op)
	}
	if cmd.Args["target_object_id"] != float64(17) {
		t.Errorf("target_object_id not forwarded: %v", cmd.Args)
	}
	if cmd.Args["include_global"] != true {
		t.Errorf("include_global not forwarded: %v", cmd.Args)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil || payload["ok"] != true {
		t.Fatalf("response missing ok=true: %v", resp.Payload)
	}
	// Assert the bot's query payload survives the forward.
	inner, _ := payload["payload"].(map[string]any)
	if inner == nil {
		t.Fatalf("inner payload missing: %v", payload)
	}
	if _, ok := inner["interactions"]; !ok {
		t.Errorf("interactions missing in forwarded payload: %v", inner)
	}
}

// ---- bulletin_board path (freesoexperiment-2ac) ----

// TestBulletinBoardAffordanceCheck_NoEnv asserts that hasBulletinBoardAffordance
// returns false when FREESO_COMMUNITY_LOT_ID is unset.
func TestBulletinBoardAffordanceCheck_NoEnv(t *testing.T) {
	t.Setenv("FREESO_COMMUNITY_LOT_ID", "")
	if hasBulletinBoardAffordance() {
		t.Error("want false when FREESO_COMMUNITY_LOT_ID is empty")
	}
}

// TestBulletinBoardAffordanceCheck_ZeroID asserts false for lot_id=0.
func TestBulletinBoardAffordanceCheck_ZeroID(t *testing.T) {
	t.Setenv("FREESO_COMMUNITY_LOT_ID", "0")
	if hasBulletinBoardAffordance() {
		t.Error("want false when FREESO_COMMUNITY_LOT_ID=0")
	}
}

// TestBulletinBoardAffordanceCheck_Set asserts true for a valid non-zero lot_id.
func TestBulletinBoardAffordanceCheck_Set(t *testing.T) {
	t.Setenv("FREESO_COMMUNITY_LOT_ID", "2")
	if !hasBulletinBoardAffordance() {
		t.Error("want true when FREESO_COMMUNITY_LOT_ID=2")
	}
}

// TestInteractWithBulletinBoard_NoAffordance asserts that interact-with with
// object_type=bulletin_board returns ok:false reason=NO_AFFORDANCE when
// FREESO_COMMUNITY_LOT_ID is not set. This is the residential-lot path: the
// sidecar mirrors the perception augmentor's community-lot gate.
func TestInteractWithBulletinBoard_NoAffordance(t *testing.T) {
	// Ensure env var is not set for this test.
	t.Setenv("FREESO_COMMUNITY_LOT_ID", "")

	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	// Drain stdin so no goroutine leaks — handler returns before any IPC.
	go func() {
		for range fake.stdinLines {
		}
	}()
	pump := NewBotCmdPump(fake.bot)

	handler := interactWithHandler(ipc, pump)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"object_type": "bulletin_board",
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	if resp == nil {
		t.Fatal("nil response")
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for NO_AFFORDANCE, got %v", payload)
	}
	if payload["reason"] != "NO_AFFORDANCE" {
		t.Errorf("want reason=NO_AFFORDANCE, got %v", payload["reason"])
	}
}

// TestInteractWithBulletinBoard_Success exercises the community-lot path:
// FREESO_COMMUNITY_LOT_ID is set, stub bot returns probe-bulletin with messages,
// handler returns ok:true bulletin_listing with count and messages.
//
// This is the feature integration test for freesoexperiment-2ac done condition:
// "persona on community lot issues interact-with --object_type bulletin_board;
//
//	perception emits one-shot bulletin_listing with current bulletin posts."
func TestInteractWithBulletinBoard_Success(t *testing.T) {
	// Set FREESO_COMMUNITY_LOT_ID to enable the community-lot affordance gate.
	t.Setenv("FREESO_COMMUNITY_LOT_ID", "2")

	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	// Stub bot: reads probe-bulletin bot-cmd from stdin, returns fake bulletin messages.
	fakeMsgs := []map[string]any{
		{"subject": "Welcome to Alphaville!", "body": "Check the bulletin board.", "sender": "admin", "timestamp": 1714000000},
		{"subject": "Pizza night Friday", "body": "All welcome.", "sender": "botrous", "timestamp": 1714001000},
	}
	fakeMsgsJSON, _ := json.Marshal(fakeMsgs)

	go func() {
		line := <-fake.stdinLines
		var req BotCmdRequest
		if err := json.Unmarshal(line, &req); err != nil {
			t.Errorf("unmarshal bot-cmd req: %v", err)
			return
		}
		if req.Kind != "bot-cmd" {
			t.Errorf("want kind=bot-cmd, got %s", req.Kind)
		}
		if req.Cmd != "probe-bulletin" {
			t.Errorf("want cmd=probe-bulletin, got %s", req.Cmd)
		}
		// Verify neighborhood_id was forwarded.
		nhoodID, _ := req.Args["neighborhood_id"].(float64)
		if nhoodID != 1 {
			t.Errorf("want neighborhood_id=1, got %v", req.Args["neighborhood_id"])
		}
		reply := map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             true,
			"data": map[string]any{
				"messages": json.RawMessage(fakeMsgsJSON),
				"count":    2,
			},
		}
		data, _ := json.Marshal(reply)
		pump.Deliver(data)
	}()

	// ipc is nil here — bulletin_board path does not use IPC.
	handler := interactWithHandler(nil, pump)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"object_type": "bulletin_board",
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	if resp == nil {
		t.Fatal("nil response")
	}

	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != true {
		t.Errorf("want ok=true on community lot, got %v", payload)
	}
	if payload["kind"] != "bulletin_listing" {
		t.Errorf("want kind=bulletin_listing, got %v", payload["kind"])
	}
	if payload["count"] == nil {
		t.Error("want count in payload")
	}
	// messages must be present (slice).
	if _, ok := payload["messages"]; !ok {
		t.Error("want messages key in payload")
	}
	t.Logf("bulletin_listing payload: ok=%v kind=%v count=%v", payload["ok"], payload["kind"], payload["count"])
}

// TestInteractWithBulletinBoard_BotCmdRefused asserts that when the bot returns
// ok=false on probe-bulletin, the handler surfaces it as ok:false to the caller.
func TestInteractWithBulletinBoard_BotCmdRefused(t *testing.T) {
	t.Setenv("FREESO_COMMUNITY_LOT_ID", "2")

	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	go func() {
		line := <-fake.stdinLines
		var req BotCmdRequest
		if err := json.Unmarshal(line, &req); err != nil {
			t.Errorf("unmarshal: %v", err)
			return
		}
		reply := map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             false,
			"error":          "NHOOD_NOT_FOUND",
		}
		data, _ := json.Marshal(reply)
		pump.Deliver(data)
	}()

	handler := interactWithHandler(nil, pump)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{"object_type": "bulletin_board"},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false on bot refusal, got %v", payload)
	}
	errStr, _ := payload["error"].(string)
	if errStr == "" {
		t.Error("want error string in payload")
	}
	t.Logf("refused payload: %v", payload)
}

// TestInteractWithBulletinBoard_NoBotCmds asserts that when botCmds is nil
// and affordance passes, the handler returns ok:false with deferred=true.
func TestInteractWithBulletinBoard_NoBotCmds(t *testing.T) {
	t.Setenv("FREESO_COMMUNITY_LOT_ID", "2")

	// ipc nil, botCmds nil — --no-bot mode.
	handler := interactWithHandler(nil, nil)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{"object_type": "bulletin_board"},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false when botCmds nil, got %v", payload)
	}
	if payload["deferred"] != true {
		t.Errorf("want deferred=true when botCmds nil, got %v", payload["deferred"])
	}
}

// TestInteractWithObjectTypeNotBulletinBoard asserts that a non-bulletin_board
// object_type still routes to the IPC path, not the bulletin_board branch.
func TestInteractWithObjectTypeNotBulletinBoard(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{"queued": true},
	})

	handler := interactWithHandler(ipc, nil)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	_, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"object_type": "refrigerator", // not bulletin_board
			"interaction": float64(1),
			"callee_id":   float64(5),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	cmd := <-gotCmd
	// Should have gone to IPC with op=interact-with, NOT bulletin_board path.
	if cmd.Op != "interact-with" {
		t.Errorf("want op=interact-with for non-bulletin_board object_type, got %q", cmd.Op)
	}
	// object_type must NOT be forwarded to the VM (not in whitelist).
	if _, bad := cmd.Args["object_type"]; bad {
		t.Error("object_type must NOT be forwarded to the bot's IPC command")
	}
}

// TestBulletinBoardDeclarationHasObjectTypeArg verifies the interact-with declaration
// carries the object_type arg (freesoexperiment-2ac deliverable).
func TestBulletinBoardDeclarationHasObjectTypeArg(t *testing.T) {
	// Ensure env is clean.
	os.Unsetenv("FREESO_COMMUNITY_LOT_ID")

	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}
	var found *convention.Declaration
	for _, d := range decls {
		if d.Operation == "interact-with" {
			found = d
			break
		}
	}
	if found == nil {
		t.Fatal("interact-with declaration not found")
	}
	hasObjectType := false
	for _, a := range found.Args {
		if a.Name == "object_type" {
			hasObjectType = true
			break
		}
	}
	if !hasObjectType {
		t.Error("interact-with declaration missing object_type arg (freesoexperiment-2ac)")
	}
}

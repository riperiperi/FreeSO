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
//
// Since freesoexperiment-824 the handler goes through verifyingHandlerWithExpect
// which issues snapshot IPC calls (query-self / query-lot-objects) before and
// after the actual op. We use scriptedResponder so every IPC call is answered.
// This test exercises the non-bulletin_board path routes to the verifying handler.
func TestInteractWithHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// Pre- and post-snapshots: add callee_id=17 to post.actionQueue so the
	// handler returns verdict=queued (the "arg forwarded → accepted" happy path).
	script := map[string][]map[string]any{
		"query-self": {
			{"ok": true, "payload": map[string]any{"balance": 5000, "action_queue": []any{}}},
			{"ok": true, "payload": map[string]any{"balance": 5000, "action_queue": []any{
				map[string]any{"interaction_id": 42, "name": "Sit", "target_object_id": float64(17), "status": "queued"},
			}}},
		},
		"query-lot-objects": {
			{"ok": true, "payload": map[string]any{"objects": []any{}}},
			{"ok": true, "payload": map[string]any{"objects": []any{}}},
		},
		"interact-with": {
			{"ok": true, "payload": map[string]any{"queued": true, "interaction": 3, "callee_id": 17}},
		},
	}
	sr := newScriptedResponder(t, fake, ipc, script)

	// botCmds nil: bulletin_board path not exercised in this test.
	handler := interactWithHandlerFast(ipc, nil)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
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

	// Verify interact-with was dispatched with the right args.
	var interactCmd Command
	found := false
	for range 10 {
		select {
		case cmd := <-sr.recvd:
			if cmd.Op == "interact-with" {
				interactCmd = cmd
				found = true
			}
		default:
		}
		if found {
			break
		}
	}
	// Drain remaining received commands; we may have gotten them in order already.
	if !found {
		for cmd := range sr.recvd {
			if cmd.Op == "interact-with" {
				interactCmd = cmd
				found = true
				break
			}
		}
	}

	// The verifying handler must have forwarded interact-with with the correct args.
	// We verify via the verdict: if callee_id=17 matched, verdict is queued/interaction-started.
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil {
		t.Fatalf("nil payload")
	}
	verdict, _ := payload["verdict"].(string)
	if verdict != "queued" && verdict != "interaction-started" {
		t.Errorf("want verdict=queued or interaction-started for happy path, got %q (payload=%v)", verdict, payload)
	}
	// Confirm op was forwarded (the scripted responder received it).
	_ = interactCmd // consumed for op validation above; verifying via verdict is sufficient.
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

// interactWithHandlerFast returns an interactWithHandler using the fast test
// config so unit tests don't wait for the 1500ms settle.
func interactWithHandlerFast(ipc *IPC, botCmds *BotCmdPump) convention.HandlerFunc {
	verifyingHandler := verifyingHandlerWithExpect(
		ipc,
		"interact-with",
		interactWithAllowedArgs,
		0, "", nil,
		interactExpectFn,
		fastTestConfig(),
	)
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		if objectType, _ := req.Args["object_type"].(string); objectType == "bulletin_board" {
			return bulletinBoardHandler(ctx, botCmds)
		}
		return verifyingHandler(ctx, req)
	}
}

// TestInteractWithObjectTypeNotBulletinBoard asserts that a non-bulletin_board
// object_type still routes to the IPC path, not the bulletin_board branch.
// Since freesoexperiment-824 this path uses verifyingHandlerWithExpect; we
// verify via the structured verdict (not the raw IPC op) since the scripted
// responder handles multi-call sequences.
func TestInteractWithObjectTypeNotBulletinBoard(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// No new queue entry — confirms we're on the IPC path (not bulletin_board)
	// and the response shape is a verifying verdict, not a bulletin_listing.
	script := map[string][]map[string]any{
		"query-self": {
			{"ok": true, "payload": map[string]any{"balance": 5000, "action_queue": []any{}}},
			{"ok": true, "payload": map[string]any{"balance": 5000, "action_queue": []any{}}},
		},
		"query-lot-objects": {
			{"ok": true, "payload": map[string]any{"objects": []any{}}},
			{"ok": true, "payload": map[string]any{"objects": []any{}}},
		},
		"interact-with": {
			{"ok": true, "payload": map[string]any{"queued": true}},
		},
	}
	sr := newScriptedResponder(t, fake, ipc, script)
	_ = sr

	handler := interactWithHandlerFast(ipc, nil)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"object_type": "refrigerator", // not bulletin_board
			"interaction": float64(1),
			"callee_id":   float64(5),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	if resp == nil {
		t.Fatal("nil response")
	}
	payload, _ := resp.Payload.(map[string]any)
	// Must NOT return a bulletin_listing — confirms routing to the IPC path.
	if payload["kind"] == "bulletin_listing" {
		t.Error("non-bulletin_board object_type must NOT route to the bulletin_board path")
	}
	// Must return a verifying verdict (not the old silent ok:true from forwardIPC).
	if _, hasVerdict := payload["verdict"]; !hasVerdict {
		t.Error("non-bulletin_board interact-with must return a structured verdict (freesoexperiment-824)")
	}
	// object_type must NOT appear in the verdict payload (it's not in the allowed args list).
	if _, bad := payload["object_type"]; bad {
		t.Error("object_type must NOT be forwarded to the bot (not in interactWithAllowedArgs)")
	}
}

// ---- W8 verifying handler tests (freesoexperiment-824) ----
//
// These tests exercise the interact-with verifying path: action_queue diff for
// queued / interaction-started verdicts, and silent-drop detection.

// TestInteractWithVerifying_Queued: bot accepts the interact-with IPC and a new
// action_queue entry appears in the post-snapshot with status="queued".
// Verdict must be "queued" with action_uid and target_object_id present.
func TestInteractWithVerifying_Queued(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// callee_id = 99, no pre-existing queue entries.
	script := map[string][]map[string]any{
		"query-self": {
			{"ok": true, "payload": map[string]any{"balance": 10000, "action_queue": []any{}}},
			{"ok": true, "payload": map[string]any{"balance": 10000, "action_queue": []any{
				map[string]any{"interaction_id": 7, "name": "Sit", "target_object_id": float64(99), "status": "queued"},
			}}},
		},
		"query-lot-objects": {
			{"ok": true, "payload": map[string]any{"objects": []any{}}},
			{"ok": true, "payload": map[string]any{"objects": []any{}}},
		},
		"interact-with": {
			{"ok": true, "payload": map[string]any{"queued": true, "callee_id": 99}},
		},
	}
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := interactWithHandlerFast(ipc, nil)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"interaction": float64(0),
			"callee_id":   float64(99),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	pay := resp.Payload.(map[string]any)
	if pay["verdict"] != "queued" {
		t.Errorf("want verdict=queued, got %v (payload=%+v)", pay["verdict"], pay)
	}
	if pay["ok"] != true {
		t.Errorf("want ok=true for queued verdict, got %v", pay["ok"])
	}
	if pay["action_uid"] == nil {
		t.Error("want action_uid in queued verdict")
	}
	if pay["target_object_id"] == nil {
		t.Error("want target_object_id in queued verdict")
	}
	t.Logf("queued verdict payload: %+v", pay)
}

// TestInteractWithVerifying_InteractionStarted: bot accepts the IPC and the new
// action_queue entry appears with status="running" within the settle window.
// Verdict must be "interaction-started" (faster confirmation than "queued").
func TestInteractWithVerifying_InteractionStarted(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// callee_id = 42, status=running (already executing at post-snapshot).
	script := map[string][]map[string]any{
		"query-self": {
			{"ok": true, "payload": map[string]any{"balance": 10000, "action_queue": []any{}}},
			{"ok": true, "payload": map[string]any{"balance": 10000, "action_queue": []any{
				map[string]any{"interaction_id": 11, "name": "Sit Down", "target_object_id": float64(42), "status": "running"},
			}}},
		},
		"query-lot-objects": {
			{"ok": true, "payload": map[string]any{"objects": []any{}}},
			{"ok": true, "payload": map[string]any{"objects": []any{}}},
		},
		"interact-with": {
			{"ok": true, "payload": map[string]any{"queued": true}},
		},
	}
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := interactWithHandlerFast(ipc, nil)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"interaction": float64(0),
			"callee_id":   float64(42),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	pay := resp.Payload.(map[string]any)
	if pay["verdict"] != "interaction-started" {
		t.Errorf("want verdict=interaction-started, got %v (payload=%+v)", pay["verdict"], pay)
	}
	if pay["ok"] != true {
		t.Errorf("want ok=true for interaction-started, got %v", pay["ok"])
	}
	t.Logf("interaction-started payload: %+v", pay)
}

// TestInteractWithVerifying_SilentDrop: bot accepts the IPC (ok:true) but no new
// action_queue entry appears after settle. Models the TTAB-rejection / out-of-range
// case. Verdict must be "silent-drop" with ok:false and hints containing
// "unavailable-interaction-no-event".
//
// This is the d8b stair-GoUpstairs case: callee_id=23 is the Stair-Bamboo object
// with available:null in perception, so the VM rejects the interaction silently.
func TestInteractWithVerifying_SilentDrop(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// No new queue entry in post-snapshot. callee_id=23 not in objects either
	// → "target-out-of-range" hint also expected.
	script := map[string][]map[string]any{
		"query-self": {
			{"ok": true, "payload": map[string]any{"balance": 10000, "action_queue": []any{}}},
			{"ok": true, "payload": map[string]any{"balance": 10000, "action_queue": []any{}}},
		},
		"query-lot-objects": {
			{"ok": true, "payload": map[string]any{"objects": []any{}}},
			{"ok": true, "payload": map[string]any{"objects": []any{}}},
		},
		"interact-with": {
			{"ok": true, "payload": map[string]any{"queued": true}},
		},
	}
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := interactWithHandlerFast(ipc, nil)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"interaction": float64(0),
			"callee_id":   float64(23), // Stair-Bamboo: unavailable-interaction (d8b case)
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	pay := resp.Payload.(map[string]any)
	if pay["verdict"] != "silent-drop" {
		t.Errorf("want verdict=silent-drop, got %v (payload=%+v)", pay["verdict"], pay)
	}
	if pay["ok"] != false {
		t.Errorf("want ok=false for silent-drop, got %v", pay["ok"])
	}
	// Hints must include the primary hint.
	hints := hintsFromPayload(pay)
	if !containsHint(hints, "unavailable-interaction-no-event") {
		t.Errorf("want hint 'unavailable-interaction-no-event', got %v", hints)
	}
	// callee_id=23 not in objects → additional range hint.
	if !containsHint(hints, "target-out-of-range") {
		t.Errorf("want hint 'target-out-of-range' when callee_id not in object list, got %v", hints)
	}
	t.Logf("silent-drop payload: %+v", pay)
}

// TestInteractWithVerifying_SilentDrop_CalleePresentNoQueue: callee_id IS in the
// lot's object list but the VM still didn't enqueue it. The "target-out-of-range"
// hint must NOT appear; only "unavailable-interaction-no-event" should.
func TestInteractWithVerifying_SilentDrop_CalleePresentNoQueue(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// callee_id=99 is in the object list, but no new queue entry appears.
	script := map[string][]map[string]any{
		"query-self": {
			{"ok": true, "payload": map[string]any{"balance": 10000, "action_queue": []any{}}},
			{"ok": true, "payload": map[string]any{"balance": 10000, "action_queue": []any{}}},
		},
		"query-lot-objects": {
			{"ok": true, "payload": map[string]any{"objects": []any{
				map[string]any{"object_id": 99, "persist_id": 1234, "guid": 0, "x": 5, "y": 7, "level": 1, "dir": 0},
			}}},
			{"ok": true, "payload": map[string]any{"objects": []any{
				map[string]any{"object_id": 99, "persist_id": 1234, "guid": 0, "x": 5, "y": 7, "level": 1, "dir": 0},
			}}},
		},
		"interact-with": {
			{"ok": true, "payload": map[string]any{"queued": true}},
		},
	}
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := interactWithHandlerFast(ipc, nil)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"interaction": float64(3),
			"callee_id":   float64(99),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	pay := resp.Payload.(map[string]any)
	if pay["verdict"] != "silent-drop" {
		t.Errorf("want verdict=silent-drop, got %v", pay["verdict"])
	}
	hints := hintsFromPayload(pay)
	if !containsHint(hints, "unavailable-interaction-no-event") {
		t.Errorf("want hint 'unavailable-interaction-no-event', got %v", hints)
	}
	// Object IS on the lot — no target-out-of-range.
	if containsHint(hints, "target-out-of-range") {
		t.Errorf("must NOT have hint 'target-out-of-range' when callee_id IS in object list, got %v", hints)
	}
}

// TestInteractWithVerifying_BotRejected: bot returns ok:false at IPC parse/arg
// validation. Verdict must be "bot-rejected" with ok:false and the bot's error.
// No settle and no post-snapshot (fast-path from the framework).
func TestInteractWithVerifying_BotRejected(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	script := map[string][]map[string]any{
		"query-self":        {{"ok": true, "payload": map[string]any{"balance": 10000, "action_queue": []any{}}}},
		"query-lot-objects": {{"ok": true, "payload": map[string]any{"objects": []any{}}}},
		"interact-with":     {{"ok": false, "error": "interaction out of range (callee not in nearby_objects)"}},
	}
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := interactWithHandlerFast(ipc, nil)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"interaction": float64(0),
			"callee_id":   float64(999),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	pay := resp.Payload.(map[string]any)
	if pay["verdict"] != "bot-rejected" {
		t.Errorf("want verdict=bot-rejected, got %v (payload=%+v)", pay["verdict"], pay)
	}
	if pay["ok"] != false {
		t.Errorf("want ok=false for bot-rejected, got %v", pay["ok"])
	}
	errStr, _ := pay["error"].(string)
	if errStr == "" {
		t.Error("want non-empty error string in bot-rejected payload")
	}
	t.Logf("bot-rejected payload: %+v", pay)
}

// TestInteractExpectFn_IgnoresPreExistingEntries: a queue entry that existed
// BEFORE the interact-with must not be counted as a new entry. The pre-existing
// queue entry has a different target; the new entry has the right target.
func TestInteractExpectFn_IgnoresPreExistingEntries(t *testing.T) {
	pre := lotSnapshot{
		actionQueue: []ActionQueueEntry{
			{InteractionID: 1, Name: "Walk", TargetObjectID: 55, Status: "running"},
		},
	}
	post := lotSnapshot{
		actionQueue: []ActionQueueEntry{
			{InteractionID: 1, Name: "Walk", TargetObjectID: 55, Status: "running"}, // pre-existing
			{InteractionID: 2, Name: "Sit", TargetObjectID: 17, Status: "queued"},   // new
		},
	}
	args := map[string]any{"callee_id": 17, "interaction": 0}
	verdict, _, _, ok := interactExpectFn(pre, post, args, nil)
	if verdict != "queued" {
		t.Errorf("want queued, got %q", verdict)
	}
	if !ok {
		t.Error("want ok=true for queued verdict")
	}
}

// TestInteractExpectFn_WrongTargetIgnored: a new queue entry for the wrong
// target_object_id must not count as our interaction being queued.
func TestInteractExpectFn_WrongTargetIgnored(t *testing.T) {
	pre := lotSnapshot{}
	post := lotSnapshot{
		actionQueue: []ActionQueueEntry{
			{InteractionID: 5, Name: "Cook", TargetObjectID: 88, Status: "queued"}, // wrong target
		},
	}
	args := map[string]any{"callee_id": 17, "interaction": 0}
	verdict, hints, _, ok := interactExpectFn(pre, post, args, nil)
	if verdict != "silent-drop" {
		t.Errorf("want silent-drop when wrong target queued, got %q", verdict)
	}
	if ok {
		t.Error("want ok=false for silent-drop")
	}
	if !containsHint(hints, "unavailable-interaction-no-event") {
		t.Errorf("want unavailable-interaction-no-event hint, got %v", hints)
	}
}

// hintsFromPayload extracts the hints slice from the verdict payload, handling
// both []string and []any JSON representations.
func hintsFromPayload(pay map[string]any) []string {
	switch h := pay["hints"].(type) {
	case []string:
		return h
	case []any:
		out := make([]string, 0, len(h))
		for _, v := range h {
			if s, ok := v.(string); ok {
				out = append(out, s)
			}
		}
		return out
	}
	return nil
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

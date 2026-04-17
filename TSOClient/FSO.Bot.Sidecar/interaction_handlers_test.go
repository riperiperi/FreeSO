/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
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

	handler := interactWithHandler(ipc)
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

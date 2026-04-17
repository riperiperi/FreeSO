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

// TestVisitLotHandlerDispatchesIPC asserts target_lot_location is forwarded.
func TestVisitLotHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{
			"deferred":            true,
			"deferred_reason":     "unimplemented",
			"target_lot_location": "0x123456",
			"bot_state_unchanged": true,
		},
	})

	handler := visitLotHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	_, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x123456",
	}})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	cmd := <-gotCmd
	if cmd.Op != "visit-lot" {
		t.Errorf("want op=visit-lot got %q", cmd.Op)
	}
	if cmd.Args["target_lot_location"] != "0x123456" {
		t.Errorf("target_lot_location not forwarded: %v", cmd.Args)
	}
}

// TestVisitLotHandlerDropsUnknownArgs asserts pickArgs filters out convention-
// framework args that the bot doesn't know about (same contract as the
// movement handlers).
func TestVisitLotHandlerDropsUnknownArgs(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true, "payload": map[string]any{},
	})

	handler := visitLotHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	_, _ = handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x123456",
		"extra_junk_field":    "ignore-me",
		"message":             "also-ignore",
	}})
	cmd := <-gotCmd
	if _, has := cmd.Args["extra_junk_field"]; has {
		t.Errorf("extra_junk_field should not have been forwarded: %v", cmd.Args)
	}
	if _, has := cmd.Args["message"]; has {
		t.Errorf("framework 'message' arg should not have been forwarded: %v", cmd.Args)
	}
	if cmd.Args["target_lot_location"] != "0x123456" {
		t.Errorf("target_lot_location not forwarded: %v", cmd.Args)
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

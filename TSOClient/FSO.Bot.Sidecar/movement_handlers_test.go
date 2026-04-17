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

	"github.com/campfire-net/campfire/pkg/convention"
)

// TestWalkToHandlerDispatchesIPC asserts that a walk-to convention invocation
// produces an IPC command with op="walk-to" carrying the declared args. This
// is the outer Go-side veracity guarantee: the handler translates the
// convention request into the right IPC frame, which the bot dispatcher then
// turns into the right VMNetGotoCmd (asserted by the C# xUnit golden test).
func TestWalkToHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{"queued": true, "x": 100, "y": 200},
	})

	handler := walkToHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"x": float64(100), "y": float64(200), "level": float64(1),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	if resp == nil {
		t.Fatal("nil response")
	}
	cmd := <-gotCmd
	if cmd.Op != "walk-to" {
		t.Errorf("want op=walk-to got %q", cmd.Op)
	}
	if cmd.Args["x"] != float64(100) || cmd.Args["y"] != float64(200) {
		t.Errorf("args not forwarded: %v", cmd.Args)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil || payload["ok"] != true {
		t.Errorf("response payload missing ok=true: %v", resp.Payload)
	}
}

func TestCancelHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{"cancelled": 2},
	})

	handler := cancelHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	_, err := handler(ctx, &convention.Request{Args: map[string]any{}})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	cmd := <-gotCmd
	if cmd.Op != "cancel" {
		t.Errorf("want op=cancel got %q", cmd.Op)
	}
}

func TestQueueInteractionHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{"queued": true},
	})

	handler := queueInteractionHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	_, err := handler(ctx, &convention.Request{
		Args: map[string]any{"interaction": float64(1), "callee_id": float64(17)},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	cmd := <-gotCmd
	if cmd.Op != "queue-interaction" {
		t.Errorf("want op=queue-interaction got %q", cmd.Op)
	}
	if cmd.Args["interaction"] != float64(1) || cmd.Args["callee_id"] != float64(17) {
		t.Errorf("args not forwarded: %v", cmd.Args)
	}
}

// TestForwardIPCTimeoutReturnsError asserts that when the bot never responds,
// the handler surfaces an error payload to the convention fulfillment.
func TestForwardIPCTimeoutReturnsError(t *testing.T) {
	fake := newFakeBotProcess()
	// Drain stdin but never respond.
	go func() {
		for range fake.stdinLines {
		}
	}()
	ipc := NewIPC(fake.bot)

	ctx, cancel := context.WithTimeout(context.Background(), 50*time.Millisecond)
	defer cancel()
	resp, err := forwardIPC(ctx, ipc, "walk-to", map[string]any{})
	if err != nil {
		t.Fatalf("forwardIPC: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil || payload["ok"] != false {
		t.Fatalf("expected ok=false, got %v", resp.Payload)
	}
}

// captureOneCommand spins a goroutine that waits for one stdin frame, echoes
// the given response, and returns a channel delivering the parsed Command so
// the test can assert on its shape.
func captureOneCommand(t *testing.T, fake *fakeBotProcess, ipc *IPC, resp map[string]any) <-chan Command {
	t.Helper()
	ch := make(chan Command, 1)
	go func() {
		select {
		case line := <-fake.stdinLines:
			var cmd Command
			if err := json.Unmarshal(line, &cmd); err != nil {
				t.Errorf("unmarshal: %v", err)
				return
			}
			ch <- cmd
			// Populate cmd_id to correlate.
			resp["cmd_id"] = cmd.ID
			data, _ := json.Marshal(resp)
			ipc.Deliver(data)
		case <-time.After(2 * time.Second):
			t.Error("captureOneCommand: stdin frame never arrived")
		}
	}()
	return ch
}

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

// TestWalkToHandlerDispatchesIPC asserts that a walk-to convention invocation
// produces an IPC command with op="walk-to" carrying the declared args. This
// is the outer Go-side veracity guarantee: the handler translates the
// convention request into the right IPC frame, which the bot dispatcher then
// turns into the right VMNetGotoCmd (asserted by the C# xUnit golden test).
//
// The handler now performs a query-self call first when level is specified (to
// detect cross-level navigation). This test wires query-self to return level=1
// so the same-level pass-through fires and walk-to is forwarded normally.
func TestWalkToHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// Wire a multi-op responder: query-self returns level=1 (same level as
	// target), so the handler forwards walk-to directly without stair logic.
	responses := map[string]map[string]any{
		"query-self": {
			"ok": true,
			"payload": map[string]any{
				"position": map[string]any{"x": 10.0, "y": 20.0, "level": 1.0},
			},
		},
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
	if resp == nil {
		t.Fatal("nil response")
	}

	// Collect commands and find walk-to.
	var walkCmd *Command
	drainLoop:
	for {
		select {
		case cmd := <-received:
			c := cmd // copy
			if cmd.Op == "walk-to" {
				walkCmd = &c
			}
		case <-time.After(200 * time.Millisecond):
			break drainLoop
		}
	}
	if walkCmd == nil {
		t.Fatal("walk-to IPC command never arrived")
	}
	if walkCmd.Args["x"] != float64(100) || walkCmd.Args["y"] != float64(200) {
		t.Errorf("args not forwarded: %v", walkCmd.Args)
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

func TestWalkToHandlerForwardsQueueMode(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{"queued": true, "queue_mode": "preempt"},
	})

	handler := walkToHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	_, err := handler(ctx, &convention.Request{
		Args: map[string]any{"x": float64(512), "y": float64(640), "queue_mode": "preempt"},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	cmd := <-gotCmd
	if cmd.Op != "walk-to" {
		t.Errorf("want op=walk-to got %q", cmd.Op)
	}
	if cmd.Args["queue_mode"] != "preempt" {
		t.Errorf("queue_mode not forwarded: %v", cmd.Args)
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

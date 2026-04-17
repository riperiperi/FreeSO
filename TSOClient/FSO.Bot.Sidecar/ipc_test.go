/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"encoding/json"
	"errors"
	"io"
	"sync"
	"testing"
	"time"
)

// TestIPCSendAwaitsResponse asserts that Send() serialises a correlated
// Command frame to stdin and resolves when a matching response arrives via
// Deliver(). Validates the wire contract freesoexperiment-b9c locks down.
func TestIPCSendAwaitsResponse(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// Arrange: a goroutine that reads any frame we write to stdin, echoes a
	// matching response, and pushes it to ipc.Deliver.
	go func() {
		line := <-fake.stdinLines
		var cmd Command
		if err := json.Unmarshal(line, &cmd); err != nil {
			t.Errorf("unmarshal cmd: %v", err)
			return
		}
		if cmd.Op != "walk-to" {
			t.Errorf("want op=walk-to, got %s", cmd.Op)
		}
		resp := map[string]any{
			"kind":    "response",
			"cmd_id":  cmd.ID,
			"ok":      true,
			"payload": map[string]any{"queued": true},
		}
		data, _ := json.Marshal(resp)
		ipc.Deliver(data)
	}()

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := ipc.Send(ctx, "walk-to", map[string]any{"x": 100, "y": 200})
	if err != nil {
		t.Fatalf("Send: %v", err)
	}
	if !resp.Ok {
		t.Fatalf("want ok=true, got ok=%v error=%q", resp.Ok, resp.Error)
	}
}

// TestIPCSendTimesOut asserts that Send respects ctx deadline when no
// response arrives.
func TestIPCSendTimesOut(t *testing.T) {
	fake := newFakeBotProcess()
	// Drain stdin so WriteStdin doesn't block, but never deliver a response.
	go func() {
		for range fake.stdinLines {
		}
	}()

	ipc := NewIPC(fake.bot)
	ctx, cancel := context.WithTimeout(context.Background(), 50*time.Millisecond)
	defer cancel()
	_, err := ipc.Send(ctx, "walk-to", nil)
	if err == nil {
		t.Fatal("want timeout error, got nil")
	}
	if !errors.Is(err, context.DeadlineExceeded) {
		t.Errorf("want DeadlineExceeded, got %v", err)
	}
}

// TestIPCDeliverUnknownCmdID logs and drops stray responses without panic.
func TestIPCDeliverUnknownCmdID(t *testing.T) {
	ipc := NewIPC(newFakeBotProcess().bot)
	// No pending — this should just log.
	ipc.Deliver([]byte(`{"kind":"response","cmd_id":"not-there","ok":true}`))
}

// TestIPCSendFailPayload propagates bot-side failures verbatim.
func TestIPCSendFailPayload(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	go func() {
		line := <-fake.stdinLines
		var cmd Command
		_ = json.Unmarshal(line, &cmd)
		resp := map[string]any{
			"kind":   "response",
			"cmd_id": cmd.ID,
			"ok":     false,
			"error":  "no live avatar",
		}
		data, _ := json.Marshal(resp)
		ipc.Deliver(data)
	}()
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := ipc.Send(ctx, "walk-to", nil)
	if err != nil {
		t.Fatalf("Send: %v", err)
	}
	if resp.Ok {
		t.Fatal("want ok=false")
	}
	if resp.Error != "no live avatar" {
		t.Errorf("want error='no live avatar', got %q", resp.Error)
	}
}

// fakeBotProcess wraps a BotProcess with an in-memory stdin pipe so tests
// don't need to spawn a real child. The bot field is the thing exposed to IPC.
type fakeBotProcess struct {
	bot         *BotProcess
	stdinR      *io.PipeReader
	stdinLines  chan []byte
	wg          sync.WaitGroup
}

func newFakeBotProcess() *fakeBotProcess {
	r, w := io.Pipe()
	bot := &BotProcess{stdin: w}
	f := &fakeBotProcess{
		bot:        bot,
		stdinR:     r,
		stdinLines: make(chan []byte, 16),
	}
	f.wg.Add(1)
	go func() {
		defer f.wg.Done()
		defer close(f.stdinLines)
		buf := make([]byte, 0, 4096)
		chunk := make([]byte, 4096)
		for {
			n, err := f.stdinR.Read(chunk)
			if n > 0 {
				buf = append(buf, chunk[:n]...)
				for {
					idx := -1
					for i, b := range buf {
						if b == '\n' {
							idx = i
							break
						}
					}
					if idx < 0 {
						break
					}
					line := make([]byte, idx)
					copy(line, buf[:idx])
					buf = buf[idx+1:]
					f.stdinLines <- line
				}
			}
			if err != nil {
				return
			}
		}
	}()
	return f
}

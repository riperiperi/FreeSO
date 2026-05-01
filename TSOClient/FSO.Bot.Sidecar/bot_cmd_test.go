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
	"os"
	"strings"
	"sync"
	"testing"
	"time"
)

// TestBotCmdPumpSendAwaitsReply asserts that Send() serialises a BotCmdRequest
// with a correlation_id to stdin and resolves when a matching BotCmdReply arrives
// via Deliver(). Validates the bot-cmd wire contract (freesoexperiment-0de).
func TestBotCmdPumpSendAwaitsReply(t *testing.T) {
	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	// Goroutine that reads the bot-cmd frame from stdin, echoes a bot-cmd-reply
	// with the same correlation_id, and pushes it to pump.Deliver.
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
		if req.Cmd != "probe-lot" {
			t.Errorf("want cmd=probe-lot, got %s", req.Cmd)
		}
		if req.CorrelationID == "" {
			t.Errorf("correlation_id must be non-empty")
		}
		reply := map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"status": "FOUND", "lot_id": 2},
		}
		data, _ := json.Marshal(reply)
		pump.Deliver(data)
	}()

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	reply, err := pump.Send(ctx, "probe-lot", map[string]any{"lot_location": 16318812})
	if err != nil {
		t.Fatalf("Send: %v", err)
	}
	if !reply.Ok {
		t.Fatalf("want ok=true, got ok=%v error=%q", reply.Ok, reply.Error)
	}
	var data struct {
		Status string `json:"status"`
		LotID  int    `json:"lot_id"`
	}
	if err := json.Unmarshal(reply.Data, &data); err != nil {
		t.Fatalf("unmarshal reply data: %v", err)
	}
	if data.Status != "FOUND" {
		t.Errorf("want status=FOUND, got %q", data.Status)
	}
	if data.LotID != 2 {
		t.Errorf("want lot_id=2, got %d", data.LotID)
	}
}

// TestBotCmdPumpSendCLOSED asserts that a NOT_OPEN status reply is delivered
// faithfully. This is the "CLOSED" path the done condition requires.
func TestBotCmdPumpSendCLOSED(t *testing.T) {
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
			"ok":             true,
			"data":           map[string]any{"status": "NOT_OPEN"},
		}
		data, _ := json.Marshal(reply)
		pump.Deliver(data)
	}()

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	reply, err := pump.Send(ctx, "probe-lot", map[string]any{"lot_location": 9999})
	if err != nil {
		t.Fatalf("Send: %v", err)
	}
	if !reply.Ok {
		t.Fatalf("want ok=true (NOT_OPEN status is in data, not ok=false)")
	}
	var data struct {
		Status string `json:"status"`
	}
	if err := json.Unmarshal(reply.Data, &data); err != nil {
		t.Fatalf("unmarshal: %v", err)
	}
	if data.Status != "NOT_OPEN" {
		t.Errorf("want status=NOT_OPEN, got %q", data.Status)
	}
}

// TestBotCmdPumpSendTimesOut asserts that Send respects ctx deadline when no
// reply arrives.
func TestBotCmdPumpSendTimesOut(t *testing.T) {
	fake := newFakeBotProcess()
	go func() {
		for range fake.stdinLines {
		}
	}()

	pump := NewBotCmdPump(fake.bot)
	ctx, cancel := context.WithTimeout(context.Background(), 50*time.Millisecond)
	defer cancel()
	_, err := pump.Send(ctx, "probe-lot", nil)
	if err == nil {
		t.Fatal("want timeout error, got nil")
	}
	if !errors.Is(err, context.DeadlineExceeded) {
		t.Errorf("want DeadlineExceeded, got %v", err)
	}
}

// TestBotCmdPumpDeliverUnknownCorrID logs and drops stray replies without panic.
func TestBotCmdPumpDeliverUnknownCorrID(t *testing.T) {
	pump := NewBotCmdPump(newFakeBotProcess().bot)
	pump.Deliver([]byte(`{"kind":"bot-cmd-reply","correlation_id":"not-there","ok":true}`))
}

// TestBotCmdPumpBotExitRequest asserts that bot-exit-request is serialised
// correctly and the reply is correlated back.
func TestBotCmdPumpBotExitRequest(t *testing.T) {
	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	go func() {
		line := <-fake.stdinLines
		var req BotCmdRequest
		if err := json.Unmarshal(line, &req); err != nil {
			t.Errorf("unmarshal: %v", err)
			return
		}
		if req.Kind != "bot-cmd" {
			t.Errorf("want kind=bot-cmd, got %s", req.Kind)
		}
		if req.Cmd != "bot-exit-request" {
			t.Errorf("want cmd=bot-exit-request, got %s", req.Cmd)
		}
		reply := map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"accepted": true},
		}
		data, _ := json.Marshal(reply)
		pump.Deliver(data)
	}()

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	reply, err := pump.Send(ctx, "bot-exit-request", nil)
	if err != nil {
		t.Fatalf("Send bot-exit-request: %v", err)
	}
	if !reply.Ok {
		t.Fatalf("want ok=true, got ok=%v", reply.Ok)
	}
}

// TestBotCmdPumpFailError asserts that ok=false replies surface reply.Error.
func TestBotCmdPumpFailError(t *testing.T) {
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
			"error":          "lot not found",
		}
		data, _ := json.Marshal(reply)
		pump.Deliver(data)
	}()

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	reply, err := pump.Send(ctx, "probe-lot", map[string]any{"lot_location": 0})
	if err != nil {
		t.Fatalf("Send: %v", err)
	}
	if reply.Ok {
		t.Fatal("want ok=false")
	}
	if reply.Error != "lot not found" {
		t.Errorf("want error='lot not found', got %q", reply.Error)
	}
}

// TestBotCmdPumpUpdateBot asserts that UpdateBot swaps the underlying process.
func TestBotCmdPumpUpdateBot(t *testing.T) {
	oldFake := newFakeBotProcess()
	go func() {
		for range oldFake.stdinLines {
		}
	}()

	pump := NewBotCmdPump(oldFake.bot)

	newFake := newFakeBotProcess()
	pump.UpdateBot(newFake.bot)

	go func() {
		line := <-newFake.stdinLines
		var req BotCmdRequest
		if err := json.Unmarshal(line, &req); err != nil {
			t.Errorf("unmarshal after UpdateBot: %v", err)
			return
		}
		reply := map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"status": "FOUND"},
		}
		data, _ := json.Marshal(reply)
		pump.Deliver(data)
	}()

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	reply, err := pump.Send(ctx, "probe-lot", nil)
	if err != nil {
		t.Fatalf("Send after UpdateBot: %v", err)
	}
	if !reply.Ok {
		t.Fatalf("want ok=true after UpdateBot, got %v", reply.Ok)
	}
}

// TestBotCmdPumpConcurrent asserts N concurrent Send calls are correctly demultiplexed.
func TestBotCmdPumpConcurrent(t *testing.T) {
	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	const N = 5
	go func() {
		for i := 0; i < N; i++ {
			line, ok := <-fake.stdinLines
			if !ok {
				return
			}
			var req BotCmdRequest
			if err := json.Unmarshal(line, &req); err != nil {
				t.Errorf("concurrent: unmarshal: %v", err)
				return
			}
			reply := map[string]any{
				"kind":           "bot-cmd-reply",
				"correlation_id": req.CorrelationID,
				"ok":             true,
				"data":           map[string]any{"status": "FOUND"},
			}
			data, _ := json.Marshal(reply)
			pump.Deliver(data)
		}
	}()

	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	var wg sync.WaitGroup
	errs := make(chan error, N)
	for i := 0; i < N; i++ {
		wg.Add(1)
		go func(idx int) {
			defer wg.Done()
			reply, err := pump.Send(ctx, "probe-lot", map[string]any{"idx": idx})
			if err != nil {
				errs <- err
				return
			}
			if !reply.Ok {
				errs <- errors.New("concurrent: got ok=false")
			}
		}(i)
	}
	wg.Wait()
	close(errs)
	for err := range errs {
		t.Errorf("concurrent: %v", err)
	}
}

// TestBotCmdEnvelopeWireShape verifies that BotCmdRequest marshals to the
// exact JSON shape the C# BotCmdHandler.cs expects, and that BotCmdReply
// unmarshals from the C# emitter's output. Cross-language contract test.
func TestBotCmdEnvelopeWireShape(t *testing.T) {
	// Sidecar → bot: verify JSON shape.
	req := BotCmdRequest{
		Kind:          "bot-cmd",
		Cmd:           "probe-lot",
		CorrelationID: "test-uuid-1234",
		Args:          map[string]any{"lot_location": uint32(16318812)},
	}
	data, err := json.Marshal(req)
	if err != nil {
		t.Fatalf("marshal request: %v", err)
	}
	s := string(data)
	for _, field := range []string{`"kind":"bot-cmd"`, `"cmd":"probe-lot"`, `"correlation_id":"test-uuid-1234"`, `"lot_location"`} {
		if !strings.Contains(s, field) {
			t.Errorf("serialized request missing field %q: %s", field, s)
		}
	}

	// Bot → sidecar: unmarshal from C#-like output.
	cSharpLike := `{"kind":"bot-cmd-reply","correlation_id":"test-uuid-1234","ok":true,"data":{"status":"FOUND","lot_id":2}}`
	var reply BotCmdReply
	if err := json.Unmarshal([]byte(cSharpLike), &reply); err != nil {
		t.Fatalf("unmarshal reply: %v", err)
	}
	if reply.Kind != "bot-cmd-reply" {
		t.Errorf("want kind=bot-cmd-reply, got %q", reply.Kind)
	}
	if reply.CorrelationID != "test-uuid-1234" {
		t.Errorf("want corr_id=test-uuid-1234, got %q", reply.CorrelationID)
	}
	if !reply.Ok {
		t.Errorf("want ok=true")
	}
	var dataObj struct {
		Status string `json:"status"`
		LotID  int    `json:"lot_id"`
	}
	if err := json.Unmarshal(reply.Data, &dataObj); err != nil {
		t.Fatalf("unmarshal reply.Data: %v", err)
	}
	if dataObj.Status != "FOUND" {
		t.Errorf("want status=FOUND, got %q", dataObj.Status)
	}
	if dataObj.LotID != 2 {
		t.Errorf("want lot_id=2, got %d", dataObj.LotID)
	}

	// Bot-exit-request shape (no args omitted).
	exitReq := BotCmdRequest{
		Kind:          "bot-cmd",
		Cmd:           "bot-exit-request",
		CorrelationID: "exit-uuid-5678",
	}
	exitData, err := json.Marshal(exitReq)
	if err != nil {
		t.Fatalf("marshal exit-request: %v", err)
	}
	es := string(exitData)
	for _, field := range []string{`"kind":"bot-cmd"`, `"cmd":"bot-exit-request"`, `"correlation_id":"exit-uuid-5678"`} {
		if !strings.Contains(es, field) {
			t.Errorf("exit-request missing field %q: %s", field, es)
		}
	}
}

// ---- integration test: real subprocess JSONL round-trip ----

// TestBotCmdIntegration_ProbeLotFOUNDCLOSED is the feature integration test for
// freesoexperiment-0de. It spawns a real stub bot subprocess that reads bot-cmd
// frames on stdin and echoes bot-cmd-reply on stdout, then exercises the full
// BotCmdPump → subprocess → bridgesRunNoCampfire round-trip.
//
// Done condition (from item): "Standalone Go test issues probe-lot, asserts FOUND/CLOSED."
//
// Skip condition: FREESO_SKIP_INTEGRATION=1.
func TestBotCmdIntegration_ProbeLotFOUNDCLOSED(t *testing.T) {
	if os.Getenv("FREESO_SKIP_INTEGRATION") == "1" {
		t.Skip("FREESO_SKIP_INTEGRATION=1")
	}

	tmp := t.TempDir()

	// Stub bot: reads bot-cmd lines from stdin, echoes bot-cmd-reply.
	// Alternates FOUND (odd) and NOT_OPEN (even) for the first two probe-lot calls,
	// then echoes accepted:true for bot-exit-request.
	stubScript := `#!/bin/sh
# Emit system:ready so the bridge can initialize.
printf '{"kind":"system","payload":{"event":"ready"}}\n'
# Read commands from stdin and echo replies.
count=0
while IFS= read -r line; do
    corr=$(printf '%s' "$line" | sed 's/.*"correlation_id":"\([^"]*\)".*/\1/')
    cmd=$(printf '%s' "$line" | sed 's/.*"cmd":"\([^"]*\)".*/\1/')
    count=$((count+1))
    if [ "$cmd" = "bot-exit-request" ]; then
        printf '{"kind":"bot-cmd-reply","correlation_id":"%s","ok":true,"data":{"accepted":true}}\n' "$corr"
        continue
    fi
    if [ $((count % 2)) -eq 1 ]; then
        status="FOUND"
    else
        status="NOT_OPEN"
    fi
    printf '{"kind":"bot-cmd-reply","correlation_id":"%s","ok":true,"data":{"status":"%s","lot_id":2}}\n' "$corr" "$status"
done
`
	stubBotPath := tmp + "/stub-bot.sh"
	if err := os.WriteFile(stubBotPath, []byte(stubScript), 0o700); err != nil {
		t.Fatalf("write stub bot: %v", err)
	}

	ctx, cancel := context.WithTimeout(context.Background(), 15*time.Second)
	defer cancel()

	proc, err := LaunchBot(ctx, BotConfig{
		Exec: stubBotPath,
		Args: []string{},
		Env:  []string{},
	})
	if err != nil {
		t.Fatalf("LaunchBot: %v", err)
	}
	defer proc.Stop()

	pump := NewBotCmdPump(proc)

	// bridgesRunNoCampfire routes bot-cmd-reply frames from real stdout to pump.Deliver.
	go bridgesRunNoCampfire(ctx, proc, pump)

	// Give the stub bot a moment to emit system:ready and settle.
	time.Sleep(150 * time.Millisecond)

	// First probe: expect FOUND (count=1, odd).
	ctx1, cancel1 := context.WithTimeout(ctx, 5*time.Second)
	defer cancel1()
	reply1, err := pump.Send(ctx1, "probe-lot", map[string]any{"lot_location": 16318812})
	if err != nil {
		t.Fatalf("probe-lot #1: %v", err)
	}
	if !reply1.Ok {
		t.Fatalf("probe-lot #1: want ok=true, error=%q", reply1.Error)
	}
	var d1 struct {
		Status string `json:"status"`
	}
	if err := json.Unmarshal(reply1.Data, &d1); err != nil {
		t.Fatalf("probe-lot #1 unmarshal: %v", err)
	}
	if d1.Status != "FOUND" {
		t.Errorf("probe-lot #1: want status=FOUND, got %q", d1.Status)
	}
	t.Logf("probe-lot #1: status=%s — FOUND path OK, correlation_id round-trip verified", d1.Status)

	// Second probe: expect NOT_OPEN (count=2, even) — the CLOSED path.
	ctx2, cancel2 := context.WithTimeout(ctx, 5*time.Second)
	defer cancel2()
	reply2, err := pump.Send(ctx2, "probe-lot", map[string]any{"lot_location": 9999})
	if err != nil {
		t.Fatalf("probe-lot #2: %v", err)
	}
	if !reply2.Ok {
		t.Fatalf("probe-lot #2: want ok=true, error=%q", reply2.Error)
	}
	var d2 struct {
		Status string `json:"status"`
	}
	if err := json.Unmarshal(reply2.Data, &d2); err != nil {
		t.Fatalf("probe-lot #2 unmarshal: %v", err)
	}
	if d2.Status != "NOT_OPEN" {
		t.Errorf("probe-lot #2: want status=NOT_OPEN (CLOSED path), got %q", d2.Status)
	}
	t.Logf("probe-lot #2: status=%s — CLOSED path OK, correlation_id round-trip verified", d2.Status)

	// Third: bot-exit-request round-trip.
	ctx3, cancel3 := context.WithTimeout(ctx, 5*time.Second)
	defer cancel3()
	reply3, err := pump.Send(ctx3, "bot-exit-request", nil)
	if err != nil {
		t.Fatalf("bot-exit-request: %v", err)
	}
	if !reply3.Ok {
		t.Fatalf("bot-exit-request: want ok=true, error=%q", reply3.Error)
	}
	t.Logf("bot-exit-request: OK — correlation_id round-trip verified")

	t.Log("PASS: TestBotCmdIntegration_ProbeLotFOUNDCLOSED — FOUND, CLOSED (NOT_OPEN), and bot-exit-request paths all verified via real subprocess JSONL round-trip")
}

// bridgesRunNoCampfire is a lightweight bridge runner for integration tests
// that have a real BotProcess but no campfire. Routes bot-cmd-reply frames to
// the BotCmdPump.
func bridgesRunNoCampfire(ctx context.Context, proc *BotProcess, pump *BotCmdPump) {
	for {
		select {
		case <-ctx.Done():
			return
		case line, ok := <-proc.Lines():
			if !ok {
				return
			}
			var env struct {
				Kind string `json:"kind"`
			}
			if jerr := json.Unmarshal(line, &env); jerr != nil {
				continue
			}
			if env.Kind == "bot-cmd-reply" {
				pump.Deliver(line)
			}
		}
	}
}

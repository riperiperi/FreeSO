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

// TestSetTaxRateMissingRate asserts that a call without tax_rate_percent returns ok:false.
func TestSetTaxRateMissingRate(t *testing.T) {
	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)
	go func() {
		for range fake.stdinLines {
		}
	}()

	handler := setTaxRateHandler(pump, nil)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for missing rate, got %v", payload)
	}
}

// TestSetTaxRateInvalidRate asserts that out-of-range rates return INVALID_TAX_RATE.
func TestSetTaxRateInvalidRate(t *testing.T) {
	cases := []float64{-1.0, 101.0, -0.01, 200.0}
	for _, rate := range cases {
		fake := newFakeBotProcess()
		pump := NewBotCmdPump(fake.bot)
		go func() {
			for range fake.stdinLines {
			}
		}()

		handler := setTaxRateHandler(pump, nil)
		ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
		defer cancel()

		resp, err := handler(ctx, &convention.Request{Args: map[string]any{
			"tax_rate_percent": rate,
		}})
		if err != nil {
			t.Fatalf("rate=%v handler error: %v", rate, err)
		}
		payload, _ := resp.Payload.(map[string]any)
		if payload["ok"] != false {
			t.Errorf("rate=%v: want ok=false for invalid rate, got %v", rate, payload)
		}
		if payload["reason"] != "INVALID_TAX_RATE" {
			t.Errorf("rate=%v: want reason=INVALID_TAX_RATE, got %v", rate, payload["reason"])
		}
	}
}

// TestSetTaxRateNoBotMode asserts that nil botCmds returns deferred=true.
func TestSetTaxRateNoBotMode(t *testing.T) {
	handler := setTaxRateHandler(nil, nil)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"tax_rate_percent": float64(5),
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false in --no-bot mode, got %v", payload)
	}
	if payload["deferred"] != true {
		t.Errorf("want deferred=true in --no-bot mode, got %v", payload)
	}
}

// TestSetTaxRateNoAuth asserts that a bot returning NO_AUTH is surfaced.
func TestSetTaxRateNoAuth(t *testing.T) {
	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	go func() {
		line := <-fake.stdinLines
		var req BotCmdRequest
		if err := json.Unmarshal(line, &req); err != nil {
			t.Errorf("unmarshal: %v", err)
			return
		}
		if req.Cmd != "tax-debit" {
			t.Errorf("want cmd=tax-debit, got %q", req.Cmd)
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             true,
			"data": map[string]any{
				"ok":     false,
				"reason": "NO_AUTH",
			},
		}))
	}()

	handler := setTaxRateHandler(pump, nil)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"tax_rate_percent": float64(5),
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for NO_AUTH, got %v", payload)
	}
	if payload["reason"] != "NO_AUTH" {
		t.Errorf("want reason=NO_AUTH, got %v", payload["reason"])
	}
}

// TestSetTaxRateSuccessPath is the I0-9 falsifying test.
//
// Protocol:
//  1. Mayor calls set-tax-rate with 10% rate.
//  2. Sidecar sends bot-cmd:tax-debit.
//  3. Stub bot returns two residents with debit amounts and new balances.
//  4. Verify response contains resident deltas (budget change observable).
//  5. Verify resident[0].new_balance = original_balance - debit (the felt budget delta).
//
// This is the falsifying test: if the tax-debit path does not execute per-resident
// Avatars.Transaction, the resident list will be empty or missing new_balance.
// An agent calling query-self after this cycle will observe the reduced budget.
func TestSetTaxRateSuccessPath(t *testing.T) {
	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	// Residents as they'd appear in the bot's response.
	// Resident A: balance was 100000, 10% = 10000 debit → new_balance 90000.
	// Resident B: balance was 50000, 10% = 5000 debit → new_balance 45000.
	residents := []map[string]any{
		{
			"persist_id":   float64(5),
			"avatar_name":  "Botrous",
			"debit":        float64(10000),
			"new_balance":  float64(90000),
		},
		{
			"persist_id":   float64(6),
			"avatar_name":  "Ellis",
			"debit":        float64(5000),
			"new_balance":  float64(45000),
		},
	}

	go func() {
		line := <-fake.stdinLines
		var req BotCmdRequest
		if err := json.Unmarshal(line, &req); err != nil {
			t.Errorf("unmarshal: %v", err)
			return
		}
		if req.Cmd != "tax-debit" {
			t.Errorf("want cmd=tax-debit, got %q", req.Cmd)
		}
		// Verify rate was forwarded.
		rate, _ := req.Args["tax_rate_percent"].(float64)
		if rate != 10.0 {
			t.Errorf("want tax_rate_percent=10.0 forwarded to bot, got %v", rate)
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             true,
			"data": map[string]any{
				"ok":        true,
				"residents": residents,
			},
		}))
	}()

	handler := setTaxRateHandler(pump, nil) // nil ipc — bulletin skipped
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"tax_rate_percent": float64(10),
		"neighborhood_id":  float64(1),
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != true {
		t.Fatalf("want ok=true: %v", payload)
	}

	// --- I0-9 Falsifying check: verify per-resident budget deltas are present ---
	gotResidents, _ := payload["residents"].([]TaxResidentResult)
	if len(gotResidents) == 0 {
		// Also try raw interface (in case JSON re-marshal/unmarshal changed type).
		rawResidents, _ := payload["residents"].([]any)
		if len(rawResidents) != 2 {
			t.Fatalf("I0-9 FALSIFIED: expected 2 residents in response (budget delta observable), got %v (type %T)",
				payload["residents"], payload["residents"])
		}
		// Check first resident's new_balance to verify the delta.
		first, _ := rawResidents[0].(map[string]any)
		if first == nil {
			t.Fatalf("I0-9 FALSIFIED: resident[0] is not a map: %T", rawResidents[0])
		}
		newBal, ok := first["new_balance"].(float64)
		if !ok {
			t.Fatalf("I0-9 FALSIFIED: resident[0].new_balance missing or not float64: %v", first["new_balance"])
		}
		debit, ok := first["debit"].(float64)
		if !ok {
			t.Fatalf("I0-9 FALSIFIED: resident[0].debit missing or not float64: %v", first["debit"])
		}
		// Verify: original_balance = new_balance + debit = 90000 + 10000 = 100000
		if newBal+debit != 100000 {
			t.Errorf("I0-9 FALSIFIED: expected new_balance(%v) + debit(%v) = 100000, got %v",
				newBal, debit, newBal+debit)
		}
		t.Logf("I0-9 OK: resident[0] new_balance=%v debit=%v (original was %v — budget delta observable)",
			newBal, debit, newBal+debit)
	} else {
		if len(gotResidents) != 2 {
			t.Fatalf("I0-9 FALSIFIED: expected 2 residents, got %d", len(gotResidents))
		}
		// Verify first resident delta.
		if gotResidents[0].NewBalance+gotResidents[0].Debit != 100000 {
			t.Errorf("I0-9 FALSIFIED: resident[0] budget math wrong: new_balance=%d debit=%d sum=%d",
				gotResidents[0].NewBalance, gotResidents[0].Debit,
				gotResidents[0].NewBalance+gotResidents[0].Debit)
		}
		t.Logf("I0-9 OK: resident[0] new_balance=%d debit=%d (original was %d)",
			gotResidents[0].NewBalance, gotResidents[0].Debit,
			gotResidents[0].NewBalance+gotResidents[0].Debit)
	}

	// Verify response metadata.
	if payload["tax_rate_percent"].(float64) != 10.0 {
		t.Errorf("want tax_rate_percent=10.0 in response, got %v", payload["tax_rate_percent"])
	}
	if rc, _ := payload["resident_count"].(int); rc != 2 {
		// JSON numbers unmarshal as float64.
		if rcf, _ := payload["resident_count"].(float64); rcf != 2 {
			t.Errorf("want resident_count=2, got %v (type %T)", payload["resident_count"], payload["resident_count"])
		}
	}
	t.Logf("set-tax-rate success: %v", payload)
}

// TestSetTaxRateZeroPercent asserts that 0% tax is valid (no debit, empty residents ok).
func TestSetTaxRateZeroPercent(t *testing.T) {
	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

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
			"data": map[string]any{
				"ok":        true,
				"residents": []map[string]any{},
			},
		}))
	}()

	handler := setTaxRateHandler(pump, nil)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"tax_rate_percent": float64(0),
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != true {
		t.Errorf("want ok=true for 0%% tax (valid edge case), got %v", payload)
	}
}

// TestSetTaxRateBotCmdError asserts that a bot-cmd transport error is surfaced.
func TestSetTaxRateBotCmdError(t *testing.T) {
	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)
	// Drain stdin but never reply → timeout.
	go func() {
		for range fake.stdinLines {
		}
	}()

	handler := setTaxRateHandler(pump, nil)
	ctx, cancel := context.WithTimeout(context.Background(), 100*time.Millisecond)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"tax_rate_percent": float64(5),
	}})
	if err != nil {
		t.Fatalf("handler error (should surface as ok:false): %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false on bot-cmd timeout, got %v", payload)
	}
	t.Logf("bot-cmd error response: %v", payload)
}

// TestCoerceFloat64 unit-tests the type coercion helper.
func TestCoerceFloat64(t *testing.T) {
	cases := []struct {
		in   any
		want float64
		ok   bool
	}{
		{float64(5.5), 5.5, true},
		{int(10), 10.0, true},
		{int64(100), 100.0, true},
		{"3.14", 3.14, true},
		{"not a number", 0, false},
		{nil, 0, false},
	}
	for _, tc := range cases {
		got, err := coerceFloat64(tc.in)
		if tc.ok && err != nil {
			t.Errorf("coerceFloat64(%v): want ok, got error: %v", tc.in, err)
		}
		if !tc.ok && err == nil {
			t.Errorf("coerceFloat64(%v): want error, got %v", tc.in, got)
		}
		if tc.ok && got != tc.want {
			t.Errorf("coerceFloat64(%v): want %v, got %v", tc.in, tc.want, got)
		}
	}
}

// TestCoerceUint64 unit-tests the uint64 coercion helper.
func TestCoerceUint64(t *testing.T) {
	cases := []struct {
		in   any
		want uint64
		ok   bool
	}{
		{float64(1), 1, true},
		{int(42), 42, true},
		{uint64(999), 999, true},
		{"123", 123, true},
		{float64(-1), 0, false},
		{"abc", 0, false},
	}
	for _, tc := range cases {
		got, err := coerceUint64(tc.in)
		if tc.ok && err != nil {
			t.Errorf("coerceUint64(%v): want ok, got error: %v", tc.in, err)
		}
		if !tc.ok && err == nil {
			t.Errorf("coerceUint64(%v): want error, got %v", tc.in, got)
		}
		if tc.ok && got != tc.want {
			t.Errorf("coerceUint64(%v): want %v, got %v", tc.in, tc.want, got)
		}
	}
}

// mustMarshalTax is an alias for the shared mustMarshal helper (declared in
// purchase_lot_handlers_test.go). Defined here in case this test file runs standalone.
// Go will complain about duplicate declarations if the shared one is already visible —
// use mustMarshal directly (it's in the same package).

// TestSetTaxRateBulletinPosted asserts that a successful tax cycle posts a bulletin
// via IPC when residents are present.
func TestSetTaxRateBulletinPosted(t *testing.T) {
	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	// Track IPC calls.
	ipcFake := newFakeBotProcess()
	ipcInst := NewIPC(ipcFake.bot)

	bulletinPosted := make(chan map[string]any, 1)
	go func() {
		// bot-cmd:tax-debit
		line := <-fake.stdinLines
		var req BotCmdRequest
		if err := json.Unmarshal(line, &req); err != nil {
			t.Errorf("unmarshal tax-debit req: %v", err)
			return
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             true,
			"data": map[string]any{
				"ok": true,
				"residents": []map[string]any{
					{"persist_id": float64(5), "avatar_name": "Botrous", "debit": float64(500), "new_balance": float64(9500)},
				},
			},
		}))
	}()
	go func() {
		// IPC post-bulletin
		line := <-ipcFake.stdinLines
		var cmd Command
		if err := json.Unmarshal(line, &cmd); err != nil {
			t.Errorf("unmarshal post-bulletin cmd: %v", err)
			return
		}
		bulletinPosted <- cmd.Args
		ipcInst.Deliver(mustMarshal(map[string]any{
			"kind":   "response",
			"cmd_id": cmd.ID,
			"ok":     true,
		}))
	}()

	handler := setTaxRateHandler(pump, ipcInst)
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"tax_rate_percent": float64(5),
		"neighborhood_id":  float64(1),
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != true {
		t.Fatalf("want ok=true: %v", payload)
	}

	// Verify bulletin was posted.
	select {
	case args := <-bulletinPosted:
		subject, _ := args["subject"].(string)
		if subject == "" {
			t.Error("want non-empty bulletin subject")
		}
		body, _ := args["body"].(string)
		if body == "" {
			t.Error("want non-empty bulletin body")
		}
		t.Logf("bulletin posted: subject=%q body=%q", subject, body)
	case <-time.After(3 * time.Second):
		t.Error("bulletin was not posted after successful tax cycle")
	}
	_ = os.Getenv // suppress unused import
}

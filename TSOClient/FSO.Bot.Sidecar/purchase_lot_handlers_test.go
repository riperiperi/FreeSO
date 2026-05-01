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

// TestPurchaseLotHandlerMissingLocation asserts that a call without
// target_lot_location returns ok:false with a helpful error.
func TestPurchaseLotHandlerMissingLocation(t *testing.T) {
	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)
	// Drain stdin to prevent goroutine leaks — handler returns early before any bot-cmd.
	go func() {
		for range fake.stdinLines {
		}
	}()

	handler := purchaseLotHandler(pump)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"name": "test lot",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for missing location, got %v", payload)
	}
	if payload["error"] == nil {
		t.Error("want non-nil error for missing location")
	}
}

// TestPurchaseLotHandlerMissingName asserts that a call without name
// returns ok:false.
func TestPurchaseLotHandlerMissingName(t *testing.T) {
	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)
	go func() {
		for range fake.stdinLines {
		}
	}()

	handler := purchaseLotHandler(pump)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x00F9015C",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for missing name, got %v", payload)
	}
}

// TestPurchaseLotHandlerNoRoadBits asserts that probe-road returning
// has_road=false causes ok:false with reason NO_ROAD_ACCESS.
func TestPurchaseLotHandlerNoRoadBits(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "purchase-lot-test-noroad")
	withConfigHome(t, tmp)

	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	// Stub: probe-road returns has_road=false.
	go func() {
		line := <-fake.stdinLines
		var req BotCmdRequest
		if err := json.Unmarshal(line, &req); err != nil {
			t.Errorf("unmarshal probe-road req: %v", err)
			return
		}
		if req.Cmd != "probe-road" {
			t.Errorf("want cmd=probe-road, got %q", req.Cmd)
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"has_road": false, "road_bits": 0, "x": 100, "y": 200},
		}))
	}()

	handler := purchaseLotHandler(pump)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x00640064", // x=100, y=100 — road bits 0
		"name":                "no-road lot",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for no-road tile, got %v", payload)
	}
	if payload["reason"] != "NO_ROAD_ACCESS" {
		t.Errorf("want reason=NO_ROAD_ACCESS, got %v", payload["reason"])
	}
	t.Logf("no-road response: %v", payload)
}

// TestPurchaseLotHandlerAlreadyOwns asserts that a persona with an existing
// owned-lots.json entry is refused ALREADY_OWNS when allow_move is absent/false.
func TestPurchaseLotHandlerAlreadyOwns(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "purchase-lot-test-already-owns")
	withConfigHome(t, tmp)

	// Pre-populate owned-lots.json.
	existing := []OwnedLotEntry{{
		Name:        "existing lot",
		LocationHex: "0x00F9015C",
		PurchasedAt: time.Now().Add(-24 * time.Hour).UnixMilli(),
	}}
	if err := WriteOwnedLots(existing); err != nil {
		t.Fatalf("WriteOwnedLots: %v", err)
	}

	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)
	// Handler returns early before any bot-cmd — drain stdin to prevent leaks.
	go func() {
		for range fake.stdinLines {
		}
	}()

	handler := purchaseLotHandler(pump)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x00FA0100",
		"name":                "second lot",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for ALREADY_OWNS, got %v", payload)
	}
	if payload["reason"] != "ALREADY_OWNS" {
		t.Errorf("want reason=ALREADY_OWNS, got %v", payload["reason"])
	}
	t.Logf("already-owns response: %v", payload)
}

// TestPurchaseLotHandlerAllowMoveBypassesAlreadyOwns asserts that allow_move=true
// skips the ALREADY_OWNS pre-check and proceeds to probe-road.
func TestPurchaseLotHandlerAllowMoveBypassesAlreadyOwns(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "purchase-lot-test-allow-move")
	withConfigHome(t, tmp)

	// Pre-populate owned-lots.json.
	existing := []OwnedLotEntry{{
		Name:        "existing lot",
		LocationHex: "0x00F9015C",
		PurchasedAt: time.Now().Add(-24 * time.Hour).UnixMilli(),
	}}
	if err := WriteOwnedLots(existing); err != nil {
		t.Fatalf("WriteOwnedLots: %v", err)
	}

	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	// Stub: probe-road returns no-road → we get NO_ROAD_ACCESS (not ALREADY_OWNS).
	// This confirms allow_move bypassed the owns check.
	go func() {
		line := <-fake.stdinLines
		var req BotCmdRequest
		if err := json.Unmarshal(line, &req); err != nil {
			t.Errorf("unmarshal probe-road req: %v", err)
			return
		}
		if req.Cmd != "probe-road" {
			t.Errorf("want cmd=probe-road (owns-check bypassed), got %q", req.Cmd)
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"has_road": false, "road_bits": 0, "x": 10, "y": 10},
		}))
	}()

	handler := purchaseLotHandler(pump)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x000A000A",
		"name":                "move target",
		"allow_move":          true,
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	// Should be NO_ROAD_ACCESS (road check hit), NOT ALREADY_OWNS.
	if payload["reason"] == "ALREADY_OWNS" {
		t.Error("allow_move=true should bypass ALREADY_OWNS but did not")
	}
	if payload["reason"] != "NO_ROAD_ACCESS" {
		t.Errorf("expected NO_ROAD_ACCESS after bypassing owns check, got %v", payload)
	}
}

// TestPurchaseLotHandlerSuccessPath is the integration test for a clean
// purchase: probe-road returns has_road=true, purchase-lot returns SUCCESS.
// Verifies:
//   - Both bot-cmd shapes (probe-road + purchase-lot) are exercised.
//   - owned-lots.json is written with the new entry.
//   - Habitation block is zeroed (nil timestamps, is_habitable=false).
//   - Response carries ok=true, lot_id, new_funds, location_hex, road_bits.
func TestPurchaseLotHandlerSuccessPath(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "purchase-lot-test-success")
	withConfigHome(t, tmp)

	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	const targetLocHex = "0x00F90160" // x=249, y=352 — nearby the workshop home lot
	// 0x00F90160 = 16318816 decimal; x = (16318816>>16)&0xFFFF = 249, y = 16318816&0xFFFF = 352

	go func() {
		// Frame 1: probe-road
		line1 := <-fake.stdinLines
		var req1 BotCmdRequest
		if err := json.Unmarshal(line1, &req1); err != nil {
			t.Errorf("unmarshal probe-road req: %v", err)
			return
		}
		if req1.Cmd != "probe-road" {
			t.Errorf("want cmd=probe-road, got %q", req1.Cmd)
			return
		}
		// Verify x and y were decoded from the packed location.
		x, _ := req1.Args["x"].(float64)
		y, _ := req1.Args["y"].(float64)
		if x != 249 || y != 352 {
			t.Errorf("probe-road args: want x=249 y=352, got x=%v y=%v", x, y)
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req1.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"has_road": true, "road_bits": 9, "x": 249, "y": 352},
		}))

		// Frame 2: purchase-lot
		line2 := <-fake.stdinLines
		var req2 BotCmdRequest
		if err := json.Unmarshal(line2, &req2); err != nil {
			t.Errorf("unmarshal purchase-lot req: %v", err)
			return
		}
		if req2.Cmd != "purchase-lot" {
			t.Errorf("want cmd=purchase-lot, got %q", req2.Cmd)
			return
		}
		// Verify location args.
		lx, _ := req2.Args["location_x"].(float64)
		ly, _ := req2.Args["location_y"].(float64)
		name, _ := req2.Args["name"].(string)
		if lx != 249 || ly != 352 {
			t.Errorf("purchase-lot args: want location_x=249 location_y=352, got %v %v", lx, ly)
		}
		if name != "marlo's place" {
			t.Errorf("purchase-lot name: want %q, got %q", "marlo's place", name)
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req2.CorrelationID,
			"ok":             true,
			"data": map[string]any{
				"status":    "SUCCESS",
				"lot_id":    float64(99),
				"new_funds": float64(950000),
			},
		}))
	}()

	handler := purchaseLotHandler(pump)
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": targetLocHex,
		"name":                "marlo's place",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil {
		t.Fatal("nil payload")
	}

	// --- Verify response shape ---
	if payload["ok"] != true {
		t.Errorf("want ok=true: %v", payload)
	}
	// lot_id and new_funds are int64 in the Go response (handler uses int64 fields).
	// road_bits and x/y are int.
	if lotID, _ := payload["lot_id"].(int64); lotID != 99 {
		t.Errorf("lot_id: want 99, got %v (type %T)", payload["lot_id"], payload["lot_id"])
	}
	if newFunds, _ := payload["new_funds"].(int64); newFunds != 950000 {
		t.Errorf("new_funds: want 950000, got %v (type %T)", payload["new_funds"], payload["new_funds"])
	}
	if payload["location_hex"] != "0x00F90160" {
		t.Errorf("location_hex: want 0x00F90160, got %v", payload["location_hex"])
	}
	if roadBits, _ := payload["road_bits"].(int); roadBits != 9 {
		t.Errorf("road_bits: want 9, got %v (type %T)", payload["road_bits"], payload["road_bits"])
	}
	if payload["name"] != "marlo's place" {
		t.Errorf("name: want %q, got %v", "marlo's place", payload["name"])
	}

	// --- Verify owned-lots.json was written ---
	dir := filepath.Join(tmp, "freeso-souls", "purchase-lot-test-success")
	ownedPath := filepath.Join(dir, "owned-lots.json")
	data, readErr := os.ReadFile(ownedPath)
	if readErr != nil {
		t.Fatalf("owned-lots.json not written (I0-7 invariant violated): %v", readErr)
	}
	var entries []OwnedLotEntry
	if err := json.Unmarshal(data, &entries); err != nil {
		t.Fatalf("parse owned-lots.json: %v", err)
	}
	if len(entries) != 1 {
		t.Fatalf("expected 1 entry in owned-lots.json, got %d", len(entries))
	}
	entry := entries[0]
	if entry.LocationHex != "0x00F90160" {
		t.Errorf("entry.LocationHex: want 0x00F90160, got %q", entry.LocationHex)
	}
	if entry.Name != "marlo's place" {
		t.Errorf("entry.Name: want %q, got %q", "marlo's place", entry.Name)
	}

	// --- Verify habitation block is zeroed (I0-7) ---
	if entry.Habitation.FirstMealEatenHere != nil {
		t.Errorf("I0-7 violated: first_meal_eaten_here should be nil at purchase time")
	}
	if entry.Habitation.FirstSleepHere != nil {
		t.Errorf("I0-7 violated: first_sleep_here should be nil at purchase time")
	}
	if entry.Habitation.FirstUseToiletHere != nil {
		t.Errorf("I0-7 violated: first_use_toilet_here should be nil at purchase time")
	}
	if entry.IsHabitable {
		t.Errorf("I0-7 violated: is_habitable should be false at purchase time")
	}
	t.Logf("owned-lots.json entry: name=%q location=%q purchased_at=%d habitation=%+v",
		entry.Name, entry.LocationHex, entry.PurchasedAt, entry.Habitation)
}

// TestPurchaseLotHandlerServerRefuseNhoodReserved asserts that a
// NHOOD_RESERVED server refuse is surfaced with escalation hint.
func TestPurchaseLotHandlerServerRefuseNhoodReserved(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "purchase-lot-test-nhood")
	withConfigHome(t, tmp)

	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	go func() {
		// Frame 1: probe-road → has_road=true
		line1 := <-fake.stdinLines
		var req1 BotCmdRequest
		if err := json.Unmarshal(line1, &req1); err != nil {
			t.Errorf("probe-road unmarshal: %v", err)
			return
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req1.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"has_road": true, "road_bits": 9, "x": 249, "y": 348},
		}))

		// Frame 2: purchase-lot → FAILED with NHOOD_RESERVED
		line2 := <-fake.stdinLines
		var req2 BotCmdRequest
		if err := json.Unmarshal(line2, &req2); err != nil {
			t.Errorf("purchase-lot unmarshal: %v", err)
			return
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req2.CorrelationID,
			"ok":             true,
			"data": map[string]any{
				"status":    "FAILED",
				"reason":    "NHOOD_RESERVED",
				"lot_id":    float64(0),
				"new_funds": float64(1000000),
			},
		}))
	}()

	handler := purchaseLotHandler(pump)
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x00F9015C",
		"name":                "blocked lot",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for NHOOD_RESERVED, got %v", payload)
	}
	if payload["reason"] != "NHOOD_RESERVED" {
		t.Errorf("want reason=NHOOD_RESERVED, got %v", payload["reason"])
	}
	hint, _ := payload["hint"].(string)
	if hint == "" {
		t.Error("want non-empty escalation hint for NHOOD_RESERVED")
	}
	// Verify owned-lots.json was NOT updated (failed purchase).
	dir := filepath.Join(tmp, "freeso-souls", "purchase-lot-test-nhood")
	ownedPath := filepath.Join(dir, "owned-lots.json")
	if _, err := os.Stat(ownedPath); err == nil {
		t.Error("owned-lots.json should NOT be written on a failed purchase")
	}
	t.Logf("NHOOD_RESERVED hint: %s", hint)
}

// TestPurchaseLotHandlerNoBotMode asserts that a nil botCmds returns a
// deferred marker rather than panicking.
func TestPurchaseLotHandlerNoBotMode(t *testing.T) {
	handler := purchaseLotHandler(nil) // nil = --no-bot mode
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x00F9015C",
		"name":                "test lot",
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

// TestOwnedLotsRoundTrip tests the ReadOwnedLots / WriteOwnedLots /
// AppendOwnedLot round-trip in persona_state.go (I0-7 backing store).
func TestOwnedLotsRoundTrip(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "owned-lots-rt")
	withConfigHome(t, tmp)

	// 1. Empty state: ReadOwnedLots returns empty slice.
	entries, err := ReadOwnedLots()
	if err != nil {
		t.Fatalf("ReadOwnedLots empty: %v", err)
	}
	if len(entries) != 0 {
		t.Fatalf("want 0 entries, got %d", len(entries))
	}

	// 2. AppendOwnedLot writes the first entry.
	now := time.Now().UnixMilli()
	first := OwnedLotEntry{
		Name:        "marlo's place",
		LocationHex: "0x00F9015C",
		PurchasedAt: now,
	}
	if err := AppendOwnedLot(first); err != nil {
		t.Fatalf("AppendOwnedLot: %v", err)
	}

	// 3. ReadOwnedLots returns 1 entry with correct fields.
	entries, err = ReadOwnedLots()
	if err != nil {
		t.Fatalf("ReadOwnedLots after first write: %v", err)
	}
	if len(entries) != 1 {
		t.Fatalf("want 1 entry, got %d", len(entries))
	}
	if entries[0].Name != "marlo's place" {
		t.Errorf("name: want %q, got %q", "marlo's place", entries[0].Name)
	}
	if entries[0].Habitation.FirstMealEatenHere != nil {
		t.Error("habitation.first_meal_eaten_here should be nil")
	}
	if entries[0].IsHabitable {
		t.Error("is_habitable should be false")
	}

	// 4. AppendOwnedLot with same LocationHex replaces the entry (allow_move pattern).
	updated := OwnedLotEntry{
		Name:        "marlo's place v2",
		LocationHex: "0x00F9015C",
		PurchasedAt: now + 1000,
	}
	if err := AppendOwnedLot(updated); err != nil {
		t.Fatalf("AppendOwnedLot (replace): %v", err)
	}
	entries, err = ReadOwnedLots()
	if err != nil {
		t.Fatalf("ReadOwnedLots after replace: %v", err)
	}
	if len(entries) != 1 {
		t.Fatalf("want 1 entry after replace, got %d", len(entries))
	}
	if entries[0].Name != "marlo's place v2" {
		t.Errorf("replaced name: want %q, got %q", "marlo's place v2", entries[0].Name)
	}

	// 5. AppendOwnedLot with different LocationHex adds a second entry.
	second := OwnedLotEntry{
		Name:        "jin's corner",
		LocationHex: "0x00FA0200",
		PurchasedAt: now + 2000,
	}
	if err := AppendOwnedLot(second); err != nil {
		t.Fatalf("AppendOwnedLot (second): %v", err)
	}
	entries, err = ReadOwnedLots()
	if err != nil {
		t.Fatalf("ReadOwnedLots after second: %v", err)
	}
	if len(entries) != 2 {
		t.Fatalf("want 2 entries, got %d", len(entries))
	}
}

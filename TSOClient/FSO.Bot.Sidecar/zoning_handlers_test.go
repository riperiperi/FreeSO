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

// zoningTestFile returns a temp zoning file path for test isolation.
// Tests that write zoning.json must restore zoningFilePath after they're done.
func withZoningFile(t *testing.T) (path string) {
	t.Helper()
	tmp := t.TempDir()
	path = filepath.Join(tmp, "zoning.json")
	// Patch the package-level zoningFilePath for this test.
	// This works because tests run in the same process and package.
	// We use t.Cleanup to restore.
	original := zoningFilePath
	// Go doesn't let us assign to a const, so we use an indirect approach:
	// override via the module-level variable (zoningFilePath is a const —
	// we need to make it a var for test isolation).
	// Since zoningFilePath is a const, we test via the exported functions
	// but write to the test temp path.
	// For test isolation, we write the temp path to the zoningFilePath const
	// indirectly by calling writeZoning/readZoning with the real path.
	// WORKAROUND: Override by setting env var or using the real path.
	// The simplest approach: accept the const path and clean up after the test.
	_ = original // const can't be reassigned — tests use the real path with cleanup
	return path
}

// TestSetZoningMissingZoneType asserts that a missing zone_type returns INVALID_ZONE_TYPE.
func TestSetZoningMissingZoneType(t *testing.T) {
	os.Setenv("FSO_MAYOR_NHOOD", "1")
	t.Cleanup(func() { os.Unsetenv("FSO_MAYOR_NHOOD") })

	handler := setZoningHandler(nil)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"x1": float64(100), "y1": float64(100), "x2": float64(200), "y2": float64(200),
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for missing zone_type, got %v", payload)
	}
	if payload["reason"] != "INVALID_ZONE_TYPE" {
		t.Errorf("want reason=INVALID_ZONE_TYPE, got %v", payload["reason"])
	}
}

// TestSetZoningInvalidZoneType asserts that an unknown zone_type returns INVALID_ZONE_TYPE.
func TestSetZoningInvalidZoneType(t *testing.T) {
	os.Setenv("FSO_MAYOR_NHOOD", "1")
	t.Cleanup(func() { os.Unsetenv("FSO_MAYOR_NHOOD") })

	handler := setZoningHandler(nil)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"zone_type": "industrial",
		"x1": float64(100), "y1": float64(100), "x2": float64(200), "y2": float64(200),
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for invalid zone_type, got %v", payload)
	}
	if payload["reason"] != "INVALID_ZONE_TYPE" {
		t.Errorf("want reason=INVALID_ZONE_TYPE, got %v", payload["reason"])
	}
}

// TestSetZoningInvalidBounds asserts that x2 < x1 or y2 < y1 returns INVALID_BOUNDS.
func TestSetZoningInvalidBounds(t *testing.T) {
	os.Setenv("FSO_MAYOR_NHOOD", "1")
	t.Cleanup(func() { os.Unsetenv("FSO_MAYOR_NHOOD") })

	cases := []map[string]any{
		{"x1": float64(200), "y1": float64(100), "x2": float64(100), "y2": float64(200)}, // x2 < x1
		{"x1": float64(100), "y1": float64(200), "x2": float64(200), "y2": float64(100)}, // y2 < y1
	}
	for _, bounds := range cases {
		bounds["zone_type"] = "residential"
		handler := setZoningHandler(nil)
		ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
		defer cancel()

		resp, err := handler(ctx, &convention.Request{Args: bounds})
		if err != nil {
			t.Fatalf("handler error: %v", err)
		}
		payload, _ := resp.Payload.(map[string]any)
		if payload["ok"] != false {
			t.Errorf("want ok=false for invalid bounds %v, got %v", bounds, payload)
		}
		if payload["reason"] != "INVALID_BOUNDS" {
			t.Errorf("want reason=INVALID_BOUNDS, got %v", payload["reason"])
		}
	}
}

// TestSetZoningNoAuth asserts that a non-mayor (FSO_MAYOR_NHOOD unset) gets NO_AUTH.
func TestSetZoningNoAuth(t *testing.T) {
	os.Unsetenv("FSO_MAYOR_NHOOD")

	handler := setZoningHandler(nil)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"zone_type": "residential",
		"x1": float64(100), "y1": float64(100), "x2": float64(200), "y2": float64(200),
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for non-mayor, got %v", payload)
	}
	if payload["reason"] != "NO_AUTH" {
		t.Errorf("want reason=NO_AUTH, got %v", payload["reason"])
	}
}

// TestCheckZoningViolation_ParkBlocks is the ZONING_VIOLATION falsifying test.
//
// Protocol:
//  1. Write a park zone covering tiles (250,350)–(255,355) to a temp file.
//     (Can't override zoningFilePath const, so write to the real path then clean up.)
//  2. Call CheckZoningViolation for a tile inside the park.
//  3. Assert violated=true, reason contains "ZONING_VIOLATION".
//  4. Call CheckZoningViolation for a tile outside the park.
//  5. Assert violated=false.
//
// This is the felt-purchase-veto test: a purchase attempt into a park zone
// must be refused, which is observable by the calling agent (they see
// ZONING_VIOLATION instead of a lot creation).
func TestCheckZoningViolation_ParkBlocks(t *testing.T) {
	// Backup and restore the existing zoning.json (if any).
	existingData, existingErr := os.ReadFile(zoningFilePath)
	t.Cleanup(func() {
		if existingErr == nil {
			_ = os.WriteFile(zoningFilePath, existingData, 0o644)
		} else {
			_ = os.Remove(zoningFilePath)
		}
	})

	// Write a test park zone.
	if err := os.MkdirAll(filepath.Dir(zoningFilePath), 0o755); err != nil {
		t.Fatalf("mkdir civic dir: %v", err)
	}
	testZones := []ZoneEntry{
		{ZoneType: "park", X1: 250, Y1: 350, X2: 255, Y2: 355, ZoneName: "Town Green", CreatedAt: 1000},
	}
	if err := writeZoning(testZones); err != nil {
		t.Fatalf("writeZoning: %v", err)
	}

	// Tile inside the park: (252, 352) — should be violated.
	violated, reason := CheckZoningViolation(252, 352, "residential")
	if !violated {
		t.Error("ZONING_VIOLATION FALSIFIED: expected violated=true for tile (252,352) inside park zone")
	}
	if reason == "" {
		t.Error("expected non-empty reason for park zone violation")
	}
	t.Logf("park violation: %q", reason)

	// Tile outside the park: (100, 100) — should be ok.
	violated2, reason2 := CheckZoningViolation(100, 100, "residential")
	if violated2 {
		t.Errorf("expected violated=false for tile (100,100) outside park zone, got reason=%q", reason2)
	}

	// Tile at exact boundary: (250, 350) — inside (inclusive boundary).
	violated3, reason3 := CheckZoningViolation(250, 350, "residential")
	if !violated3 {
		t.Error("expected violated=true for tile at park boundary (250,350)")
	}
	t.Logf("boundary violation: %q", reason3)

	// Tile just outside: (249, 350) — outside (x < x1=250).
	violated4, _ := CheckZoningViolation(249, 350, "residential")
	if violated4 {
		t.Error("expected violated=false for tile just outside park (249,350)")
	}
}

// TestCheckZoningViolation_ResidentialAllows asserts that a residential zone
// allows residential purchases but blocks commercial.
func TestCheckZoningViolation_ResidentialAllows(t *testing.T) {
	existingData, existingErr := os.ReadFile(zoningFilePath)
	t.Cleanup(func() {
		if existingErr == nil {
			_ = os.WriteFile(zoningFilePath, existingData, 0o644)
		} else {
			_ = os.Remove(zoningFilePath)
		}
	})

	if err := os.MkdirAll(filepath.Dir(zoningFilePath), 0o755); err != nil {
		t.Fatalf("mkdir: %v", err)
	}
	if err := writeZoning([]ZoneEntry{
		{ZoneType: "residential", X1: 200, Y1: 300, X2: 260, Y2: 400},
	}); err != nil {
		t.Fatalf("writeZoning: %v", err)
	}

	// Residential lot in residential zone: OK.
	violated, _ := CheckZoningViolation(230, 350, "residential")
	if violated {
		t.Error("expected violated=false for residential lot in residential zone")
	}

	// Commercial lot in residential zone: VIOLATION.
	violated2, reason2 := CheckZoningViolation(230, 350, "commercial")
	if !violated2 {
		t.Error("expected violated=true for commercial lot in residential zone")
	}
	t.Logf("commercial-in-residential violation: %q", reason2)

	// Empty lotType ("") defaults to residential: OK.
	violated3, _ := CheckZoningViolation(230, 350, "")
	if violated3 {
		t.Error("expected violated=false for empty lotType (defaults to residential) in residential zone")
	}
}

// TestCheckZoningViolation_NoZoning asserts that CheckZoningViolation is a no-op
// when zoning.json is absent (fail-open).
func TestCheckZoningViolation_NoZoning(t *testing.T) {
	existingData, existingErr := os.ReadFile(zoningFilePath)
	t.Cleanup(func() {
		if existingErr == nil {
			_ = os.WriteFile(zoningFilePath, existingData, 0o644)
		} else {
			_ = os.Remove(zoningFilePath)
		}
	})

	// Remove zoning file entirely.
	_ = os.Remove(zoningFilePath)

	violated, reason := CheckZoningViolation(100, 100, "residential")
	if violated {
		t.Errorf("expected violated=false when no zoning.json, got reason=%q", reason)
	}
}

// TestZoningRoundTrip tests the readZoning / writeZoning / ZoneEntry round-trip.
func TestZoningRoundTrip(t *testing.T) {
	existingData, existingErr := os.ReadFile(zoningFilePath)
	t.Cleanup(func() {
		if existingErr == nil {
			_ = os.WriteFile(zoningFilePath, existingData, 0o644)
		} else {
			_ = os.Remove(zoningFilePath)
		}
	})

	_ = os.Remove(zoningFilePath) // start clean

	// Empty: readZoning returns empty slice.
	zones, err := readZoning()
	if err != nil {
		t.Fatalf("readZoning empty: %v", err)
	}
	if len(zones) != 0 {
		t.Fatalf("want 0 zones, got %d", len(zones))
	}

	// Write two zones.
	toWrite := []ZoneEntry{
		{ZoneType: "residential", X1: 100, Y1: 100, X2: 200, Y2: 200},
		{ZoneType: "commercial", X1: 50, Y1: 50, X2: 90, Y2: 90, ZoneName: "Market"},
	}
	if err := os.MkdirAll(filepath.Dir(zoningFilePath), 0o755); err != nil {
		t.Fatalf("mkdir: %v", err)
	}
	if err := writeZoning(toWrite); err != nil {
		t.Fatalf("writeZoning: %v", err)
	}

	zones, err = readZoning()
	if err != nil {
		t.Fatalf("readZoning: %v", err)
	}
	if len(zones) != 2 {
		t.Fatalf("want 2 zones, got %d", len(zones))
	}
	if zones[0].ZoneType != "residential" {
		t.Errorf("zones[0].ZoneType: want 'residential', got %q", zones[0].ZoneType)
	}
	if zones[1].ZoneName != "Market" {
		t.Errorf("zones[1].ZoneName: want 'Market', got %q", zones[1].ZoneName)
	}
}

// TestPurchaseLotHandlerZoningViolation verifies that the purchase-lot handler
// refuses a purchase into a park zone with ZONING_VIOLATION.
//
// This is the falsifying test for the zoning-purchase integration:
// mayor sets park zone covering tile (252,352), agent tries to purchase at
// that tile, must get ZONING_VIOLATION (not a successful purchase).
func TestPurchaseLotHandlerZoningViolation(t *testing.T) {
	existingData, existingErr := os.ReadFile(zoningFilePath)
	t.Cleanup(func() {
		if existingErr == nil {
			_ = os.WriteFile(zoningFilePath, existingData, 0o644)
		} else {
			_ = os.Remove(zoningFilePath)
		}
	})

	tmp := t.TempDir()
	withFSO_USER(t, "purchase-lot-zoning-test")
	withConfigHome(t, tmp)

	// Write park zone covering tile x=252, y=352 → packed location 0x00FC0160.
	// (x=252 << 16 | y=352) = 0x00FC0160 = 16515424 decimal
	if err := os.MkdirAll(filepath.Dir(zoningFilePath), 0o755); err != nil {
		t.Fatalf("mkdir: %v", err)
	}
	if err := writeZoning([]ZoneEntry{
		{ZoneType: "park", X1: 250, Y1: 350, X2: 255, Y2: 355, ZoneName: "Town Green"},
	}); err != nil {
		t.Fatalf("writeZoning: %v", err)
	}

	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	// Stub: probe-road returns has_road=true (so we get past road check).
	// The zoning check should fire before the purchase-lot bot-cmd.
	go func() {
		line := <-fake.stdinLines
		var req BotCmdRequest
		if err := unmarshalZoning(line, &req); err != nil {
			t.Errorf("unmarshal req: %v", err)
			return
		}
		if req.Cmd != "probe-road" {
			t.Errorf("want cmd=probe-road first (before zoning check), got %q", req.Cmd)
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             true,
			"data":           map[string]any{"has_road": true, "road_bits": 9, "x": 252, "y": 352},
		}))

		// Any subsequent lines indicate the handler proceeded to purchase-lot, which is wrong.
		select {
		case unexpected := <-fake.stdinLines:
			var unexpReq BotCmdRequest
			_ = unmarshalZoning(unexpected, &unexpReq)
			t.Errorf("handler should NOT reach purchase-lot after zoning violation, but got cmd=%q", unexpReq.Cmd)
		case <-time.After(200 * time.Millisecond):
			// Good: no further bot-cmd issued after zoning violation.
		}
	}()

	handler := purchaseLotHandler(pump)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	// Target: x=252, y=352 → packed = (252 << 16) | 352 = 16515424 = 0x00FC0160
	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x00FC0160",
		"name":                "my park home",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for ZONING_VIOLATION, got %v", payload)
	}
	if payload["reason"] != "ZONING_VIOLATION" {
		t.Errorf("want reason=ZONING_VIOLATION, got %v", payload["reason"])
	}
	t.Logf("zoning violation response: %v", payload)
}

// unmarshalZoning is a test helper to unmarshal JSON into v and return an error.
// Named to avoid collision with other test file helpers in the same package.
// Note: mustMarshal is defined in purchase_lot_handlers_test.go (same package).
func unmarshalZoning(data []byte, v any) error {
	return json.Unmarshal(data, v)
}

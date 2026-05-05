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

// TestSetTaxRateMissingRate asserts that a call without tax_rate_percent returns ok:false.
func TestSetTaxRateMissingRate(t *testing.T) {
	handler := setTaxRateHandler()
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for missing tax_rate_percent, got %v", payload)
	}
}

// TestSetTaxRateInvalidRate asserts that out-of-range rates return ok:false.
func TestSetTaxRateInvalidRate(t *testing.T) {
	handler := setTaxRateHandler()
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	for _, rate := range []float64{-1.0, 101.0, 200.0} {
		resp, err := handler(ctx, &convention.Request{Args: map[string]any{
			"tax_rate_percent": rate,
		}})
		if err != nil {
			t.Fatalf("rate=%.1f: handler error: %v", rate, err)
		}
		payload, _ := resp.Payload.(map[string]any)
		if payload["ok"] != false {
			t.Errorf("rate=%.1f: want ok=false for invalid rate, got %v", rate, payload)
		}
		if payload["reason"] != "INVALID_TAX_RATE" {
			t.Errorf("rate=%.1f: want reason=INVALID_TAX_RATE, got %v", rate, payload["reason"])
		}
	}
}

// TestSetTaxRateStubSuccessPath verifies that a valid rate returns ok=true with
// deferred=true and the recorded rate. No bot-cmd or tax collection happens
// (freesoexperiment-ea0: stub, pending engine-side TaxCycleHandler M2/M3).
func TestSetTaxRateStubSuccessPath(t *testing.T) {
	handler := setTaxRateHandler()
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
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
		t.Errorf("want ok=true for valid rate, got %v", payload)
	}
	if payload["deferred"] != true {
		t.Errorf("want deferred=true (stub), got %v", payload["deferred"])
	}
	// Verify the recorded rate is echoed back.
	rate, _ := payload["tax_rate_percent"].(float64)
	if rate != 10.0 {
		t.Errorf("want tax_rate_percent=10.0, got %v", rate)
	}
}

// TestSetTaxRateZeroPercent verifies that 0% is a valid rate (no tax, but still accepted).
func TestSetTaxRateZeroPercent(t *testing.T) {
	handler := setTaxRateHandler()
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"tax_rate_percent": float64(0),
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != true {
		t.Errorf("want ok=true for 0%% rate, got %v", payload)
	}
}

// TestSetTaxRateDefaultNeighborhoodID verifies that neighborhood_id defaults to 1
// when not provided.
func TestSetTaxRateDefaultNeighborhoodID(t *testing.T) {
	handler := setTaxRateHandler()
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"tax_rate_percent": float64(5),
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != true {
		t.Fatalf("want ok=true: %v", payload)
	}
	nhoodID, _ := payload["neighborhood_id"].(uint64)
	if nhoodID != 1 {
		t.Errorf("want neighborhood_id=1 (default), got %v", payload["neighborhood_id"])
	}
}

// TestCoerceFloat64 unit-tests the float64 coercion helper.
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

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

// TestRateLimitSoul verifies that the soul-level rate limit (1 per 10s) works.
func TestRateLimitSoul(t *testing.T) {
	rl := &screenshotRateLimiter{
		souls: make(map[string][]time.Time),
		lots:  make(map[string][]time.Time),
	}

	// First request should pass
	if !rl.check("soul1", "lot1") {
		t.Error("first request denied")
	}

	// Second request immediately after should fail
	if rl.check("soul1", "lot1") {
		t.Error("second request allowed within 10s window")
	}

	// After 10s + 1ms, should pass again
	time.Sleep(100 * time.Millisecond) // Stub; real test would time-mock
	// For a real test, we'd mock time or patch the rateLimiter's clock
}

// TestRateLimitLot verifies that the lot-level rate limit (10 per 60s) works.
func TestRateLimitLot(t *testing.T) {
	rl := &screenshotRateLimiter{
		souls: make(map[string][]time.Time),
		lots:  make(map[string][]time.Time),
	}

	// 10 requests from different souls on the same lot should all pass
	for i := 0; i < 10; i++ {
		soul := "soul" + string(rune(i))
		if !rl.check(soul, "lot1") {
			t.Errorf("request %d denied", i)
		}
	}

	// 11th request should fail
	if rl.check("soul10", "lot1") {
		t.Error("11th request allowed despite lot limit")
	}
}

// TestMapZoom verifies that the zoom vocabulary translation is correct.
// freesoexperiment-b85: "medium" was forwarded verbatim; renderer only accepts far/med/near.
func TestMapZoom(t *testing.T) {
	cases := []struct {
		in   string
		want string
	}{
		{"small", "far"},
		{"medium", "med"},
		{"large", "near"},
		// Renderer-native values pass through unchanged.
		{"far", "far"},
		{"med", "med"},
		{"near", "near"},
		// Unknown values fall back to "far".
		{"", "far"},
		{"huge", "far"},
	}
	for _, c := range cases {
		got := mapZoom(c.in)
		if got != c.want {
			t.Errorf("mapZoom(%q) = %q, want %q", c.in, got, c.want)
		}
	}
}

// TestTakeScreenshotNotPermitted verifies that the permission gate (d49) rejects
// a caller who is not the lot owner, not a roommate, and not the mayor.
//
// Strategy: invoke the real takeScreenshotHandler function with a fake IPC that
// returns a query-lot payload where owner_is_me=false and is_roommate=false.
// The augmentor has IsMayor=false (zero value). The handler must return
// ok=false with reason="NOT_PERMITTED" before reaching the renderer HTTP POST.
func TestTakeScreenshotNotPermitted(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// Wire query-lot to return a lot where the caller is neither owner nor roommate.
	// The handler reads owner_is_me and is_roommate from the inner "payload" map.
	responses := map[string]map[string]any{
		"query-lot": {
			"ok": true,
			"payload": map[string]any{
				"lot_id":      float64(42),
				"owner_is_me": false,
				"is_roommate": false,
				"name":        "SomeLot",
			},
		},
	}
	multiAutoResponder(t, fake, ipc, responses)

	// Augmentor with IsMayor=false (the zero value — no mayor tick cached).
	augmentor := &PerceptionAugmentor{}

	handler := takeScreenshotHandler(ipc, augmentor)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Sender: "test-non-roommate",
		Args:   map[string]any{},
	})
	if err != nil {
		t.Fatalf("handler returned error: %v", err)
	}
	if resp == nil {
		t.Fatal("handler returned nil response")
	}

	payload, ok := resp.Payload.(map[string]any)
	if !ok {
		t.Fatalf("payload is not map[string]any: %T", resp.Payload)
	}

	// Must be denied.
	if payload["ok"] != false {
		t.Errorf("expected ok=false for non-permitted caller, got ok=%v (full payload: %v)", payload["ok"], payload)
	}
	reason, _ := payload["reason"].(string)
	if reason != "NOT_PERMITTED" {
		t.Errorf("expected reason=NOT_PERMITTED, got %q (full payload: %v)", reason, payload)
	}
}

// TestTakeScreenshotRateLimited verifies that the per-soul rate limit is
// enforced: a second request from the same soul within 10 seconds is denied.
//
// Strategy: invoke the real takeScreenshotHandler function twice. The first
// call uses a fresh rate-limiter state and a query-lot that grants permission
// (owner_is_me=true). After that first call records a timestamp in the global
// rateLimiter, a second call must be denied with reason="RATE_LIMITED".
//
// We use a private rateLimiter instance (not the global one) to avoid
// cross-test interference. The handler's rateLimiter reference is the global
// package-level variable; we swap it temporarily under the test and restore it
// via t.Cleanup.
func TestTakeScreenshotRateLimited(t *testing.T) {
	// Replace the global rateLimiter with a fresh instance for test isolation.
	origRL := rateLimiter
	testRL := &screenshotRateLimiter{
		souls: make(map[string][]time.Time),
		lots:  make(map[string][]time.Time),
	}
	rateLimiter = testRL
	t.Cleanup(func() { rateLimiter = origRL })

	// Pre-seed the rate-limiter so the soul "rl-test-soul" already has one
	// request recorded within the last 10 seconds. This simulates the case where
	// a first screenshot was already taken, causing the next call to be denied.
	testRL.mu.Lock()
	testRL.souls["rl-test-soul"] = []time.Time{time.Now()}
	testRL.mu.Unlock()

	// Wire query-lot to return a permitted lot (owner_is_me=true) so that the
	// handler passes the auth gate and reaches the rate-limit check.
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	responses := map[string]map[string]any{
		"query-lot": {
			"ok": true,
			"payload": map[string]any{
				"lot_id":      float64(7),
				"owner_is_me": true,
				"is_roommate": false,
				"name":        "MyLot",
			},
		},
	}
	multiAutoResponder(t, fake, ipc, responses)

	augmentor := &PerceptionAugmentor{}

	handler := takeScreenshotHandler(ipc, augmentor)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Sender: "rl-test-soul",
		Args:   map[string]any{},
	})
	if err != nil {
		t.Fatalf("handler returned error: %v", err)
	}
	if resp == nil {
		t.Fatal("handler returned nil response")
	}

	payload, ok := resp.Payload.(map[string]any)
	if !ok {
		t.Fatalf("payload is not map[string]any: %T", resp.Payload)
	}

	// Must be rate-limited.
	if payload["ok"] != false {
		t.Errorf("expected ok=false for rate-limited caller, got ok=%v (full payload: %v)", payload["ok"], payload)
	}
	reason, _ := payload["reason"].(string)
	if reason != "RATE_LIMITED" {
		t.Errorf("expected reason=RATE_LIMITED, got %q (full payload: %v)", reason, payload)
	}
}

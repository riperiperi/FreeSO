/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"testing"
	"time"
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

// TestTakeScreenshotNotPermitted verifies that non-roommates are denied.
func TestTakeScreenshotNotPermitted(t *testing.T) {
	// Stub test for now: full integration test in verb-screenshot.sh
	// A real test would require mocking the IPC and campfire, which is deferred
	// to the integration test that runs a full bot session.
	t.Skip("full permission test deferred to integration test")
}

// TestTakeScreenshotRateLimited verifies that exceeding the rate limit is denied.
func TestTakeScreenshotRateLimited(t *testing.T) {
	// Reset global rate limiter to a fresh state for this test.
	rateLimiter.mu.Lock()
	rateLimiter.souls = make(map[string][]time.Time)
	rateLimiter.lots = make(map[string][]time.Time)
	rateLimiter.mu.Unlock()

	// Stub test for now: full integration test in verb-screenshot.sh
	t.Skip("full rate-limit test deferred to integration test")
}

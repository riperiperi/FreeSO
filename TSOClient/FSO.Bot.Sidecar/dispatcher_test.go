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

// TestRouterSubscribeRequest_ExcludesBridgeBroadcasts asserts the Router's
// subscription filter excludes the three bridge-broadcast tags
// (freeso:perception, freeso:dialog, freeso:system). Without these
// excludes the Router would re-ingest every perception tick (1Hz per sim)
// and emit `router: no handler registered for op "perception"` as a
// convention:error message — which then floods the campfire and pollutes
// every cf read.
//
// Pinning this in a test prevents a future refactor from accidentally
// removing the exclusion. The bug was discovered in a live session; we
// don't want it to come back silently.
func TestRouterSubscribeRequest_ExcludesBridgeBroadcasts(t *testing.T) {
	r := NewRouter()
	req := r.routerSubscribeRequest("cf-id")
	want := []string{"fulfills", "convention:operation", "freeso:perception", "freeso:dialog", "freeso:system"}
	for _, w := range want {
		found := false
		for _, got := range req.ExcludeTags {
			if got == w {
				found = true
				break
			}
		}
		if !found {
			t.Errorf("ExcludeTags missing %q (got %v)", w, req.ExcludeTags)
		}
	}
}

// TestRouterSubscribeRequest_PollIntervalSane: 250ms is the chosen default —
// fast enough that build-loop agents don't notice the receive-side wait,
// cheap enough that SQLite read cost stays in the negligible regime. If
// someone bumps this back to 2s+ "for safety," builds regress to 5s+
// round-trips; we want a test that fails loudly.
func TestRouterSubscribeRequest_PollIntervalSane(t *testing.T) {
	r := NewRouter()
	req := r.routerSubscribeRequest("cf-id")
	if req.PollInterval <= 0 {
		t.Errorf("PollInterval = %v; must be positive", req.PollInterval)
	}
	if req.PollInterval > 500*time.Millisecond {
		t.Errorf("PollInterval = %v; want <= 500ms for tight build loops (was 2s before robustness pass; raising it again would regress per-op latency)", req.PollInterval)
	}
}

// TestRouterSubscribeRequest_FreesoPrefix: defensive — the freeso: prefix
// is what links bridge broadcasts to op-dispatch; if it changes, both the
// dispatcher and the bridges/declaration publishing layer have to move in
// lockstep. A failing assertion here is the canary for that drift.
func TestRouterSubscribeRequest_FreesoPrefix(t *testing.T) {
	r := NewRouter()
	req := r.routerSubscribeRequest("cf-id")
	if len(req.TagPrefixes) == 0 || req.TagPrefixes[0] != "freeso:" {
		t.Errorf("TagPrefixes = %v; want exactly [\"freeso:\"] as the first entry", req.TagPrefixes)
	}
}

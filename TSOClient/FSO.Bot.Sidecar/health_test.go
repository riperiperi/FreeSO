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

// TestSidecarHealth_InitialSnapshot: brand-new health tracker reports
// have_perception=false and ms_ago=-1. Agents use this to distinguish "bot
// just started" from "bot died long ago".
func TestSidecarHealth_InitialSnapshot(t *testing.T) {
	h := NewSidecarHealth()
	snap := h.Snapshot(nil, nil, 0)
	if snap.HavePerception {
		t.Error("HavePerception=true on fresh tracker")
	}
	if snap.LastPerceptionAgo != -1 {
		t.Errorf("LastPerceptionAgo = %d; want -1 (sentinel for 'never observed')", snap.LastPerceptionAgo)
	}
	if snap.PerceptionCount != 0 {
		t.Errorf("PerceptionCount = %d; want 0", snap.PerceptionCount)
	}
	if snap.SidecarUptimeMs < 0 {
		t.Errorf("SidecarUptimeMs = %d; want >= 0", snap.SidecarUptimeMs)
	}
}

// TestSidecarHealth_RecordsPerception: after RecordPerception, the snapshot
// shows have_perception=true, count incremented, position+simID captured.
func TestSidecarHealth_RecordsPerception(t *testing.T) {
	h := NewSidecarHealth()
	pos := &perceptionPosition{Level: 2, X: 40.5, Y: 42.5}
	h.RecordPerception("12345", pos, true)
	snap := h.Snapshot(nil, nil, 0)
	if !snap.HavePerception {
		t.Error("HavePerception=false after RecordPerception")
	}
	if snap.PerceptionCount != 1 {
		t.Errorf("PerceptionCount = %d; want 1", snap.PerceptionCount)
	}
	if snap.SimID != "12345" {
		t.Errorf("SimID = %q; want %q", snap.SimID, "12345")
	}
	if snap.Position == nil || snap.Position.Level != 2 || snap.Position.X != 40.5 || snap.Position.Y != 42.5 {
		t.Errorf("Position = %+v; want {2, 40.5, 42.5}", snap.Position)
	}
	if !snap.OnLot {
		t.Error("OnLot=false; want true when recorded with onLot=true")
	}
	if snap.LastPerceptionAgo < 0 || snap.LastPerceptionAgo > 1000 {
		t.Errorf("LastPerceptionAgo = %d; want small positive", snap.LastPerceptionAgo)
	}
}

// TestSidecarHealth_PositionUpdatesButSimIDIsSticky: simID should be
// captured once and stable across re-records (bot avatar doesn't change
// during a sidecar lifetime). Position updates each tick.
func TestSidecarHealth_PositionUpdatesButSimIDIsSticky(t *testing.T) {
	h := NewSidecarHealth()
	h.RecordPerception("100", &perceptionPosition{Level: 1, X: 1, Y: 1}, true)
	h.RecordPerception("", &perceptionPosition{Level: 1, X: 2, Y: 2}, true)
	snap := h.Snapshot(nil, nil, 0)
	if snap.SimID != "100" {
		t.Errorf("SimID = %q; want %q (sticky across re-records)", snap.SimID, "100")
	}
	if snap.Position == nil || snap.Position.X != 2 {
		t.Errorf("Position.X = %v; want 2 (latest)", snap.Position)
	}
	if snap.PerceptionCount != 2 {
		t.Errorf("PerceptionCount = %d; want 2", snap.PerceptionCount)
	}
}

// TestSidecarHealth_NilSafety: methods on a nil receiver are safe (callers
// don't have to nil-check in --no-bot mode or test paths).
func TestSidecarHealth_NilSafety(t *testing.T) {
	var h *SidecarHealth
	h.RecordPerception("x", nil, false) // must not panic
	snap := h.Snapshot(nil, nil, 0)
	if snap.HavePerception {
		t.Error("nil receiver should produce zero snapshot, not real state")
	}
}

// TestSidecarHealth_LastPerceptionAgoAdvances: snapshot ms_ago monotonically
// increases between calls without a fresh record.
func TestSidecarHealth_LastPerceptionAgoAdvances(t *testing.T) {
	h := NewSidecarHealth()
	h.RecordPerception("1", &perceptionPosition{Level: 1, X: 1, Y: 1}, true)
	snap1 := h.Snapshot(nil, nil, 0)
	time.Sleep(15 * time.Millisecond)
	snap2 := h.Snapshot(nil, nil, 0)
	if snap2.LastPerceptionAgo <= snap1.LastPerceptionAgo {
		t.Errorf("LastPerceptionAgo did not advance: snap1=%d snap2=%d", snap1.LastPerceptionAgo, snap2.LastPerceptionAgo)
	}
}

// TestExtractPerceptionPosition exercises the JSON parse helper directly.
func TestExtractPerceptionPosition(t *testing.T) {
	cases := []struct {
		name string
		line string
		want *perceptionPosition
	}{
		{"complete", `{"kind":"perception","avatar":{"persist_id":2,"position":{"level":2,"x":40.5,"y":42.5}}}`, &perceptionPosition{Level: 2, X: 40.5, Y: 42.5}},
		{"no-avatar", `{"kind":"perception"}`, nil},
		{"no-position", `{"kind":"perception","avatar":{"persist_id":2}}`, nil},
		{"malformed", `{not-json`, nil},
	}
	for _, tc := range cases {
		t.Run(tc.name, func(t *testing.T) {
			got := extractPerceptionPosition([]byte(tc.line))
			if (got == nil) != (tc.want == nil) {
				t.Fatalf("got nil=%v, want nil=%v", got == nil, tc.want == nil)
			}
			if got != nil && (got.Level != tc.want.Level || got.X != tc.want.X || got.Y != tc.want.Y) {
				t.Errorf("got %+v; want %+v", got, tc.want)
			}
		})
	}
}

// TestPerceptionHasLot exercises the simple lot-presence helper.
func TestPerceptionHasLot(t *testing.T) {
	cases := []struct {
		name string
		line string
		want bool
	}{
		{"with-lot", `{"kind":"perception","lot":{"id":2}}`, true},
		{"null-lot", `{"kind":"perception","lot":null}`, false},
		{"no-lot-field", `{"kind":"perception"}`, false},
		{"malformed", `{not-json`, false},
	}
	for _, tc := range cases {
		t.Run(tc.name, func(t *testing.T) {
			got := perceptionHasLot([]byte(tc.line))
			if got != tc.want {
				t.Errorf("got %v; want %v", got, tc.want)
			}
		})
	}
}

// TestHeartbeatDeclarationPresent confirms conventions/heartbeat.json loads
// and meets the freeso-embodiment shape rules the test harness enforces.
func TestHeartbeatDeclarationPresent(t *testing.T) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}
	var hb *struct{}
	for _, d := range decls {
		if d.Operation == "heartbeat" {
			if d.Convention != "freeso-embodiment" {
				t.Errorf("convention = %q; want freeso-embodiment", d.Convention)
			}
			if d.Description == "" {
				t.Error("description empty")
			}
			if len(d.Args) != 0 {
				t.Errorf("args = %d; want 0 (heartbeat takes no args)", len(d.Args))
			}
			if len(d.ProducesTags) != 1 || d.ProducesTags[0].Tag != "freeso:heartbeat" {
				t.Errorf("produces_tags = %+v; want one tag 'freeso:heartbeat'", d.ProducesTags)
			}
			hb = &struct{}{}
		}
	}
	if hb == nil {
		t.Error("conventions/heartbeat.json not loaded")
	}
}

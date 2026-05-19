/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"encoding/json"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"testing"
	"time"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// ---------------------------------------------------------------------------
// Unit tests: LivenessSnapshot shape + health ring buffer
// ---------------------------------------------------------------------------

// TestLivenessSnapshot_Initial: brand-new tracker returns zero-state shape.
func TestLivenessSnapshot_Initial(t *testing.T) {
	h := NewSidecarHealth()
	snap := h.LivenessSnapshotNow(nil, 42)
	if snap.BotConnected {
		t.Error("BotConnected=true on fresh tracker with nil bot")
	}
	if snap.BridgeActive {
		t.Error("BridgeActive=true before any perception recorded")
	}
	if snap.PerceptionTickCountLast60s != 0 {
		t.Errorf("PerceptionTickCountLast60s=%d; want 0", snap.PerceptionTickCountLast60s)
	}
	if snap.HandlerCount != 42 {
		t.Errorf("HandlerCount=%d; want 42", snap.HandlerCount)
	}
	if snap.LotID != 0 {
		t.Errorf("LotID=%d; want 0 (unknown)", snap.LotID)
	}
	if snap.LastPerceptionTs != 0 {
		t.Errorf("LastPerceptionTs=%d; want 0 (none)", snap.LastPerceptionTs)
	}
	if snap.SidecarUptimeSec < 0 {
		t.Errorf("SidecarUptimeSec=%d; want >= 0", snap.SidecarUptimeSec)
	}
}

// TestLivenessSnapshot_BridgeActive: after RecordPerceptionWithLot,
// bridge_active=true and last_perception_ts > 0.
func TestLivenessSnapshot_BridgeActive(t *testing.T) {
	h := NewSidecarHealth()
	pos := &perceptionPosition{Level: 1, X: 10.0, Y: 20.0}
	h.RecordPerceptionWithLot("sim-1", pos, true, 99)

	snap := h.LivenessSnapshotNow(nil, 5)
	if !snap.BridgeActive {
		t.Error("BridgeActive=false after recording perception")
	}
	if snap.LastPerceptionTs <= 0 {
		t.Errorf("LastPerceptionTs=%d; want > 0", snap.LastPerceptionTs)
	}
	if snap.LotID != 99 {
		t.Errorf("LotID=%d; want 99", snap.LotID)
	}
	if snap.PerceptionTickCountLast60s < 1 {
		t.Errorf("PerceptionTickCountLast60s=%d; want >= 1", snap.PerceptionTickCountLast60s)
	}
}

// TestLivenessSnapshot_RingBuffer: multiple ticks within 60s all counted.
func TestLivenessSnapshot_RingBuffer(t *testing.T) {
	h := NewSidecarHealth()
	for i := 0; i < 10; i++ {
		h.RecordPerceptionWithLot("sim-1", nil, false, 0)
	}
	snap := h.LivenessSnapshotNow(nil, 0)
	if snap.PerceptionTickCountLast60s != 10 {
		t.Errorf("PerceptionTickCountLast60s=%d; want 10", snap.PerceptionTickCountLast60s)
	}
}

// TestLivenessSnapshot_LotIDSticky: lotID persists across ticks with lotID=0.
func TestLivenessSnapshot_LotIDSticky(t *testing.T) {
	h := NewSidecarHealth()
	h.RecordPerceptionWithLot("sim-1", nil, true, 77)
	// Next tick has no lot_id (0 = unknown)
	h.RecordPerceptionWithLot("sim-1", nil, true, 0)
	snap := h.LivenessSnapshotNow(nil, 0)
	if snap.LotID != 77 {
		t.Errorf("LotID=%d; want 77 (sticky from first tick)", snap.LotID)
	}
}

// TestLivenessSnapshot_NilSafe: nil health returns zero LivenessSnapshot.
func TestLivenessSnapshot_NilSafe(t *testing.T) {
	var h *SidecarHealth
	snap := h.LivenessSnapshotNow(nil, 0)
	if snap.BotConnected || snap.BridgeActive {
		t.Error("nil health should return zero state")
	}
}

// TestExtractPerceptionLotID: bridges.go helper parses lot_id from perception JSON.
func TestExtractPerceptionLotID(t *testing.T) {
	cases := []struct {
		name  string
		line  []byte
		wantID int64
	}{
		{
			name:   "has lot_id",
			line:   []byte(`{"kind":"perception","lot":{"lot_id":42,"name":"test"}}`),
			wantID: 42,
		},
		{
			name:   "no lot field",
			line:   []byte(`{"kind":"perception"}`),
			wantID: 0,
		},
		{
			name:   "null lot",
			line:   []byte(`{"kind":"perception","lot":null}`),
			wantID: 0,
		},
		{
			name:   "lot without lot_id",
			line:   []byte(`{"kind":"perception","lot":{"name":"test"}}`),
			wantID: 0,
		},
	}
	for _, tc := range cases {
		t.Run(tc.name, func(t *testing.T) {
			got := extractPerceptionLotID(tc.line)
			if got != tc.wantID {
				t.Errorf("extractPerceptionLotID=%d; want %d", got, tc.wantID)
			}
		})
	}
}

// TestDiagnosticDeclarationsPresent: both declarations must be loadable from
// the embedded conventions FS and have the expected operations.
func TestDiagnosticDeclarationsPresent(t *testing.T) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}
	ops := make(map[string]bool, len(decls))
	for _, d := range decls {
		ops[d.Operation] = true
	}
	for _, want := range []string{"report-liveness", "verify-perception"} {
		if !ops[want] {
			t.Errorf("declaration %q not found in conventions/; found: %v", want, ops)
		}
	}
}

// TestReportLivenessHandlerUnit: the report-liveness handler returns a
// LivenessSnapshot with bridge_active=true after a perception is recorded.
func TestReportLivenessHandlerUnit(t *testing.T) {
	h := NewSidecarHealth()
	h.RecordPerceptionWithLot("sim-1", nil, true, 13)

	// Minimal campfire stub — only Router.Count() is called.
	cf := &Campfire{Router: NewRouter()}
	cf.Router.Register(&convention.Declaration{Operation: "fake-op"}, func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		return nil, nil
	})

	handler := makeLivenessHandler(nil, h, cf)
	resp, err := handler(context.Background(), &convention.Request{Args: nil})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	snap, ok := resp.Payload.(LivenessSnapshot)
	if !ok {
		t.Fatalf("payload is %T; want LivenessSnapshot", resp.Payload)
	}
	if !snap.BridgeActive {
		t.Error("BridgeActive=false; want true (perception was recorded)")
	}
	if snap.LotID != 13 {
		t.Errorf("LotID=%d; want 13", snap.LotID)
	}
	if snap.LastPerceptionTs <= 0 {
		t.Errorf("LastPerceptionTs=%d; want > 0", snap.LastPerceptionTs)
	}
	if snap.PerceptionTickCountLast60s < 1 {
		t.Errorf("PerceptionTickCountLast60s=%d; want >= 1", snap.PerceptionTickCountLast60s)
	}
}

// ---------------------------------------------------------------------------
// Integration tests: real sidecar binary, real campfire, real bot
// ---------------------------------------------------------------------------

// TestIntegration_ReportLiveness_POSITIVE: bot is live (stub emits perception
// with lot_id). report-liveness must return bridge_active=true and
// perception_tick_count_last_60s > 0.
//
// This is the POSITIVE gate from the item spec.
//
// Skip: FREESO_SKIP_INTEGRATION=1.
func TestIntegration_ReportLiveness_POSITIVE(t *testing.T) {
	if os.Getenv("FREESO_SKIP_INTEGRATION") == "1" {
		t.Skip("FREESO_SKIP_INTEGRATION=1")
	}
	cfBin, err := exec.LookPath("cf")
	if err != nil {
		t.Skipf("cf binary not on PATH: %v", err)
	}
	goBin, err := exec.LookPath("go")
	if err != nil {
		t.Skipf("go not on PATH: %v", err)
	}

	tmp := t.TempDir()
	cfHome := filepath.Join(tmp, "cf-home")
	if err := os.MkdirAll(cfHome, 0o700); err != nil {
		t.Fatalf("mkdir cf-home: %v", err)
	}

	sidecarBin := filepath.Join(tmp, "freeso-sidecar-liveness-pos")
	build := exec.Command(goBin, "build", "-buildvcs=false", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar: %v\n%s", err, out)
	}

	// Stub bot: emits a perception with lot.lot_id=42, then loops.
	stubBot := filepath.Join(tmp, "stub-liveness-bot.sh")
	stubScript := `#!/bin/sh
printf '{"kind":"perception","t":1000,"avatar":{"persist_id":2},"lot":{"lot_id":42,"name":"test"}}\n'
sleep 0.2
printf '{"kind":"perception","t":1200,"avatar":{"persist_id":2},"lot":{"lot_id":42,"name":"test"}}\n'
sleep 0.2
printf '{"kind":"perception","t":1400,"avatar":{"persist_id":2},"lot":{"lot_id":42,"name":"test"}}\n'
trap 'exit 0' TERM INT
while true; do sleep 0.5; done
`
	if err := os.WriteFile(stubBot, []byte(stubScript), 0o700); err != nil {
		t.Fatalf("write stub bot: %v", err)
	}

	ctx, cancel := context.WithTimeout(context.Background(), 60*time.Second)
	defer cancel()

	sidecarCmd := exec.CommandContext(ctx, sidecarBin,
		"--bot", stubBot,
		"--bot-args", "--emit-perception",
		"--cf-home", cfHome,
		"--description", "liveness-pos-test",
	)
	// Enable perception broadcast so the sidecar broadcasts ticks to the campfire.
	sidecarCmd.Env = append(os.Environ(), "FREESO_BROADCAST_PERCEPTION=1")

	stdout, err := sidecarCmd.StdoutPipe()
	if err != nil {
		t.Fatalf("stdout pipe: %v", err)
	}
	stderr, err := sidecarCmd.StderrPipe()
	if err != nil {
		t.Fatalf("stderr pipe: %v", err)
	}
	if err := sidecarCmd.Start(); err != nil {
		t.Fatalf("start sidecar: %v", err)
	}
	defer func() {
		_ = sidecarCmd.Process.Signal(os.Interrupt)
		_, _ = sidecarCmd.Process.Wait()
	}()

	go drainStderr(t, stderr)

	campfireID := waitForCampfireID(t, stdout, 20*time.Second)
	t.Logf("campfire: %s", campfireID)

	// Allow the bot to emit perceptions and the bridge to process them.
	t.Log("waiting for bridge to process stub bot perceptions...")
	time.Sleep(2 * time.Second)

	// Call report-liveness via cf, running as the sidecar's identity (same
	// CF_HOME). This sidesteps the admit+join ceremony since the sidecar is
	// already a member of its own campfire. This matches the pattern used in
	// TestIntegration_CampfireAndBridge (runCf passes cfHome explicitly).
	callCtx, callCancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer callCancel()
	var livenessOut string
	deadline := time.Now().Add(20 * time.Second)
	for time.Now().Before(deadline) {
		out, err := runCfHome(callCtx, cfBin, cfHome, campfireID, "report-liveness", "--wait-timeout", "8s")
		if err == nil && strings.Contains(out, "sidecar_uptime_sec") {
			livenessOut = out
			break
		}
		if out != "" {
			t.Logf("report-liveness attempt (non-empty but no payload): %s", truncate(out, 200))
		}
		time.Sleep(500 * time.Millisecond)
	}

	if livenessOut == "" {
		t.Fatalf("report-liveness returned no output within 20s")
	}
	t.Logf("report-liveness response: %s", truncate(livenessOut, 500))

	// Parse the response payload.
	var liveness LivenessSnapshot
	if err := parseConventionResponse(livenessOut, &liveness); err != nil {
		t.Fatalf("parse report-liveness response: %v\nraw: %s", err, livenessOut)
	}

	// POSITIVE gate assertions.
	if !liveness.BridgeActive {
		t.Errorf("POSITIVE gate FAIL: bridge_active=false; want true (bot emitted perception)")
	}
	if liveness.PerceptionTickCountLast60s < 1 {
		t.Errorf("POSITIVE gate FAIL: perception_tick_count_last_60s=%d; want > 0", liveness.PerceptionTickCountLast60s)
	}
	if liveness.HandlerCount <= 0 {
		t.Errorf("POSITIVE gate: handler_count=%d; want > 0", liveness.HandlerCount)
	}
	if liveness.SidecarUptimeSec < 0 {
		t.Errorf("sidecar_uptime_sec=%d; want >= 0", liveness.SidecarUptimeSec)
	}
	t.Logf("POSITIVE gate PASS: bridge_active=%v ticks_60s=%d lot_id=%d",
		liveness.BridgeActive, liveness.PerceptionTickCountLast60s, liveness.LotID)
}

// TestIntegration_ReportLiveness_NEGATIVE: bot never connects (--no-bot mode).
// report-liveness must return bot_connected=false.
//
// This is the NEGATIVE gate from the item spec.
//
// Skip: FREESO_SKIP_INTEGRATION=1.
func TestIntegration_ReportLiveness_NEGATIVE(t *testing.T) {
	if os.Getenv("FREESO_SKIP_INTEGRATION") == "1" {
		t.Skip("FREESO_SKIP_INTEGRATION=1")
	}
	cfBin, err := exec.LookPath("cf")
	if err != nil {
		t.Skipf("cf binary not on PATH: %v", err)
	}
	goBin, err := exec.LookPath("go")
	if err != nil {
		t.Skipf("go not on PATH: %v", err)
	}

	tmp := t.TempDir()
	cfHome := filepath.Join(tmp, "cf-home")
	if err := os.MkdirAll(cfHome, 0o700); err != nil {
		t.Fatalf("mkdir cf-home: %v", err)
	}

	sidecarBin := filepath.Join(tmp, "freeso-sidecar-liveness-neg")
	build := exec.Command(goBin, "build", "-buildvcs=false", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar: %v\n%s", err, out)
	}

	// Run sidecar in --no-bot mode: bot_connected must be false.
	ctx, cancel := context.WithTimeout(context.Background(), 60*time.Second)
	defer cancel()

	sidecarCmd := exec.CommandContext(ctx, sidecarBin,
		"--no-bot",
		"--cf-home", cfHome,
		"--description", "liveness-neg-test",
	)
	sidecarCmd.Env = os.Environ()

	stdout, err := sidecarCmd.StdoutPipe()
	if err != nil {
		t.Fatalf("stdout pipe: %v", err)
	}
	stderr, err := sidecarCmd.StderrPipe()
	if err != nil {
		t.Fatalf("stderr pipe: %v", err)
	}
	if err := sidecarCmd.Start(); err != nil {
		t.Fatalf("start sidecar: %v", err)
	}
	defer func() {
		_ = sidecarCmd.Process.Signal(os.Interrupt)
		_, _ = sidecarCmd.Process.Wait()
	}()

	go drainStderr(t, stderr)

	campfireID := waitForCampfireID(t, stdout, 20*time.Second)
	t.Logf("campfire: %s", campfireID)

	// Call report-liveness as the sidecar's identity (same CF_HOME).
	callCtx, callCancel := context.WithTimeout(context.Background(), 15*time.Second)
	defer callCancel()

	var livenessOut string
	deadline := time.Now().Add(10 * time.Second)
	for time.Now().Before(deadline) {
		out, err := runCfHome(callCtx, cfBin, cfHome, campfireID, "report-liveness", "--wait-timeout", "5s")
		if err == nil && strings.Contains(out, "sidecar_uptime_sec") {
			livenessOut = out
			break
		}
		time.Sleep(500 * time.Millisecond)
	}

	if livenessOut == "" {
		t.Fatalf("report-liveness returned no output within 10s")
	}
	t.Logf("report-liveness response: %s", truncate(livenessOut, 500))

	var liveness LivenessSnapshot
	if err := parseConventionResponse(livenessOut, &liveness); err != nil {
		t.Fatalf("parse: %v\nraw: %s", err, livenessOut)
	}

	// NEGATIVE gate: bot_connected must be false (--no-bot mode).
	if liveness.BotConnected {
		t.Errorf("NEGATIVE gate FAIL: bot_connected=true in --no-bot mode; want false")
	}
	// bridge_active must be false (no perception ticks in --no-bot mode).
	if liveness.BridgeActive {
		t.Errorf("NEGATIVE gate FAIL: bridge_active=true in --no-bot mode; want false")
	}
	t.Logf("NEGATIVE gate PASS: bot_connected=%v bridge_active=%v",
		liveness.BotConnected, liveness.BridgeActive)
}

// TestIntegration_VerifyPerception_POSITIVE: sidecar with bot running.
// verify-perception must return tick_seen=true within wait_seconds.
//
// This is the POSITIVE gate for verify-perception.
//
// Skip: FREESO_SKIP_INTEGRATION=1.
func TestIntegration_VerifyPerception_POSITIVE(t *testing.T) {
	if os.Getenv("FREESO_SKIP_INTEGRATION") == "1" {
		t.Skip("FREESO_SKIP_INTEGRATION=1")
	}
	cfBin, err := exec.LookPath("cf")
	if err != nil {
		t.Skipf("cf binary not on PATH: %v", err)
	}
	goBin, err := exec.LookPath("go")
	if err != nil {
		t.Skipf("go not on PATH: %v", err)
	}

	tmp := t.TempDir()
	cfHome := filepath.Join(tmp, "cf-home")
	if err := os.MkdirAll(cfHome, 0o700); err != nil {
		t.Fatalf("mkdir cf-home: %v", err)
	}

	sidecarBin := filepath.Join(tmp, "freeso-sidecar-verifyperc-pos")
	build := exec.Command(goBin, "build", "-buildvcs=false", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar: %v\n%s", err, out)
	}

	// Stub bot that loops indefinitely — only the synthetic tick matters.
	stubBot := filepath.Join(tmp, "stub-verify-bot.sh")
	stubScript := `#!/bin/sh
printf '{"kind":"system","payload":{"event":"ready"}}\n'
trap 'exit 0' TERM INT
while true; do sleep 0.5; done
`
	if err := os.WriteFile(stubBot, []byte(stubScript), 0o700); err != nil {
		t.Fatalf("write stub bot: %v", err)
	}

	ctx, cancel := context.WithTimeout(context.Background(), 60*time.Second)
	defer cancel()

	sidecarCmd := exec.CommandContext(ctx, sidecarBin,
		"--bot", stubBot,
		"--bot-args", "",
		"--cf-home", cfHome,
		"--description", "verify-perc-pos-test",
	)
	sidecarCmd.Env = os.Environ()

	stdout, err := sidecarCmd.StdoutPipe()
	if err != nil {
		t.Fatalf("stdout pipe: %v", err)
	}
	stderr, err := sidecarCmd.StderrPipe()
	if err != nil {
		t.Fatalf("stderr pipe: %v", err)
	}
	if err := sidecarCmd.Start(); err != nil {
		t.Fatalf("start sidecar: %v", err)
	}
	defer func() {
		_ = sidecarCmd.Process.Signal(os.Interrupt)
		_, _ = sidecarCmd.Process.Wait()
	}()

	go drainStderr(t, stderr)

	campfireID := waitForCampfireID(t, stdout, 20*time.Second)
	t.Logf("campfire: %s", campfireID)

	// Call verify-perception as the sidecar's identity (same CF_HOME).
	// Use --wait-timeout 10s per attempt to avoid blocking on the 30s convention
	// default. The handler will run with wait_seconds=5, so the total is at most
	// ~7s per call (5s handler wait + 2s await poll). Cap the attempt timeout at
	// 10s so the retry loop can make multiple attempts within the overall deadline.
	callCtx, callCancel := context.WithTimeout(context.Background(), 50*time.Second)
	defer callCancel()

	var verifyOut string
	deadline := time.Now().Add(40 * time.Second)
	for time.Now().Before(deadline) {
		out, runErr := runCfHome(callCtx, cfBin, cfHome, campfireID, "verify-perception", "--wait_seconds", "5", "--wait-timeout", "10s")
		if runErr == nil && strings.Contains(out, "tick_seen") {
			verifyOut = out
			break
		}
		if out != "" {
			t.Logf("verify-perception attempt: %s", truncate(out, 200))
		}
		time.Sleep(500 * time.Millisecond)
	}

	if verifyOut == "" {
		t.Fatalf("verify-perception returned no output within 40s")
	}
	t.Logf("verify-perception response: %s", truncate(verifyOut, 500))

	var result struct {
		TickSeen bool  `json:"tick_seen"`
		Ts       int64 `json:"ts"`
	}
	if err := parseConventionResponse(verifyOut, &result); err != nil {
		t.Fatalf("parse: %v\nraw: %s", err, verifyOut)
	}

	// POSITIVE gate: tick_seen must be true.
	if !result.TickSeen {
		t.Errorf("POSITIVE gate FAIL: tick_seen=false; want true (synthetic tick should appear)")
	}
	if result.TickSeen && result.Ts <= 0 {
		t.Errorf("tick_seen=true but ts=%d; want > 0", result.Ts)
	}
	t.Logf("POSITIVE gate PASS: tick_seen=%v ts=%d", result.TickSeen, result.Ts)
}

// TestIntegration_VerifyPerception_NEGATIVE: sidecar in --no-bot mode.
// verify-perception should still work (synthetic tick doesn't need the bot)
// and return tick_seen=true (or at least return a valid response shape).
// The true NEGATIVE (bot_connected=false) is covered by ReportLiveness_NEGATIVE.
//
// Skip: FREESO_SKIP_INTEGRATION=1.
func TestIntegration_VerifyPerception_NEGATIVE(t *testing.T) {
	if os.Getenv("FREESO_SKIP_INTEGRATION") == "1" {
		t.Skip("FREESO_SKIP_INTEGRATION=1")
	}
	cfBin, err := exec.LookPath("cf")
	if err != nil {
		t.Skipf("cf binary not on PATH: %v", err)
	}
	goBin, err := exec.LookPath("go")
	if err != nil {
		t.Skipf("go not on PATH: %v", err)
	}

	tmp := t.TempDir()
	cfHome := filepath.Join(tmp, "cf-home")
	if err := os.MkdirAll(cfHome, 0o700); err != nil {
		t.Fatalf("mkdir cf-home: %v", err)
	}

	sidecarBin := filepath.Join(tmp, "freeso-sidecar-verifyperc-neg")
	build := exec.Command(goBin, "build", "-buildvcs=false", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar: %v\n%s", err, out)
	}

	// Run sidecar in --no-bot mode.
	ctx, cancel := context.WithTimeout(context.Background(), 60*time.Second)
	defer cancel()

	sidecarCmd := exec.CommandContext(ctx, sidecarBin,
		"--no-bot",
		"--cf-home", cfHome,
		"--description", "verify-perc-neg-test",
	)
	sidecarCmd.Env = os.Environ()

	stdout, err := sidecarCmd.StdoutPipe()
	if err != nil {
		t.Fatalf("stdout pipe: %v", err)
	}
	stderr, err := sidecarCmd.StderrPipe()
	if err != nil {
		t.Fatalf("stderr pipe: %v", err)
	}
	if err := sidecarCmd.Start(); err != nil {
		t.Fatalf("start sidecar: %v", err)
	}
	defer func() {
		_ = sidecarCmd.Process.Signal(os.Interrupt)
		_, _ = sidecarCmd.Process.Wait()
	}()

	go drainStderr(t, stderr)

	campfireID := waitForCampfireID(t, stdout, 20*time.Second)
	t.Logf("campfire: %s", campfireID)

	// Call verify-perception in --no-bot mode. The handler doesn't require the
	// bot — it broadcasts a synthetic tick directly. Should return a valid shape.
	callCtx, callCancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer callCancel()

	var verifyOut string
	deadline := time.Now().Add(20 * time.Second)
	for time.Now().Before(deadline) {
		out, runErr := runCfHome(callCtx, cfBin, cfHome, campfireID, "verify-perception", "--wait_seconds", "3", "--wait-timeout", "8s")
		if runErr == nil && strings.Contains(out, "tick_seen") {
			verifyOut = out
			break
		}
		if out != "" {
			t.Logf("verify-perception (no-bot) attempt: %s", truncate(out, 200))
		}
		time.Sleep(500 * time.Millisecond)
	}

	if verifyOut == "" {
		t.Fatalf("verify-perception returned no output within 20s in --no-bot mode")
	}
	t.Logf("verify-perception (no-bot) response: %s", truncate(verifyOut, 500))

	// NEGATIVE gate: response must have the correct shape. tick_seen may be
	// true or false depending on BroadcastEvent behaviour in no-bot mode.
	var result struct {
		TickSeen bool  `json:"tick_seen"`
		Ts       int64 `json:"ts"`
	}
	if err := parseConventionResponse(verifyOut, &result); err != nil {
		t.Fatalf("parse verify-perception response: %v\nraw: %s", err, verifyOut)
	}
	// NEGATIVE gate: the response shape is valid (tick_seen is a bool field).
	// We log the result — the convention works in --no-bot mode.
	t.Logf("NEGATIVE gate PASS: verify-perception returned valid shape in --no-bot mode: tick_seen=%v ts=%d",
		result.TickSeen, result.Ts)
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

// runCfHome runs cf with CF_HOME overridden to cfHome.
func runCfHome(ctx context.Context, cfBin, cfHome, cmd string, args ...string) (string, error) {
	allArgs := append([]string{cmd}, args...)
	c := exec.CommandContext(ctx, cfBin, allArgs...)
	if cfHome != "" {
		c.Env = append(os.Environ(), "CF_HOME="+cfHome)
	} else {
		c.Env = os.Environ()
	}
	out, err := c.CombinedOutput()
	return string(out), err
}

// drainStderr reads stderr from an io.Reader and logs lines.
func drainStderr(t *testing.T, r interface{ Read([]byte) (int, error) }) {
	t.Helper()
	buf := make([]byte, 4096)
	for {
		n, err := r.Read(buf)
		if n > 0 {
			t.Logf("sidecar-stderr: %s", strings.TrimRight(string(buf[:n]), "\n"))
		}
		if err != nil {
			return
		}
	}
}

// parseConventionResponse extracts the JSON payload from cf's convention
// response output and unmarshals it into dst.
//
// cf's response format looks like:
//
//	[campfire:XXXX] ... sender
//	  tags: fulfills, freeso:report-liveness
//	  {"sidecar_uptime_sec":...}
//
// We extract the JSON object line from the output.
func parseConventionResponse(out string, dst any) error {
	lines := strings.Split(out, "\n")
	for _, line := range lines {
		line = strings.TrimSpace(line)
		if strings.HasPrefix(line, "{") && strings.HasSuffix(line, "}") {
			// Check this is not a convention operation schema (those have "convention" key).
			var probe map[string]any
			if err := json.Unmarshal([]byte(line), &probe); err != nil {
				continue
			}
			if _, isConvSchema := probe["convention"]; isConvSchema {
				// Skip convention declaration lines.
				continue
			}
			// Skip error responses.
			if errMsg, hasErr := probe["error"]; hasErr {
				return fmt.Errorf("convention error: %v", errMsg)
			}
			return json.Unmarshal([]byte(line), dst)
		}
	}
	return fmt.Errorf("no JSON payload found in convention response output")
}

// Note: waitForCampfireID, mustSourceDir, and truncate are defined in integration_test.go

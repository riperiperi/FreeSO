/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

// readiness_chargen_integration_test.go — automataisland-05d
//
// Closes the Wave 3 closeout veracity gap on 9a8 (chargen:ready as 4th
// wake-readiness future). The unit-level coverage in readiness/futures_test.go
// uses a `fakeChargen` that satisfies the ChargenWatcher interface, but the
// production wiring — `augmentorChargen` wrapping `*PerceptionAugmentor`,
// which reads `avatar.persist_id` from real perception ticks — is not
// exercised end-to-end. This integration test boots a real sidecar binary in
// --chargen-mode against a stub bot whose perception ticks drive a
// chargen_pending=true → false transition (persist_id 0 → 42), and asserts
// the chargen:ready future fulfills on the body cf with the design-spec
// payload.
//
// Mock scope per item spec: none for sidecar internals. The stub bot is a
// real subprocess emitting NDJSON; the perception bridge, augmentor,
// chargen gate, and campfire are all production code. The stub bot
// substitution mirrors the 5a4 precedent (TestIntegration_Readiness_
// WorldReadyFulfillsOnPerception).
//
// Skip conditions match the rest of this file (FREESO_SKIP_INTEGRATION,
// cf binary on PATH, go binary on PATH).

package main

import (
	"context"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"testing"
	"time"
)

// TestIntegration_Readiness_ChargenReadyFulfills boots a real sidecar with
// --bot-args=--chargen-mode and a stub bot that drives chargen_pending=true →
// false via avatar.persist_id transitions. Asserts chargen:ready fulfills.
func TestIntegration_Readiness_ChargenReadyFulfills(t *testing.T) {
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

	// Build the sidecar binary.
	sidecarBin := filepath.Join(tmp, "freeso-sidecar-chargen-test")
	build := exec.Command(goBin, "build", "-buildvcs=false", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar: %v\n%s", err, out)
	}

	// Stub bot: emits system:ready, then perception with persist_id=0 for the
	// first 3 seconds (chargen pending), then transitions to persist_id=42
	// (avatar created — augmentor flips chargen_pending=false, chargen gate
	// fulfills). lot block is required so the bridge accepts the perception.
	//
	// Note: --chargen-mode is passed via --bot-args (the sidecar parses it
	// out of the bot-args string and sets augmentor chargenMode=true). The
	// stub bot itself ignores any args; it just streams NDJSON.
	stubBot := filepath.Join(tmp, "stub-chargen-bot.sh")
	stubScript := `#!/bin/sh
printf '{"kind":"system","payload":{"event":"ready"}}\n'
# Phase 1: persist_id=0 — augmentor reads chargen_pending=true.
i=0
while [ $i -lt 6 ]; do
    i=$((i + 1))
    printf '{"kind":"perception","t":%d,"avatar":{"persist_id":0,"name":"chargentest","position":{"level":1,"x":10.0,"y":10.0}},"lot":{"lot_id":16318812,"name":"Starter Lot"}}\n' "$i"
    sleep 0.5
done
# Phase 2: persist_id=42 — augmentor reads chargen_pending=false, gate fulfills.
while :; do
    i=$((i + 1))
    printf '{"kind":"perception","t":%d,"avatar":{"persist_id":42,"name":"chargentest","position":{"level":1,"x":10.0,"y":10.0}},"lot":{"lot_id":16318812,"name":"Starter Lot"}}\n' "$i"
    sleep 0.5
done
`
	if err := os.WriteFile(stubBot, []byte(stubScript), 0o700); err != nil {
		t.Fatalf("write stub bot: %v", err)
	}

	ctx, cancel := context.WithTimeout(context.Background(), 90*time.Second)
	defer cancel()

	sidecarCmd := exec.CommandContext(ctx, sidecarBin,
		"--bot", stubBot,
		"--bot-args", "--chargen-mode",
		"--cf-home", cfHome,
		"--description", "readiness-chargen-test",
	)
	// FREESO_BROADCAST_PERCEPTION=1 to keep the bridge alive (consistent with
	// 5a4's positive gate). FREESO_SKIP_SMOKE_TEST=1 because the readiness
	// graph is what we're testing — not the boot smoke gate (f28).
	sidecarEnv := os.Environ()
	filtered := sidecarEnv[:0]
	for _, e := range sidecarEnv {
		if !strings.HasPrefix(e, "FREESO_BROADCAST_PERCEPTION=") &&
			!strings.HasPrefix(e, "FREESO_SKIP_SMOKE_TEST=") {
			filtered = append(filtered, e)
		}
	}
	sidecarCmd.Env = append(filtered,
		"FREESO_BROADCAST_PERCEPTION=1",
		"FREESO_SKIP_SMOKE_TEST=1",
	)

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

	// Drain stderr into the test log for diagnostics on failure.
	go func() {
		buf := make([]byte, 4096)
		for {
			n, err := stderr.Read(buf)
			if n > 0 {
				t.Logf("sidecar-stderr: %s", strings.TrimRight(string(buf[:n]), "\n"))
			}
			if err != nil {
				return
			}
		}
	}()

	campfireID := waitForCampfireID(t, stdout, 30*time.Second)
	t.Logf("campfire id: %s", campfireID)

	readCtx, readCancel := context.WithTimeout(context.Background(), 80*time.Second)
	defer readCancel()

	// Confirm chargen:ready future is pre-published (gate 1).
	if !pollForTaggedMessageAnd(t, readCtx, cfBin, cfHome, campfireID, "future", "chargen:ready", 10*time.Second) {
		t.Fatalf("chargen:ready future not pre-published within 10s of boot")
	}
	t.Logf("PASS: chargen:ready future pre-published on body cf")

	// Wait for fulfillment. Phase 1 of the stub runs ~3s with persist_id=0,
	// then phase 2 begins. The chargen gate polls every 500ms; allow up to 30s
	// for the transition + bridge propagation + cf surfacing.
	fulMsg := pollForFulfillment(t, readCtx, cfBin, cfHome, campfireID, "chargen:ready", 30*time.Second)
	if fulMsg == "" {
		t.Fatalf("chargen:ready did not fulfill within 30s of phase-2 persist_id=42 emission")
	}
	t.Logf("PASS: chargen:ready fulfilled after avatar.persist_id transition")

	// Decode payload and assert the design-spec fields.
	payload := extractPayload(t, fulMsg)
	avID, ok := payload["avatar_id"].(float64)
	if !ok {
		t.Errorf("chargen:ready fulfillment missing avatar_id (or wrong type): got %v (%T)",
			payload["avatar_id"], payload["avatar_id"])
	}
	if avID != 42 {
		t.Errorf("chargen:ready avatar_id want 42 (the stub's transition value); got %v", avID)
	}
	alreadyExisted, ok := payload["already_existed"].(bool)
	if !ok {
		t.Errorf("chargen:ready fulfillment missing already_existed (or wrong type): got %v (%T)",
			payload["already_existed"], payload["already_existed"])
	}
	if alreadyExisted {
		t.Errorf("chargen:ready already_existed want false (we drove a real transition); got true")
	}
	t.Logf("PASS: chargen:ready payload {avatar_id=%v, already_existed=%v}", avID, alreadyExisted)
}

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
)

// TestIntegration_Readiness_WorldReadyFulfillsOnPerception is the POSITIVE
// gate for automataisland-5a4. It boots a real freeso-sidecar binary against
// a stub bot subprocess that emits real lot-bearing perception NDJSON. The
// sidecar's bridge processes the perception through the same code path the
// production C# bot exercises (bridges.go handle() → BroadcastEvent).
//
// Asserts:
//   1. All three futures (world:ready, world:gone, identity:resolved) are
//      pre-published on the body cf within 10s of boot, each tagged with
//      "future" + its semantic tag.
//   2. world:ready fulfills within 60s once perception flows; fulfillment
//      payload carries {lot_id, persona_pubkey, handler_count,
//      first_perception_ts, broadcast_bridge_verified: true}.
//
// Mock scope per item spec: NONE for sidecar internals. The stub bot is a
// real subprocess emitting real NDJSON; the bridge, campfire, and futures
// logic are all production code. No mock of perception (the stub emits
// genuine perception frames), no mock of the bridge (it runs as production
// goroutine), no mock of cf store (real filesystem-backed store).
//
// Skip conditions match other integration tests in this file:
//   - FREESO_SKIP_INTEGRATION=1 → opt-out
//   - cf binary not on PATH → skip
//   - go binary not on PATH → skip
func TestIntegration_Readiness_WorldReadyFulfillsOnPerception(t *testing.T) {
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
	sidecarBin := filepath.Join(tmp, "freeso-sidecar-readiness-test")
	build := exec.Command(goBin, "build", "-buildvcs=false", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar: %v\n%s", err, out)
	}

	// Stub bot: emits one system:ready, then a perception with lot=non-null
	// every 500ms. This is the same shape the real C# bot produces; the
	// sidecar bridge cannot tell the difference because the contract is just
	// NDJSON on stdout.
	stubBot := filepath.Join(tmp, "stub-readiness-bot.sh")
	stubScript := `#!/bin/sh
printf '{"kind":"system","payload":{"event":"ready"}}\n'
i=0
while :; do
    i=$((i + 1))
    printf '{"kind":"perception","t":%d,"avatar":{"persist_id":42,"name":"readinesstest","position":{"level":1,"x":10.0,"y":10.0}},"lot":{"lot_id":16318812,"name":"Starter Lot"}}\n' "$i"
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
		"--bot-args", "",
		"--cf-home", cfHome,
		"--description", "readiness-positive-test",
	)
	// FREESO_BROADCAST_PERCEPTION=1 so the perception bridge actually
	// broadcasts to the body cf. This is the gate (4) precondition.
	sidecarEnv := os.Environ()
	filtered := sidecarEnv[:0]
	for _, e := range sidecarEnv {
		if !strings.HasPrefix(e, "FREESO_BROADCAST_PERCEPTION=") {
			filtered = append(filtered, e)
		}
	}
	sidecarCmd.Env = append(filtered, "FREESO_BROADCAST_PERCEPTION=1")

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

	// Drain stderr into the test log so any failure has diagnostic context.
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

	// Step 1 — assert all three futures are present within 10s of campfire bringup.
	readCtx, readCancel := context.WithTimeout(context.Background(), 80*time.Second)
	defer readCancel()

	wantFutures := []string{"world:ready", "world:gone", "identity:resolved"}
	for _, semanticTag := range wantFutures {
		if !pollForTaggedMessageAnd(t, readCtx, cfBin, cfHome, campfireID, "future", semanticTag, 10*time.Second) {
			t.Fatalf("future with tags [future, %s] not present on body cf within 10s of boot", semanticTag)
		}
		t.Logf("PASS: future %q present on body cf", semanticTag)
	}

	// Step 2 — wait for world:ready fulfillment (up to 60s).
	fulMsg := pollForFulfillment(t, readCtx, cfBin, cfHome, campfireID, "world:ready", 60*time.Second)
	if fulMsg == "" {
		t.Fatalf("world:ready did not fulfill within 60s of sidecar boot + perception flowing")
	}
	t.Logf("PASS: world:ready fulfilled within 60s")

	// Decode fulfillment payload and assert the design-spec fields are present.
	payload := extractPayload(t, fulMsg)
	for _, key := range []string{"lot_id", "persona_pubkey", "handler_count", "first_perception_ts", "broadcast_bridge_verified"} {
		if _, ok := payload[key]; !ok {
			t.Errorf("world:ready fulfillment payload missing key %q; got %v", key, payload)
		}
	}
	if v, ok := payload["broadcast_bridge_verified"].(bool); !ok || !v {
		t.Errorf("world:ready broadcast_bridge_verified want true; got %v", payload["broadcast_bridge_verified"])
	}
	// lot_id is populated from SidecarHealth.lotID, which is wired by peer
	// item automataisland-2e8 (RecordPerceptionWithLot). On a branch where
	// 2e8 has not yet merged, this field is 0. The wave-2 integration merge
	// surfaces the real lot_id automatically. We assert presence + numeric
	// type here; the value-correctness assertion lives in 2e8's own tests.
	if _, ok := payload["lot_id"].(float64); !ok {
		t.Errorf("world:ready lot_id want numeric; got %v (%T)", payload["lot_id"], payload["lot_id"])
	}
	t.Logf("PASS: world:ready payload has all design-spec fields (lot_id=%v handler_count=%v broadcast_bridge_verified=true)",
		payload["lot_id"], payload["handler_count"])
}

// TestIntegration_Readiness_WorldReadyDoesNotFulfillWithoutBroadcast is the
// NEGATIVE gate for automataisland-5a4. With FREESO_BROADCAST_PERCEPTION
// unset (default off in bridges.go), the bridge does NOT broadcast perception
// ticks to the body cf, so the world:ready gate (4) (broadcast_bridge_verified
// via live probe) is the only one this exercise targets. We also assert that
// world:gone fulfills within ~35s once perception is starved.
//
// In this test we use --no-bot so there is NEVER any perception — the
// world:gone heartbeat MUST fire after its threshold elapses, and world:ready
// MUST NOT fire.
//
// Lowers the world:gone threshold to 5s via env var so the test completes
// quickly while still preserving the production semantic.
func TestIntegration_Readiness_WorldReadyDoesNotFulfillWithoutBroadcast(t *testing.T) {
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

	sidecarBin := filepath.Join(tmp, "freeso-sidecar-readiness-neg-test")
	build := exec.Command(goBin, "build", "-buildvcs=false", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar: %v\n%s", err, out)
	}

	ctx, cancel := context.WithTimeout(context.Background(), 90*time.Second)
	defer cancel()

	sidecarCmd := exec.CommandContext(ctx, sidecarBin,
		"--no-bot",
		"--cf-home", cfHome,
		"--description", "readiness-negative-test",
	)
	// Strip any inherited FREESO_BROADCAST_PERCEPTION and force 5s world:gone
	// threshold for a tight test.
	sidecarEnv := os.Environ()
	filtered := sidecarEnv[:0]
	for _, e := range sidecarEnv {
		if strings.HasPrefix(e, "FREESO_BROADCAST_PERCEPTION=") ||
			strings.HasPrefix(e, "FREESO_WORLD_GONE_THRESHOLD_SEC=") {
			continue
		}
		filtered = append(filtered, e)
	}
	sidecarCmd.Env = append(filtered,
		"FREESO_BROADCAST_PERCEPTION=0",
		"FREESO_WORLD_GONE_THRESHOLD_SEC=5",
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

	// All three futures still pre-published (gate is independent of
	// fulfillment).
	for _, semanticTag := range []string{"world:ready", "world:gone", "identity:resolved"} {
		if !pollForTaggedMessageAnd(t, readCtx, cfBin, cfHome, campfireID, "future", semanticTag, 10*time.Second) {
			t.Fatalf("future with tags [future, %s] not present at boot", semanticTag)
		}
	}
	t.Logf("PASS: all three futures pre-published at boot")

	// world:gone MUST fulfill within threshold (5s) + heartbeat cycle (1s) + buffer.
	fulGone := pollForFulfillment(t, readCtx, cfBin, cfHome, campfireID, "world:gone", 15*time.Second)
	if fulGone == "" {
		t.Fatalf("world:gone did NOT fulfill within 15s — heartbeat is not firing under perception starvation")
	}
	t.Logf("PASS: world:gone fulfilled under perception starvation")
	payload := extractPayload(t, fulGone)
	if r, _ := payload["reason"].(string); r == "" {
		t.Error("world:gone fulfillment payload.reason is empty")
	} else {
		t.Logf("PASS: world:gone reason=%q", r)
	}

	// world:ready MUST NOT have fulfilled. Re-read once.
	fulReady := readFulfillmentOnce(readCtx, cfBin, cfHome, campfireID, "world:ready")
	if fulReady != "" {
		t.Errorf("world:ready unexpectedly fulfilled in --no-bot mode with no perception:\n%s", fulReady)
	} else {
		t.Logf("PASS: world:ready did NOT fulfill (correct — no perception ever observed)")
	}
}

// pollForFulfillment polls for a fulfillment message tagged with both
// "fulfills" AND the given semantic tag. Since cf --tag is OR semantics, we
// read messages tagged with the semantic tag and then walk the output looking
// for a per-message block whose tags contain BOTH "fulfills" and the
// semantic tag. Returns the matching message block on success, "" on timeout.
func pollForFulfillment(t *testing.T, ctx context.Context, cfBin, cfHome, campfireID, semanticTag string, timeout time.Duration) string {
	t.Helper()
	deadline := time.Now().Add(timeout)
	for time.Now().Before(deadline) {
		c := exec.CommandContext(ctx, cfBin,
			"read", campfireID, "--all", "--peek",
			"--tag", semanticTag,
		)
		c.Env = append(os.Environ(), "CF_HOME="+cfHome)
		out, _ := c.CombinedOutput()
		if msg := findMessageBlockWithBothTags(string(out), "fulfills", semanticTag); msg != "" {
			return msg
		}
		time.Sleep(250 * time.Millisecond)
	}
	return ""
}

// readFulfillmentOnce does a single read pass; "" if no fulfillment present.
func readFulfillmentOnce(ctx context.Context, cfBin, cfHome, campfireID, semanticTag string) string {
	c := exec.CommandContext(ctx, cfBin,
		"read", campfireID, "--all", "--peek",
		"--tag", semanticTag,
	)
	c.Env = append(os.Environ(), "CF_HOME="+cfHome)
	out, _ := c.CombinedOutput()
	return findMessageBlockWithBothTags(string(out), "fulfills", semanticTag)
}

// findMessageBlockWithBothTags splits the cf read text dump into per-message
// blocks (each starts with "[campfire:") and returns the first block whose
// "tags:" line contains BOTH wantTagA and wantTagB. Returns "" if none.
func findMessageBlockWithBothTags(output string, wantTagA, wantTagB string) string {
	// Splitter: each message begins with "[campfire:" at the start of a line.
	// We use a coarse split on "[campfire:" with a marker reattach so each
	// block is self-contained.
	lines := strings.Split(output, "\n")
	var blocks [][]string
	var cur []string
	for _, ln := range lines {
		if strings.HasPrefix(ln, "[campfire:") {
			if len(cur) > 0 {
				blocks = append(blocks, cur)
			}
			cur = []string{ln}
		} else if len(cur) > 0 {
			cur = append(cur, ln)
		}
	}
	if len(cur) > 0 {
		blocks = append(blocks, cur)
	}
	for _, block := range blocks {
		var tagsLine string
		for _, ln := range block {
			trimmed := strings.TrimSpace(ln)
			if strings.HasPrefix(trimmed, "tags:") {
				tagsLine = trimmed
				break
			}
		}
		if tagsLine == "" {
			continue
		}
		if !strings.Contains(tagsLine, wantTagA) || !strings.Contains(tagsLine, wantTagB) {
			continue
		}
		return strings.Join(block, "\n")
	}
	return ""
}

// pollForTaggedMessageAnd polls until a message block has BOTH tags present
// (more strict than pollForTaggedMessage which uses cf's OR semantics).
func pollForTaggedMessageAnd(t *testing.T, ctx context.Context, cfBin, cfHome, campfireID string, tagA, tagB string, timeout time.Duration) bool {
	t.Helper()
	deadline := time.Now().Add(timeout)
	for time.Now().Before(deadline) {
		c := exec.CommandContext(ctx, cfBin,
			"read", campfireID, "--all", "--peek",
			"--tag", tagB,
		)
		c.Env = append(os.Environ(), "CF_HOME="+cfHome)
		out, _ := c.CombinedOutput()
		if findMessageBlockWithBothTags(string(out), tagA, tagB) != "" {
			return true
		}
		time.Sleep(250 * time.Millisecond)
	}
	return false
}

// extractPayload pulls the JSON body out of a cf read text dump. cf read
// outputs each message as a header followed by the indented payload. We grab
// the last "{...}" block (the most recent fulfillment if multiple are present).
func extractPayload(t *testing.T, cfReadOutput string) map[string]any {
	t.Helper()
	// Find the LAST '{' in the output and JSON-decode from there.
	idx := strings.LastIndex(cfReadOutput, "{")
	if idx < 0 {
		t.Fatalf("extractPayload: no '{' in cf read output:\n%s", cfReadOutput)
	}
	// Walk forward to find the matching closing brace via depth count
	// (handles nested objects in payload).
	depth := 0
	endIdx := -1
	for i := idx; i < len(cfReadOutput); i++ {
		switch cfReadOutput[i] {
		case '{':
			depth++
		case '}':
			depth--
			if depth == 0 {
				endIdx = i + 1
			}
		}
		if endIdx > 0 {
			break
		}
	}
	if endIdx < 0 {
		t.Fatalf("extractPayload: unbalanced braces:\n%s", cfReadOutput[idx:])
	}
	js := cfReadOutput[idx:endIdx]
	var payload map[string]any
	if err := json.Unmarshal([]byte(js), &payload); err != nil {
		t.Fatalf("extractPayload: json decode: %v\n%s", err, js)
	}
	return payload
}

// compile-time guard against unused imports if a future edit drops fmt usage.
var _ = fmt.Sprintf

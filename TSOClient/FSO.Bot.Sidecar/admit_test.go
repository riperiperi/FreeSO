/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

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

// TestAdmitAgentScript_EndToEnd runs scripts/admit-agent.sh against a real
// sidecar campfire and verifies the agent-side block is emitted correctly.
// This is the ergonomic-contract test: one command, real cf, no hand-crafted
// args. If this test regresses, the "operator copy-pastes one block" promise
// in the -fa0 decision is broken.
func TestAdmitAgentScript_EndToEnd(t *testing.T) {
	if os.Getenv("FREESO_SKIP_INTEGRATION") == "1" {
		t.Skip("FREESO_SKIP_INTEGRATION=1")
	}
	cfBin, err := exec.LookPath("cf")
	if err != nil {
		t.Skipf("cf not on PATH: %v", err)
	}
	sh, err := exec.LookPath("bash")
	if err != nil {
		t.Skipf("bash not on PATH: %v", err)
	}

	tmp := t.TempDir()
	sidecarHome := filepath.Join(tmp, "sidecar-cf-home")
	if err := os.MkdirAll(sidecarHome, 0o700); err != nil {
		t.Fatalf("mkdir: %v", err)
	}

	// 1. Build sidecar (cheap — already in build cache from prior test).
	goBin, err := exec.LookPath("go")
	if err != nil {
		t.Skipf("go not on PATH: %v", err)
	}
	sidecarBin := filepath.Join(tmp, "freeso-sidecar")
	build := exec.Command(goBin, "build", "-buildvcs=false", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build: %v\n%s", err, out)
	}

	// 2. Boot sidecar in --no-bot mode so the test doesn't need a real C# bot.
	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel()
	sidecar := exec.CommandContext(ctx, sidecarBin,
		"--no-bot",
		"--cf-home", sidecarHome,
		"--description", "admit-e2e",
	)
	stdout, err := sidecar.StdoutPipe()
	if err != nil {
		t.Fatalf("stdout pipe: %v", err)
	}
	sidecar.Stderr = os.Stderr
	if err := sidecar.Start(); err != nil {
		t.Fatalf("sidecar start: %v", err)
	}
	defer func() {
		_ = sidecar.Process.Signal(os.Interrupt)
		_, _ = sidecar.Process.Wait()
	}()

	campfireID := waitForCampfireID(t, stdout, 20*time.Second)

	// Wait briefly for .admit-info to be written.
	infoPath := filepath.Join(sidecarHome, ".admit-info")
	for i := 0; i < 20; i++ {
		if _, err := os.Stat(infoPath); err == nil {
			break
		}
		time.Sleep(50 * time.Millisecond)
	}
	data, err := os.ReadFile(infoPath)
	if err != nil {
		t.Fatalf("expected .admit-info at %s: %v", infoPath, err)
	}
	if !strings.Contains(string(data), "CAMPFIRE_ID="+campfireID) {
		t.Errorf(".admit-info missing CAMPFIRE_ID=%s: got %s", campfireID, data)
	}

	// 3. Run admit-agent.sh against a fresh tmp work directory so the agent
	// identity lands somewhere clean.
	workDir := filepath.Join(tmp, "workdir")
	if err := os.MkdirAll(workDir, 0o700); err != nil {
		t.Fatalf("mkdir workdir: %v", err)
	}
	helperPath := filepath.Join(mustSourceDir(t), "scripts", "admit-agent.sh")

	cmd := exec.CommandContext(ctx, sh, helperPath, "test-agent",
		"--cf-home", sidecarHome,
		"--campfire-id", campfireID,
	)
	cmd.Dir = workDir
	// Ensure cf is on PATH and no inherited CF_HOME confuses the helper.
	env := []string{"PATH=" + os.Getenv("PATH")}
	env = append(env, "HOME="+workDir)
	cmd.Env = env
	out, err := cmd.CombinedOutput()
	if err != nil {
		t.Fatalf("admit-agent.sh failed: %v\n%s", err, out)
	}
	t.Logf("admit-agent.sh output:\n%s", out)

	// The output MUST contain the agent commands block with the campfire id.
	if !strings.Contains(string(out), "cf join "+campfireID) {
		t.Errorf("expected 'cf join %s' in admit-agent.sh output", campfireID)
	}
	if !strings.Contains(string(out), "export CF_HOME=") {
		t.Errorf("expected 'export CF_HOME=' in admit-agent.sh output")
	}

	// And the agent's identity directory should exist under workdir/agents/test-agent/cf.
	agentCF := filepath.Join(workDir, "agents", "test-agent", "cf")
	if _, err := os.Stat(filepath.Join(agentCF, "identity.json")); err != nil {
		t.Errorf("expected agent identity at %s: %v", filepath.Join(agentCF, "identity.json"), err)
	}

	// The admit itself should have succeeded — verify by reading the members
	// list from the sidecar campfire and ensuring there are at least two
	// entries (sidecar + agent).
	readCtx, readCancel := context.WithTimeout(ctx, 5*time.Second)
	defer readCancel()
	memOut, err := runCf(readCtx, cfBin, sidecarHome, "members", campfireID)
	if err != nil && memOut == "" {
		t.Fatalf("cf members: %v", err)
	}
	// Count non-empty lines as a proxy for member count.
	lines := 0
	for _, ln := range strings.Split(memOut, "\n") {
		if strings.TrimSpace(ln) != "" {
			lines++
		}
	}
	if lines < 2 {
		t.Errorf("expected >=2 members after admit, got %d\n%s", lines, memOut)
	}
}

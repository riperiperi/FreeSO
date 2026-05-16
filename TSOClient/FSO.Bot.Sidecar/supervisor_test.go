/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

// Package main — supervisor exhaustion tests (freesoexperiment-a99).
//
// TestSupervisor_AllRelaunchAttemptsFail verifies the three properties required
// by the M4 review finding on e5f: when all 3 bot relaunch attempts fail, the
// sidecar must NOT silently zombie. It must:
//
//  1. Log a clear ERROR line ("all relaunch attempts failed, sidecar exiting").
//  2. Write a sidecar-failed marker file in PersonaStateDir().
//  3. Exit with a non-zero exit code.
//
// This is a regression test for the zombie behaviour where newProc==nil caused
// a silent `continue` — indistinguishable from a healthy sidecar from the outside.
//
// Test approach: builds the real sidecar binary (same as chaos tests), launches
// it with a stub bot that always exits 1 immediately. After the first launch
// the bot loops exit-immediately for all 3 relaunch attempts. Asserts that:
//   - sidecar stderr contains the ERROR sentinel
//   - sidecar-failed marker file exists in XDG_CONFIG_HOME
//   - sidecar process has exited non-zero
//
// Skip conditions (same envelope as all integration tests):
//   - FREESO_SKIP_INTEGRATION=1
//   - go binary not on PATH
package main

import (
	"context"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"testing"
	"time"
)

// TestSupervisor_AllRelaunchAttemptsFail — after the initial bot launch
// succeeds and the bot exits cleanly, all 3 relaunch attempts fail immediately.
// The sidecar must log ERROR + write sidecar-failed marker + exit non-zero.
func TestSupervisor_AllRelaunchAttemptsFail(t *testing.T) {
	if os.Getenv("FREESO_SKIP_INTEGRATION") == "1" {
		t.Skip("FREESO_SKIP_INTEGRATION=1")
	}
	goBin, err := exec.LookPath("go")
	if err != nil {
		t.Skipf("go binary not on PATH: %v", err)
	}

	tmp := t.TempDir()
	cfHome := filepath.Join(tmp, "cf-home")
	xdgConfigHome := filepath.Join(tmp, "config")
	_ = os.MkdirAll(cfHome, 0o700)
	_ = os.MkdirAll(xdgConfigHome, 0o700)
	persona := "supervisor-exhaust"

	// Build the real sidecar binary.
	sidecarBin := filepath.Join(tmp, "freeso-sidecar-exhaust")
	build := exec.Command(goBin, "build", "-buildvcs=false", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar: %v\n%s", err, out)
	}

	// Stub bot: first launch emits system:ready then exits 0 (clean exit —
	// triggers the supervisor loop). All subsequent launches exit 1 immediately
	// so each relaunch attempt fails immediately (LaunchBot returns an error
	// when the bot exits before it can be supervised, OR the bot exits non-zero
	// which the supervisor treats as a failed launch attempt).
	//
	// Wait: LaunchBot succeeds even for a quickly-exiting bot (it just starts
	// the process). The retry loop retries on LaunchBot error, not on bot
	// quick-exit. We need the stub to fail at launch time (non-executable) for
	// exactly attempts 2-4. The simplest approach: use a run-count file; the
	// first launch is a valid bot; relaunches (attempts 1-3) are scripts that
	// exec a non-existent binary to cause LaunchBot to fail.
	//
	// Actually: LaunchBot calls exec.CommandContext(...).Start(). If the script
	// is executable and starts, Start() succeeds regardless of the bot's exit
	// code. The retry in main.go only retries when LaunchBot itself returns an
	// error (e.g., ENOENT, permission denied).
	//
	// To simulate 3 failed LaunchBot calls we need to make the stub bot binary
	// non-executable or non-existent on relaunch. The cleanest way: the stub
	// script deletes ITSELF after the first run, so the second and third launch
	// attempts get ENOENT from the OS (or EACCES if we chmod it instead).
	//
	// Approach: First run → emit ready, then exit 0. After exit, rename/chmod
	// stub so subsequent launches fail.
	readyMarker := filepath.Join(tmp, "first-bot-ready.marker")
	runCountFile := filepath.Join(tmp, "run-count")
	_ = os.WriteFile(runCountFile, []byte("0"), 0o600)

	stubBot := filepath.Join(tmp, "stub-bot.sh")
	// The stub script:
	//   - On count=0: emit system:ready, write ready marker, exit 0.
	//   - After count=0: chmod itself 000 so future launches get EACCES.
	//     The chmod runs AFTER the first launch writes the count, so the
	//     supervisor's relaunch attempts 1-3 each see EACCES on exec.
	stubScript := fmt.Sprintf(`#!/bin/sh
COUNT=$(cat %s 2>/dev/null || echo 0)
echo $((COUNT+1)) > %s
if [ "$COUNT" -eq 0 ]; then
    # First launch: emit ready, write marker, then make self non-executable for relaunches.
    printf '{"kind":"system","payload":{"event":"ready"}}\n'
    touch %s
    # Make self non-executable so LaunchBot fails on all relaunch attempts.
    chmod 000 "$0"
    exit 0
fi
# Should never reach here — script is non-executable after count=0.
exit 1
`, runCountFile, runCountFile, readyMarker)
	_ = os.WriteFile(stubBot, []byte(stubScript), 0o700)

	ctx, cancel := context.WithTimeout(context.Background(), 90*time.Second)
	defer cancel()

	cmd := exec.CommandContext(ctx, sidecarBin,
		"--bot", stubBot,
		"--bot-args", "",
		"--cf-home", cfHome,
		"--description", "supervisor-exhaust-test",
	)
	// Isolate persona state.
	filtered := filterEnv(os.Environ(), "FSO_USER", "XDG_CONFIG_HOME", "FSO_LOT_LOCATION")
	cmd.Env = append(filtered,
		"FSO_USER="+persona,
		"XDG_CONFIG_HOME="+xdgConfigHome,
	)

	// Capture stderr to check for the ERROR sentinel.
	var stderrBuf strings.Builder
	cmd.Stderr = &multiWriter{t: t, tag: "sidecar-stderr", buf: &stderrBuf}

	// Drain stdout (campfire admission block etc.) so the sidecar doesn't block
	// on a full pipe.
	stdoutPipe, err := cmd.StdoutPipe()
	if err != nil {
		t.Fatalf("stdout pipe: %v", err)
	}
	if err := cmd.Start(); err != nil {
		t.Fatalf("start sidecar: %v", err)
	}

	// Drain stdout in background.
	go func() {
		buf := make([]byte, 4096)
		for {
			n, rErr := stdoutPipe.Read(buf)
			if n > 0 {
				t.Logf("sidecar-stdout: %s", strings.TrimRight(string(buf[:n]), "\n"))
			}
			if rErr != nil {
				return
			}
		}
	}()

	// Wait for first bot ready marker (proves the first launch succeeded).
	readyDeadline := time.Now().Add(20 * time.Second)
	for time.Now().Before(readyDeadline) {
		if _, statErr := os.Stat(readyMarker); statErr == nil {
			break
		}
		time.Sleep(50 * time.Millisecond)
	}
	if _, statErr := os.Stat(readyMarker); statErr != nil {
		t.Fatalf("first bot did not emit system:ready within 20s (ready marker absent)")
	}
	t.Logf("first bot ready; bot has made itself non-executable — supervisor will exhaust retries")

	// Wait for sidecar to exit. The supervisor pauses 2s before each retry
	// attempt plus 5s between attempts = about 17s total (2s initial sleep +
	// attempt 1 + 5s + attempt 2 + 5s + attempt 3). Give generous 60s.
	exitErrCh := make(chan error, 1)
	go func() { exitErrCh <- cmd.Wait() }()

	var sidecarExitErr error
	select {
	case sidecarExitErr = <-exitErrCh:
	case <-time.After(60 * time.Second):
		_ = cmd.Process.Kill()
		t.Fatal("sidecar did not exit within 60s after all relaunch attempts exhausted — zombie behaviour detected")
	}

	// Assert 1: sidecar exited with non-zero code.
	if sidecarExitErr == nil {
		t.Errorf("sidecar exited 0 — expected non-zero exit after exhausted retries")
	} else {
		t.Logf("sidecar exited: %v (non-zero — correct)", sidecarExitErr)
	}

	// Assert 2: ERROR log line present in stderr.
	stderr := stderrBuf.String()
	const errSentinel = "all relaunch attempts failed, sidecar exiting"
	if !strings.Contains(stderr, errSentinel) {
		t.Errorf("expected stderr to contain %q\ngot stderr:\n%s", errSentinel, stderr)
	} else {
		t.Logf("PASS: ERROR sentinel found in stderr")
	}

	// Assert 3: sidecar-failed marker file exists in PersonaStateDir.
	// XDG_CONFIG_HOME is set to xdgConfigHome so PersonaStateDir() resolves
	// to xdgConfigHome/freeso-souls/<persona>/sidecar-failed.
	markerPath := filepath.Join(xdgConfigHome, "freeso-souls",
		strings.ToLower(persona), "sidecar-failed")
	data, readErr := os.ReadFile(markerPath)
	if readErr != nil {
		t.Errorf("sidecar-failed marker not found at %s: %v", markerPath, readErr)
	} else {
		content := strings.TrimSpace(string(data))
		if content == "" {
			t.Errorf("sidecar-failed marker exists but is empty")
		} else {
			t.Logf("PASS: sidecar-failed marker present: %q", content)
		}
	}

	t.Log("PASS: sidecar exited non-zero + logged ERROR + wrote failure marker after exhausting 3 relaunch attempts")
}

// multiWriter sends writes to both a test log and an in-memory buffer.
// Used to capture stderr for assertion while also surfacing it in test output.
type multiWriter struct {
	t   *testing.T
	tag string
	buf *strings.Builder
}

func (m *multiWriter) Write(p []byte) (int, error) {
	m.buf.Write(p)
	m.t.Logf("%s: %s", m.tag, strings.TrimRight(string(p), "\n"))
	return len(p), nil
}

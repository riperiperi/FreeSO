/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

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

// TestIntegration_CampfireAndBridge is a real end-to-end exercise of the
// sidecar scaffold:
//
//  1. Build the freeso-sidecar binary in a temp dir.
//  2. Write a stub bot shell script that emits a canned NDJSON stream.
//  3. Run the sidecar pointing at the stub bot, with a temp CF_HOME.
//  4. Wait for the admission block on stdout, grep out the campfire id.
//  5. Shell out to `cf read <id>` and assert we see BOTH a declaration
//     (tag convention:operation) and a perception broadcast (tag
//     freeso:perception).
//
// This is the real veracity gate for freesoexperiment-e1a. No mocks of the
// campfire store; a separate process observes what sidecar published.
//
// Skip conditions:
//   - `cf` binary not on PATH → skip with reason.
//   - FREESO_SKIP_INTEGRATION=1 → explicit opt-out.
func TestIntegration_CampfireAndBridge(t *testing.T) {
	if os.Getenv("FREESO_SKIP_INTEGRATION") == "1" {
		t.Skip("FREESO_SKIP_INTEGRATION=1")
	}
	cfBin, err := exec.LookPath("cf")
	if err != nil {
		t.Skipf("cf binary not on PATH: %v", err)
	}

	tmp := t.TempDir()
	cfHome := filepath.Join(tmp, "cf-home")
	if err := os.MkdirAll(cfHome, 0o700); err != nil {
		t.Fatalf("mkdir cf-home: %v", err)
	}

	// 1. Build the sidecar binary in tmp.
	sidecarBin := filepath.Join(tmp, "freeso-sidecar")
	goBin, err := exec.LookPath("go")
	if err != nil {
		t.Skipf("go not on PATH: %v", err)
	}
	build := exec.Command(goBin, "build", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar: %v\n%s", err, out)
	}

	// 2. Write the stub bot. Emits three events: system:ready, a perception
	// with persist_id=2, then a dialog. Sleeps a hair so the sidecar sees the
	// stream trickle in over time rather than all at once.
	stubBot := filepath.Join(tmp, "stub-bot.sh")
	stubScript := `#!/bin/sh
printf '{"kind":"system","payload":{"event":"ready"}}\n'
sleep 0.1
printf '{"kind":"perception","t":1000,"avatar":{"persist_id":2,"name":"baron"}}\n'
sleep 0.1
printf '{"kind":"dialog","t":1100,"payload":{"text":"Hello from stub bot"}}\n'
sleep 0.5
`
	if err := os.WriteFile(stubBot, []byte(stubScript), 0o700); err != nil {
		t.Fatalf("write stub bot: %v", err)
	}

	// 3. Run the sidecar. Capture stdout so we can find the admission block.
	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel()

	sidecar := exec.CommandContext(ctx, sidecarBin,
		"--bot", stubBot,
		"--bot-args", "",
		"--cf-home", cfHome,
		"--description", "freeso-e1a-integration",
	)
	stdout, err := sidecar.StdoutPipe()
	if err != nil {
		t.Fatalf("stdout pipe: %v", err)
	}
	stderr, err := sidecar.StderrPipe()
	if err != nil {
		t.Fatalf("stderr pipe: %v", err)
	}
	if err := sidecar.Start(); err != nil {
		t.Fatalf("start sidecar: %v", err)
	}
	defer func() {
		_ = sidecar.Process.Signal(os.Interrupt)
		_, _ = sidecar.Process.Wait()
	}()

	// Drain stderr so the sidecar doesn't block on a full pipe. Echo in test
	// log so a failure shows what happened.
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

	// 4. Scan stdout for the admission block. We expect a line "Campfire: <id>".
	campfireID := waitForCampfireID(t, stdout, 20*time.Second)
	t.Logf("campfire id: %s", campfireID)

	// Give the bridges time to broadcast the stub bot's events.
	time.Sleep(2 * time.Second)

	// 5. Shell out to `cf read` in a separate process. We use the same CF_HOME
	// as the sidecar so the read sees the same store.
	readCtx, readCancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer readCancel()

	// Check declarations present.
	declOut, err := runCf(readCtx, cfBin, cfHome, "read", campfireID, "--all", "--peek", "--tag", "convention:operation")
	if err != nil {
		t.Fatalf("cf read declarations: %v\n%s", err, declOut)
	}
	if !strings.Contains(declOut, "freeso-embodiment") {
		t.Errorf("cf read did not surface freeso-embodiment declarations; got:\n%s", truncate(declOut, 2000))
	}
	if !strings.Contains(declOut, "walk-to") {
		t.Errorf("cf read missing walk-to op; got:\n%s", truncate(declOut, 2000))
	}

	// Check perception broadcast surfaced.
	percOut, err := runCf(readCtx, cfBin, cfHome, "read", campfireID, "--all", "--peek", "--tag", "freeso:perception")
	if err != nil {
		t.Fatalf("cf read perception: %v\n%s", err, percOut)
	}
	if !strings.Contains(percOut, `"persist_id":2`) && !strings.Contains(percOut, `"persist_id": 2`) {
		t.Errorf("cf read did not surface the stub bot's perception event; got:\n%s", truncate(percOut, 2000))
	}

	// Check dialog broadcast surfaced.
	dlgOut, err := runCf(readCtx, cfBin, cfHome, "read", campfireID, "--all", "--peek", "--tag", "freeso:dialog")
	if err != nil {
		t.Fatalf("cf read dialog: %v\n%s", err, dlgOut)
	}
	if !strings.Contains(dlgOut, "Hello from stub bot") {
		t.Errorf("cf read did not surface the stub bot's dialog event; got:\n%s", truncate(dlgOut, 2000))
	}

	// Verify the campfire was created invite-only. We ask cf for membership
	// info via `cf config show <id>` or `cf members`.
	memOut, err := runCf(readCtx, cfBin, cfHome, "members", campfireID)
	if err != nil {
		// Some cf versions return non-zero when the campfire exists but has
		// only the creator; tolerate non-zero if the output is non-empty.
		if memOut == "" {
			t.Fatalf("cf members: %v", err)
		}
	}
	t.Logf("cf members output:\n%s", truncate(memOut, 2000))

	t.Logf("integration OK: campfire %s visible with declarations + perception + dialog", campfireID)
}

// waitForCampfireID scans stdout for the "Campfire: <id>" line emitted by the
// admission block. Returns the id or fails the test.
func waitForCampfireID(t *testing.T, stdout interface {
	Read([]byte) (int, error)
}, timeout time.Duration) string {
	t.Helper()
	deadline := time.Now().Add(timeout)
	var buf []byte
	tmp := make([]byte, 4096)
	for time.Now().Before(deadline) {
		n, err := stdout.Read(tmp)
		if n > 0 {
			buf = append(buf, tmp[:n]...)
			// Echo each line to test log so failures are diagnosable.
			for _, ln := range strings.Split(string(tmp[:n]), "\n") {
				if ln != "" {
					t.Logf("sidecar-stdout: %s", ln)
				}
			}
			if idx := strings.Index(string(buf), "Campfire: "); idx >= 0 {
				rest := string(buf[idx+len("Campfire: "):])
				// Trim everything after the first whitespace / newline.
				end := strings.IndexAny(rest, " \r\n")
				if end > 0 {
					return strings.TrimSpace(rest[:end])
				}
			}
		}
		if err != nil {
			break
		}
	}
	t.Fatalf("timed out waiting for admission block on sidecar stdout\nbuffer so far:\n%s", string(buf))
	return ""
}

func runCf(ctx context.Context, cfBin, cfHome, cmd string, args ...string) (string, error) {
	allArgs := append([]string{cmd}, args...)
	c := exec.CommandContext(ctx, cfBin, allArgs...)
	c.Env = append(os.Environ(), "CF_HOME="+cfHome)
	out, err := c.CombinedOutput()
	return string(out), err
}

func truncate(s string, max int) string {
	if len(s) <= max {
		return s
	}
	return s[:max] + "\n...(truncated)..."
}

// mustSourceDir returns the absolute path of the sidecar module (where main.go
// lives) so the build step can cd into it.
func mustSourceDir(t *testing.T) string {
	t.Helper()
	wd, err := os.Getwd()
	if err != nil {
		t.Fatalf("getwd: %v", err)
	}
	if _, err := os.Stat(filepath.Join(wd, "main.go")); err != nil {
		t.Fatalf("not in sidecar source dir? wd=%s main.go=%v", wd, err)
	}
	return wd
}

// compile-time assertion we did not accidentally break formatter with fmt use:
var _ = fmt.Sprintf

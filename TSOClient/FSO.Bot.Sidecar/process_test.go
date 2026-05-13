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
	"runtime"
	"strings"
	"sync"
	"testing"
	"time"
)

// TestFSOPassNotOnCLI asserts credentials never appear on the bot's command
// line. This preempts the credential-leak class the test harness in -8bc is
// fixing: FSO_PASS *must* reach the bot only through exec.Cmd.Env, never as
// an argument, never through shell interpolation.
//
// We launch a real subprocess (sleep-equivalent) with FSO_PASS in the env
// and then inspect /proc/<pid>/cmdline to confirm the secret value does not
// appear among the command-line arguments. On non-Linux systems the test
// uses `ps -o args= -p <pid>` as the fallback read path.
func TestFSOPassNotOnCLI(t *testing.T) {
	secret := "super-secret-password-" + fmt.Sprint(time.Now().UnixNano())

	// Use a portable long-running no-op: "sleep 2" on unix.
	execPath, err := exec.LookPath("sleep")
	if err != nil {
		t.Skipf("no 'sleep' binary on PATH: %v", err)
	}

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	proc, err := LaunchBot(ctx, BotConfig{
		Exec: execPath,
		Args: []string{"2"}, // sleep 2 seconds
		Env: append(os.Environ(),
			"FSO_PASS="+secret,
			"FSO_USER=test-user",
		),
	})
	if err != nil {
		t.Fatalf("LaunchBot: %v", err)
	}
	defer proc.Stop()

	pid := proc.Pid()
	if pid == 0 {
		t.Fatalf("no pid")
	}

	// Give the OS a beat to expose /proc state.
	time.Sleep(50 * time.Millisecond)

	args := readCmdline(t, pid)
	t.Logf("bot cmdline: %s", args)

	// The SECRET must not appear anywhere in the command-line arguments.
	if strings.Contains(args, secret) {
		t.Fatalf("FSO_PASS leaked onto bot command line (args contained the secret)\ncmdline=%s", args)
	}
	if !strings.Contains(args, "2") {
		t.Fatalf("sanity check: expected '2' in cmdline, got: %s", args)
	}

	// Bonus: verify /proc/<pid>/environ on Linux DOES contain FSO_PASS — that
	// confirms we routed it through Env and not lost it entirely. On other
	// platforms we skip this half.
	if runtime.GOOS == "linux" {
		envData, err := os.ReadFile(fmt.Sprintf("/proc/%d/environ", pid))
		if err == nil {
			if !strings.Contains(string(envData), secret) {
				t.Errorf("expected FSO_PASS to reach bot via env (not found in /proc/%d/environ)", pid)
			}
		}
	}
}

// readCmdline returns the argv of pid as a single readable string. On Linux,
// /proc/<pid>/cmdline is NUL-separated. On other platforms we fall back to
// `ps`.
func readCmdline(t *testing.T, pid int) string {
	t.Helper()
	if runtime.GOOS == "linux" {
		path := filepath.Join("/proc", fmt.Sprint(pid), "cmdline")
		data, err := os.ReadFile(path)
		if err != nil {
			t.Fatalf("read %s: %v", path, err)
		}
		// NUL-separated; make it readable.
		return strings.ReplaceAll(string(data), "\x00", " ")
	}
	out, err := exec.Command("ps", "-o", "args=", "-p", fmt.Sprint(pid)).Output()
	if err != nil {
		t.Fatalf("ps: %v", err)
	}
	return string(out)
}

// TestLaunchBotPipesStdoutNoPipeRace is a regression test for the pipe-close
// race introduced by calling cmd.Wait() before the scan goroutine has finished
// reading stdout. The race manifests as "expected 2 lines, got 0" because
// cmd.Wait() closes the StdoutPipe, truncating any buffered output.
//
// The fix: the Wait goroutine blocks on stdoutDone before calling cmd.Wait(),
// ensuring all stdout has been delivered to the Lines() channel first.
//
// This test runs the scenario 50 times in parallel to saturate the race window.
// Without the fix, at least one iteration fails with 0 lines (seen in ~1/10
// runs with -count=20 in CI). With the fix, all 50 must produce exactly 2 lines.
func TestLaunchBotPipesStdoutNoPipeRace(t *testing.T) {
	const iterations = 50
	var wg sync.WaitGroup
	wg.Add(iterations)
	for i := 0; i < iterations; i++ {
		i := i
		go func() {
			defer wg.Done()
			ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
			defer cancel()

			proc, err := LaunchBot(ctx, BotConfig{
				Exec: "sh",
				Args: []string{"-c", `printf '{"kind":"system","payload":{"event":"ready"}}\n{"kind":"perception","avatar":{"persist_id":2}}\n'`},
				Env:  os.Environ(),
			})
			if err != nil {
				t.Errorf("iter %d: LaunchBot: %v", i, err)
				return
			}
			defer proc.Stop()

			got := []string{}
			for line := range proc.Lines() {
				got = append(got, string(line))
				if len(got) >= 2 {
					break
				}
			}
			if len(got) < 2 {
				t.Errorf("iter %d: pipe-close race: expected 2 lines, got %d: %v", i, len(got), got)
			}
		}()
	}
	wg.Wait()
}

// TestLaunchBotPipesStdout: launch a simple program that writes a known line,
// consume it via the Lines() channel. Proves subprocess plumbing works before
// any real bot is involved.
func TestLaunchBotPipesStdout(t *testing.T) {
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	proc, err := LaunchBot(ctx, BotConfig{
		Exec: "sh",
		Args: []string{"-c", `printf '{"kind":"system","payload":{"event":"ready"}}\n{"kind":"perception","avatar":{"persist_id":2}}\n'`},
		Env:  os.Environ(),
	})
	if err != nil {
		t.Fatalf("LaunchBot: %v", err)
	}
	defer proc.Stop()

	got := []string{}
	for line := range proc.Lines() {
		got = append(got, string(line))
		if len(got) >= 2 {
			break
		}
	}
	if len(got) < 2 {
		t.Fatalf("expected 2 lines, got %d: %v", len(got), got)
	}
	if !strings.Contains(got[0], "system") || !strings.Contains(got[1], "perception") {
		t.Errorf("lines out of order / malformed: %v", got)
	}
}

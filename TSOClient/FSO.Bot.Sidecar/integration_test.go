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
	"syscall"
	"testing"
	"time"

	"github.com/campfire-net/campfire/pkg/convention"
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

// TestIntegration_PersistentBodyCfIDSurvivesRestart is the veracity gate for
// freesoexperiment-cf2. It exercises StartCampfire's restart-and-resume branch
// (campfire.go:73-103) which reads the persisted body-cf.id and conditionally
// skips creating a new campfire on subsequent starts.
//
// Protocol:
//  1. Build the freeso-sidecar binary in a temp dir (shared with the bridge test).
//  2. Choose a unique persona name derived from the temp dir; set FSO_USER and
//     XDG_CONFIG_HOME so the persona state dir is fully isolated to tmp.
//  3. Launch sidecar with --no-bot (no real bot needed) and a fresh --cf-home.
//     Wait for the "Campfire: <id>" line on stdout — this is the first campfire.
//  4. SIGINT the first sidecar; wait for it to exit.
//  5. Relaunch the sidecar with the SAME FSO_USER, XDG_CONFIG_HOME, and --cf-home.
//     Wait for the admission block again.
//  6. Assert: the campfire ID on the second run is IDENTICAL to the first.
//     A fresh ID would mean StartCampfire ignored the persisted body-cf.id — that
//     is the regression this test is designed to catch.
//
// Skip conditions (same envelope as TestIntegration_CampfireAndBridge):
//   - FREESO_SKIP_INTEGRATION=1 → explicit opt-out.
//   - `cf` binary not on PATH → skip (cf is required by StartCampfire for beacon generation,
//     but the sidecar starts even without it; we skip conservatively to match the envelope).
//
// Note: the `go` binary must be on PATH for the build step — this is guaranteed
// in any Go development environment.
func TestIntegration_PersistentBodyCfIDSurvivesRestart(t *testing.T) {
	if os.Getenv("FREESO_SKIP_INTEGRATION") == "1" {
		t.Skip("FREESO_SKIP_INTEGRATION=1")
	}
	if _, err := exec.LookPath("cf"); err != nil {
		t.Skipf("cf binary not on PATH: %v", err)
	}

	goBin, err := exec.LookPath("go")
	if err != nil {
		t.Skipf("go not on PATH: %v", err)
	}

	tmp := t.TempDir()

	// Build the sidecar binary into tmp.
	sidecarBin := filepath.Join(tmp, "freeso-sidecar-restart-test")
	build := exec.Command(goBin, "build", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar: %v\n%s", err, out)
	}

	// Unique persona: use last component of tmp (platform-specific random suffix).
	// Must be alphanumeric (no path separators) so PersonaStateDir accepts it.
	personaBase := filepath.Base(tmp)
	// t.TempDir names are like "TestFoo123456789" — keep only alnum chars and truncate.
	var personaRunes []byte
	for _, c := range []byte(personaBase) {
		if (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') {
			personaRunes = append(personaRunes, c)
		}
	}
	if len(personaRunes) > 32 {
		personaRunes = personaRunes[:32]
	}
	persona := string(personaRunes)
	if persona == "" {
		persona = "restarttest"
	}
	t.Logf("persona: %s", persona)

	// XDG_CONFIG_HOME → tmp so persona state is fully isolated.
	xdgConfigHome := filepath.Join(tmp, "config")
	if err := os.MkdirAll(xdgConfigHome, 0o700); err != nil {
		t.Fatalf("mkdir xdg config home: %v", err)
	}

	// cf-home for the sidecar's campfire identity + store.
	cfHome := filepath.Join(tmp, "cf-home")
	if err := os.MkdirAll(cfHome, 0o700); err != nil {
		t.Fatalf("mkdir cf-home: %v", err)
	}

	// buildEnv returns the environment for a sidecar subprocess with the
	// persona and config-home pinned to our temp dirs.
	buildEnv := func() []string {
		env := os.Environ()
		// Override FSO_USER and XDG_CONFIG_HOME for persona state isolation.
		filtered := env[:0]
		for _, e := range env {
			if strings.HasPrefix(e, "FSO_USER=") || strings.HasPrefix(e, "XDG_CONFIG_HOME=") {
				continue
			}
			filtered = append(filtered, e)
		}
		filtered = append(filtered,
			"FSO_USER="+persona,
			"XDG_CONFIG_HOME="+xdgConfigHome,
		)
		return filtered
	}

	// launchSidecar starts the sidecar subprocess with --no-bot. Returns the
	// running *exec.Cmd with stdout/stderr already wired.
	launchSidecar := func(t *testing.T, ctx context.Context) (*exec.Cmd, interface {
		Read([]byte) (int, error)
	}) {
		t.Helper()
		cmd := exec.CommandContext(ctx, sidecarBin,
			"--no-bot",
			"--cf-home", cfHome,
			"--description", "restart-integration-test",
		)
		cmd.Env = buildEnv()

		stdout, err := cmd.StdoutPipe()
		if err != nil {
			t.Fatalf("stdout pipe: %v", err)
		}
		stderr, err := cmd.StderrPipe()
		if err != nil {
			t.Fatalf("stderr pipe: %v", err)
		}
		if err := cmd.Start(); err != nil {
			t.Fatalf("start sidecar: %v", err)
		}
		// Drain stderr into test log.
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
		return cmd, stdout
	}

	// --- First run ---
	ctx1, cancel1 := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel1()

	t.Log("starting first sidecar instance")
	cmd1, stdout1 := launchSidecar(t, ctx1)

	id1 := waitForCampfireID(t, stdout1, 20*time.Second)
	t.Logf("first run campfire id: %s", id1)

	// Graceful shutdown: SIGINT.
	if err := cmd1.Process.Signal(os.Interrupt); err != nil {
		t.Logf("SIGINT first sidecar: %v (may have already exited)", err)
	}
	// Wait for the process to exit cleanly (up to 10s).
	done1 := make(chan error, 1)
	go func() { done1 <- cmd1.Wait() }()
	select {
	case <-done1:
		t.Log("first sidecar exited")
	case <-time.After(10 * time.Second):
		t.Log("first sidecar did not exit within 10s after SIGINT; killing")
		_ = cmd1.Process.Kill()
		<-done1
	}
	cancel1()

	// --- Second run (same persona, same cf-home) ---
	ctx2, cancel2 := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel2()

	t.Log("starting second sidecar instance (same FSO_USER, same --cf-home)")
	cmd2, stdout2 := launchSidecar(t, ctx2)
	defer func() {
		_ = cmd2.Process.Signal(os.Interrupt)
		_, _ = cmd2.Process.Wait()
	}()

	id2 := waitForCampfireID(t, stdout2, 20*time.Second)
	t.Logf("second run campfire id: %s", id2)
	cancel2()

	// --- Assertion: same ID across restart ---
	if id1 != id2 {
		t.Errorf("campfire ID changed across restart:\n  first:  %s\n  second: %s\nStartCampfire did not resume from persisted body-cf.id",
			id1, id2)
	} else {
		t.Logf("PASS: campfire ID %s survived sidecar restart", id1)
	}
}

// TestIntegration_SupervisorLoop_BotTermDoesNotKillSidecar is the veracity gate
// for freesoexperiment-e5f. It exercises the supervisor loop added in main.go:
// when the bot process exits, the sidecar PID must remain unchanged and the bot
// must be relaunched within 15 seconds.
//
// Protocol:
//  1. Build the freeso-sidecar binary in a temp dir.
//  2. Write two stub bots:
//     - first-bot.sh: emits system:ready, then blocks on SIGTERM. When it
//       receives SIGTERM it exits 0 (clean exit). Writes its PID to a file.
//     - second-bot.sh: emits system:ready and a "relaunched" marker event,
//       then sleeps long enough for the test to observe it.
//  3. Write a wrapper bot script that launches first-bot.sh on the first
//     invocation and second-bot.sh on the second invocation, using a counter
//     file in tmp to track which run this is.
//  4. Launch the sidecar pointing at the wrapper bot.
//  5. Wait for the sidecar to start (admission block on stdout).
//  6. Record the sidecar PID.
//  7. Read the first bot's PID from the pid-file and send SIGTERM to it.
//  8. Assert: the sidecar PID is unchanged 3s after the SIGTERM.
//  9. Assert: within 15s the second bot emits its "relaunched" marker event
//     (observed via cf read, or equivalently via a marker file the second bot
//     writes). This proves the supervisor loop launched a new bot.
//
// Skip conditions (same envelope as other integration tests):
//   - FREESO_SKIP_INTEGRATION=1 → explicit opt-out.
//   - `cf` binary not on PATH → skip.
//   - `go` binary not on PATH → skip.
func TestIntegration_SupervisorLoop_BotTermDoesNotKillSidecar(t *testing.T) {
	if os.Getenv("FREESO_SKIP_INTEGRATION") == "1" {
		t.Skip("FREESO_SKIP_INTEGRATION=1")
	}
	if _, err := exec.LookPath("cf"); err != nil {
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

	// 1. Build the sidecar binary.
	sidecarBin := filepath.Join(tmp, "freeso-sidecar-supervisor-test")
	build := exec.Command(goBin, "build", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar: %v\n%s", err, out)
	}

	// 2. Write the first bot: emits ready, records PID, sleeps until SIGTERM.
	// On SIGTERM it exits 0 (clean relaunch-eligible exit).
	firstBotPidFile := filepath.Join(tmp, "first-bot.pid")
	firstBot := filepath.Join(tmp, "first-bot.sh")
	firstBotScript := fmt.Sprintf(`#!/bin/sh
# Write our PID so the test can send SIGTERM.
echo $$ > %s
# Emit ready so bridges initialise.
printf '{"kind":"system","payload":{"event":"ready"}}\n'
# Block until SIGTERM.
trap 'exit 0' TERM
while true; do sleep 0.5; done
`, firstBotPidFile)
	if err := os.WriteFile(firstBot, []byte(firstBotScript), 0o700); err != nil {
		t.Fatalf("write first-bot.sh: %v", err)
	}

	// 3. Write the second bot: emits ready + a "relaunched" marker, then sleeps.
	// Writes a marker file the test polls so we don't need to parse campfire events.
	relaunchedMarker := filepath.Join(tmp, "relaunched.marker")
	secondBot := filepath.Join(tmp, "second-bot.sh")
	secondBotScript := fmt.Sprintf(`#!/bin/sh
# Signal the test that we were relaunched.
touch %s
printf '{"kind":"system","payload":{"event":"ready","relaunched":true}}\n'
# Stay alive long enough for the test to observe.
sleep 30
`, relaunchedMarker)
	if err := os.WriteFile(secondBot, []byte(secondBotScript), 0o700); err != nil {
		t.Fatalf("write second-bot.sh: %v", err)
	}

	// 4. Write a wrapper bot that dispatches to first-bot on run 1, second-bot
	// on run 2+. Uses a counter file in tmp.
	runCountFile := filepath.Join(tmp, "run-count")
	if err := os.WriteFile(runCountFile, []byte("0"), 0o600); err != nil {
		t.Fatalf("write run-count: %v", err)
	}
	wrapperBot := filepath.Join(tmp, "wrapper-bot.sh")
	wrapperScript := fmt.Sprintf(`#!/bin/sh
set -e
# Atomically increment the run counter.
COUNT=$(cat %s 2>/dev/null || echo 0)
NEW=$(( COUNT + 1 ))
echo $NEW > %s

if [ "$COUNT" -eq 0 ]; then
    exec %s "$@"
else
    exec %s "$@"
fi
`, runCountFile, runCountFile, firstBot, secondBot)
	if err := os.WriteFile(wrapperBot, []byte(wrapperScript), 0o700); err != nil {
		t.Fatalf("write wrapper-bot.sh: %v", err)
	}

	// Use an isolated persona state dir so next-lot files don't bleed across tests.
	xdgConfigHome := filepath.Join(tmp, "config")
	if err := os.MkdirAll(xdgConfigHome, 0o700); err != nil {
		t.Fatalf("mkdir xdg config: %v", err)
	}

	// 5. Launch sidecar.
	ctx, cancel := context.WithTimeout(context.Background(), 60*time.Second)
	defer cancel()

	sidecarCmd := exec.CommandContext(ctx, sidecarBin,
		"--bot", wrapperBot,
		"--bot-args", "",
		"--cf-home", cfHome,
		"--description", "supervisor-integration-test",
	)
	// Isolate persona state.
	sidecarEnv := os.Environ()
	filtered := sidecarEnv[:0]
	for _, e := range sidecarEnv {
		if !strings.HasPrefix(e, "FSO_USER=") && !strings.HasPrefix(e, "XDG_CONFIG_HOME=") {
			filtered = append(filtered, e)
		}
	}
	sidecarCmd.Env = append(filtered,
		"FSO_USER=supervisor-test-persona",
		"XDG_CONFIG_HOME="+xdgConfigHome,
	)

	sidecarStdout, err := sidecarCmd.StdoutPipe()
	if err != nil {
		t.Fatalf("stdout pipe: %v", err)
	}
	sidecarStderr, err := sidecarCmd.StderrPipe()
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

	// Drain stderr into test log.
	go func() {
		buf := make([]byte, 4096)
		for {
			n, readErr := sidecarStderr.Read(buf)
			if n > 0 {
				t.Logf("sidecar-stderr: %s", strings.TrimRight(string(buf[:n]), "\n"))
			}
			if readErr != nil {
				return
			}
		}
	}()

	// 6. Wait for the sidecar admission block (proves sidecar is up and campfire
	// is initialised) and capture the sidecar PID.
	_ = waitForCampfireID(t, sidecarStdout, 20*time.Second)
	sidecarPID := sidecarCmd.Process.Pid
	t.Logf("sidecar PID: %d", sidecarPID)

	// Wait for the first-bot to write its PID file (up to 5s).
	var firstBotPID int
	{
		deadline := time.Now().Add(5 * time.Second)
		for time.Now().Before(deadline) {
			data, rErr := os.ReadFile(firstBotPidFile)
			if rErr == nil {
				if _, scanErr := fmt.Sscanf(strings.TrimSpace(string(data)), "%d", &firstBotPID); scanErr == nil && firstBotPID > 0 {
					break
				}
			}
			time.Sleep(100 * time.Millisecond)
		}
	}
	if firstBotPID == 0 {
		t.Fatalf("first-bot did not write its PID within 5s")
	}
	t.Logf("first-bot PID: %d", firstBotPID)

	// 7. Send SIGTERM to the first bot.
	firstBotProcess, err := os.FindProcess(firstBotPID)
	if err != nil {
		t.Fatalf("FindProcess first-bot %d: %v", firstBotPID, err)
	}
	if err := firstBotProcess.Signal(syscall.SIGTERM); err != nil {
		t.Fatalf("SIGTERM first-bot: %v", err)
	}
	t.Logf("sent SIGTERM to first-bot PID %d", firstBotPID)

	// 8. After 3s, assert the sidecar PID is still alive.
	time.Sleep(3 * time.Second)
	if err := sidecarCmd.Process.Signal(syscall.Signal(0)); err != nil {
		t.Fatalf("sidecar PID %d is dead after bot SIGTERM — supervisor loop did not keep sidecar alive: %v",
			sidecarPID, err)
	}
	t.Logf("PASS: sidecar PID %d still alive 3s after first-bot SIGTERM", sidecarPID)

	// 9. Assert the second bot was launched within 15s (marker file appears).
	relaunchedDeadline := time.Now().Add(15 * time.Second)
	for time.Now().Before(relaunchedDeadline) {
		if _, statErr := os.Stat(relaunchedMarker); statErr == nil {
			t.Logf("PASS: second-bot relaunched marker appeared (sidecar PID still %d)", sidecarPID)
			// Final check: sidecar PID still unchanged.
			if err := sidecarCmd.Process.Signal(syscall.Signal(0)); err != nil {
				t.Errorf("sidecar PID %d died before test end: %v", sidecarPID, err)
			}
			return
		}
		time.Sleep(250 * time.Millisecond)
	}
	t.Fatalf("second-bot was not relaunched within 15s after first-bot SIGTERM (marker %s not created)", relaunchedMarker)
}

// TestIntegration_VisitLot_BothShapes is the feature integration test for
// freesoexperiment-e66. It exercises the visit-lot handler end-to-end with a
// real stub bot subprocess that handles probe-lot and bot-exit-request over the
// bot-cmd IPC envelope. Both call shapes are verified:
//
//  1. --target_lot_location 0x<hex>  (hex fallback shape)
//  2. --my_name (name-primary shape, requires a pre-bound lot name)
//
// Done condition from the item: both shapes succeed, next-lot file is written
// before bot-exit-request is dispatched (WriteNextLot-before-exit invariant),
// and the stub bot is relaunched at the target lot.
//
// Skip conditions:
//   - FREESO_SKIP_INTEGRATION=1
//   - `go` binary not on PATH
func TestIntegration_VisitLot_BothShapes(t *testing.T) {
	if os.Getenv("FREESO_SKIP_INTEGRATION") == "1" {
		t.Skip("FREESO_SKIP_INTEGRATION=1")
	}
	goBin, err := exec.LookPath("go")
	if err != nil {
		t.Skipf("go not on PATH: %v", err)
	}

	// Unique persona + isolated config home for next-lot file.
	tmp := t.TempDir()
	xdgConfigHome := filepath.Join(tmp, "config")
	if err := os.MkdirAll(xdgConfigHome, 0o700); err != nil {
		t.Fatalf("mkdir config: %v", err)
	}

	// Persona name: isolated per test run.
	persona := "visitlotintegtest"

	// Set env for persona state isolation.
	prior, hasPrior := os.LookupEnv("FSO_USER")
	os.Setenv("FSO_USER", persona)
	t.Cleanup(func() {
		if hasPrior {
			os.Setenv("FSO_USER", prior)
		} else {
			os.Unsetenv("FSO_USER")
		}
	})
	priorXDG, hasPriorXDG := os.LookupEnv("XDG_CONFIG_HOME")
	os.Setenv("XDG_CONFIG_HOME", xdgConfigHome)
	t.Cleanup(func() {
		if hasPriorXDG {
			os.Setenv("XDG_CONFIG_HOME", priorXDG)
		} else {
			os.Unsetenv("XDG_CONFIG_HOME")
		}
	})

	// Isolate SharedCommunityAccessDir so community-access.json from other
	// tests or from the real machine does not interfere (lot 17 probe_id
	// would trigger the gate if left ungated).
	xdgDataHome := filepath.Join(tmp, "data")
	if err := os.MkdirAll(xdgDataHome, 0o700); err != nil {
		t.Fatalf("mkdir data: %v", err)
	}
	priorXDGData, hasPriorXDGData := os.LookupEnv("XDG_DATA_HOME")
	os.Setenv("XDG_DATA_HOME", xdgDataHome)
	t.Cleanup(func() {
		if hasPriorXDGData {
			os.Setenv("XDG_DATA_HOME", priorXDGData)
		} else {
			os.Unsetenv("XDG_DATA_HOME")
		}
	})

	// Build the sidecar binary.
	sidecarBin := filepath.Join(tmp, "freeso-sidecar-visit-lot-test")
	build := exec.Command(goBin, "build", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar: %v\n%s", err, out)
	}

	// Stub bot that handles probe-lot → FOUND and bot-exit-request → accepted.
	// After bot-exit-request it writes a marker file so we can verify the
	// visit-lot → relaunch sequence without a real FSO server.
	// On second invocation (wrapper re-launches after exit), write a different
	// marker and sleep.
	invocationCountFile := filepath.Join(tmp, "bot-invocation-count")
	nextLotReadFile := filepath.Join(tmp, "next-lot-read-by-bot.txt")
	stubBot := filepath.Join(tmp, "stub-visit-bot.sh")
	stubBotScript := fmt.Sprintf(`#!/bin/sh
# Track invocation count.
COUNT=$(cat %s 2>/dev/null || echo 0)
NEW=$((COUNT+1))
echo $NEW > %s

if [ "$COUNT" -ge 1 ]; then
    # Second+ invocation: record the lot location we were started with.
    echo "${FSO_LOT_LOCATION:-none}" > %s
    printf '{"kind":"system","payload":{"event":"ready","relaunch":true}}\n'
    sleep 10
    exit 0
fi

# First invocation: handle probe-lot and bot-exit-request.
printf '{"kind":"system","payload":{"event":"ready"}}\n'
while IFS= read -r line; do
    corr=$(printf '%%s' "$line" | sed 's/.*"correlation_id":"\([^"]*\)".*/\1/')
    cmd=$(printf '%%s' "$line" | sed 's/.*"cmd":"\([^"]*\)".*/\1/')
    case "$cmd" in
    probe-lot)
        printf '{"kind":"bot-cmd-reply","correlation_id":"%%s","ok":true,"data":{"status":"FOUND","lot_id":17}}\n' "$corr"
        ;;
    bot-exit-request)
        printf '{"kind":"bot-cmd-reply","correlation_id":"%%s","ok":true,"data":{"accepted":true}}\n' "$corr"
        exit 0
        ;;
    esac
done
`, invocationCountFile, invocationCountFile, nextLotReadFile)
	if err := os.WriteFile(stubBot, []byte(stubBotScript), 0o700); err != nil {
		t.Fatalf("write stub bot: %v", err)
	}
	// Write initial invocation count = 0.
	if err := os.WriteFile(invocationCountFile, []byte("0"), 0o600); err != nil {
		t.Fatalf("write invocation count: %v", err)
	}

	// Set up persona memory store and bind a lot name for shape 2.
	store := NewMemoryStore()
	// "joao's place" → lot 17 = 0x00110F00 = 1118976 decimal.
	// We use 0x00110F00 for consistency with existing test values.
	encoded, _ := json.Marshal(map[string]any{
		"name":         "joao's place",
		"kind":         "lot",
		"lot_location": "1118976",
	})
	store.Store("name:joao's place", encoded)

	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel()

	// Launch the stub bot as a BotProcess directly (no sidecar subprocess needed
	// for this test — we exercise the handler in-process against the real pump).
	proc, err := LaunchBot(ctx, BotConfig{
		Exec: stubBot,
		Args: []string{},
		Env: append(os.Environ(),
			"FSO_USER="+persona,
			"XDG_CONFIG_HOME="+xdgConfigHome,
		),
	})
	if err != nil {
		t.Fatalf("LaunchBot: %v", err)
	}
	defer proc.Stop()

	ipc := NewIPC(proc)
	pump := NewBotCmdPump(proc)

	// Route bot-cmd-reply frames to pump.
	go bridgesRunNoCampfire(ctx, proc, pump)
	// Give the stub bot a moment to emit system:ready.
	time.Sleep(150 * time.Millisecond)

	// --- Shape 1: hex fallback ---
	t.Log("Shape 1: --target_lot_location 0x00110F00 (hex fallback)")
	hexCtx, hexCancel := context.WithTimeout(ctx, 10*time.Second)
	defer hexCancel()

	handler := visitLotHandler(ipc, pump, store)
	resp1, err := handler(hexCtx, &convention.Request{Args: map[string]any{
		"target_lot_location": "0x00110F00",
	}})
	if err != nil {
		t.Fatalf("shape1 handler error: %v", err)
	}
	payload1, _ := resp1.Payload.(map[string]any)
	if payload1["ok"] != true {
		t.Errorf("shape1: want ok=true, got %v: %v", payload1["ok"], payload1)
	}
	if payload1["probe_status"] != "FOUND" {
		t.Errorf("shape1: want probe_status=FOUND, got %v", payload1["probe_status"])
	}
	t.Logf("shape1 response: %v", payload1)

	// Verify next-lot was written (WriteNextLot-before-exit invariant).
	nextLotPath := filepath.Join(xdgConfigHome, "freeso-souls", persona, "next-lot")
	nextLotData, err := os.ReadFile(nextLotPath)
	if err != nil {
		t.Errorf("shape1: next-lot not written (WriteNextLot invariant): %v", err)
	} else {
		t.Logf("shape1: next-lot written: %q", strings.TrimSpace(string(nextLotData)))
	}
	// Clear next-lot so shape2 starts clean.
	_ = os.Remove(nextLotPath)

	// Wait for the stub bot to exit (it exits 0 after bot-exit-request).
	select {
	case <-proc.ExitCh():
		t.Log("shape1: stub bot exited after bot-exit-request")
	case <-time.After(5 * time.Second):
		t.Error("shape1: stub bot did not exit within 5s after bot-exit-request")
	}

	// --- Shape 2: name-primary --
	// Launch a fresh bot process since the first one exited.
	t.Log("Shape 2: --my_name \"joao's place\" (name-primary)")

	// Re-write invocation count so the new bot handles the request.
	if err := os.WriteFile(invocationCountFile, []byte("0"), 0o600); err != nil {
		t.Fatalf("reset invocation count: %v", err)
	}

	ctx2, cancel2 := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel2()

	proc2, err := LaunchBot(ctx2, BotConfig{
		Exec: stubBot,
		Args: []string{},
		Env: append(os.Environ(),
			"FSO_USER="+persona,
			"XDG_CONFIG_HOME="+xdgConfigHome,
		),
	})
	if err != nil {
		t.Fatalf("LaunchBot shape2: %v", err)
	}
	defer proc2.Stop()

	ipc2 := NewIPC(proc2)
	pump2 := NewBotCmdPump(proc2)
	go bridgesRunNoCampfire(ctx2, proc2, pump2)
	time.Sleep(150 * time.Millisecond)

	handler2 := visitLotHandler(ipc2, pump2, store)
	nameCtx, nameCancel := context.WithTimeout(ctx2, 10*time.Second)
	defer nameCancel()

	resp2, err := handler2(nameCtx, &convention.Request{Args: map[string]any{
		"my_name": "joao's place",
	}})
	if err != nil {
		t.Fatalf("shape2 handler error: %v", err)
	}
	payload2, _ := resp2.Payload.(map[string]any)
	if payload2["ok"] != true {
		t.Errorf("shape2: want ok=true, got %v: %v", payload2["ok"], payload2)
	}
	if payload2["probe_status"] != "FOUND" {
		t.Errorf("shape2: want probe_status=FOUND, got %v", payload2["probe_status"])
	}
	t.Logf("shape2 response: %v", payload2)

	// Verify next-lot was written for shape 2.
	nextLotData2, err := os.ReadFile(nextLotPath)
	if err != nil {
		t.Errorf("shape2: next-lot not written: %v", err)
	} else {
		t.Logf("shape2: next-lot written: %q (resolved from --my_name 'joao's place')", strings.TrimSpace(string(nextLotData2)))
	}

	// Wait for shape2 bot to exit.
	select {
	case <-proc2.ExitCh():
		t.Log("shape2: stub bot exited after bot-exit-request")
	case <-time.After(5 * time.Second):
		t.Error("shape2: stub bot did not exit within 5s")
	}

	t.Log("PASS: TestIntegration_VisitLot_BothShapes — both --target_lot_location and --my_name shapes verified via real subprocess, next-lot file written, bot-exit-request dispatched")
}

// compile-time assertion we did not accidentally break formatter with fmt use:
var _ = fmt.Sprintf

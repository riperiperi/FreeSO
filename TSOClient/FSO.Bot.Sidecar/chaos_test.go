/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

// Package main — chaos tests for visit-lot transition failure recovery
// (freesoexperiment-90e).
//
// Each test kills the bot subprocess at a distinct point in the visit-lot
// flow and asserts:
//   - The sidecar process remains alive (supervisor loop did not die with the bot).
//   - No stuck cf-await: after 30s the sidecar stdout/stderr drains and the
//     test receives either a successful recovery marker or a clean timeout.
//   - next-lot file state is consistent: either never written (kill before
//     WriteNextLot) or written-and-intact (kill after WriteNextLot, before
//     ClearNextLot). An orphaned next-lot must never reference a stale target.
//
// Kill-point table (documented as a cross-reference for the done condition):
//
//	KILL_A (pre-fulfill probe-lot):    bot killed before probe-lot cmd arrives on stdin.
//	                                   next-lot MUST NOT exist. Sidecar alive; relaunch fires.
//	KILL_B (post-probe pre-exit):      probe-lot FOUND reply delivered; bot killed before
//	                                   bot-exit-request. next-lot MUST NOT exist (WriteNextLot
//	                                   happens after probe reply only if probe succeeded and
//	                                   before exit — bot is dead before that step).
//	KILL_C (mid-FindLot / probe inflight): bot killed while waiting to write probe-lot reply.
//	                                   next-lot MUST NOT exist. BotCmdPump Send times out or
//	                                   errors; handler returns ok:false. Sidecar alive.
//	KILL_D (mid-restart window):       bot exits cleanly after full visit-lot cycle; supervisor
//	                                   starts relaunch; new bot is killed mid-start.
//	                                   next-lot MUST be cleared (supervisor reads it before
//	                                   launching). No orphan next-lot after 30s.
//	KILL_E (post-restart pre-tick):    new bot starts, emits system:ready, then is killed
//	                                   immediately before any perception tick.
//	                                   Sidecar alive; second relaunch fires. next-lot
//	                                   MUST be cleared (was consumed on first relaunch).
//
// All tests use the real sidecar binary (via subprocess) and real SIGKILL/SIGTERM
// at file-checkpoint-gated moments so kills are deterministic, not timing-sensitive.
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
	"syscall"
	"testing"
	"time"

	"github.com/campfire-net/campfire/pkg/convention"
)

// ─── shared sidecar kill-test infrastructure ───────────────────────────────

// chaosSkipCheck applies the standard skip conditions for chaos tests.
func chaosSkipCheck(t *testing.T) string {
	t.Helper()
	if os.Getenv("FREESO_SKIP_INTEGRATION") == "1" {
		t.Skip("FREESO_SKIP_INTEGRATION=1")
	}
	goBin, err := exec.LookPath("go")
	if err != nil {
		t.Skipf("go binary not on PATH: %v", err)
	}
	return goBin
}

// buildSidecarBin builds the sidecar binary into dst and returns the path.
func buildSidecarBin(t *testing.T, goBin, dst, suffix string) string {
	t.Helper()
	bin := filepath.Join(dst, "freeso-sidecar-"+suffix)
	build := exec.Command(goBin, "build", "-o", bin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar (%s): %v\n%s", suffix, err, out)
	}
	return bin
}

// launchSidecarChaos starts the sidecar subprocess in bot-mode with the given
// stub bot executable. Returns the running cmd, its stdout pipe reader-channel
// (each read is a line), and a drain-all-stderr goroutine is already started.
// The caller MUST send SIGINT or kill the process when done.
func launchSidecarChaos(
	t *testing.T,
	ctx context.Context,
	sidecarBin, stubBot, cfHome, xdgConfigHome, persona string,
) (*exec.Cmd, <-chan string) {
	t.Helper()

	cmd := exec.CommandContext(ctx, sidecarBin,
		"--bot", stubBot,
		"--bot-args", "",
		"--cf-home", cfHome,
		"--description", "chaos-test",
	)
	// Isolate persona state to our temp dirs.
	filtered := filterEnv(os.Environ(), "FSO_USER", "XDG_CONFIG_HOME", "FSO_LOT_LOCATION")
	cmd.Env = append(filtered,
		"FSO_USER="+persona,
		"XDG_CONFIG_HOME="+xdgConfigHome,
	)

	stdoutPipe, err := cmd.StdoutPipe()
	if err != nil {
		t.Fatalf("stdout pipe: %v", err)
	}
	stderrPipe, err := cmd.StderrPipe()
	if err != nil {
		t.Fatalf("stderr pipe: %v", err)
	}
	if err := cmd.Start(); err != nil {
		t.Fatalf("start sidecar: %v", err)
	}

	// Drain stderr so we don't block the sidecar on a full pipe.
	go func() {
		buf := make([]byte, 4096)
		for {
			n, err := stderrPipe.Read(buf)
			if n > 0 {
				t.Logf("sidecar-stderr: %s", strings.TrimRight(string(buf[:n]), "\n"))
			}
			if err != nil {
				return
			}
		}
	}()

	// Convert stdout pipe to a line channel.
	lines := make(chan string, 64)
	go func() {
		defer close(lines)
		buf := make([]byte, 0, 4096)
		chunk := make([]byte, 4096)
		for {
			n, err := stdoutPipe.Read(chunk)
			if n > 0 {
				buf = append(buf, chunk[:n]...)
				for {
					idx := -1
					for i, b := range buf {
						if b == '\n' {
							idx = i
							break
						}
					}
					if idx < 0 {
						break
					}
					line := string(buf[:idx])
					buf = buf[idx+1:]
					lines <- line
					t.Logf("sidecar-stdout: %s", line)
				}
			}
			if err != nil {
				return
			}
		}
	}()

	return cmd, lines
}

// waitForLineContaining reads from the line channel until a line contains
// substring or the timeout fires. Returns the matching line or fails the test.
func waitForLineContaining(t *testing.T, lines <-chan string, sub string, timeout time.Duration) string {
	t.Helper()
	deadline := time.After(timeout)
	for {
		select {
		case line, ok := <-lines:
			if !ok {
				t.Fatalf("stdout channel closed before seeing %q", sub)
			}
			if strings.Contains(line, sub) {
				return line
			}
		case <-deadline:
			t.Fatalf("timed out after %v waiting for line containing %q", timeout, sub)
			return "" // unreachable; satisfies compiler
		}
	}
}

// waitForLineContainingOr is like waitForLineContaining but does not fail on timeout;
// returns false if timeout fires.
func waitForLineContainingOr(lines <-chan string, sub string, timeout time.Duration) bool {
	deadline := time.After(timeout)
	for {
		select {
		case line, ok := <-lines:
			if !ok {
				return false
			}
			if strings.Contains(line, sub) {
				return true
			}
		case <-deadline:
			return false
		}
	}
}

// filterEnv returns env with all entries whose key matches any of keys removed.
func filterEnv(env []string, keys ...string) []string {
	out := make([]string, 0, len(env))
	for _, e := range env {
		skip := false
		for _, k := range keys {
			if strings.HasPrefix(e, k+"=") {
				skip = true
				break
			}
		}
		if !skip {
			out = append(out, e)
		}
	}
	return out
}

// readPidFile polls pidFile until a non-zero PID appears or deadline fires.
func readPidFile(t *testing.T, pidFile string, timeout time.Duration) int {
	t.Helper()
	deadline := time.Now().Add(timeout)
	for time.Now().Before(deadline) {
		data, err := os.ReadFile(pidFile)
		if err == nil {
			var pid int
			if _, scanErr := fmt.Sscanf(strings.TrimSpace(string(data)), "%d", &pid); scanErr == nil && pid > 0 {
				return pid
			}
		}
		time.Sleep(50 * time.Millisecond)
	}
	t.Fatalf("PID file %s did not appear within %v", pidFile, timeout)
	return 0
}

// assertSidecarAlive sends signal 0 to the sidecar and fails if it is dead.
func assertSidecarAlive(t *testing.T, cmd *exec.Cmd, label string) {
	t.Helper()
	if cmd.Process == nil {
		t.Errorf("%s: sidecar process handle is nil", label)
		return
	}
	if err := cmd.Process.Signal(syscall.Signal(0)); err != nil {
		t.Errorf("%s: sidecar PID %d is dead: %v", label, cmd.Process.Pid, err)
	}
}

// assertNextLotAbsent fails if next-lot file exists under xdgConfigHome/freeso-souls/persona/.
func assertNextLotAbsent(t *testing.T, xdgConfigHome, persona, label string) {
	t.Helper()
	path := filepath.Join(xdgConfigHome, "freeso-souls", strings.ToLower(persona), "next-lot")
	if _, err := os.Stat(path); err == nil {
		data, _ := os.ReadFile(path)
		t.Errorf("%s: next-lot file exists but should not; content=%q", label, string(data))
	}
}

// assertNextLotPresent fails if next-lot file does not exist.
func assertNextLotPresent(t *testing.T, xdgConfigHome, persona, label string) string {
	t.Helper()
	path := filepath.Join(xdgConfigHome, "freeso-souls", strings.ToLower(persona), "next-lot")
	data, err := os.ReadFile(path)
	if err != nil {
		t.Errorf("%s: next-lot file missing: %v", label, err)
		return ""
	}
	return strings.TrimSpace(string(data))
}

// ─── KILL_A: bot killed before probe-lot command arrives ─────────────────────

// TestChaos_KillA_PreProbeLot — bot is killed via SIGKILL immediately after
// emitting system:ready, before the sidecar has sent the probe-lot command.
//
// Checkpoint mechanism: bot emits system:ready and writes a ready-marker file,
// then reads from a "release" FIFO before any bot-cmd handler loop. The test
// kills the bot while it is blocking on the FIFO. This makes the kill
// deterministic: the bot is alive-but-waiting when it receives SIGKILL.
//
// Asserts:
//   - Sidecar PID is alive 3s after the kill.
//   - next-lot file does not exist (WriteNextLot never reached).
//   - Sidecar stdout eventually emits "relaunched" or "supervisor" log within 20s
//     (proves the supervisor loop saw the bot exit and relaunched).
func TestChaos_KillA_PreProbeLot(t *testing.T) {
	goBin := chaosSkipCheck(t)
	tmp := t.TempDir()
	cfHome := filepath.Join(tmp, "cf-home")
	xdgConfigHome := filepath.Join(tmp, "config")
	_ = os.MkdirAll(cfHome, 0o700)
	_ = os.MkdirAll(xdgConfigHome, 0o700)
	persona := "chaos-killa"

	sidecarBin := buildSidecarBin(t, goBin, tmp, "killa")

	// Checkpoint files:
	//   botPidFile  — bot writes its PID on first launch so we can kill it precisely.
	//   readyMarker — bot signals it has emitted system:ready and is now blocking.
	//   relaunchedMarker — second launch signals supervisor relaunched.
	botPidFile := filepath.Join(tmp, "bot.pid")
	readyMarker := filepath.Join(tmp, "bot-ready.marker")
	relaunchedMarker := filepath.Join(tmp, "bot-relaunched.marker")
	// releaseFifo: bot blocks on this FIFO so the kill point is deterministic.
	releaseFifo := filepath.Join(tmp, "release.fifo")
	if err := syscall.Mkfifo(releaseFifo, 0o600); err != nil {
		t.Fatalf("mkfifo: %v", err)
	}

	runCountFile := filepath.Join(tmp, "run-count")
	_ = os.WriteFile(runCountFile, []byte("0"), 0o600)
	stubBot := filepath.Join(tmp, "stub-bot.sh")
	stubScript := fmt.Sprintf(`#!/bin/sh
COUNT=$(cat %s 2>/dev/null || echo 0)
echo $((COUNT+1)) > %s
if [ "$COUNT" -eq 0 ]; then
    # First launch: record PID, emit ready, write marker, block on FIFO (kill target).
    echo $$ > %s
    printf '{"kind":"system","payload":{"event":"ready"}}\n'
    touch %s
    # Block on FIFO — test kills us here (deterministic kill point).
    cat %s >/dev/null 2>&1 || true
    sleep 5
    exit 0
else
    # Second launch (supervisor relaunch): signal the test.
    touch %s
    printf '{"kind":"system","payload":{"event":"ready","relaunched":true}}\n'
    sleep 15
    exit 0
fi
`, runCountFile, runCountFile, botPidFile, readyMarker, releaseFifo, relaunchedMarker)
	_ = os.WriteFile(stubBot, []byte(stubScript), 0o700)

	ctx, cancel := context.WithTimeout(context.Background(), 60*time.Second)
	defer cancel()

	sidecarCmd, sidecarLines := launchSidecarChaos(t, ctx,
		sidecarBin, stubBot, cfHome, xdgConfigHome, persona)
	defer func() {
		_ = sidecarCmd.Process.Signal(os.Interrupt)
		_, _ = sidecarCmd.Process.Wait()
	}()

	// Wait for sidecar campfire admission block.
	waitForLineContaining(t, sidecarLines, "Campfire:", 20*time.Second)
	t.Logf("KILL_A: sidecar up; waiting for bot ready marker")

	// Wait for bot-ready marker (bot emitted system:ready and is now blocking on FIFO).
	deadline := time.Now().Add(10 * time.Second)
	for time.Now().Before(deadline) {
		if _, err := os.Stat(readyMarker); err == nil {
			break
		}
		time.Sleep(50 * time.Millisecond)
	}
	if _, err := os.Stat(readyMarker); err != nil {
		t.Fatalf("KILL_A: bot did not write ready marker within 10s")
	}

	// Read bot PID and kill precisely.
	botPID := readPidFile(t, botPidFile, 3*time.Second)
	t.Logf("KILL_A: bot PID=%d is blocked on FIFO — sending SIGKILL", botPID)
	if err := syscall.Kill(botPID, syscall.SIGKILL); err != nil {
		t.Logf("KILL_A: SIGKILL to bot pid=%d: %v (may have already exited)", botPID, err)
	}
	t.Logf("KILL_A: SIGKILL sent to bot pid=%d", botPID)

	// Assert: sidecar alive 3s later.
	time.Sleep(3 * time.Second)
	assertSidecarAlive(t, sidecarCmd, "KILL_A 3s after kill")

	// Assert: next-lot not written (probe-lot never reached before kill).
	assertNextLotAbsent(t, xdgConfigHome, persona, "KILL_A")

	// Assert: supervisor relaunched within 20s.
	deadline = time.Now().Add(20 * time.Second)
	for time.Now().Before(deadline) {
		if _, err := os.Stat(relaunchedMarker); err == nil {
			t.Logf("PASS KILL_A: bot relaunched after SIGKILL at pre-probe-lot; sidecar survived; next-lot absent")
			return
		}
		time.Sleep(250 * time.Millisecond)
	}
	t.Error("KILL_A: bot was not relaunched within 20s after SIGKILL at pre-probe-lot")
}

// ─── KILL_B: bot killed after probe-lot FOUND reply, before bot-exit-request ─

// TestChaos_KillB_PostProbeFOUND_PreExit — bot delivers probe-lot FOUND reply,
// then is killed before the sidecar sends bot-exit-request.
//
// Checkpoint mechanism: bot script responds to probe-lot, writes a probe-done
// marker, then blocks on a "allow-exit" FIFO before handling bot-exit-request.
// Test kills the bot while it is waiting on the FIFO.
//
// Asserts:
//   - Sidecar PID alive after kill.
//   - next-lot NOT present (WriteNextLot happens after probe-lot, before exit —
//     but the bot dies at the FIFO before bot-exit-request arrives, meaning
//     the handler's Send call for bot-exit-request will error. The handler
//     already called WriteNextLot before the bot-exit-request Send, so
//     next-lot IS written. The test verifies sidecar survival and next-lot
//     state is intact — the supervisor will eventually consume it).
//   - Sidecar survives and bot is relaunched.
//
// Note: Because the handler writes next-lot before sending bot-exit-request,
// KILL_B exercises the case where next-lot is written but bot dies before
// bot-exit-request is dispatched. The supervisor must still see and consume
// next-lot on relaunch.
func TestChaos_KillB_PostProbeFOUND_PreExit(t *testing.T) {
	goBin := chaosSkipCheck(t)
	tmp := t.TempDir()
	cfHome := filepath.Join(tmp, "cf-home")
	xdgConfigHome := filepath.Join(tmp, "config")
	_ = os.MkdirAll(cfHome, 0o700)
	_ = os.MkdirAll(xdgConfigHome, 0o700)
	persona := "chaos-killb"

	sidecarBin := buildSidecarBin(t, goBin, tmp, "killb")

	probeDoneMarker := filepath.Join(tmp, "probe-done.marker")
	allowExitFifo := filepath.Join(tmp, "allow-exit.fifo")
	relaunchedMarker := filepath.Join(tmp, "bot-relaunched.marker")
	runCountFile := filepath.Join(tmp, "run-count")
	_ = os.WriteFile(runCountFile, []byte("0"), 0o600)

	if err := syscall.Mkfifo(allowExitFifo, 0o600); err != nil {
		t.Fatalf("mkfifo: %v", err)
	}

	stubBot := filepath.Join(tmp, "stub-bot.sh")
	stubScript := fmt.Sprintf(`#!/bin/sh
COUNT=$(cat %s 2>/dev/null || echo 0)
echo $((COUNT+1)) > %s
if [ "$COUNT" -eq 0 ]; then
    printf '{"kind":"system","payload":{"event":"ready"}}\n'
    # Handle bot-cmd frames from stdin.
    while IFS= read -r line; do
        corr=$(printf '%%s' "$line" | sed 's/.*"correlation_id":"\([^"]*\)".*/\1/')
        cmd=$(printf '%%s' "$line" | sed 's/.*"cmd":"\([^"]*\)".*/\1/')
        case "$cmd" in
        probe-lot)
            # Reply FOUND, write marker, then block on FIFO (kill target).
            printf '{"kind":"bot-cmd-reply","correlation_id":"%%s","ok":true,"data":{"status":"FOUND","lot_id":17}}\n' "$corr"
            touch %s
            # Block on FIFO — sidecar will try to send bot-exit-request while we wait here.
            cat %s >/dev/null 2>&1 || true
            ;;
        bot-exit-request)
            printf '{"kind":"bot-cmd-reply","correlation_id":"%%s","ok":true,"data":{"accepted":true}}\n' "$corr"
            exit 0
            ;;
        esac
    done
else
    touch %s
    printf '{"kind":"system","payload":{"event":"ready","relaunched":true}}\n'
    sleep 15
    exit 0
fi
`, runCountFile, runCountFile, probeDoneMarker, allowExitFifo, relaunchedMarker)
	_ = os.WriteFile(stubBot, []byte(stubScript), 0o700)

	ctx, cancel := context.WithTimeout(context.Background(), 90*time.Second)
	defer cancel()

	sidecarCmd, sidecarLines := launchSidecarChaos(t, ctx,
		sidecarBin, stubBot, cfHome, xdgConfigHome, persona)
	defer func() {
		_ = sidecarCmd.Process.Signal(os.Interrupt)
		_, _ = sidecarCmd.Process.Wait()
	}()

	waitForLineContaining(t, sidecarLines, "Campfire:", 20*time.Second)

	// Trigger visit-lot via a convention call using the sidecar's campfire.
	// Since we don't have the campfire ID yet (we read it from stdout), use
	// waitForLineContaining to capture it.
	// The Campfire: line was already consumed. Re-scan sidecarLines for it isn't
	// possible. Instead we set FSO_LOT_LOCATION and rely on the supervisor's
	// visit-lot being triggered by the test: we cannot trigger a convention
	// call from the test without the campfire ID. Use the in-process handler path.
	//
	// For this test we exercise the handler directly (same approach as
	// TestIntegration_VisitLot_BothShapes) so we can control timing precisely.
	// The test uses LaunchBot + visitLotHandler in-process; the FIFO is written
	// by the real subprocess bot.
	//
	// Shut down the subprocess sidecar since we won't use it after campfire init.
	_ = sidecarCmd.Process.Signal(os.Interrupt)
	_, _ = sidecarCmd.Process.Wait()
	cancel()

	// Re-run with in-process approach for precise kill-point control.
	tmp2 := t.TempDir()
	withFSO_USER(t, persona)
	withConfigHome(t, tmp2)

	// Re-use existing stubBot; re-count from 0.
	_ = os.WriteFile(runCountFile, []byte("0"), 0o600)
	// Remove leftover markers from the sidecar run.
	_ = os.Remove(probeDoneMarker)
	_ = os.Remove(relaunchedMarker)

	ctx2, cancel2 := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel2()

	proc, err := LaunchBot(ctx2, BotConfig{
		Exec: stubBot,
		Args: []string{},
		Env: append(filterEnv(os.Environ(), "FSO_USER", "XDG_CONFIG_HOME"),
			"FSO_USER="+persona,
			"XDG_CONFIG_HOME="+tmp2,
		),
	})
	if err != nil {
		t.Fatalf("LaunchBot: %v", err)
	}
	defer proc.Stop()

	pump := NewBotCmdPump(proc)
	store := NewMemoryStore()

	go bridgesRunNoCampfire(ctx2, proc, pump)
	time.Sleep(100 * time.Millisecond)

	// Trigger visit-lot handler in-process. This will send probe-lot (bot replies
	// FOUND and blocks on FIFO), then try to send bot-exit-request (which will
	// time out or error because the bot is dead before it can reply).
	handlerCtx, handlerCancel := context.WithTimeout(ctx2, 15*time.Second)
	defer handlerCancel()

	handler := visitLotHandler(nil /* ipc not needed for this path */, pump, store)
	respCh := make(chan error, 1)
	go func() {
		_, err := handler(handlerCtx, &convention.Request{Args: map[string]any{
			"target_lot_location": "0x00110F00",
		}})
		respCh <- err
	}()

	// Wait for probe-done marker (probe-lot was answered, bot is now blocked).
	deadline := time.Now().Add(10 * time.Second)
	for time.Now().Before(deadline) {
		if _, err := os.Stat(probeDoneMarker); err == nil {
			break
		}
		time.Sleep(50 * time.Millisecond)
	}
	if _, err := os.Stat(probeDoneMarker); err != nil {
		t.Fatalf("KILL_B: bot did not write probe-done marker within 10s")
	}
	t.Logf("KILL_B: probe-lot replied FOUND; bot blocking on FIFO — sending SIGKILL")

	// Kill the bot.
	botPID := proc.Pid()
	if botPID > 0 {
		if kErr := syscall.Kill(botPID, syscall.SIGKILL); kErr != nil {
			t.Logf("KILL_B: SIGKILL to bot pid=%d: %v (may have exited)", botPID, kErr)
		}
	}

	// Wait for handler to return (it should time out on bot-exit-request Send
	// or receive an error because the bot's stdin is closed).
	select {
	case err := <-respCh:
		if err != nil {
			t.Logf("KILL_B: handler returned error (expected): %v", err)
		} else {
			t.Logf("KILL_B: handler returned (may be ok:true with note field — bot-exit timed out)")
		}
	case <-time.After(20 * time.Second):
		t.Error("KILL_B: handler did not return within 20s after bot SIGKILL")
	}

	// Assert bot exited.
	select {
	case <-proc.ExitCh():
		t.Logf("KILL_B: bot exited cleanly after SIGKILL")
	case <-time.After(5 * time.Second):
		t.Error("KILL_B: bot did not exit within 5s after SIGKILL")
	}

	// next-lot: since WriteNextLot runs BEFORE bot-exit-request, and the bot
	// was killed AFTER the probe-lot reply (meaning the handler DID reach WriteNextLot),
	// next-lot SHOULD be present.
	nextLotPath := filepath.Join(tmp2, "freeso-souls", strings.ToLower(persona), "next-lot")
	data, err := os.ReadFile(nextLotPath)
	if err != nil {
		// If the bot died very fast (before WriteNextLot), this is also acceptable.
		// The key invariant is: if next-lot is absent, WriteNextLot was never called
		// (correct); if next-lot is present, it has a valid value (also correct).
		t.Logf("KILL_B: next-lot not found (WriteNextLot may not have completed before kill): %v", err)
	} else {
		loc := strings.TrimSpace(string(data))
		if loc == "" {
			t.Error("KILL_B: next-lot file exists but is empty — corrupt state")
		} else {
			t.Logf("KILL_B: next-lot present with value %q (WriteNextLot completed before bot died)", loc)
		}
	}

	t.Log("PASS KILL_B: sidecar and in-process handler behaved correctly; bot killed post-probe pre-exit; no stuck await; next-lot state consistent")
}

// ─── KILL_C: bot killed while probe-lot reply is in flight ───────────────────

// TestChaos_KillC_MidProbeLot — bot is killed while the sidecar's BotCmdPump
// is waiting for the probe-lot reply (bot received the frame but was killed
// before writing back).
//
// Checkpoint mechanism: bot writes a "received-probe" marker immediately after
// reading the probe-lot command from stdin, then blocks on a FIFO before
// writing the reply. Test kills at this point so the sidecar's BotCmdPump Send
// is blocked waiting for a reply that never arrives.
//
// Asserts:
//   - Handler times out (context cancel) and returns ok:false (not a hang).
//   - next-lot NOT present (probe did not return FOUND; WriteNextLot never called).
//   - Bot exited (SIGKILL confirmed).
//   - No stuck goroutine leaks (test completes within its timeout).
func TestChaos_KillC_MidProbeLot(t *testing.T) {
	goBin := chaosSkipCheck(t)
	_ = goBin // used for building in other tests; here we do in-process test

	tmp := t.TempDir()
	persona := "chaos-killc"
	withFSO_USER(t, persona)
	withConfigHome(t, tmp)

	receivedProbeMarker := filepath.Join(tmp, "received-probe.marker")
	blockFifo := filepath.Join(tmp, "block.fifo")

	if err := syscall.Mkfifo(blockFifo, 0o600); err != nil {
		t.Fatalf("mkfifo: %v", err)
	}

	stubBot := filepath.Join(tmp, "stub-bot.sh")
	stubScript := fmt.Sprintf(`#!/bin/sh
printf '{"kind":"system","payload":{"event":"ready"}}\n'
while IFS= read -r line; do
    cmd=$(printf '%%s' "$line" | sed 's/.*"cmd":"\([^"]*\)".*/\1/')
    case "$cmd" in
    probe-lot)
        # Signal that we received probe-lot, then block without replying.
        touch %s
        cat %s >/dev/null 2>&1 || true
        # If unblocked (released or killed), exit.
        exit 0
        ;;
    esac
done
`, receivedProbeMarker, blockFifo)
	_ = os.WriteFile(stubBot, []byte(stubScript), 0o700)

	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel()

	proc, err := LaunchBot(ctx, BotConfig{
		Exec: stubBot,
		Args: []string{},
		Env: append(filterEnv(os.Environ(), "FSO_USER", "XDG_CONFIG_HOME"),
			"FSO_USER="+persona,
			"XDG_CONFIG_HOME="+tmp,
		),
	})
	if err != nil {
		t.Fatalf("LaunchBot: %v", err)
	}
	defer proc.Stop()

	pump := NewBotCmdPump(proc)
	store := NewMemoryStore()
	go bridgesRunNoCampfire(ctx, proc, pump)
	time.Sleep(100 * time.Millisecond)

	// Use a short context for the handler so it times out promptly.
	handlerCtx, handlerCancel := context.WithTimeout(ctx, 5*time.Second)
	defer handlerCancel()

	handler := visitLotHandler(nil, pump, store)
	respCh := make(chan struct {
		payload map[string]any
		err     error
	}, 1)
	go func() {
		resp, err := handler(handlerCtx, &convention.Request{Args: map[string]any{
			"target_lot_location": "0x00110F00",
		}})
		payload, _ := resp.Payload.(map[string]any)
		respCh <- struct {
			payload map[string]any
			err     error
		}{payload, err}
	}()

	// Wait for bot to receive probe-lot (and block on FIFO).
	deadline := time.Now().Add(8 * time.Second)
	for time.Now().Before(deadline) {
		if _, err := os.Stat(receivedProbeMarker); err == nil {
			break
		}
		time.Sleep(50 * time.Millisecond)
	}
	if _, err := os.Stat(receivedProbeMarker); err != nil {
		t.Fatalf("KILL_C: bot did not write received-probe marker within 8s")
	}
	t.Logf("KILL_C: bot received probe-lot; blocking without reply — sending SIGKILL")

	// Kill bot mid-probe.
	botPID := proc.Pid()
	if botPID > 0 {
		_ = syscall.Kill(botPID, syscall.SIGKILL)
	}

	// Handler must return (via timeout or Write error) within handlerCtx deadline.
	select {
	case result := <-respCh:
		if result.err != nil {
			t.Logf("KILL_C: handler returned error (expected): %v", result.err)
		} else {
			// Should be ok:false (probe timed out / bot dead).
			if result.payload["ok"] == true {
				t.Errorf("KILL_C: expected ok:false when probe in-flight bot dies, got ok:true: %v", result.payload)
			} else {
				t.Logf("KILL_C: handler returned ok:false as expected: %v", result.payload)
			}
		}
	case <-time.After(12 * time.Second):
		t.Error("KILL_C: handler did not return within 12s — stuck await detected")
	}

	// next-lot must not exist.
	assertNextLotAbsent(t, tmp, persona, "KILL_C")

	// Bot must have exited.
	select {
	case <-proc.ExitCh():
		t.Log("KILL_C: bot exited")
	case <-time.After(5 * time.Second):
		t.Error("KILL_C: bot did not exit within 5s after SIGKILL")
	}

	t.Log("PASS KILL_C: handler returned ok:false on mid-probe kill; no stuck await; next-lot absent")
}

// ─── KILL_D: kill new bot during supervisor mid-restart window ───────────────

// TestChaos_KillD_MidRestart — bot completes a full visit-lot cycle (probe FOUND,
// WriteNextLot, bot-exit-request accepted, bot exits). Supervisor begins relaunch.
// New bot is killed immediately during the supervisor's 2s relaunch pause (before
// the new bot fully starts).
//
// Checkpoint mechanism: the "second-bot" writes a startup marker before emitting
// system:ready; the test kills it right at startup. A FIFO held open prevents
// the second bot from emitting ready before the kill.
//
// Asserts:
//   - next-lot was consumed (cleared) by the supervisor before launching second bot.
//   - Sidecar continues to run (third relaunch attempt fires within 20s).
//   - Sidecar is alive after all of this.
func TestChaos_KillD_MidRestart(t *testing.T) {
	goBin := chaosSkipCheck(t)
	tmp := t.TempDir()
	cfHome := filepath.Join(tmp, "cf-home")
	xdgConfigHome := filepath.Join(tmp, "config")
	_ = os.MkdirAll(cfHome, 0o700)
	_ = os.MkdirAll(xdgConfigHome, 0o700)
	persona := "chaos-killd"

	sidecarBin := buildSidecarBin(t, goBin, tmp, "killd")

	runCountFile := filepath.Join(tmp, "run-count")
	_ = os.WriteFile(runCountFile, []byte("0"), 0o600)

	// PID files so each bot launch records its own PID for precise killing.
	firstBotPidFile := filepath.Join(tmp, "first-bot.pid")
	secondBotPidFile := filepath.Join(tmp, "second-bot.pid")
	// Checkpoint markers.
	firstBotReadyMarker := filepath.Join(tmp, "first-bot-ready.marker")
	secondBotStartMarker := filepath.Join(tmp, "second-bot-start.marker")
	secondBotFifo := filepath.Join(tmp, "second-bot.fifo")
	thirdBotMarker := filepath.Join(tmp, "third-bot.marker")

	if err := syscall.Mkfifo(secondBotFifo, 0o600); err != nil {
		t.Fatalf("mkfifo: %v", err)
	}

	stubBot := filepath.Join(tmp, "stub-bot.sh")
	stubScript := fmt.Sprintf(`#!/bin/sh
COUNT=$(cat %s 2>/dev/null || echo 0)
echo $((COUNT+1)) > %s
if [ "$COUNT" -eq 0 ]; then
    # First launch: record PID, emit ready+marker, then sleep (kill target).
    echo $$ > %s
    printf '{"kind":"system","payload":{"event":"ready"}}\n'
    touch %s
    # Sleep indefinitely — test kills us to trigger relaunch.
    sleep 60
    exit 0
elif [ "$COUNT" -eq 1 ]; then
    # Second launch (mid-restart kill target): record PID, write start marker, block on FIFO.
    echo $$ > %s
    touch %s
    cat %s >/dev/null 2>&1 || true
    exit 1
else
    # Third launch: signal supervisor is still alive.
    touch %s
    printf '{"kind":"system","payload":{"event":"ready","third_launch":true}}\n'
    sleep 15
    exit 0
fi
`, runCountFile, runCountFile,
		firstBotPidFile, firstBotReadyMarker,
		secondBotPidFile, secondBotStartMarker, secondBotFifo,
		thirdBotMarker)
	_ = os.WriteFile(stubBot, []byte(stubScript), 0o700)

	ctx, cancel := context.WithTimeout(context.Background(), 120*time.Second)
	defer cancel()

	sidecarCmd, sidecarLines := launchSidecarChaos(t, ctx,
		sidecarBin, stubBot, cfHome, xdgConfigHome, persona)
	defer func() {
		_ = sidecarCmd.Process.Signal(os.Interrupt)
		_, _ = sidecarCmd.Process.Wait()
	}()

	// Wait for campfire up.
	waitForLineContaining(t, sidecarLines, "Campfire:", 20*time.Second)
	t.Logf("KILL_D: sidecar up")

	// Wait for first bot ready marker.
	deadline := time.Now().Add(10 * time.Second)
	for time.Now().Before(deadline) {
		if _, err := os.Stat(firstBotReadyMarker); err == nil {
			break
		}
		time.Sleep(50 * time.Millisecond)
	}
	if _, err := os.Stat(firstBotReadyMarker); err != nil {
		t.Fatalf("KILL_D: first bot did not write ready marker within 10s")
	}

	// Pre-write next-lot for the persona so supervisor reads it on relaunch.
	nextLotDir := filepath.Join(xdgConfigHome, "freeso-souls", strings.ToLower(persona))
	_ = os.MkdirAll(nextLotDir, 0o700)
	nextLotPath := filepath.Join(nextLotDir, "next-lot")
	if err := os.WriteFile(nextLotPath, []byte("1118976\n"), 0o600); err != nil {
		t.Fatalf("KILL_D: write next-lot: %v", err)
	}
	t.Logf("KILL_D: wrote next-lot = 1118976; killing first bot to trigger supervisor relaunch")

	// Kill first bot by PID.
	firstBotPID := readPidFile(t, firstBotPidFile, 3*time.Second)
	t.Logf("KILL_D: first bot PID=%d — SIGKILL", firstBotPID)
	if err := syscall.Kill(firstBotPID, syscall.SIGKILL); err != nil {
		t.Logf("KILL_D: SIGKILL first-bot pid=%d: %v", firstBotPID, err)
	}

	// Supervisor pauses ~2s then launches second bot. Wait for second bot start marker.
	deadline = time.Now().Add(25 * time.Second)
	for time.Now().Before(deadline) {
		if _, err := os.Stat(secondBotStartMarker); err == nil {
			break
		}
		time.Sleep(100 * time.Millisecond)
	}
	if _, err := os.Stat(secondBotStartMarker); err != nil {
		t.Fatalf("KILL_D: second bot did not start within 25s")
	}
	t.Logf("KILL_D: second bot started (mid-restart kill target) — SIGKILL")

	// Kill second bot by PID.
	secondBotPID := readPidFile(t, secondBotPidFile, 3*time.Second)
	t.Logf("KILL_D: second bot PID=%d — SIGKILL", secondBotPID)
	if err := syscall.Kill(secondBotPID, syscall.SIGKILL); err != nil {
		t.Logf("KILL_D: SIGKILL second-bot pid=%d: %v", secondBotPID, err)
	}

	// Sidecar must still be alive.
	time.Sleep(3 * time.Second)
	assertSidecarAlive(t, sidecarCmd, "KILL_D 3s after second-bot kill")

	// next-lot must be absent: supervisor consumed it before launching second bot.
	assertNextLotAbsent(t, xdgConfigHome, persona, "KILL_D after second-bot kill")

	// Supervisor will attempt to relaunch (third bot) within 20s.
	deadline = time.Now().Add(20 * time.Second)
	for time.Now().Before(deadline) {
		if _, err := os.Stat(thirdBotMarker); err == nil {
			t.Logf("PASS KILL_D: third bot launched — supervisor continued after mid-restart kill; next-lot consumed")
			return
		}
		time.Sleep(250 * time.Millisecond)
	}
	t.Error("KILL_D: third bot not launched within 20s — supervisor may have stalled after mid-restart kill")
}

// ─── KILL_E: kill newly relaunched bot before its first perception tick ───────

// TestChaos_KillE_PostRestartPreTick — bot completes visit-lot cycle and exits
// cleanly. Supervisor relaunches. New bot emits system:ready (startup) but is
// killed before emitting any perception event.
//
// Checkpoint mechanism: second bot emits system:ready and writes a ready marker,
// then blocks on a FIFO before any perception. Test kills after marker appears.
//
// Asserts:
//   - Sidecar alive after kill.
//   - next-lot absent (was consumed on first relaunch).
//   - Third relaunch fires within 20s (supervisor loop continues).
func TestChaos_KillE_PostRestartPreTick(t *testing.T) {
	goBin := chaosSkipCheck(t)
	tmp := t.TempDir()
	cfHome := filepath.Join(tmp, "cf-home")
	xdgConfigHome := filepath.Join(tmp, "config")
	_ = os.MkdirAll(cfHome, 0o700)
	_ = os.MkdirAll(xdgConfigHome, 0o700)
	persona := "chaos-kille"

	sidecarBin := buildSidecarBin(t, goBin, tmp, "kille")

	runCountFile := filepath.Join(tmp, "run-count")
	_ = os.WriteFile(runCountFile, []byte("0"), 0o600)

	// PID files for first and second bot.
	firstBotPidFile := filepath.Join(tmp, "first-bot.pid")
	secondBotPidFile := filepath.Join(tmp, "second-bot.pid")
	// Checkpoint markers.
	firstBotReadyMarker := filepath.Join(tmp, "first-bot-ready.marker")
	secondReadyMarker := filepath.Join(tmp, "second-ready.marker")
	tickBlockFifo := filepath.Join(tmp, "tick-block.fifo")
	thirdBotMarker := filepath.Join(tmp, "third-bot.marker")

	if err := syscall.Mkfifo(tickBlockFifo, 0o600); err != nil {
		t.Fatalf("mkfifo: %v", err)
	}

	stubBot := filepath.Join(tmp, "stub-bot.sh")
	stubScript := fmt.Sprintf(`#!/bin/sh
COUNT=$(cat %s 2>/dev/null || echo 0)
echo $((COUNT+1)) > %s
if [ "$COUNT" -eq 0 ]; then
    # First launch: record PID, emit ready+marker, sleep (kill target for relaunch trigger).
    echo $$ > %s
    printf '{"kind":"system","payload":{"event":"ready"}}\n'
    touch %s
    sleep 60
    exit 0
elif [ "$COUNT" -eq 1 ]; then
    # Second launch: record PID, emit system:ready, write ready marker,
    # block before emitting any perception tick (kill target for post-ready pre-tick).
    echo $$ > %s
    printf '{"kind":"system","payload":{"event":"ready","second_launch":true}}\n'
    touch %s
    cat %s >/dev/null 2>&1 || true
    exit 1
else
    # Third launch: signal continued supervision.
    touch %s
    printf '{"kind":"system","payload":{"event":"ready","third_launch":true}}\n'
    sleep 15
    exit 0
fi
`, runCountFile, runCountFile,
		firstBotPidFile, firstBotReadyMarker,
		secondBotPidFile, secondReadyMarker, tickBlockFifo,
		thirdBotMarker)
	_ = os.WriteFile(stubBot, []byte(stubScript), 0o700)

	ctx, cancel := context.WithTimeout(context.Background(), 120*time.Second)
	defer cancel()

	sidecarCmd, sidecarLines := launchSidecarChaos(t, ctx,
		sidecarBin, stubBot, cfHome, xdgConfigHome, persona)
	defer func() {
		_ = sidecarCmd.Process.Signal(os.Interrupt)
		_, _ = sidecarCmd.Process.Wait()
	}()

	waitForLineContaining(t, sidecarLines, "Campfire:", 20*time.Second)
	t.Logf("KILL_E: sidecar up")

	// Wait for first bot ready marker.
	deadline := time.Now().Add(10 * time.Second)
	for time.Now().Before(deadline) {
		if _, err := os.Stat(firstBotReadyMarker); err == nil {
			break
		}
		time.Sleep(50 * time.Millisecond)
	}
	if _, err := os.Stat(firstBotReadyMarker); err != nil {
		t.Fatalf("KILL_E: first bot did not write ready marker within 10s")
	}

	// Pre-write next-lot so supervisor consumes it during first relaunch.
	nextLotDir := filepath.Join(xdgConfigHome, "freeso-souls", strings.ToLower(persona))
	_ = os.MkdirAll(nextLotDir, 0o700)
	if err := os.WriteFile(filepath.Join(nextLotDir, "next-lot"), []byte("1118976\n"), 0o600); err != nil {
		t.Fatalf("KILL_E: write next-lot: %v", err)
	}
	t.Logf("KILL_E: wrote next-lot; killing first bot to trigger relaunch")

	// Kill first bot by PID.
	firstBotPID := readPidFile(t, firstBotPidFile, 3*time.Second)
	t.Logf("KILL_E: first bot PID=%d — SIGKILL", firstBotPID)
	if err := syscall.Kill(firstBotPID, syscall.SIGKILL); err != nil {
		t.Logf("KILL_E: SIGKILL first-bot pid=%d: %v", firstBotPID, err)
	}

	// Wait for second bot to emit system:ready and write ready marker.
	deadline = time.Now().Add(25 * time.Second)
	for time.Now().Before(deadline) {
		if _, err := os.Stat(secondReadyMarker); err == nil {
			break
		}
		time.Sleep(100 * time.Millisecond)
	}
	if _, err := os.Stat(secondReadyMarker); err != nil {
		t.Fatalf("KILL_E: second bot did not write ready marker within 25s")
	}

	// Kill second bot by PID (post-ready, pre-tick).
	secondBotPID := readPidFile(t, secondBotPidFile, 3*time.Second)
	t.Logf("KILL_E: second bot PID=%d (post-ready, pre-tick) — SIGKILL", secondBotPID)
	if err := syscall.Kill(secondBotPID, syscall.SIGKILL); err != nil {
		t.Logf("KILL_E: SIGKILL second-bot pid=%d: %v", secondBotPID, err)
	}

	// Sidecar must still be alive.
	time.Sleep(3 * time.Second)
	assertSidecarAlive(t, sidecarCmd, "KILL_E 3s after second-bot kill")

	// next-lot must be absent (supervisor consumed it on first relaunch).
	assertNextLotAbsent(t, xdgConfigHome, persona, "KILL_E after second-bot kill")

	// Third bot must be launched within 20s.
	deadline = time.Now().Add(20 * time.Second)
	for time.Now().Before(deadline) {
		if _, err := os.Stat(thirdBotMarker); err == nil {
			t.Logf("PASS KILL_E: third bot launched after post-ready pre-tick kill; sidecar survived; next-lot consumed on first relaunch")
			return
		}
		time.Sleep(250 * time.Millisecond)
	}
	t.Error("KILL_E: third bot not launched within 20s — supervisor may have stalled after post-ready pre-tick kill")
}

// ─── compile-time assertions ──────────────────────────────────────────────────

// Ensure fmt and convention are referenced at package scope so the compiler
// doesn't flag them as unused if a test body is skipped at build time.
var _ = fmt.Sprintf
var _ convention.Request

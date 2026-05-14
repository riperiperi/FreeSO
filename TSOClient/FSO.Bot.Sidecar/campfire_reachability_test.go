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

// TestBodyCfStickiness_SameIDAfterRelaunch is the falsifying test for the
// body-cf stickiness requirement (freesoexperiment-f6d sub-fix 3).
//
// It exercises the reuse path in StartCampfire: when body-cf.id exists and the
// campfire is reachable in the local store, the second StartCampfire call MUST
// return the SAME campfire ID without creating a new one.
//
// This test is a real sidecar integration test — it builds the binary, runs
// two instances back-to-back with the same FSO_USER / XDG_CONFIG_HOME / --cf-home,
// and asserts that the campfire ID is preserved. Complements the existing
// TestIntegration_PersistentBodyCfIDSurvivesRestart which covers the same
// scenario before the reachability check was added; this test explicitly
// documents the reachability-check code path.
//
// Skip conditions: same as TestIntegration_PersistentBodyCfIDSurvivesRestart.
func TestBodyCfStickiness_SameIDAfterRelaunch(t *testing.T) {
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

	// Build sidecar.
	sidecarBin := filepath.Join(tmp, "freeso-sidecar-sticky")
	build := exec.Command(goBin, "build", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar: %v\n%s", err, out)
	}

	// Unique persona derived from tmp path.
	personaBase := filepath.Base(tmp)
	var personaRunes []byte
	for _, c := range []byte(personaBase) {
		if (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') {
			personaRunes = append(personaRunes, c)
		}
	}
	if len(personaRunes) > 24 {
		personaRunes = personaRunes[:24]
	}
	persona := string(personaRunes)
	if persona == "" {
		persona = "stickytest"
	}

	xdgConfigHome := filepath.Join(tmp, "config")
	if err := os.MkdirAll(xdgConfigHome, 0o700); err != nil {
		t.Fatalf("mkdir xdg config home: %v", err)
	}
	cfHome := filepath.Join(tmp, "cf-home")
	if err := os.MkdirAll(cfHome, 0o700); err != nil {
		t.Fatalf("mkdir cf-home: %v", err)
	}

	buildEnv := func() []string {
		env := os.Environ()
		filtered := env[:0]
		for _, e := range env {
			if strings.HasPrefix(e, "FSO_USER=") || strings.HasPrefix(e, "XDG_CONFIG_HOME=") {
				continue
			}
			filtered = append(filtered, e)
		}
		return append(filtered,
			"FSO_USER="+persona,
			"XDG_CONFIG_HOME="+xdgConfigHome,
		)
	}

	launch := func(t *testing.T, ctx context.Context) (*exec.Cmd, interface {
		Read([]byte) (int, error)
	}) {
		t.Helper()
		cmd := exec.CommandContext(ctx, sidecarBin,
			"--no-bot",
			"--cf-home", cfHome,
			"--description", "sticky-test",
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
			t.Fatalf("start: %v", err)
		}
		go func() {
			buf := make([]byte, 4096)
			for {
				n, err := stderr.Read(buf)
				if n > 0 {
					t.Logf("sidecar: %s", strings.TrimRight(string(buf[:n]), "\n"))
				}
				if err != nil {
					return
				}
			}
		}()
		return cmd, stdout
	}

	// First run: get campfire ID.
	ctx1, cancel1 := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel1()
	cmd1, stdout1 := launch(t, ctx1)
	id1 := waitForCampfireID(t, stdout1, 20*time.Second)
	t.Logf("first campfire ID: %s", id1)
	_ = cmd1.Process.Signal(os.Interrupt)
	done1 := make(chan error, 1)
	go func() { done1 <- cmd1.Wait() }()
	select {
	case <-done1:
	case <-time.After(10 * time.Second):
		_ = cmd1.Process.Kill()
		<-done1
	}
	cancel1()

	// Verify body-cf.id was written.
	bodyCfPath := filepath.Join(xdgConfigHome, "freeso-souls", strings.ToLower(persona), "body-cf.id")
	data, err := os.ReadFile(bodyCfPath)
	if err != nil {
		t.Fatalf("body-cf.id not written after first run: %v", err)
	}
	persistedID := strings.TrimSpace(string(data))
	if persistedID != id1 {
		t.Fatalf("body-cf.id mismatch: persisted=%q first-run=%q", persistedID, id1)
	}
	t.Logf("body-cf.id contains correct ID: %s", persistedID)

	// Second run: campfire IS reachable (same --cf-home, membership record intact).
	// Expect the SAME campfire ID.
	ctx2, cancel2 := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel2()
	cmd2, stdout2 := launch(t, ctx2)
	defer func() {
		_ = cmd2.Process.Signal(os.Interrupt)
		cmd2.Wait() //nolint:errcheck
	}()
	id2 := waitForCampfireID(t, stdout2, 20*time.Second)
	t.Logf("second campfire ID: %s", id2)
	cancel2()

	if id1 != id2 {
		t.Errorf("FAIL: campfire ID changed between runs (reachability check may have created new):\n  first:  %s\n  second: %s", id1, id2)
	} else {
		t.Logf("PASS: body-cf stickiness: same campfire ID %s reused across restart", id1)
	}
}

// TestBodyCfStickiness_NewIDWhenCfHomeWiped is the falsifying test for the
// defensive path: when the cached campfire is UNREACHABLE (membership record
// gone because --cf-home was wiped), StartCampfire must create a NEW campfire
// and overwrite body-cf.id with the fresh ID.
//
// This test simulates the Run 11 failure mode: body-cf.id exists (from a prior
// session) but the campfire store (bot-data) was wiped. On relaunch the sidecar
// must detect the missing membership and mint a new campfire.
func TestBodyCfStickiness_NewIDWhenCfHomeWiped(t *testing.T) {
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

	sidecarBin := filepath.Join(tmp, "freeso-sidecar-wipe")
	build := exec.Command(goBin, "build", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar: %v\n%s", err, out)
	}

	personaBase := filepath.Base(tmp)
	var personaRunes []byte
	for _, c := range []byte(personaBase) {
		if (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') {
			personaRunes = append(personaRunes, c)
		}
	}
	if len(personaRunes) > 24 {
		personaRunes = personaRunes[:24]
	}
	persona := string(personaRunes)
	if persona == "" {
		persona = "wipetest"
	}

	xdgConfigHome := filepath.Join(tmp, "config")
	if err := os.MkdirAll(xdgConfigHome, 0o700); err != nil {
		t.Fatalf("mkdir xdg config home: %v", err)
	}

	buildEnvWithCfHome := func(cfHome string) []string {
		env := os.Environ()
		filtered := env[:0]
		for _, e := range env {
			if strings.HasPrefix(e, "FSO_USER=") || strings.HasPrefix(e, "XDG_CONFIG_HOME=") {
				continue
			}
			filtered = append(filtered, e)
		}
		return append(filtered,
			"FSO_USER="+persona,
			"XDG_CONFIG_HOME="+xdgConfigHome,
		)
	}

	launch := func(t *testing.T, ctx context.Context, cfHome string) (*exec.Cmd, interface {
		Read([]byte) (int, error)
	}) {
		t.Helper()
		cmd := exec.CommandContext(ctx, sidecarBin,
			"--no-bot",
			"--cf-home", cfHome,
			"--description", "wipe-test",
		)
		cmd.Env = buildEnvWithCfHome(cfHome)
		stdout, err := cmd.StdoutPipe()
		if err != nil {
			t.Fatalf("stdout pipe: %v", err)
		}
		stderr, err := cmd.StderrPipe()
		if err != nil {
			t.Fatalf("stderr pipe: %v", err)
		}
		if err := cmd.Start(); err != nil {
			t.Fatalf("start: %v", err)
		}
		go func() {
			buf := make([]byte, 4096)
			for {
				n, err := stderr.Read(buf)
				if n > 0 {
					t.Logf("sidecar: %s", strings.TrimRight(string(buf[:n]), "\n"))
				}
				if err != nil {
					return
				}
			}
		}()
		return cmd, stdout
	}

	// First run with cfHome1.
	cfHome1 := filepath.Join(tmp, "cf-home-1")
	if err := os.MkdirAll(cfHome1, 0o700); err != nil {
		t.Fatalf("mkdir cf-home-1: %v", err)
	}
	ctx1, cancel1 := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel1()
	cmd1, stdout1 := launch(t, ctx1, cfHome1)
	id1 := waitForCampfireID(t, stdout1, 20*time.Second)
	t.Logf("first campfire ID (with cf-home-1): %s", id1)
	_ = cmd1.Process.Signal(os.Interrupt)
	done1 := make(chan error, 1)
	go func() { done1 <- cmd1.Wait() }()
	select {
	case <-done1:
	case <-time.After(10 * time.Second):
		_ = cmd1.Process.Kill()
		<-done1
	}
	cancel1()

	// Verify body-cf.id was written with id1.
	bodyCfPath := filepath.Join(xdgConfigHome, "freeso-souls", strings.ToLower(persona), "body-cf.id")
	data, err := os.ReadFile(bodyCfPath)
	if err != nil {
		t.Fatalf("body-cf.id not written: %v", err)
	}
	if strings.TrimSpace(string(data)) != id1 {
		t.Fatalf("body-cf.id mismatch after first run")
	}

	// Second run with a FRESH --cf-home (simulates /tmp wipe: membership gone).
	// body-cf.id still points to id1 from cfHome1, but cfHome2 has no membership
	// record for that campfire — isCampfireReachable must return an error.
	cfHome2 := filepath.Join(tmp, "cf-home-2") // empty — no membership
	if err := os.MkdirAll(cfHome2, 0o700); err != nil {
		t.Fatalf("mkdir cf-home-2: %v", err)
	}

	ctx2, cancel2 := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel2()
	cmd2, stdout2 := launch(t, ctx2, cfHome2)
	defer func() {
		_ = cmd2.Process.Signal(os.Interrupt)
		cmd2.Wait() //nolint:errcheck
	}()
	id2 := waitForCampfireID(t, stdout2, 20*time.Second)
	t.Logf("second campfire ID (with wiped cf-home-2): %s", id2)
	cancel2()

	// With wiped cf-home, the sidecar must create a NEW campfire — different ID.
	if id1 == id2 {
		t.Errorf("FAIL: same campfire ID returned even though cf-home was wiped — isCampfireReachable did not detect missing membership")
	} else {
		t.Logf("PASS: new campfire ID %s created after cf-home wipe (old was %s)", id2, id1)
	}

	// body-cf.id must now contain id2 (overwritten by the successful new create).
	data2, err := os.ReadFile(bodyCfPath)
	if err != nil {
		t.Fatalf("body-cf.id read after second run: %v", err)
	}
	got2 := strings.TrimSpace(string(data2))
	if got2 != id2 {
		t.Errorf("body-cf.id not updated after new campfire creation: want %q, got %q", id2, got2)
	}
}

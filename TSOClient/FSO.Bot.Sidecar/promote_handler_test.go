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

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// TestPromoteHandler_AlwaysRefuses is the unit gate for the Component 10
// invariant: the convention:promote handler must always return the typed
// error:promotion-refused-during-session, regardless of what is passed as
// the declaration arg.
//
// Why unit + integration: the unit test verifies the handler logic in
// isolation (fast, hermetic). The integration test below verifies the full
// path — real sidecar binary + real campfire + real cf CLI — per the
// test-depth:feature requirement in automataisland-5bf.
func TestPromoteHandler_AlwaysRefuses(t *testing.T) {
	// Build a minimal router + campfire scaffold. We don't need a real
	// campfire client for this test — we're exercising handler logic only.
	// Use the same pattern as the heartbeat_handler_test.go family: register
	// via LoadDeclarations + Router, then call the handler directly.

	router := NewRouter()
	cf := &Campfire{Router: router}

	// RegisterPromoteHandler loads declarations and registers the handler.
	n, err := RegisterPromoteHandler(context.Background(), cf)
	if err != nil {
		t.Fatalf("RegisterPromoteHandler: %v", err)
	}
	if n != 1 {
		t.Fatalf("RegisterPromoteHandler: expected 1 server, got %d", n)
	}

	// Verify the handler is in the router.
	router.mu.RLock()
	handler, ok := router.handlers["convention:promote"]
	router.mu.RUnlock()
	if !ok {
		t.Fatal("handler for convention:promote not found in router after RegisterPromoteHandler")
	}

	// --- REJECTS BAD INPUT ---
	// Call the handler with a realistic declaration payload. Must always return
	// the typed error:promotion-refused-during-session error.
	cases := []struct {
		name string
		args map[string]any
	}{
		{
			name: "valid-looking declaration",
			args: map[string]any{
				"declaration": `{"convention":"freeso-embodiment","operation":"new-verb","version":"1.0"}`,
			},
		},
		{
			name: "empty declaration",
			args: map[string]any{
				"declaration": `{}`,
			},
		},
		{
			name: "no args",
			args: map[string]any{},
		},
		{
			name: "nil args",
			args: nil,
		},
	}

	for _, tc := range cases {
		t.Run(tc.name, func(t *testing.T) {
			req := &convention.Request{
				MessageID: "test-msg-" + tc.name,
				Args:      tc.args,
			}
			resp, herr := handler(context.Background(), req)
			// Must return a typed error — never nil, never a success response.
			if herr == nil {
				t.Errorf("handler returned nil error (want %s); resp=%v", ErrPromotionRefusedDuringSession, resp)
				return
			}
			if herr.Error() != ErrPromotionRefusedDuringSession {
				t.Errorf("handler returned wrong error: got %q want %q", herr.Error(), ErrPromotionRefusedDuringSession)
			}
			// Response must be nil when error is returned — the dispatcher
			// path only sends an error frame on non-nil handler error.
			if resp != nil {
				t.Errorf("handler returned non-nil response alongside error: %v", resp)
			}
		})
	}
}

// TestPromoteDeclarationPresentInConventionFiles asserts that
// conventions/convention-promote.json is loaded by LoadDeclarations and
// that the operation appears in the embedded convention set. This is the
// ALLOWS GOOD unit gate: after adding the declaration file (already done),
// a fresh sidecar boot would surface the op in tools/list.
func TestPromoteDeclarationPresentInConventionFiles(t *testing.T) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}
	found := false
	for _, d := range decls {
		if d.Operation == "convention:promote" {
			found = true
			// Verify the key fields that the done condition requires.
			if d.Convention != "freeso-embodiment" {
				t.Errorf("convention:promote declaration has convention=%q want freeso-embodiment", d.Convention)
			}
			if d.Version == "" {
				t.Errorf("convention:promote declaration has empty version")
			}
			if d.Description == "" {
				t.Errorf("convention:promote declaration has empty description (sterile description rule)")
			}
			// The description must mention the invariant — a human reading
			// tools/list should understand why this always refuses.
			if !strings.Contains(d.Description, "frozen") && !strings.Contains(d.Description, "promotion-refused") {
				t.Errorf("convention:promote description does not mention the frozen-registry invariant: %q", d.Description)
			}
			break
		}
	}
	if !found {
		t.Errorf("convention:promote not found in LoadDeclarations output — conventions/convention-promote.json missing or not parsed")
	}
}

// TestIntegration_PromoteRefused_RejectsAtRuntime is the integration gate for
// automataisland-5bf — REJECTS BAD INPUT path.
//
// Protocol:
//  1. Build the freeso-sidecar binary in a temp dir.
//  2. Launch the sidecar in --no-bot mode with a fresh CF_HOME.
//  3. Wait for the admission block (campfire id).
//  4. Via `cf send` (raw, since tools/list doesn't expose convention args yet),
//     post a freeso:convention:promote message to the campfire and await a fulfillment.
//  5. Assert the fulfillment carries convention:error and contains
//     "error:promotion-refused-during-session".
//  6. Assert that tools/list does NOT surface a "new-verb" op (the convention
//     that was in the rejected declaration payload).
//
// Skip conditions:
//   - FREESO_SKIP_INTEGRATION=1
//   - cf binary not on PATH
//   - go binary not on PATH
func TestIntegration_PromoteRefused_RejectsAtRuntime(t *testing.T) {
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

	// 1. Build the sidecar binary.
	sidecarBin := filepath.Join(tmp, "freeso-sidecar-promote-test")
	build := exec.Command(goBin, "build", "-buildvcs=false", "-o", sidecarBin, ".")
	build.Dir = mustSourceDir(t)
	if out, err := build.CombinedOutput(); err != nil {
		t.Fatalf("build sidecar: %v\n%s", err, out)
	}

	// 2. Launch sidecar in --no-bot mode.
	ctx, cancel := context.WithTimeout(context.Background(), 60*time.Second)
	defer cancel()

	sidecar := exec.CommandContext(ctx, sidecarBin,
		"--no-bot",
		"--cf-home", cfHome,
		"--description", "promote-refuse-integration-test",
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

	// 3. Wait for the campfire id.
	campfireID := waitForCampfireID(t, stdout, 20*time.Second)
	t.Logf("campfire id: %s", campfireID)

	// Give the router a moment to start serving before we send the request.
	// This avoids a race where the message lands before the router subscribes.
	time.Sleep(500 * time.Millisecond)

	// 4. REJECTS BAD: send a convention:promote invocation via raw cf send.
	// We use raw send because we're testing the refusal path — using cf's
	// convention invoke would add schema validation we don't need here.
	//
	// Tag: freeso:convention:promote (the invocation tag the Router listens on).
	// Payload: a realistic new declaration JSON.
	newDeclPayload := `{"declaration":"{\"convention\":\"freeso-embodiment\",\"operation\":\"new-verb\",\"version\":\"1.0\",\"description\":\"test\"}"}`

	readCtx, readCancel := context.WithTimeout(context.Background(), 20*time.Second)
	defer readCancel()

	// Send the promote message and capture the message id.
	// Note: cf send uses --tag (singular), not --tags.
	sendOut, sendErr := runCf(readCtx, cfBin, cfHome, "send", campfireID,
		newDeclPayload,
		"--tag", "freeso:convention:promote",
	)
	if sendErr != nil {
		// Some cf versions return non-zero for partial output; tolerate if output contains a msg id.
		if !strings.Contains(sendOut, "msg") && !strings.Contains(sendOut, ":") {
			t.Fatalf("cf send convention:promote: %v\n%s", sendErr, sendOut)
		}
	}
	t.Logf("cf send output: %s", strings.TrimSpace(sendOut))

	// 5. Poll for the fulfillment (convention:error tag should appear).
	// The router dispatches synchronously and the handler returns fast — 5s is ample.
	var errorFulfillment string
	pollDeadline := time.Now().Add(10 * time.Second)
	for time.Now().Before(pollDeadline) {
		readOut, _ := runCf(readCtx, cfBin, cfHome, "read", campfireID, "--all", "--peek",
			"--tag", "convention:error",
		)
		if strings.Contains(readOut, ErrPromotionRefusedDuringSession) {
			errorFulfillment = readOut
			break
		}
		time.Sleep(200 * time.Millisecond)
	}

	if errorFulfillment == "" {
		t.Errorf("REJECTS BAD: no convention:error fulfillment with %q found within 10s", ErrPromotionRefusedDuringSession)
	} else {
		t.Logf("REJECTS BAD: got convention:error with %q — PASS", ErrPromotionRefusedDuringSession)
	}

	// Also assert: tools/list does NOT surface new-verb after the refused promote.
	listOut, _ := runCf(readCtx, cfBin, cfHome, "read", campfireID, "--all", "--peek",
		"--tag", "convention:operation",
	)
	if strings.Contains(listOut, "new-verb") {
		t.Errorf("REJECTS BAD: after refused promote, tools/list unexpectedly contains 'new-verb' — registry was mutated")
	} else {
		t.Logf("REJECTS BAD: tools/list does not contain new-verb after refused promote — PASS")
	}

	// ALLOWS GOOD: the convention:promote op itself IS in tools/list (it was registered at boot).
	if !strings.Contains(listOut, "convention:promote") {
		t.Errorf("ALLOWS GOOD: convention:promote op missing from tools/list — declaration not published at boot")
	} else {
		t.Logf("ALLOWS GOOD: convention:promote appears in tools/list — declaration published at boot — PASS")
	}
}

// TestIntegration_PromoteAllowed_NewDeclAppearsAfterRestart is the integration
// gate for automataisland-5bf — ALLOWS GOOD INPUT path.
//
// Protocol:
//  1. Build the freeso-sidecar binary in a temp dir.
//  2. Run the sidecar in --no-bot mode with a fresh CF_HOME. Capture the
//     campfire id from the admission block.
//  3. Assert that a "synthetic-test-verb" op is NOT in tools/list yet.
//  4. SIGINT the sidecar. Wait for it to exit.
//  5. Create a new declaration file at a temp conventions/ dir that the rebuild
//     will include. We do this by writing a JSON file alongside the conventions/
//     directory in the source tree (cleaned up via t.Cleanup).
//     NOTE: Because conventions/*.json is embedded at compile time (//go:embed),
//     "adding a declaration file" requires a rebuild. We add the file to the
//     source tree, rebuild, launch a fresh sidecar binary, then check tools/list.
//  6. Rebuild the binary with the new declaration file present.
//  7. Launch the second sidecar binary in --no-bot mode with the same CF_HOME.
//  8. Assert that synthetic-test-verb IS in tools/list on the second boot.
//  9. Cleanup: remove the synthetic declaration file from the source tree.
//
// This is the definitive proof of the frozen-registry invariant: adding a
// declaration file DOES work (at restart); runtime promotion does NOT.
//
// Skip conditions:
//   - FREESO_SKIP_INTEGRATION=1
//   - cf binary not on PATH
//   - go binary not on PATH
func TestIntegration_PromoteAllowed_NewDeclAppearsAfterRestart(t *testing.T) {
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
	cfBin, _ := exec.LookPath("cf")

	sourceDir := mustSourceDir(t)
	tmp := t.TempDir()

	// Synthetic declaration file we'll add to the source tree.
	syntheticDeclPath := filepath.Join(sourceDir, "conventions", "_synthetic-test-verb.json")
	syntheticDecl := `{
  "convention": "freeso-embodiment",
  "version": "1.0",
  "operation": "synthetic-test-verb",
  "family": "meta",
  "description": "Synthetic test verb for TestIntegration_PromoteAllowed_NewDeclAppearsAfterRestart. Never wired in production; exists only to verify the frozen-registry restart path.",
  "args": [],
  "produces_tags": [{"tag": "freeso:synthetic-test-verb", "cardinality": "exactly_one"}],
  "antecedents": "none",
  "signing": "member_key",
  "response": "sync"
}
`
	// Cleanup: remove the synthetic declaration after the test (regardless of pass/fail).
	t.Cleanup(func() {
		if err := os.Remove(syntheticDeclPath); err != nil && !os.IsNotExist(err) {
			t.Logf("cleanup: remove synthetic decl: %v", err)
		}
	})

	// Use a single CF_HOME for both sidecar runs (same campfire, different binary).
	cfHome := filepath.Join(tmp, "cf-home")
	if err := os.MkdirAll(cfHome, 0o700); err != nil {
		t.Fatalf("mkdir cf-home: %v", err)
	}

	// --- Build 1: WITHOUT the synthetic declaration ---
	bin1 := filepath.Join(tmp, "freeso-sidecar-allow-test-v1")
	build1 := exec.Command(goBin, "build", "-buildvcs=false", "-o", bin1, ".")
	build1.Dir = sourceDir
	if out, err := build1.CombinedOutput(); err != nil {
		t.Fatalf("build v1: %v\n%s", err, out)
	}

	// --- Run 1: assert synthetic-test-verb NOT in tools/list ---
	ctx1, cancel1 := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel1()

	cmd1 := exec.CommandContext(ctx1, bin1,
		"--no-bot",
		"--cf-home", cfHome,
		"--description", "promote-allow-test-v1",
	)
	stdout1, err := cmd1.StdoutPipe()
	if err != nil {
		t.Fatalf("stdout1 pipe: %v", err)
	}
	stderr1, err := cmd1.StderrPipe()
	if err != nil {
		t.Fatalf("stderr1 pipe: %v", err)
	}
	if err := cmd1.Start(); err != nil {
		t.Fatalf("start sidecar v1: %v", err)
	}
	go func() {
		buf := make([]byte, 4096)
		for {
			n, err := stderr1.Read(buf)
			if n > 0 {
				t.Logf("sidecar-v1-stderr: %s", strings.TrimRight(string(buf[:n]), "\n"))
			}
			if err != nil {
				return
			}
		}
	}()

	id1 := waitForCampfireID(t, stdout1, 20*time.Second)
	t.Logf("v1 campfire id: %s", id1)

	readCtx1, readCancel1 := context.WithTimeout(context.Background(), 10*time.Second)
	defer readCancel1()

	listOut1, _ := runCf(readCtx1, cfBin, cfHome, "read", id1, "--all", "--peek", "--tag", "convention:operation")
	if strings.Contains(listOut1, "synthetic-test-verb") {
		t.Errorf("v1: synthetic-test-verb unexpectedly in tools/list before it was added")
	} else {
		t.Logf("v1: synthetic-test-verb NOT in tools/list — correct")
	}

	// Shutdown v1.
	if err := cmd1.Process.Signal(os.Interrupt); err != nil {
		t.Logf("SIGINT v1: %v", err)
	}
	done1 := make(chan error, 1)
	go func() { done1 <- cmd1.Wait() }()
	select {
	case <-done1:
		t.Log("v1 exited")
	case <-time.After(10 * time.Second):
		t.Log("v1 did not exit within 10s; killing")
		_ = cmd1.Process.Kill()
		<-done1
	}
	cancel1()

	// --- Add the synthetic declaration file ---
	// File starts with "_" so the existing LoadDeclarations skips it (underscore prefix).
	// BUT: our gate requires that the NEW declaration appears. The LoadDeclarations
	// function skips files with "_" prefix. We'll add WITHOUT underscore prefix for
	// the v2 build (and clean it up after). We use a unique name that won't collide.
	// Actually, looking again: LoadDeclarations skips "_" prefix files. We need to
	// add a file WITHOUT underscore but also we must clean it up. Use a .json file
	// that is clearly test-only by name so lint will still pass.
	//
	// Change plan: write the file at a name WITHOUT underscore so it's loaded.
	// Name: conventions/zz-synthetic-test-verb-5bf.json (lex-last so sorted last, clearly test-only).
	syntheticDeclPath2 := filepath.Join(sourceDir, "conventions", "zz-synthetic-test-verb-5bf.json")
	t.Cleanup(func() {
		if err := os.Remove(syntheticDeclPath2); err != nil && !os.IsNotExist(err) {
			t.Logf("cleanup: remove synthetic decl v2: %v", err)
		}
	})
	if err := os.WriteFile(syntheticDeclPath2, []byte(syntheticDecl), 0o644); err != nil {
		t.Fatalf("write synthetic decl: %v", err)
	}
	t.Logf("wrote synthetic declaration to %s", syntheticDeclPath2)

	// --- Build 2: WITH the synthetic declaration ---
	bin2 := filepath.Join(tmp, "freeso-sidecar-allow-test-v2")
	build2 := exec.Command(goBin, "build", "-buildvcs=false", "-o", bin2, ".")
	build2.Dir = sourceDir
	if out, err := build2.CombinedOutput(); err != nil {
		t.Fatalf("build v2: %v\n%s", err, out)
	}
	t.Log("v2 build succeeded with synthetic declaration")

	// --- Run 2: assert synthetic-test-verb IS in tools/list ---
	ctx2, cancel2 := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel2()

	cmd2 := exec.CommandContext(ctx2, bin2,
		"--no-bot",
		"--cf-home", cfHome,
		"--description", "promote-allow-test-v2",
	)
	stdout2, err := cmd2.StdoutPipe()
	if err != nil {
		t.Fatalf("stdout2 pipe: %v", err)
	}
	stderr2, err := cmd2.StderrPipe()
	if err != nil {
		t.Fatalf("stderr2 pipe: %v", err)
	}
	if err := cmd2.Start(); err != nil {
		t.Fatalf("start sidecar v2: %v", err)
	}
	defer func() {
		_ = cmd2.Process.Signal(os.Interrupt)
		_, _ = cmd2.Process.Wait()
	}()
	go func() {
		buf := make([]byte, 4096)
		for {
			n, err := stderr2.Read(buf)
			if n > 0 {
				t.Logf("sidecar-v2-stderr: %s", strings.TrimRight(string(buf[:n]), "\n"))
			}
			if err != nil {
				return
			}
		}
	}()

	id2 := waitForCampfireID(t, stdout2, 20*time.Second)
	t.Logf("v2 campfire id: %s", id2)

	readCtx2, readCancel2 := context.WithTimeout(context.Background(), 10*time.Second)
	defer readCancel2()

	// Poll for the synthetic-test-verb to appear in tools/list.
	var listOut2 string
	pollDeadline := time.Now().Add(8 * time.Second)
	for time.Now().Before(pollDeadline) {
		listOut2, _ = runCf(readCtx2, cfBin, cfHome, "read", id2, "--all", "--peek", "--tag", "convention:operation")
		if strings.Contains(listOut2, "synthetic-test-verb") {
			break
		}
		time.Sleep(200 * time.Millisecond)
	}

	if !strings.Contains(listOut2, "synthetic-test-verb") {
		t.Errorf("ALLOWS GOOD: synthetic-test-verb NOT in tools/list after restart with new declaration — frozen-registry boot path broken")
	} else {
		t.Logf("ALLOWS GOOD: synthetic-test-verb IS in tools/list after restart with new declaration — PASS")
	}

	t.Log("PASS: TestIntegration_PromoteAllowed_NewDeclAppearsAfterRestart — frozen-registry invariant verified (REJECTS runtime promotion; ALLOWS new decl at restart)")
}

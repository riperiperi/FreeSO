/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package readiness

// smoketest_test.go — Boot smoke test gate pair (automataisland-f28).
//
// Test strategy: real handler counter (fakeHandlers, same as futures_test),
// real skill file parsing against temp directories written in-process.
// No mocks of the smoke logic — RunSmokeTest, RunSmokeAtBoot, and
// runWorldReadyGate are exercised against the real code paths.
//
// Gate pairs (all four are mandatory per item spec):
//   REJECTS (handler missing): handler_count < declared_op_count → world:ready does not fulfill within 60s
//   REJECTS (skill unknown ref): skill file references unknown op → world:ready does not fulfill
//   ALLOWS (handlers complete): handler_count == declared_op_count → world:ready fulfills
//   ALLOWS (skill refs resolved): skill file references known op → world:ready fulfills

import (
	"context"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"
)

// ----- Unit tests for RunSmokeTest -----

// TestRunSmokeTest_HandlerCountParity_Fail: REJECTS when handler_count < declared_op_count.
// The declared set has 3 ops but only 2 handlers are registered → smoke fails,
// MissingHandlers is populated, log shows the gap.
func TestRunSmokeTest_HandlerCountParity_Fail(t *testing.T) {
	result := RunSmokeTest(2, []string{"op-a", "op-b", "op-c"}, "")
	if result.Passed {
		t.Fatal("smoke test should FAIL when handler_count (2) < declared_op_count (3)")
	}
	if result.Skipped {
		t.Error("should not be marked Skipped when FREESO_SKIP_SMOKE_TEST is unset")
	}
	if len(result.MissingHandlers) == 0 {
		t.Error("MissingHandlers should be non-empty on handler count mismatch")
	}
}

// TestRunSmokeTest_HandlerCountParity_Pass: ALLOWS when handler_count == declared_op_count.
func TestRunSmokeTest_HandlerCountParity_Pass(t *testing.T) {
	result := RunSmokeTest(3, []string{"op-a", "op-b", "op-c"}, "")
	if !result.Passed {
		t.Fatalf("smoke test should PASS when handler_count == declared_op_count; missing=%v skill=%v",
			result.MissingHandlers, result.MissingSkillRefs)
	}
}

// TestRunSmokeTest_HandlerCountParity_Excess: REJECTS when handler_count > declared_op_count.
// More handlers than declarations is also a mismatch.
func TestRunSmokeTest_HandlerCountParity_Excess(t *testing.T) {
	result := RunSmokeTest(4, []string{"op-a", "op-b", "op-c"}, "")
	if result.Passed {
		t.Fatal("smoke test should FAIL when handler_count (4) > declared_op_count (3)")
	}
}

// TestRunSmokeTest_SkipSmokeTest_Env: FREESO_SKIP_SMOKE_TEST=1 bypasses all checks.
func TestRunSmokeTest_SkipSmokeTest_Env(t *testing.T) {
	t.Setenv("FREESO_SKIP_SMOKE_TEST", "1")
	// Deliberately wrong counts + bogus skill refs — smoke must still pass.
	result := RunSmokeTest(0, []string{"op-a", "op-b"}, "/nonexistent/path/to/skills")
	if !result.Passed {
		t.Fatal("smoke test should PASS when FREESO_SKIP_SMOKE_TEST=1 regardless of counts")
	}
	if !result.Skipped {
		t.Error("result.Skipped should be true when FREESO_SKIP_SMOKE_TEST=1")
	}
}

// TestRunSmokeTest_SkillIntegrity_Fail_CFMCPRef: REJECTS when a skill file
// references an unknown convention via the cf-mcp:<convention>__<op> pattern.
func TestRunSmokeTest_SkillIntegrity_Fail_CFMCPRef(t *testing.T) {
	dir := t.TempDir()
	// Write a skill that references "unknown-verb" via MCP pattern.
	skillContent := `# My Skill
Use cf-mcp:freeso-embodiment__unknown-verb to do something.
`
	if err := os.WriteFile(filepath.Join(dir, "my-skill.md"), []byte(skillContent), 0o644); err != nil {
		t.Fatalf("write skill file: %v", err)
	}

	// declared set does NOT include "unknown-verb"
	result := RunSmokeTest(2, []string{"op-a", "op-b"}, dir)
	if result.Passed {
		t.Fatal("smoke test should FAIL when skill references unknown convention op via cf-mcp pattern")
	}
	if len(result.MissingSkillRefs) == 0 {
		t.Error("MissingSkillRefs should be non-empty when skill has unknown op reference")
	}
	// The failure message must name the skill path and the unknown op.
	if !strings.Contains(result.MissingSkillRefs[0], "unknown-verb") {
		t.Errorf("failure message should mention the unknown op; got: %q", result.MissingSkillRefs[0])
	}
	if !strings.Contains(result.MissingSkillRefs[0], "smoke test failed") {
		t.Errorf("failure message should contain 'smoke test failed'; got: %q", result.MissingSkillRefs[0])
	}
}

// TestRunSmokeTest_SkillIntegrity_Fail_CFCLIRef: REJECTS when a skill file
// references an unknown convention via the "cf <alias> <op>" CLI pattern.
func TestRunSmokeTest_SkillIntegrity_Fail_CFCLIRef(t *testing.T) {
	dir := t.TempDir()
	skillContent := "cf $BODY_CF missing-op --arg1 value\n"
	if err := os.WriteFile(filepath.Join(dir, "skill.md"), []byte(skillContent), 0o644); err != nil {
		t.Fatalf("write skill: %v", err)
	}
	result := RunSmokeTest(1, []string{"known-op"}, dir)
	if result.Passed {
		t.Fatal("smoke test should FAIL: skill references 'missing-op' not in declared set")
	}
	if len(result.MissingSkillRefs) == 0 {
		t.Error("MissingSkillRefs should be non-empty")
	}
}

// TestRunSmokeTest_SkillIntegrity_Pass_KnownRef: ALLOWS when skill file
// references a declared convention op via cf-mcp pattern.
func TestRunSmokeTest_SkillIntegrity_Pass_KnownRef(t *testing.T) {
	dir := t.TempDir()
	skillContent := "Use cf-mcp:freeso-embodiment__speak to say something.\n"
	if err := os.WriteFile(filepath.Join(dir, "speak-skill.md"), []byte(skillContent), 0o644); err != nil {
		t.Fatalf("write skill: %v", err)
	}
	// "speak" is in the declared set.
	result := RunSmokeTest(1, []string{"speak"}, dir)
	if !result.Passed {
		t.Fatalf("smoke test should PASS: 'speak' is declared; missing=%v skill=%v",
			result.MissingHandlers, result.MissingSkillRefs)
	}
}

// TestRunSmokeTest_SkillIntegrity_Pass_CLIKnownRef: ALLOWS when skill
// references a known op via the cf CLI pattern.
func TestRunSmokeTest_SkillIntegrity_Pass_CLIKnownRef(t *testing.T) {
	dir := t.TempDir()
	skillContent := "cf $BODY_CF walk-to --target_x 10 --target_y 20\n"
	if err := os.WriteFile(filepath.Join(dir, "walk.md"), []byte(skillContent), 0o644); err != nil {
		t.Fatalf("write skill: %v", err)
	}
	result := RunSmokeTest(1, []string{"walk-to"}, dir)
	if !result.Passed {
		t.Fatalf("smoke test should PASS: 'walk-to' is declared; missing=%v skill=%v",
			result.MissingHandlers, result.MissingSkillRefs)
	}
}

// TestRunSmokeTest_SkillIntegrity_SkipsExampleFences: angle-bracket placeholders
// inside fenced blocks (<convention>, <op>) are stripped so they don't
// generate false positives.
func TestRunSmokeTest_SkillIntegrity_SkipsExampleFences(t *testing.T) {
	dir := t.TempDir()
	skillContent := "```example\ncf $CF <convention>__<op> --arg value\n```\n"
	if err := os.WriteFile(filepath.Join(dir, "docs.md"), []byte(skillContent), 0o644); err != nil {
		t.Fatalf("write skill: %v", err)
	}
	// Empty declared set — the placeholder should be stripped.
	result := RunSmokeTest(0, []string{}, dir)
	if !result.Passed {
		t.Fatalf("smoke test should PASS: example fence with placeholder should be ignored; skill=%v",
			result.MissingSkillRefs)
	}
}

// TestRunSmokeTest_SkillIntegrity_MissingDir: non-existent skills dir is
// treated as non-fatal (no skill audit) → only handler count gate runs.
func TestRunSmokeTest_SkillIntegrity_MissingDir(t *testing.T) {
	// When skills dir doesn't exist, the directory walk fails with IsNotExist.
	// The design says: "Directory scan failure is treated as a non-fatal log —
	// missing skill dir is common during dev." We don't fail the smoke test.
	result := RunSmokeTest(1, []string{"op-a"}, "/nonexistent/skills/dir")
	// Handler count matches → should pass despite missing dir.
	if !result.Passed {
		t.Fatalf("smoke test should PASS when skills dir is absent (non-fatal); missing=%v skill=%v",
			result.MissingHandlers, result.MissingSkillRefs)
	}
}

// TestRunSmokeTest_SkillIntegrity_EmptyDir: empty skills dir → skill gate passes trivially.
func TestRunSmokeTest_SkillIntegrity_EmptyDir(t *testing.T) {
	dir := t.TempDir()
	result := RunSmokeTest(2, []string{"op-a", "op-b"}, dir)
	if !result.Passed {
		t.Fatalf("smoke test should PASS with empty skills dir; missing=%v skill=%v",
			result.MissingHandlers, result.MissingSkillRefs)
	}
}

// TestRunSmokeTest_MultipleSkills_OneFailure: if one skill passes and another
// fails, the result is failed and MissingSkillRefs lists the failure.
func TestRunSmokeTest_MultipleSkills_OneFailure(t *testing.T) {
	dir := t.TempDir()
	// Good skill: references "speak" which is declared.
	if err := os.WriteFile(filepath.Join(dir, "good.md"),
		[]byte("cf $CF speak --message hello\n"), 0o644); err != nil {
		t.Fatal(err)
	}
	// Bad skill: references "phantom-op" which is not declared.
	if err := os.WriteFile(filepath.Join(dir, "bad.md"),
		[]byte("cf-mcp:freeso-embodiment__phantom-op\n"), 0o644); err != nil {
		t.Fatal(err)
	}

	result := RunSmokeTest(1, []string{"speak"}, dir)
	if result.Passed {
		t.Fatal("smoke test should FAIL: bad.md references undeclared op")
	}
	if len(result.MissingSkillRefs) == 0 {
		t.Error("MissingSkillRefs should have one entry")
	}
}

// ----- Integration tests: smoke gate blocks/allows world:ready fulfillment -----

// TestSmoke_WorldReady_BlockedWhenHandlerMissing (REJECTS gate pair):
// boot with handler_count < declared_op_count → world:ready MUST NOT fulfill
// within 60s, and the sidecar log shows the gap. This is the primary
// REJECTS test required by the item spec.
//
// We use a short observation window (2s) for test speed — the gate behavior
// is the same regardless of duration since the gate exits immediately on a
// failed smoke.
func TestSmoke_WorldReady_BlockedWhenHandlerMissing(t *testing.T) {
	pub := newFakePublisher()
	h := &fakeHandlers{}
	p := &fakePerception{}
	b := &fakeBridge{}

	// 3 declared ops, but only 2 handlers registered at boot.
	declaredOpNames := []string{"op-a", "op-b", "op-c"}
	h.set(2) // ONE missing handler

	f := New(pub, h, p, b, 3, declaredOpNames, "", "key", 30)
	f.gatePollInterval = 20 * time.Millisecond

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()
	if err := f.PublishAtBoot(ctx); err != nil {
		t.Fatalf("PublishAtBoot: %v", err)
	}

	// Smoke fails because handler count doesn't match.
	result := f.RunSmokeAtBoot()
	if result.Passed {
		t.Fatal("smoke test should have failed with handler_count=2 vs declared=3")
	}
	if len(result.MissingHandlers) == 0 {
		t.Error("smoke result should have MissingHandlers populated")
	}
	// Log message must mention the gap — we verify the result struct has the info.
	found := false
	for _, msg := range result.MissingHandlers {
		if strings.Contains(msg, "smoke test failed") && strings.Contains(msg, "handler_count=2") {
			found = true
		}
	}
	if !found {
		t.Errorf("MissingHandlers message should contain 'smoke test failed' and 'handler_count=2'; got %v",
			result.MissingHandlers)
	}

	// world:ready must NOT fulfill — RunGates exits immediately because smokeOK=false.
	// We also turn all other gates green so only the smoke blocks fulfillment.
	h.set(3) // now handler count matches (but smoke already ran)
	p.set(true, time.Now().UnixMilli(), 99)
	f.RunGates(ctx)

	// Wait 2 seconds — world:ready should never arrive.
	time.Sleep(2 * time.Second)
	if hasFulfillment(pub.snapshot(), TagWorldReady) {
		t.Fatal("world:ready fulfilled despite smoke test failure — smoke gate is not enforced")
	}
}

// TestSmoke_WorldReady_FulfillsWhenHandlersComplete (ALLOWS gate pair):
// boot with handler_count == declared_op_count → world:ready MUST fulfill
// when all other gates also turn green. This is the primary ALLOWS test.
func TestSmoke_WorldReady_FulfillsWhenHandlersComplete(t *testing.T) {
	pub := newFakePublisher()
	h := &fakeHandlers{}
	p := &fakePerception{}
	b := &fakeBridge{}

	// 3 declared ops, exactly 3 handlers registered.
	declaredOpNames := []string{"op-a", "op-b", "op-c"}
	h.set(3)

	f := New(pub, h, p, b, 3, declaredOpNames, "", "key", 30)
	f.gatePollInterval = 20 * time.Millisecond

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()
	if err := f.PublishAtBoot(ctx); err != nil {
		t.Fatalf("PublishAtBoot: %v", err)
	}

	result := f.RunSmokeAtBoot()
	if !result.Passed {
		t.Fatalf("smoke test should pass with handler_count=3 == declared=3; missing=%v skill=%v",
			result.MissingHandlers, result.MissingSkillRefs)
	}
	if !f.SmokeOK() {
		t.Error("SmokeOK() should return true after passing smoke test")
	}

	// Turn all other gates green and start RunGates.
	p.set(true, time.Now().UnixMilli(), 42)
	f.RunGates(ctx)

	// world:ready should fulfill within 2 seconds.
	deadline := time.Now().Add(2 * time.Second)
	for time.Now().Before(deadline) {
		if hasFulfillment(pub.snapshot(), TagWorldReady) {
			break
		}
		time.Sleep(20 * time.Millisecond)
	}
	if !hasFulfillment(pub.snapshot(), TagWorldReady) {
		t.Fatal("world:ready did not fulfill within 2s with all gates green including smoke")
	}
}

// TestSmoke_WorldReady_BlockedBySkillUnknownRef (REJECTS gate pair — skill):
// a skill file references an op not in the declared set → smoke fails →
// world:ready does not fulfill.
func TestSmoke_WorldReady_BlockedBySkillUnknownRef(t *testing.T) {
	dir := t.TempDir()
	// Skill references "ghost-op" which is not declared.
	if err := os.WriteFile(filepath.Join(dir, "ghost-skill.md"),
		[]byte("cf-mcp:freeso-embodiment__ghost-op\n"), 0o644); err != nil {
		t.Fatalf("write skill: %v", err)
	}

	pub := newFakePublisher()
	h := &fakeHandlers{}
	p := &fakePerception{}
	b := &fakeBridge{}

	declaredOpNames := []string{"speak", "walk-to"}
	h.set(2) // handler count matches — only skill gate fails

	f := New(pub, h, p, b, 2, declaredOpNames, dir, "key", 30)
	f.gatePollInterval = 20 * time.Millisecond

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()
	if err := f.PublishAtBoot(ctx); err != nil {
		t.Fatalf("PublishAtBoot: %v", err)
	}

	result := f.RunSmokeAtBoot()
	if result.Passed {
		t.Fatal("smoke test should fail: skill references unknown op 'ghost-op'")
	}
	if len(result.MissingSkillRefs) == 0 {
		t.Error("MissingSkillRefs should be populated")
	}
	found := false
	for _, msg := range result.MissingSkillRefs {
		if strings.Contains(msg, "ghost-op") && strings.Contains(msg, "smoke test failed") {
			found = true
		}
	}
	if !found {
		t.Errorf("failure message should mention 'ghost-op'; got %v", result.MissingSkillRefs)
	}

	// world:ready must not fulfill.
	p.set(true, time.Now().UnixMilli(), 1)
	f.RunGates(ctx)
	time.Sleep(500 * time.Millisecond)
	if hasFulfillment(pub.snapshot(), TagWorldReady) {
		t.Fatal("world:ready fulfilled despite skill integrity failure")
	}
}

// TestSmoke_WorldReady_AllowsWithKnownSkillRef (ALLOWS gate pair — skill):
// a skill file references a declared op → smoke passes (skill gate green) →
// world:ready fulfills when all other gates also turn green.
func TestSmoke_WorldReady_AllowsWithKnownSkillRef(t *testing.T) {
	dir := t.TempDir()
	// Skill references "speak" which IS declared.
	if err := os.WriteFile(filepath.Join(dir, "speak-skill.md"),
		[]byte("cf-mcp:freeso-embodiment__speak\n"), 0o644); err != nil {
		t.Fatalf("write skill: %v", err)
	}

	pub := newFakePublisher()
	h := &fakeHandlers{}
	p := &fakePerception{}
	b := &fakeBridge{}

	declaredOpNames := []string{"speak"}
	h.set(1) // exactly 1 handler registered

	f := New(pub, h, p, b, 1, declaredOpNames, dir, "key", 30)
	f.gatePollInterval = 20 * time.Millisecond

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()
	if err := f.PublishAtBoot(ctx); err != nil {
		t.Fatalf("PublishAtBoot: %v", err)
	}

	result := f.RunSmokeAtBoot()
	if !result.Passed {
		t.Fatalf("smoke test should pass: 'speak' is declared and handler count matches; missing=%v skill=%v",
			result.MissingHandlers, result.MissingSkillRefs)
	}

	p.set(true, time.Now().UnixMilli(), 7)
	f.RunGates(ctx)

	deadline := time.Now().Add(2 * time.Second)
	for time.Now().Before(deadline) {
		if hasFulfillment(pub.snapshot(), TagWorldReady) {
			break
		}
		time.Sleep(20 * time.Millisecond)
	}
	if !hasFulfillment(pub.snapshot(), TagWorldReady) {
		t.Fatal("world:ready did not fulfill within 2s with all gates green (including skill integrity)")
	}
}

// ----- Unit tests for extractConventionRefs -----

// TestExtractConventionRefs_CFMCPPattern verifies the cf-mcp regex extracts
// the op name (the part after "__").
func TestExtractConventionRefs_CFMCPPattern(t *testing.T) {
	text := "Use cf-mcp:freeso-embodiment__speak\nAlso cf-mcp:freeso-embodiment__walk-to"
	refs := extractConventionRefs(text)
	wants := map[string]bool{"speak": false, "walk-to": false}
	for _, r := range refs {
		wants[r] = true
	}
	for op, found := range wants {
		if !found {
			t.Errorf("expected op %q in refs; got %v", op, refs)
		}
	}
}

// TestExtractConventionRefs_CFCLIPattern verifies the cf <ref> <op> regex.
func TestExtractConventionRefs_CFCLIPattern(t *testing.T) {
	text := "cf $BODY_CF interact-with --target 5"
	refs := extractConventionRefs(text)
	if len(refs) == 0 {
		t.Fatal("expected at least one ref from cf CLI pattern")
	}
	found := false
	for _, r := range refs {
		if r == "interact-with" {
			found = true
		}
	}
	if !found {
		t.Errorf("expected 'interact-with' in refs; got %v", refs)
	}
}

// TestExtractConventionRefs_SkipsExampleFence verifies placeholder content
// inside example fences is not extracted.
func TestExtractConventionRefs_SkipsExampleFence(t *testing.T) {
	text := "```example\ncf $CF <op-placeholder> --arg val\ncf-mcp:conv__<placeholder-op>\n```\n"
	refs := extractConventionRefs(text)
	for _, r := range refs {
		if strings.Contains(r, "<") || strings.Contains(r, ">") {
			t.Errorf("placeholder leaked through: %q", r)
		}
	}
}

// TestExtractConventionRefs_SkipsPlaceholderFenceOnDetection verifies a
// regular fence that turns out to have placeholders mid-body is also skipped.
func TestExtractConventionRefs_SkipsPlaceholderFenceOnDetection(t *testing.T) {
	text := "```bash\ncf <campfire-id> unknown-verb\n```\n"
	refs := extractConventionRefs(text)
	// The fence body contains "<campfire-id>" which triggers skipFence.
	// "unknown-verb" must NOT appear in refs.
	for _, r := range refs {
		if r == "unknown-verb" {
			t.Errorf("op from placeholder fence leaked: %q", r)
		}
	}
}

// TestExtractConventionRefs_DeduplcatesRefs: duplicate references are emitted once.
func TestExtractConventionRefs_DeduplicatesRefs(t *testing.T) {
	text := "cf $CF speak\ncf $CF speak\ncf-mcp:freeso-embodiment__speak"
	refs := extractConventionRefs(text)
	count := 0
	for _, r := range refs {
		if r == "speak" {
			count++
		}
	}
	if count != 1 {
		t.Errorf("expected 'speak' exactly once; got %d times in %v", count, refs)
	}
}

// ----- Benchmark -----

// BenchmarkRunSmokeTest: a rough cost estimate for the one-shot boot check
// on a modest number of declarations. Should run in microseconds.
func BenchmarkRunSmokeTest(b *testing.B) {
	ops := make([]string, 80)
	for i := range ops {
		ops[i] = fmt.Sprintf("op-%d", i)
	}
	dir := b.TempDir()
	for i := 0; i < 5; i++ {
		content := fmt.Sprintf("cf $CF %s\n", ops[i])
		if err := os.WriteFile(filepath.Join(dir, fmt.Sprintf("s%d.md", i)), []byte(content), 0o644); err != nil {
			b.Fatal(err)
		}
	}
	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		RunSmokeTest(80, ops, dir)
	}
}

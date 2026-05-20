/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

// smoketest.go implements the boot smoke test for the readiness gate
// (automataisland-f28 — Component 2 / Component 10).
//
// The smoke test runs ONCE at sidecar boot and checks two invariants:
//
//  1. Handler-count parity: the number of registered convention handlers
//     equals the number of declared operations. If any declaration lacks a
//     handler, world:ready will never fulfill — the sidecar logs which ops
//     are missing so the operator can diagnose.
//
//  2. Skill referential integrity: every convention reference in skill
//     markdown files (cf-mcp:<convention>__<op> or cf <alias-or-id> <op>
//     patterns) resolves to a declared operation. An unknown reference fails
//     the smoke test for the same reason — the talent would invoke a
//     non-existent op.
//
// FREESO_SKIP_SMOKE_TEST=1 bypasses both checks (dev iteration only; never
// set in production charts).
//
// A failed smoke test does NOT crash the sidecar — it logs the gap and
// returns a non-nil error that the caller stores. world:ready gate (1) checks
// smokeOK before polling handler_count, so a failed smoke test permanently
// blocks world:ready for this sidecar lifetime.
package readiness

import (
	"fmt"
	"io/fs"
	"log"
	"os"
	"path/filepath"
	"regexp"
	"strings"
)

// SmokeResult captures the outcome of a single RunSmokeTest execution.
type SmokeResult struct {
	// Passed is true when all smoke checks succeed (or FREESO_SKIP_SMOKE_TEST=1).
	Passed bool

	// Skipped is true when FREESO_SKIP_SMOKE_TEST=1 was set.
	Skipped bool

	// MissingHandlers lists declared operation names that have no registered handler.
	MissingHandlers []string

	// MissingSkillRefs lists skill referential integrity failures.
	// Each entry is a human-readable string: "skill <path>: references unknown convention <op>".
	MissingSkillRefs []string
}

// RunSmokeTest executes the one-shot boot smoke test.
//
// Parameters:
//   - handlerCount: number of registered convention handlers (Router.Count() snapshot at boot).
//   - declaredOps: the ordered set of declared operation names from conventions/*.json.
//   - skillsDir: path to the directory containing skill markdown files to audit.
//     Empty string → skill integrity check is skipped (no skills configured).
//
// Returns a SmokeResult. The caller should treat Passed==false as a permanent
// block on world:ready fulfillment.
func RunSmokeTest(handlerCount int, declaredOps []string, skillsDir string) SmokeResult {
	if os.Getenv("FREESO_SKIP_SMOKE_TEST") == "1" {
		log.Printf("readiness: smoke test BYPASSED (FREESO_SKIP_SMOKE_TEST=1) — skipping handler-count and skill-integrity checks (dev only)")
		return SmokeResult{Passed: true, Skipped: true}
	}

	var result SmokeResult
	declaredCount := len(declaredOps)

	// Build a set of declared ops for O(1) lookup.
	declaredSet := make(map[string]bool, declaredCount)
	for _, op := range declaredOps {
		declaredSet[op] = true
	}

	// Gate 1: handler-count parity.
	//
	// We compare handlerCount against len(declaredOps). We cannot enumerate
	// which specific handlers are missing from handlerCount alone (the Router
	// only reports a count, not a list). We log the delta and mark all as
	// missing symbolically when the count is wrong.
	//
	// The item spec allows: "Sidecar logs the gap (which declared ops have no
	// handler)." With only a count available, we log the numeric delta. If a
	// richer enumeration is needed, the caller can extend HandlerCounter.
	if handlerCount != declaredCount {
		delta := declaredCount - handlerCount
		if delta < 0 {
			delta = -delta
		}
		msg := fmt.Sprintf("smoke test failed: handler_count=%d != declared_op_count=%d (delta=%d) — %d ops lack handlers",
			handlerCount, declaredCount, delta, declaredCount-handlerCount)
		log.Printf("readiness: %s", msg)
		result.MissingHandlers = append(result.MissingHandlers, msg)
	}

	// Gate 2: skill referential integrity.
	if skillsDir != "" {
		refs, err := auditSkillRefs(skillsDir, declaredSet)
		if err != nil {
			// Directory scan failure is treated as a non-fatal log — missing
			// skill dir is common during dev (no jail configured). We do NOT
			// fail the smoke test on a directory-not-found error; only on
			// resolved-but-bad references.
			log.Printf("readiness: skill integrity audit skipped: %v", err)
		} else {
			result.MissingSkillRefs = refs
			for _, r := range refs {
				log.Printf("readiness: %s", r)
			}
		}
	}

	result.Passed = len(result.MissingHandlers) == 0 && len(result.MissingSkillRefs) == 0
	if result.Passed {
		log.Printf("readiness: smoke test PASSED (handler_count=%d == declared_op_count=%d, skill integrity OK)",
			handlerCount, declaredCount)
	}
	return result
}

// skillConventionRefRe matches the two reference patterns the design specifies:
//
//  1. cf-mcp:<convention>__<op>   — MCP tool reference in a skill markdown
//  2. cf <alias-or-id> <op>       — direct campfire CLI call
//
// Pattern 1: literal "cf-mcp:" followed by a convention name, "__", op name.
// Pattern 2: word "cf" followed by whitespace, any non-whitespace campfire
// alias or hex ID, whitespace, then an op name (word chars + hyphens).
//
// False-positive bias is acceptable (a placeholder like "<convention>" treated
// as a missing op is a loud, diagnosable failure). We skip code-fence blocks
// marked as examples to reduce noise from tutorial text.
var (
	// cfMCPRefRe matches "cf-mcp:<convention>__<op>" patterns.
	// Capture group 1 = op name (the part after "__").
	cfMCPRefRe = regexp.MustCompile(`cf-mcp:[a-zA-Z0-9_-]+__([a-zA-Z0-9_-]+)`)

	// cfCLIRefRe matches "cf <campfire-ref> <op>" patterns.
	// Capture group 1 = op name (the third token).
	cfCLIRefRe = regexp.MustCompile(`\bcf\s+\S+\s+([a-zA-Z0-9_-]+)`)
)

// auditSkillRefs scans skillsDir for *.md files, extracts convention
// references, and returns a list of failure strings for any reference that
// does not resolve to a declared op. Returns an error if the directory cannot
// be read (caller decides how to handle).
func auditSkillRefs(skillsDir string, declaredSet map[string]bool) ([]string, error) {
	var failures []string

	err := filepath.WalkDir(skillsDir, func(path string, d fs.DirEntry, werr error) error {
		if werr != nil {
			return werr
		}
		if d.IsDir() {
			return nil
		}
		if strings.ToLower(filepath.Ext(path)) != ".md" {
			return nil
		}

		data, rerr := os.ReadFile(path)
		if rerr != nil {
			log.Printf("readiness: skill integrity: read %s: %v (skipping)", path, rerr)
			return nil
		}

		refs := extractConventionRefs(string(data))
		for _, op := range refs {
			if !declaredSet[op] {
				failures = append(failures,
					fmt.Sprintf("smoke test failed: skill %s references unknown convention %q", path, op))
			}
		}
		return nil
	})

	if err != nil {
		if os.IsNotExist(err) {
			return nil, fmt.Errorf("skills directory %q not found", skillsDir)
		}
		return nil, fmt.Errorf("walk skills dir %q: %w", skillsDir, err)
	}

	return failures, nil
}

// extractConventionRefs returns the set of op names referenced by convention
// calls in the markdown text. Strips code-fence blocks marked as "example"
// or that contain a description suggesting they are illustrative
// (fenced blocks starting with ```example or containing placeholder tokens
// like "<convention>" or "<op>").
//
// We do NOT strip all code fences — real skill invocations often live inside
// fences. We strip only fences explicitly tagged as examples or containing
// angle-bracket placeholders, per the design note: "Skip code blocks marked
// as examples. False-positive bias acceptable."
func extractConventionRefs(text string) []string {
	// Strip "example" fences: ```example ... ``` or ``` ... ``` blocks
	// containing angle-bracket placeholders ("<convention>", "<op>", "<id>").
	stripped := stripExampleFences(text)

	seen := map[string]bool{}
	var ops []string

	addOp := func(op string) {
		if seen[op] {
			return
		}
		// Skip obvious placeholders: op names containing angle brackets or
		// that are pure whitespace.
		if strings.ContainsAny(op, "<>") || strings.TrimSpace(op) == "" {
			return
		}
		seen[op] = true
		ops = append(ops, op)
	}

	for _, m := range cfMCPRefRe.FindAllStringSubmatch(stripped, -1) {
		addOp(m[1])
	}
	for _, m := range cfCLIRefRe.FindAllStringSubmatch(stripped, -1) {
		addOp(m[1])
	}

	return ops
}

// stripExampleFences removes fenced code blocks that are annotated as
// examples or contain angle-bracket placeholders, per the design note.
// We use a simple line-scanner (not a full Markdown parser) — good enough
// for the false-positive-bias policy.
func stripExampleFences(text string) string {
	lines := strings.Split(text, "\n")
	var out []string
	inFence := false
	skipFence := false

	for _, line := range lines {
		trimmed := strings.TrimSpace(line)
		if !inFence {
			if strings.HasPrefix(trimmed, "```") {
				// Check if this is an example fence or placeholder fence.
				tag := strings.ToLower(strings.TrimPrefix(trimmed, "```"))
				if tag == "example" || strings.Contains(tag, "example") {
					inFence = true
					skipFence = true
					continue
				}
				inFence = true
				skipFence = false
			}
			out = append(out, line)
		} else {
			if strings.HasPrefix(trimmed, "```") {
				inFence = false
				skipFence = false
				continue
			}
			// If we detect angle-bracket placeholders inside the fence, mark
			// the whole fence as skip (upgrade decision mid-fence).
			if !skipFence && (strings.Contains(line, "<convention>") ||
				strings.Contains(line, "<op>") ||
				strings.Contains(line, "<campfire-id>") ||
				strings.Contains(line, "<cf-id>")) {
				skipFence = true
			}
			if !skipFence {
				out = append(out, line)
			}
		}
	}

	return strings.Join(out, "\n")
}

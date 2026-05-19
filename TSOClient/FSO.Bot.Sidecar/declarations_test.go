/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"testing"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// TestLoadDeclarations asserts every conventions/*.json parses into a valid
// convention.Declaration. This guards against a verb-catalog sweep landing a
// broken declaration that'd take down sidecar startup in production.
func TestLoadDeclarations(t *testing.T) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}
	if len(decls) == 0 {
		t.Fatalf("no declarations loaded — expected at least the freeso-embodiment verb set")
	}

	ops := map[string]int{}
	for _, d := range decls {
		if d.Convention != "freeso-embodiment" {
			t.Errorf("decl %q: convention=%q want freeso-embodiment", d.Operation, d.Convention)
		}
		if d.Version == "" {
			t.Errorf("decl %q: empty version", d.Operation)
		}
		if d.Operation == "" {
			t.Errorf("decl with empty operation (malformed file)")
		}
		if d.Signing == "" {
			t.Errorf("decl %q: empty signing", d.Operation)
		}
		if d.Description == "" {
			t.Errorf("decl %q: empty description (sterile description rule, Finding #1 from FreeSims)", d.Operation)
		}
		ops[d.Operation]++
	}
	for op, n := range ops {
		if n > 1 {
			t.Errorf("duplicate declaration for op %q (%d copies)", op, n)
		}
	}

	// Spot-check a few we know must exist — these are the canonical verbs
	// from the catalog that d87-d-* children will wire handlers for first.
	required := []string{"walk-to", "speak", "interact-with", "instant-message", "buy-object"}
	for _, op := range required {
		if ops[op] == 0 {
			t.Errorf("required op %q missing from declarations", op)
		}
	}

	t.Logf("parsed %d declarations (verb-catalog.md)", len(decls))
}

// TestDeclarationsSerialize asserts each declaration round-trips through
// json.Marshal. This is what the campfire send path does — if it fails at
// runtime the declaration never gets published.
func TestDeclarationsSerialize(t *testing.T) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}
	for _, d := range decls {
		data, err := json.Marshal(d)
		if err != nil {
			t.Errorf("marshal %s: %v", d.Operation, err)
			continue
		}
		if len(data) == 0 {
			t.Errorf("marshal %s: empty", d.Operation)
		}
	}
}

// TestDeclarationsLintClean asserts every conventions/*.json passes cf's lint —
// the same gate that `cf convention adopt` runs before publishing. This is the
// regression lock for freesoexperiment-f47: prior to that fix, 37/109 decls
// failed lint on unknown arg-types (int64, uint64, float64, list[int64], …)
// and `cf convention adopt` rejected them, leaving ~third of the verb catalog
// uninvokable from cf CLI. The accepted vocabulary is fixed in
// cf-conventions/cf-convention/parser.go (knownArgTypes): string, integer,
// duration, boolean, key, campfire, message_id, json, tag_set, enum.
func TestDeclarationsLintClean(t *testing.T) {
	entries, err := conventionFiles.ReadDir("conventions")
	if err != nil {
		t.Fatalf("read embedded conventions dir: %v", err)
	}
	checked := 0
	for _, e := range entries {
		if e.IsDir() || filepath.Ext(e.Name()) != ".json" {
			continue
		}
		payload, err := conventionFiles.ReadFile("conventions/" + e.Name())
		if err != nil {
			t.Errorf("read %s: %v", e.Name(), err)
			continue
		}
		result := convention.Lint(payload)
		for _, f := range result.Errors {
			field := ""
			if f.Field != "" {
				field = " [" + f.Field + "]"
			}
			t.Errorf("%s: lint error%s: %s", e.Name(), field, f.Message)
		}
		checked++
	}
	if checked == 0 {
		t.Fatalf("no conventions/*.json files found")
	}
	t.Logf("lint-checked %d declarations", checked)
}

// TestDeclarationArgNameParity is the schema-parity gate introduced in
// automataisland-6dc. It prevents the class of silent-drop bug where a
// convention declaration advertises an arg name that the Go handler never
// reads — so callers pass the arg, the handler silently ignores it, and the
// op misbehaves without any error.
//
// This is the same class of bug as the TTAB silent-drop (automataisland-063):
// the `interact-with` declaration said target_sim_id; the handler read callee_id.
// The find-avatar bug (automataisland-6dc) was the same pattern.
//
// Algorithm:
//  1. Read all conventions/*.json declarations and collect {op → [arg_name, ...]}.
//  2. Read the corpus of non-test Go source files in this package. Build a map
//     of string literals (anything that appears as "..." in source).
//  3. For each op that has at least one required arg:
//     a. Check if the op name itself appears as a string literal in Go source
//        (i.e. there is a Go handler for this op). If not, skip — catalog-only
//        declarations have no Go handler and no arg reading to check.
//     b. For each required arg of an op with a Go handler, assert the arg name
//        appears as a string literal in the Go source corpus.
//
// NOTE: This is a static-source check, not a runtime check. It catches renames
// and typos at CI time, not at test execution time. An arg that appears ONLY in
// test files is NOT counted — the handler itself must reference the name.
func TestDeclarationArgNameParity(t *testing.T) {
	// Step 1: load declarations from embedded files.
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}

	// Step 2: read all non-test Go source files in this package.
	entries, err := os.ReadDir(".")
	if err != nil {
		t.Fatalf("ReadDir(.): %v", err)
	}
	// sourceCorpus contains all string literals extracted from production Go files.
	// A "string literal" here means content between double-quotes: we look for
	// "arg_name" as a quoted substring anywhere in the file (grep-style).
	// We do NOT include _test.go files — the handler must reference the arg, not just tests.
	sourceCorpus := strings.Builder{}
	for _, e := range entries {
		name := e.Name()
		if !strings.HasSuffix(name, ".go") {
			continue
		}
		if strings.HasSuffix(name, "_test.go") {
			continue
		}
		data, err := os.ReadFile(name)
		if err != nil {
			t.Errorf("read %s: %v", name, err)
			continue
		}
		sourceCorpus.Write(data)
		sourceCorpus.WriteByte('\n')
	}
	corpus := sourceCorpus.String()

	// Helper: is a quoted string literal present in the corpus?
	hasLiteral := func(s string) bool {
		return strings.Contains(corpus, fmt.Sprintf("%q", s))
	}

	// Step 3: check parity for each declaration.
	failures := 0
	for _, d := range decls {
		if len(d.Args) == 0 {
			continue // no args → nothing to check
		}
		// Determine if there is a Go handler for this op (op name appears as literal
		// in non-test source). Catalog-only ops have no handler.
		if !hasLiteral(d.Operation) {
			// No Go handler registered for this op — catalog-only. Skip arg check.
			t.Logf("skip %s: no Go handler found in source corpus (catalog-only op)", d.Operation)
			continue
		}
		// Check each required arg.
		for _, arg := range d.Args {
			if !arg.Required {
				continue // optional args may be conditionally read; skip
			}
			if !hasLiteral(arg.Name) {
				t.Errorf(
					"ARG-NAME PARITY FAILURE: op=%q declares required arg %q but %q does not appear as a string literal in any non-test Go source file — handler may never read it (silent-drop risk, same class as automataisland-063 TTAB bug)",
					d.Operation, arg.Name, arg.Name,
				)
				failures++
			}
		}
	}
	if failures == 0 {
		t.Logf("arg-name parity OK: all required args in ops with Go handlers appear in source")
	}
}

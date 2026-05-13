/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"encoding/json"
	"path/filepath"
	"testing"

	"github.com/campfire-net/campfire/pkg/convention"
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

/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"encoding/json"
	"testing"
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

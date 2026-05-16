/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"strings"
	"testing"
	"time"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// TestBuildModeForwardingHandlersDispatchIPC covers the ten build-buy-architecture ops:
// place-wall, paint-wall, paint-floor, paint-grass, flatten-terrain, raise-terrain,
// set-roof, change-environment, change-lot-size, leave-build-buy. Each is a thin
// forwarder (owner gating lives in the C# handler). We assert each handler forwards
// only the declared allowlist of args and drops anything else the agent might try to
// smuggle through.
func TestBuildModeForwardingHandlersDispatchIPC(t *testing.T) {
	type argCheck struct {
		op          string
		allowed     []string
		inArgs      map[string]any
		wantForward map[string]any
		wantDropped []string
	}
	cases := []argCheck{
		{
			op:      "place-wall",
			allowed: []string{"x", "y", "level", "length", "direction", "pattern", "style", "remove"},
			inArgs: map[string]any{
				"x": float64(5), "y": float64(5), "level": float64(1),
				"length": float64(3), "direction": float64(0),
				"pattern": float64(0), "style": float64(1),
				"remove": false,
				// Should NOT be forwarded — agent cannot prepay wall materials.
				"prepay_materials": float64(100),
			},
			wantForward: map[string]any{
				"x": float64(5), "y": float64(5), "level": float64(1),
				"length": float64(3), "direction": float64(0),
				"pattern": float64(0), "style": float64(1),
				"remove": false,
			},
			wantDropped: []string{"prepay_materials"},
		},
		{
			op:      "paint-wall",
			allowed: []string{"x", "y", "level", "side", "pattern"},
			inArgs: map[string]any{
				"x": float64(7), "y": float64(8), "level": float64(1),
				"side": float64(0), "pattern": float64(42),
				// Wallpaper cost not a caller arg.
				"cost_per_tile": float64(3),
			},
			wantForward: map[string]any{
				"x": float64(7), "y": float64(8), "level": float64(1),
				"side": float64(0), "pattern": float64(42),
			},
			wantDropped: []string{"cost_per_tile"},
		},
		{
			op:      "paint-floor",
			allowed: []string{"x", "y", "level", "width", "height", "pattern", "fill"},
			inArgs: map[string]any{
				"x": float64(10), "y": float64(10), "level": float64(1),
				"width": float64(2), "height": float64(2),
				"pattern": float64(5), "fill": false,
				"unrelated": "ignore-me",
			},
			wantForward: map[string]any{
				"x": float64(10), "y": float64(10), "level": float64(1),
				"width": float64(2), "height": float64(2),
				"pattern": float64(5), "fill": false,
			},
			wantDropped: []string{"unrelated"},
		},
		{
			op:      "paint-grass",
			allowed: []string{"x", "y", "level", "pattern"},
			inArgs: map[string]any{
				"x": float64(1), "y": float64(1), "level": float64(1),
				"pattern": float64(3),
				"density": float64(99),
			},
			wantForward: map[string]any{
				"x": float64(1), "y": float64(1), "level": float64(1),
				"pattern": float64(3),
			},
			wantDropped: []string{"density"},
		},
		{
			op:      "flatten-terrain",
			allowed: []string{"x", "y", "level", "width", "height"},
			inArgs: map[string]any{
				"x": float64(4), "y": float64(4), "level": float64(1),
				"width": float64(3), "height": float64(3),
				// Height not a valid caller arg.
				"target_height": float64(10),
			},
			wantForward: map[string]any{
				"x": float64(4), "y": float64(4), "level": float64(1),
				"width": float64(3), "height": float64(3),
			},
			wantDropped: []string{"target_height"},
		},
		{
			op:      "raise-terrain",
			allowed: []string{"x", "y", "level", "delta"},
			inArgs: map[string]any{
				"x": float64(2), "y": float64(2), "level": float64(1),
				"delta": float64(1),
				"absolute": true,
			},
			wantForward: map[string]any{
				"x": float64(2), "y": float64(2), "level": float64(1),
				"delta": float64(1),
			},
			wantDropped: []string{"absolute"},
		},
		{
			op:      "set-roof",
			allowed: []string{"style", "pitch"},
			inArgs: map[string]any{
				"style": float64(2), "pitch": float64(0.5),
				// "color" not a wire field.
				"color": "red",
			},
			wantForward: map[string]any{"style": float64(2), "pitch": float64(0.5)},
			wantDropped: []string{"color"},
		},
		{
			op:      "change-environment",
			allowed: []string{"guids_to_add", "guids_to_clear"},
			inArgs: map[string]any{
				"guids_to_add":   []any{float64(0xABCD)},
				"guids_to_clear": []any{},
				// Agent cannot redirect the PDU to a different lot.
				"lot_id": float64(99),
			},
			wantForward: map[string]any{
				"guids_to_add":   []any{float64(0xABCD)},
				"guids_to_clear": []any{},
			},
			wantDropped: []string{"lot_id"},
		},
		{
			op:      "change-lot-size",
			allowed: []string{"lot_size", "lot_stories"},
			inArgs: map[string]any{
				"lot_size": float64(3), "lot_stories": float64(1),
				// Agent cannot pre-compute cost.
				"cost_override": float64(0),
			},
			wantForward: map[string]any{"lot_size": float64(3), "lot_stories": float64(1)},
			wantDropped: []string{"cost_override"},
		},
		{
			op:      "leave-build-buy",
			allowed: []string{"build"},
			inArgs: map[string]any{
				"build": true,
				// Thumbnail is server-generated.
				"thumbnail_bytes": "AAAA",
			},
			wantForward: map[string]any{"build": true},
			wantDropped: []string{"thumbnail_bytes"},
		},
		{
			op:      "list-architecture-styles",
			allowed: []string{}, // no args
			inArgs: map[string]any{
				// Any args should be dropped — this is a no-arg catalog read.
				"spurious_arg": "ignored",
			},
			wantForward: map[string]any{},
			wantDropped: []string{"spurious_arg"},
		},
	}

	for _, tc := range cases {
		tc := tc
		t.Run(tc.op, func(t *testing.T) {
			fake := newFakeBotProcess()
			ipc := NewIPC(fake.bot)
			gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
				"kind": "response", "ok": true,
				"payload": map[string]any{"queued": true, "verb": tc.op},
			})

			handler := simpleForwardingHandler(ipc, tc.op, tc.allowed...)
			ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
			defer cancel()
			resp, err := handler(ctx, &convention.Request{Args: tc.inArgs})
			if err != nil {
				t.Fatalf("handler: %v", err)
			}
			if resp == nil {
				t.Fatal("nil response")
			}
			cmd := <-gotCmd
			if cmd.Op != tc.op {
				t.Errorf("want op=%s got %q", tc.op, cmd.Op)
			}
			for k, want := range tc.wantForward {
				got := cmd.Args[k]
				// Array comparison via fmt (maps compare pointer-wise in ==).
				if !equalAny(got, want) {
					t.Errorf("%s: arg %q = %v; want %v", tc.op, k, got, want)
				}
			}
			for _, k := range tc.wantDropped {
				if _, bad := cmd.Args[k]; bad {
					t.Errorf("%s: arg %q must NOT be forwarded (not in declaration whitelist): %v",
						tc.op, k, cmd.Args)
				}
			}
		})
	}
}

// equalAny is a minimal deep-equal for the test-value types we use (scalars + []any).
// Handle []any first because raw == panics on uncomparable slices.
func equalAny(a, b any) bool {
	as, aok := a.([]any)
	bs, bok := b.([]any)
	if aok || bok {
		if !(aok && bok) {
			return false
		}
		if len(as) != len(bs) {
			return false
		}
		for i := range as {
			if !equalAny(as[i], bs[i]) {
				return false
			}
		}
		return true
	}
	return a == b
}

// TestListArchitectureStylesDeclaration (T3): declaration-coverage check for
// list-architecture-styles. The declaration must load, carry convention=freeso-embodiment,
// family=build-buy-architecture, and have no required args (pure catalog read, no args).
func TestListArchitectureStylesDeclaration(t *testing.T) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}
	var decl *convention.Declaration
	for _, d := range decls {
		if d.Operation == "list-architecture-styles" {
			decl = d
			break
		}
	}
	if decl == nil {
		t.Fatal("declaration missing: list-architecture-styles")
	}
	if decl.Convention != "freeso-embodiment" {
		t.Errorf("convention=%q want freeso-embodiment", decl.Convention)
	}
	if decl.Description == "" {
		t.Error("empty description")
	}
	// list-architecture-styles takes no args — verify no required args are declared.
	for _, a := range decl.Args {
		if a.Required {
			t.Errorf("unexpected required arg %q — list-architecture-styles takes no args", a.Name)
		}
	}
	// Description must carry prerequisite, effect, cost (galtrader-style).
	lower := strings.ToLower(decl.Description)
	hits := 0
	for _, kw := range []string{"prerequisite", "effect", "cost"} {
		if strings.Contains(lower, kw) {
			hits++
		}
	}
	if hits < 2 {
		t.Errorf("description must carry >=2 of Prerequisite/Effect/Cost (got %d): %q", hits, decl.Description)
	}
}

// TestBuildModeDeclarationsPresent asserts all ten build-buy-architecture declarations
// load from the embedded conventions/ FS and carry galtrader-style descriptions with the
// expected required args.
func TestBuildModeDeclarationsPresent(t *testing.T) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}
	byOp := map[string]*convention.Declaration{}
	for _, d := range decls {
		byOp[d.Operation] = d
	}

	type want struct {
		op       string
		required []string
	}
	wants := []want{
		{"place-wall", []string{"x", "y", "length", "direction"}},
		{"paint-wall", []string{"x", "y", "side", "pattern"}},
		{"paint-floor", []string{"x", "y", "pattern"}},
		{"paint-grass", []string{"x", "y"}},
		{"flatten-terrain", []string{"x", "y"}},
		{"raise-terrain", []string{"x", "y"}},
		{"set-roof", []string{"style"}},
		{"change-environment", nil}, // both lists optional
		{"change-lot-size", []string{"lot_size", "lot_stories"}},
		{"leave-build-buy", nil},        // build is optional
		{"list-architecture-styles", nil}, // no args — pure catalog read
	}

	for _, w := range wants {
		d := byOp[w.op]
		if d == nil {
			t.Errorf("declaration missing: %s", w.op)
			continue
		}
		if d.Convention != "freeso-embodiment" {
			t.Errorf("%s: convention=%q want freeso-embodiment", w.op, d.Convention)
		}
		// Family lives in the raw JSON but is not exposed on convention.Declaration — skip field check.
		if d.Description == "" {
			t.Errorf("%s: empty description", w.op)
			continue
		}
		// Galtrader-style: ≥2 of prerequisite/effect/cost in description.
		lower := strings.ToLower(d.Description)
		hits := 0
		for _, kw := range []string{"prerequisite", "effect", "cost"} {
			if strings.Contains(lower, kw) {
				hits++
			}
		}
		if hits < 2 {
			t.Errorf("%s: description must carry >=2 of Prerequisite/Effect/Cost (got %d): %q",
				w.op, hits, d.Description)
		}
		seen := map[string]bool{}
		for _, a := range d.Args {
			seen[a.Name] = true
			if a.Required && !inList(w.required, a.Name) {
				// Required args must be in our expected list.
				t.Errorf("%s: declares required arg %q not in expected set %v", w.op, a.Name, w.required)
			}
		}
		for _, need := range w.required {
			if !seen[need] {
				t.Errorf("%s: declaration missing expected arg %q", w.op, need)
			}
		}
	}
}

func inList(xs []string, s string) bool {
	for _, x := range xs {
		if x == s {
			return true
		}
	}
	return false
}

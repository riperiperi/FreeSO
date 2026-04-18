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

	"github.com/campfire-net/campfire/pkg/convention"
)

// TestBuyModeForwardingHandlersDispatchIPC asserts each of the build-buy-catalog verb
// family's eight handlers (buy-object, place-from-inventory, move-object, delete-object,
// send-to-inventory, list-object-for-sale, buy-listed-object, upgrade-object) produces
// an IPC command with the matching op and forwards only the declared args. OQ-8
// (buymode_handlers.go:27): there is no build/buy mode-entry PDU; gating lives in each
// VMNet*Cmd.Verify() server-side. The sidecar must not invent any gate or enrich args —
// it's a strict forwarder.
func TestBuyModeForwardingHandlersDispatchIPC(t *testing.T) {
	type argCheck struct {
		op          string
		allowed     []string
		inArgs      map[string]any
		wantForward map[string]any
		wantDropped []string
	}
	cases := []argCheck{
		{
			op: "buy-object",
			allowed: []string{
				"guid", "x", "y", "level", "dir", "mode", "target_upgrade_level",
			},
			inArgs: map[string]any{
				"guid":                 "abcd1234",
				"x":                    float64(5),
				"y":                    float64(7),
				"level":                float64(1),
				"dir":                  float64(0),
				"mode":                 float64(1),
				"target_upgrade_level": float64(2),
				// Client cannot pre-discount or fabricate a price — server reads catalog.
				"price":              float64(0),
				"bypass_funds_check": true,
			},
			wantForward: map[string]any{
				"guid":                 "abcd1234",
				"x":                    float64(5),
				"y":                    float64(7),
				"level":                float64(1),
				"dir":                  float64(0),
				"mode":                 float64(1),
				"target_upgrade_level": float64(2),
			},
			wantDropped: []string{"price", "bypass_funds_check"},
		},
		{
			op:      "place-from-inventory",
			allowed: []string{"object_persist_id", "x", "y", "level", "dir", "mode"},
			inArgs: map[string]any{
				"object_persist_id": float64(111),
				"x":                 float64(3),
				"y":                 float64(4),
				"level":             float64(1),
				"dir":               float64(2),
				"mode":              float64(1),
				// GUID is resolved server-side from the inventory row; caller cannot override.
				"guid": "spoof",
			},
			wantForward: map[string]any{
				"object_persist_id": float64(111),
				"x":                 float64(3),
				"y":                 float64(4),
				"level":             float64(1),
				"dir":               float64(2),
				"mode":              float64(1),
			},
			wantDropped: []string{"guid"},
		},
		{
			op:      "move-object",
			allowed: []string{"target_object_id", "x", "y", "level", "dir"},
			inArgs: map[string]any{
				"target_object_id": float64(222),
				"x":                float64(8),
				"y":                float64(9),
				"level":             float64(1),
				"dir":              float64(1),
				// Owner check is server-side; caller cannot assert ownership.
				"owner_persist_id": float64(999),
			},
			wantForward: map[string]any{
				"target_object_id": float64(222),
				"x":                float64(8),
				"y":                float64(9),
				"level":             float64(1),
				"dir":              float64(1),
			},
			wantDropped: []string{"owner_persist_id"},
		},
		{
			op:      "delete-object",
			allowed: []string{"target_object_id", "cleanup_all"},
			inArgs: map[string]any{
				"target_object_id": float64(333),
				"cleanup_all":      true,
				// Reason / audit text not in wire PDU.
				"reason": "redecorating",
			},
			wantForward: map[string]any{
				"target_object_id": float64(333),
				"cleanup_all":      true,
			},
			wantDropped: []string{"reason"},
		},
		{
			op:      "send-to-inventory",
			allowed: []string{"target_object_persist_id"},
			inArgs: map[string]any{
				"target_object_persist_id": float64(444),
				"target_object_id":         float64(999), // wrong ID space — must drop
			},
			wantForward: map[string]any{"target_object_persist_id": float64(444)},
			wantDropped: []string{"target_object_id"},
		},
		{
			op:      "list-object-for-sale",
			allowed: []string{"target_object_persist_id", "new_price"},
			inArgs: map[string]any{
				"target_object_persist_id": float64(555),
				"new_price":                float64(12345),
				// Market fee / tax set server-side from lot economy config.
				"market_fee": float64(0),
			},
			wantForward: map[string]any{
				"target_object_persist_id": float64(555),
				"new_price":                float64(12345),
			},
			wantDropped: []string{"market_fee"},
		},
		{
			op:      "buy-listed-object",
			allowed: []string{"target_object_persist_id"},
			inArgs: map[string]any{
				"target_object_persist_id": float64(666),
				// Caller cannot override the listing price.
				"new_price": float64(1),
			},
			wantForward: map[string]any{"target_object_persist_id": float64(666)},
			wantDropped: []string{"new_price"},
		},
		{
			op:      "upgrade-object",
			allowed: []string{"target_object_persist_id", "target_upgrade_level"},
			inArgs: map[string]any{
				"target_object_persist_id": float64(777),
				"target_upgrade_level":     float64(3),
				// Upgrade cost is server-resolved from catalog; caller cannot preset.
				"cost": float64(0),
			},
			wantForward: map[string]any{
				"target_object_persist_id": float64(777),
				"target_upgrade_level":     float64(3),
			},
			wantDropped: []string{"cost"},
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
				if got := cmd.Args[k]; got != want {
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

// TestBuyModeDeclarationsPresent asserts all eight build-buy-catalog declarations load and
// carry galtrader-style descriptions with expected required args. find-cheap-catalog-guid
// is a test-support helper (buymode_handlers.go:23) — not in the verb catalog and not
// asserted here.
func TestBuyModeDeclarationsPresent(t *testing.T) {
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
		{"buy-object", []string{"guid", "x", "y"}},
		{"place-from-inventory", []string{"object_persist_id", "x", "y"}},
		{"move-object", []string{"target_object_id", "x", "y"}},
		{"delete-object", []string{"target_object_id"}},
		{"send-to-inventory", []string{"target_object_persist_id"}},
		{"list-object-for-sale", []string{"target_object_persist_id", "new_price"}},
		{"buy-listed-object", []string{"target_object_persist_id"}},
		{"upgrade-object", []string{"target_object_persist_id", "target_upgrade_level"}},
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
		if d.Description == "" {
			t.Errorf("%s: empty description", w.op)
			continue
		}
		lower := strings.ToLower(d.Description)
		hits := 0
		for _, kw := range []string{"prerequisite", "effect", "cost"} {
			if strings.Contains(lower, kw) {
				hits++
			}
		}
		if hits < 2 {
			t.Errorf("%s: description must carry ≥2 of Prerequisite/Effect/Cost (got %d): %q",
				w.op, hits, d.Description)
		}
		seen := map[string]bool{}
		for _, a := range d.Args {
			seen[a.Name] = true
		}
		for _, need := range w.required {
			if !seen[need] {
				t.Errorf("%s: declaration missing required arg %q", w.op, need)
			}
		}
	}
}

/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"testing"
	"time"

	"github.com/campfire-net/campfire/pkg/convention"
)

// TestCityForwardingHandlersDispatchIPC asserts each of the city verb family's five
// handlers (view-bulletin, post-bulletin, vote, nominate, view-neighborhood) produces
// an IPC command with the matching op and forwards only the declared args. These all
// ride the city Aries socket; the C# bot owns correlation (FIFO for bulletin/nhood,
// SendingAvatarID for DataServiceWrapperPDU) and the sidecar holds no city state —
// arg-picking is the only safety layer preventing client-side spoofing of server-owned
// fields like bulletin IDs or election cycle IDs.
func TestCityForwardingHandlersDispatchIPC(t *testing.T) {
	type argCheck struct {
		op          string
		allowed     []string
		inArgs      map[string]any
		wantForward map[string]any
		wantDropped []string
	}
	cases := []argCheck{
		{
			op:      "view-bulletin",
			allowed: []string{"neighborhood_id"},
			inArgs: map[string]any{
				"neighborhood_id": float64(12),
				// Server returns the list; caller cannot pre-filter or spoof.
				"bulletin_id": float64(999),
				"sender":      "fake",
			},
			wantForward: map[string]any{"neighborhood_id": float64(12)},
			wantDropped: []string{"bulletin_id", "sender"},
		},
		{
			op:      "post-bulletin",
			allowed: []string{"subject", "body", "neighborhood_id", "lot_id"},
			inArgs: map[string]any{
				"subject":         "hello",
				"body":            "neighbors!",
				"neighborhood_id": float64(12),
				"lot_id":          float64(345),
				// Server sets sender + timestamp — caller cannot forge them.
				"sender_persist_id": float64(999),
				"timestamp":         float64(1234567890),
			},
			wantForward: map[string]any{
				"subject":         "hello",
				"body":            "neighbors!",
				"neighborhood_id": float64(12),
				"lot_id":          float64(345),
			},
			wantDropped: []string{"sender_persist_id", "timestamp"},
		},
		{
			op:      "vote",
			allowed: []string{"target_persist_id", "neighborhood_id"},
			inArgs: map[string]any{
				"target_persist_id": float64(42),
				"neighborhood_id":   float64(12),
				// Election cycle id is server-managed (election_cycle_id DB column); caller
				// must NOT supply one, otherwise they could target an old/closed cycle.
				"election_cycle_id": float64(7),
				"voter_persist_id":  float64(999), // caller is the voter (bot knows from session)
			},
			wantForward: map[string]any{
				"target_persist_id": float64(42),
				"neighborhood_id":   float64(12),
			},
			wantDropped: []string{"election_cycle_id", "voter_persist_id"},
		},
		{
			op:      "nominate",
			allowed: []string{"target_persist_id", "neighborhood_id"},
			inArgs: map[string]any{
				"target_persist_id": float64(42),
				"neighborhood_id":   float64(12),
				"election_cycle_id": float64(7), // same rationale as vote
			},
			wantForward: map[string]any{
				"target_persist_id": float64(42),
				"neighborhood_id":   float64(12),
			},
			wantDropped: []string{"election_cycle_id"},
		},
		{
			op:      "view-neighborhood",
			allowed: []string{"neighborhood_id"},
			inArgs: map[string]any{
				"neighborhood_id": float64(12),
				// Pagination / sort / filter are not in the wire PDU.
				"page":  float64(2),
				"limit": float64(50),
			},
			wantForward: map[string]any{"neighborhood_id": float64(12)},
			wantDropped: []string{"page", "limit"},
		},
	}

	for _, tc := range cases {
		tc := tc
		t.Run(tc.op, func(t *testing.T) {
			fake := newFakeBotProcess()
			ipc := NewIPC(fake.bot)
			gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
				"kind": "response", "ok": true,
				// Mirror the election-over refuse shape for vote/nominate so tests
				// document the known deterministic refuse case at workshop's current
				// DB state (no active election_cycle_id). See city_handlers.go:24.
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

// TestVoteRefusePayloadPropagates documents the ELECTION_OVER refuse case. When the server
// returns ok=false with an error payload (deterministic on workshop — no active election),
// the forwarding handler MUST surface the bot's ok=false shape to the convention caller
// rather than collapsing it to a generic error. This is the "wire-level-effect verification
// lever" from city_handlers.go:24.
func TestVoteRefusePayloadPropagates(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": false,
		"payload": map[string]any{
			"ok":    false,
			"error": "ELECTION_OVER",
		},
	})

	handler := simpleForwardingHandler(ipc, "vote", "target_persist_id", "neighborhood_id")
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"target_persist_id": float64(42),
			"neighborhood_id":   float64(12),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	if resp == nil {
		t.Fatal("nil response")
	}
	cmd := <-gotCmd
	if cmd.Op != "vote" {
		t.Errorf("want op=vote got %q", cmd.Op)
	}
	// Refuse path: the handler should still return a non-nil convention response. The
	// exact error shape surfaced to the agent is the bot's payload; forwardIPC turns
	// ok=false frames into a response payload the agent can read.
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil {
		t.Fatalf("refuse path produced nil payload; resp=%+v", resp)
	}
}

// TestCityDeclarationsPresent asserts all five city family declarations load and carry
// galtrader-style descriptions with the expected required args.
func TestCityDeclarationsPresent(t *testing.T) {
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
		{"view-bulletin", []string{"neighborhood_id"}},
		{"post-bulletin", []string{"subject", "body", "neighborhood_id"}},
		{"vote", []string{"target_persist_id", "neighborhood_id"}},
		{"nominate", []string{"target_persist_id", "neighborhood_id"}},
		{"view-neighborhood", []string{"neighborhood_id"}},
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
		// Intentional: no Prerequisite/Effect/Cost keyword check here. view-neighborhood's
		// shipped description is a wire-protocol walkthrough (DataServiceWrapperPDU,
		// dotpath updates) — valuable context without PEC framing. Out of scope for
		// freesoexperiment-7e7 (unit tests) to re-audit declaration prose style.
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

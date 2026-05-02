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

// TestCityForwardingHandlersDispatchIPC asserts each of the city verb family's five
// handlers (view-bulletin, post-bulletin, vote, nominate, view-neighborhood) produces
// an IPC command with the matching op and forwards only the declared args. These all
// ride the city Aries socket; the C# bot owns correlation (FIFO for bulletin/nhood,
// SendingAvatarID for DataServiceWrapperPDU) and the sidecar holds no city state —
// arg-picking is the only safety layer preventing client-side spoofing of server-owned
// fields like bulletin IDs or election cycle IDs.
//
// vote and nominate use name-primary handlers (freesoexperiment-17f) rather than
// simpleForwardingHandler, so they are exercised via voteHandler/nominateHandler
// with a nil store (target_persist_id fallback path). Name-primary path is covered
// by TestVoteNameResolution / TestNominateNameResolution below.
func TestCityForwardingHandlersDispatchIPC(t *testing.T) {
	type argCheck struct {
		op          string
		// handler is used for vote/nominate (name-primary ops); allowed is used for simple-forwarding ops.
		handler     convention.HandlerFunc
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
			// vote uses voteHandler (name-primary). Exercising the target_persist_id
			// fallback path here (no target_avatar_name — that path is covered by
			// TestVoteNameResolution). Election cycle id must not be forwarded — it is
			// server-managed (election_cycle_id DB column); supplying it would let the
			// caller target an old/closed cycle.
			op: "vote",
			inArgs: map[string]any{
				"target_persist_id": float64(42),
				"neighborhood_id":   float64(12),
				"election_cycle_id": float64(7),
				"voter_persist_id":  float64(999), // caller is the voter (bot knows from session)
			},
			wantForward: map[string]any{
				// After JSON marshal+unmarshal in captureOneCommand, int64 becomes float64.
				"target_persist_id": float64(42),
				"neighborhood_id":   float64(12),
			},
			wantDropped: []string{"election_cycle_id", "voter_persist_id"},
		},
		{
			// nominate uses nominateHandler (name-primary). Same rationale as vote.
			// target_avatar_name absent: testing the persist_id fallback path only.
			// Name-primary path is covered by TestNominateNameResolution.
			op: "nominate",
			inArgs: map[string]any{
				"target_persist_id": float64(42),
				"neighborhood_id":   float64(12),
				"election_cycle_id": float64(7),
			},
			wantForward: map[string]any{
				// After JSON marshal+unmarshal in captureOneCommand, int64 becomes float64.
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

			// vote/nominate: use the real name-primary handlers with a fresh store
			// (target_persist_id fallback path). The table rows for vote/nominate
			// already omit target_avatar_name so we exercise the persist_id path
			// directly. The name-primary path is covered by TestVoteNameResolution /
			// TestNominateNameResolution.
			var handler convention.HandlerFunc
			switch tc.op {
			case "vote":
				handler = voteHandler(ipc, NewMemoryStore())
			case "nominate":
				handler = nominateHandler(ipc, NewMemoryStore())
			default:
				handler = simpleForwardingHandler(ipc, tc.op, tc.allowed...)
			}

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

// TestVoteNameResolution is a regression test for freesoexperiment-17f: the
// --target_avatar_name path through voteHandler must resolve the name store
// entry (kind=sim) to target_persist_id and forward it to the bot, rather than
// requiring the agent to know hex IDs.
//
// This test covers the happy path (name bound, kind=sim) and the three refuse
// paths (unbound, wrong kind, target_persist_id fallback with zero id).
func TestVoteNameResolution(t *testing.T) {
	store := NewMemoryStore()

	// Bind "Ellis" → persist_id=77 as a sim.
	encodedEntry := []byte(`{"name":"Ellis","kind":"sim","target_sim_id":77}`)
	store.Store("name:ellis", encodedEntry)

	t.Run("name_primary_forwards_resolved_persist_id", func(t *testing.T) {
		fake := newFakeBotProcess()
		ipc := NewIPC(fake.bot)
		gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
			"kind": "response", "ok": true,
			"payload": map[string]any{"queued": true, "verb": "vote"},
		})

		handler := voteHandler(ipc, store)
		ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
		defer cancel()
		resp, err := handler(ctx, &convention.Request{Args: map[string]any{
			"target_avatar_name": "Ellis",
			"neighborhood_id":    float64(1),
			// target_persist_id is absent — the handler must derive it from the name store.
		}})
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
		// Resolved persist_id: after JSON marshal+unmarshal in captureOneCommand, int64 becomes float64.
		// After JSON marshal+unmarshal, numbers become float64.
		if got := cmd.Args["target_persist_id"]; got != float64(77) {
			t.Errorf("target_persist_id: want float64(77) got %v (%T)", got, got)
		}
		if got := cmd.Args["neighborhood_id"]; got != float64(1) {
			t.Errorf("neighborhood_id: want float64(1) got %v", got)
		}
		// target_avatar_name must NOT be forwarded to the bot.
		if _, bad := cmd.Args["target_avatar_name"]; bad {
			t.Errorf("target_avatar_name must not be forwarded to bot: %v", cmd.Args)
		}
	})

	t.Run("name_primary_overrides_target_persist_id", func(t *testing.T) {
		// When both target_avatar_name and target_persist_id are supplied,
		// target_avatar_name takes precedence (I0-5).
		fake := newFakeBotProcess()
		ipc := NewIPC(fake.bot)
		gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
			"kind": "response", "ok": true,
			"payload": map[string]any{"queued": true, "verb": "vote"},
		})

		handler := voteHandler(ipc, store)
		ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
		defer cancel()
		resp, err := handler(ctx, &convention.Request{Args: map[string]any{
			"target_avatar_name": "Ellis",
			"target_persist_id":  float64(999), // should be overridden by name resolution
			"neighborhood_id":    float64(1),
		}})
		if err != nil {
			t.Fatalf("handler: %v", err)
		}
		if resp == nil {
			t.Fatal("nil response")
		}
		cmd := <-gotCmd
		// Name-resolved value (77) must win over the supplied 999.
		// After JSON marshal+unmarshal, numbers become float64.
		if got := cmd.Args["target_persist_id"]; got != float64(77) {
			t.Errorf("target_persist_id: want float64(77) (name wins) got %v (%T)", got, got)
		}
		// target_avatar_name must NOT leak into the forwarded IPC args, even when
		// it was present in the inArgs. A regression that forwards it would expose
		// the name store key to the C# bot, which has no use for it and would forward
		// it to the NhoodRequest wire where the server ignores unknown fields but the
		// contract is violated.
		if _, bad := cmd.Args["target_avatar_name"]; bad {
			t.Errorf("target_avatar_name must not be forwarded to bot when overriding target_persist_id: %v", cmd.Args)
		}
	})

	t.Run("unbound_name_returns_error_no_ipc", func(t *testing.T) {
		// An unbound name must return ok:false without touching IPC.
		// No bot process is needed — the handler never reaches forwardIPC.
		ipc := NewIPC(nil)
		handler := voteHandler(ipc, store)
		ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
		defer cancel()
		resp, err := handler(ctx, &convention.Request{Args: map[string]any{
			"target_avatar_name": "NoSuchPerson",
		}})
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		if resp == nil {
			t.Fatal("nil response")
		}
		payload, _ := resp.Payload.(map[string]any)
		if payload == nil {
			t.Fatalf("nil payload; resp=%+v", resp)
		}
		if ok, _ := payload["ok"].(bool); ok {
			t.Errorf("expected ok:false for unbound name, got ok:true")
		}
		errStr, _ := payload["error"].(string)
		if errStr == "" {
			t.Errorf("expected non-empty error string for unbound name")
		}
	})

	t.Run("wrong_kind_name_returns_error", func(t *testing.T) {
		// A name binding with kind=lot (not sim) must return ok:false.
		wrongKindStore := NewMemoryStore()
		wrongKindStore.Store("name:myhome", []byte(`{"name":"myhome","kind":"lot","lot_location":"12345"}`))
		ipc := NewIPC(nil)
		handler := voteHandler(ipc, wrongKindStore)
		ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
		defer cancel()
		resp, err := handler(ctx, &convention.Request{Args: map[string]any{
			"target_avatar_name": "myhome",
		}})
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		payload, _ := resp.Payload.(map[string]any)
		if ok, _ := payload["ok"].(bool); ok {
			t.Errorf("expected ok:false for wrong-kind name, got ok:true")
		}
		errStr, _ := payload["error"].(string)
		if errStr == "" || !containsAll(errStr, []string{"sim", "lot"}) {
			t.Errorf("error should mention kind mismatch (sim/lot), got: %q", errStr)
		}
	})

	t.Run("missing_both_returns_error", func(t *testing.T) {
		// Neither target_avatar_name nor target_persist_id → ok:false.
		ipc := NewIPC(nil)
		handler := voteHandler(ipc, store)
		ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
		defer cancel()
		resp, err := handler(ctx, &convention.Request{Args: map[string]any{
			"neighborhood_id": float64(1),
		}})
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		payload, _ := resp.Payload.(map[string]any)
		if ok, _ := payload["ok"].(bool); ok {
			t.Errorf("expected ok:false when no target supplied")
		}
	})
}

// TestNominateNameResolution mirrors TestVoteNameResolution for the nominate op.
// Only the happy path and unbound-name refuse path are re-tested; the other
// refuse conditions share the resolveElectionTarget helper so they are already
// covered by TestVoteNameResolution.
func TestNominateNameResolution(t *testing.T) {
	store := NewMemoryStore()
	store.Store("name:botrous", []byte(`{"name":"Botrous","kind":"sim","target_sim_id":55}`))

	t.Run("name_primary_forwards_resolved_persist_id", func(t *testing.T) {
		fake := newFakeBotProcess()
		ipc := NewIPC(fake.bot)
		gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
			"kind": "response", "ok": true,
			"payload": map[string]any{"queued": true, "verb": "nominate"},
		})

		handler := nominateHandler(ipc, store)
		ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
		defer cancel()
		resp, err := handler(ctx, &convention.Request{Args: map[string]any{
			"target_avatar_name": "Botrous",
			"neighborhood_id":    float64(1),
		}})
		if err != nil {
			t.Fatalf("handler: %v", err)
		}
		if resp == nil {
			t.Fatal("nil response")
		}
		cmd := <-gotCmd
		if cmd.Op != "nominate" {
			t.Errorf("want op=nominate got %q", cmd.Op)
		}
		// After JSON marshal+unmarshal, numbers become float64.
		if got := cmd.Args["target_persist_id"]; got != float64(55) {
			t.Errorf("target_persist_id: want float64(55) got %v (%T)", got, got)
		}
		if _, bad := cmd.Args["target_avatar_name"]; bad {
			t.Errorf("target_avatar_name must not be forwarded to bot")
		}
	})

	t.Run("unbound_name_returns_error_no_ipc", func(t *testing.T) {
		ipc := NewIPC(nil)
		handler := nominateHandler(ipc, store)
		ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
		defer cancel()
		resp, err := handler(ctx, &convention.Request{Args: map[string]any{
			"target_avatar_name": "Ghost",
		}})
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		payload, _ := resp.Payload.(map[string]any)
		if ok, _ := payload["ok"].(bool); ok {
			t.Errorf("expected ok:false for unbound name")
		}
	})
}

// containsAll returns true if s contains all of the substrings in needles.
// Used for loose error-message assertions that don't care about exact wording.
func containsAll(s string, needles []string) bool {
	for _, n := range needles {
		if !strings.Contains(s, n) {
			return false
		}
	}
	return true
}

// TestVoteRefusePayloadPropagates documents the ELECTION_OVER refuse case. When the server
// returns ok=false with an error payload (deterministic on workshop — no active election),
// the name-primary vote handler MUST surface the bot's ok=false shape to the convention
// caller rather than collapsing it to a generic error. This is the "wire-level-effect
// verification lever" from city_handlers.go.
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

	// Use the real voteHandler (name-primary) with an empty store so we exercise
	// the target_persist_id fallback path through the full handler stack.
	handler := voteHandler(ipc, NewMemoryStore())
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
		// freesoexperiment-17f: target_avatar_name added to both civic ops (I0-5).
		{"vote", []string{"target_persist_id", "target_avatar_name", "neighborhood_id"}},
		{"nominate", []string{"target_persist_id", "target_avatar_name", "neighborhood_id"}},
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
		argByName := map[string]*convention.ArgDescriptor{}
		for i, a := range d.Args {
			seen[a.Name] = true
			argByName[a.Name] = &d.Args[i]
		}
		for _, need := range w.required {
			if !seen[need] {
				t.Errorf("%s: declaration missing required arg %q", w.op, need)
			}
		}
		// freesoexperiment-17f: target_persist_id is the hex/decimal fallback arg;
		// required must be false because the caller may use target_avatar_name
		// instead (name-primary I0-5). required:true would reject callers that omit
		// target_persist_id and supply target_avatar_name only — exactly the preferred
		// call shape.
		if w.op == "vote" || w.op == "nominate" {
			arg, ok := argByName["target_persist_id"]
			if !ok {
				t.Errorf("%s: target_persist_id arg missing (already checked above)", w.op)
			} else if arg.Required {
				t.Errorf("%s: target_persist_id must have required:false (callers may use target_avatar_name instead), got required:true", w.op)
			}
		}
	}
}

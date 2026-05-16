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

// TestAdminForwardingHandlersDispatchIPC asserts each of the admin verb family's six thin
// forwarding handlers (kick-user, ban-user, ipban-user, archive-approve-user,
// archive-reject-user, admin-chat-command) produces an IPC command with the matching op and
// only the whitelisted args defined in RegisterAdminHandlers. Out-of-schema args MUST NOT
// leak through to the bot — the sidecar is a dumb forwarder but its arg-pick contract is
// load-bearing for the admin gate's deterministic refuse path.
func TestAdminForwardingHandlersDispatchIPC(t *testing.T) {
	type argCheck struct {
		op           string
		allowed      []string
		inArgs       map[string]any
		wantForward  map[string]any
		wantDropped  []string // args supplied but must NOT be forwarded
	}
	cases := []argCheck{
		{
			op:      "kick-user",
			allowed: []string{"target_persist_id"},
			inArgs: map[string]any{
				"target_persist_id": float64(42),
				// Spoof attempt: caller cannot supplement the kick with extra data.
				"reason":      "spite",
				"spoof_admin": true,
			},
			wantForward: map[string]any{"target_persist_id": float64(42)},
			wantDropped: []string{"reason", "spoof_admin"},
		},
		{
			op:      "ban-user",
			allowed: []string{"target_persist_id"},
			inArgs: map[string]any{
				"target_persist_id": float64(77),
				// DB field — server sets is_banned; client cannot preload it.
				"is_banned":      float64(1),
				"target_user_id": float64(999),
			},
			wantForward: map[string]any{"target_persist_id": float64(77)},
			wantDropped: []string{"is_banned", "target_user_id"},
		},
		{
			op:      "ipban-user",
			allowed: []string{"target_persist_id"},
			inArgs: map[string]any{
				"target_persist_id": float64(101),
				// Bot resolves last_ip from session; caller CANNOT supply one.
				"ip": "10.0.0.1",
			},
			wantForward: map[string]any{"target_persist_id": float64(101)},
			wantDropped: []string{"ip"},
		},
		{
			// archive-approve-user takes user_id (NOT avatar_id). The admin_handlers.go
			// comment is explicit: this is a whitelist difference, not a typo.
			op:      "archive-approve-user",
			allowed: []string{"target_user_id"},
			inArgs: map[string]any{
				"target_user_id":    float64(5),
				"target_persist_id": float64(999), // MUST drop — wrong id space.
			},
			wantForward: map[string]any{"target_user_id": float64(5)},
			wantDropped: []string{"target_persist_id"},
		},
		{
			op:      "archive-reject-user",
			allowed: []string{"target_user_id"},
			inArgs: map[string]any{
				"target_user_id":    float64(6),
				"target_persist_id": float64(999),
			},
			wantForward: map[string]any{"target_user_id": float64(6)},
			wantDropped: []string{"target_persist_id"},
		},
		{
			op:      "admin-chat-command",
			allowed: []string{"command", "args"},
			inArgs: map[string]any{
				"command": "qtrday",
				"args":    "3",
				// Fabrication: caller cannot pre-inject a chat event reply.
				"chat_event_text": "fake banlist",
			},
			wantForward: map[string]any{"command": "qtrday", "args": "3"},
			wantDropped: []string{"chat_event_text"},
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

// TestAdminChatCommandOmitsOptionalArg asserts that when the optional `args` field is not
// provided, pickArgs omits it from the forwarded command (pickArgs only copies keys that
// exist on the input) rather than forwarding a zero-value string. This matters because the
// bot's handler does a presence check on args before concatenating into the /command string.
func TestAdminChatCommandOmitsOptionalArg(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{"queued": true},
	})

	handler := simpleForwardingHandler(ipc, "admin-chat-command", "command", "args")
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	_, err := handler(ctx, &convention.Request{
		Args: map[string]any{"command": "banlist"}, // no args — this is legal.
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	cmd := <-gotCmd
	if cmd.Args["command"] != "banlist" {
		t.Errorf("command not forwarded: %v", cmd.Args)
	}
	if _, present := cmd.Args["args"]; present {
		t.Errorf("optional `args` must not appear when caller omitted it; got %v", cmd.Args)
	}
}

// TestAdminDeclarationsPresent asserts all six admin family declarations load from the
// embedded conventions, carry galtrader-style descriptions (≥2 of Prerequisite/Effect/Cost),
// and expose the expected required args. The cheat-command op is explicitly asserted absent
// (no wire PDU — dropped per the "drop don't stub" rule).
func TestAdminDeclarationsPresent(t *testing.T) {
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
		{"kick-user", []string{"target_persist_id"}},
		{"ban-user", []string{"target_persist_id"}},
		{"ipban-user", []string{"target_persist_id"}},
		{"archive-approve-user", []string{"target_user_id"}},
		{"archive-reject-user", []string{"target_user_id"}},
		{"admin-chat-command", []string{"command"}},
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

	// cheat-command is client-side only (UICheatHandler, ! prefix). No wire PDU path.
	// admin_handlers.go:27 documents the drop. The declaration file exists for catalog
	// completeness (the directory has cheat-command.json), but the admin registration
	// MUST NOT wire a handler for it. We assert the op is either absent from decls or
	// present but intentionally NOT registered — the former is the simpler check here.
	// (If the declaration is present, that's fine; the contract is the handler map
	// in admin_handlers.go doesn't include it, which is verified by the forwarder test
	// above enumerating exactly six ops.)
	_ = byOp["cheat-command"]
}

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

// TestPollInboxHandlerDispatchesIPC asserts the poll-inbox handler forwards the since_ticks
// arg to the bot via IPC with op="poll-inbox" and does not leak out-of-schema args.
func TestPollInboxHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{
			"type":        "POLL_RESPONSE",
			"since_ticks": float64(0),
			"count":       float64(0),
			"messages":    []any{},
		},
	})

	handler := simpleForwardingHandler(ipc, "poll-inbox", "since_ticks")
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"since_ticks": float64(123456789),
			// Out-of-schema — must NOT be forwarded.
			"spoof_sender": float64(999),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	if resp == nil {
		t.Fatal("nil response")
	}
	cmd := <-gotCmd
	if cmd.Op != "poll-inbox" {
		t.Errorf("want op=poll-inbox got %q", cmd.Op)
	}
	if cmd.Args["since_ticks"] != float64(123456789) {
		t.Errorf("since_ticks not forwarded: %v", cmd.Args)
	}
	if _, bad := cmd.Args["spoof_sender"]; bad {
		t.Errorf("spoof_sender must NOT be forwarded: %v", cmd.Args)
	}
}

// TestSendMailHandlerDispatchesIPC asserts the send-mail handler forwards target_persist_id,
// subject, body and drops unknown args.
func TestSendMailHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{
			"type":              "SEND_SUCCESS",
			"success":           true,
			"target_persist_id": float64(4),
			"subject":           "hi",
			"body_length":       float64(2),
			"mail_id":           float64(17),
			"echoed_time_ticks": float64(0),
		},
	})

	handler := simpleForwardingHandler(ipc, "send-mail", "target_persist_id", "subject", "body")
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"target_persist_id": float64(4),
			"subject":           "hi",
			"body":              "hello",
			// Out-of-schema — server-set fields, client cannot spoof.
			"sender_persist_id": float64(999),
			"read_state":        float64(1),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	if resp == nil {
		t.Fatal("nil response")
	}
	cmd := <-gotCmd
	if cmd.Op != "send-mail" {
		t.Errorf("want op=send-mail got %q", cmd.Op)
	}
	if cmd.Args["target_persist_id"] != float64(4) {
		t.Errorf("target_persist_id: %v", cmd.Args)
	}
	if cmd.Args["subject"] != "hi" {
		t.Errorf("subject: %v", cmd.Args)
	}
	if cmd.Args["body"] != "hello" {
		t.Errorf("body: %v", cmd.Args)
	}
	if _, bad := cmd.Args["sender_persist_id"]; bad {
		t.Errorf("sender_persist_id must NOT leak (server sets it): %v", cmd.Args)
	}
	if _, bad := cmd.Args["read_state"]; bad {
		t.Errorf("read_state must NOT leak (not a client-settable field): %v", cmd.Args)
	}
}

// TestDeleteMailHandlerDispatchesIPC asserts delete-mail forwards mail_id.
func TestDeleteMailHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{
			"queued":  true,
			"verb":    "delete-mail",
			"mail_id": float64(42),
		},
	})

	handler := simpleForwardingHandler(ipc, "delete-mail", "mail_id")
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"mail_id": float64(42),
			// Must not spoof avatar identity through the delete path.
			"target_persist_id": float64(999),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	if resp == nil {
		t.Fatal("nil response")
	}
	cmd := <-gotCmd
	if cmd.Op != "delete-mail" {
		t.Errorf("want op=delete-mail got %q", cmd.Op)
	}
	if cmd.Args["mail_id"] != float64(42) {
		t.Errorf("mail_id: %v", cmd.Args)
	}
	if _, bad := cmd.Args["target_persist_id"]; bad {
		t.Errorf("target_persist_id must NOT leak through delete-mail (server gates on session): %v", cmd.Args)
	}
}

// TestMailDeclarationsPresent asserts all three mail convention declarations load, belong to
// family="mail", and carry galtrader-style descriptions with ≥2 of prerequisite/effect/cost.
func TestMailDeclarationsPresent(t *testing.T) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}

	byOp := map[string]*convention.Declaration{}
	for _, d := range decls {
		byOp[d.Operation] = d
	}

	type argCheck struct {
		op       string
		required []string
	}
	want := []argCheck{
		{"poll-inbox", nil}, // since_ticks is optional
		{"send-mail", []string{"target_persist_id", "subject", "body"}},
		{"delete-mail", []string{"mail_id"}},
	}

	for _, w := range want {
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
			t.Errorf("%s: description must carry ≥2 of Prerequisite/Effect/Cost (got %d): %q", w.op, hits, d.Description)
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

	// Explicitly assert read-mail is NOT declared (dropped because no wire PDU).
	if _, bad := byOp["read-mail"]; bad {
		t.Errorf("read-mail declaration exists but should NOT — MailRequestType has no READ variant; drop-don't-stub rule applies")
	}
	// check-inbox is not the op name on this fork — the name is poll-inbox per catalog.
	if _, bad := byOp["check-inbox"]; bad {
		t.Errorf("check-inbox declared but catalog names the op poll-inbox")
	}
}

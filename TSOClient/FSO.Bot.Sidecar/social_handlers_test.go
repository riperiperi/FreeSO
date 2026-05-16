/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"encoding/json"
	"strings"
	"testing"
	"time"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// verifyingDirectedSocialHandlerFast returns a verifyingDirectedSocialHandler
// using the fast test config so unit tests don't wait 1500ms settle.
func verifyingDirectedSocialHandlerFast(ipc *IPC, op string) convention.HandlerFunc {
	return verifyingHandlerWithExpect(
		ipc, op,
		[]string{"target_sim_id", "target_object_id", "queue_mode"},
		0, "", nil,
		socialExpectFn,
		fastTestConfig(),
	)
}

// TestSpeakHandlerDispatchesIPC asserts speak convention invocation produces an IPC command
// with op="speak" carrying text + channel_id args.
func TestSpeakHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{"queued": true, "text": "hello world", "channel_id": 0, "length": 11},
	})

	handler := speakHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"text":       "hello world",
			"channel_id": float64(0),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	if resp == nil {
		t.Fatal("nil response")
	}
	cmd := <-gotCmd
	if cmd.Op != "speak" {
		t.Errorf("want op=speak got %q", cmd.Op)
	}
	if cmd.Args["text"] != "hello world" {
		t.Errorf("text not forwarded: %v", cmd.Args)
	}
	if cmd.Args["channel_id"] != float64(0) {
		t.Errorf("channel_id not forwarded: %v", cmd.Args)
	}
}

// TestDirectedSocialHandlersDispatchIPC asserts each of the five directed-social handlers
// (be-friendly, tell-joke, flirt, be-mean, give-gift) produces an IPC command carrying the
// SAME op name and only forwards target_sim_id / target_object_id — nothing else. This is
// load-bearing for the "no interaction_id escape hatch" contract: the verb is the intent, the
// bot resolves the TTAB index. A caller-supplied interaction_id would bypass the verb's
// semantic guarantee and MUST NOT be forwarded.
func TestDirectedSocialHandlersDispatchIPC(t *testing.T) {
	cases := []string{"be-friendly", "tell-joke", "flirt", "be-mean", "give-gift"}
	for _, op := range cases {
		op := op
		t.Run(op, func(t *testing.T) {
			fake := newFakeBotProcess()
			ipc := NewIPC(fake.bot)
			gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
				"kind": "response", "ok": true,
				"payload": map[string]any{"queued": true, "verb": op, "interaction": 3},
			})

			handler := directedSocialHandler(ipc, op)
			ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
			defer cancel()
			_, err := handler(ctx, &convention.Request{
				Args: map[string]any{
					"target_sim_id":  float64(42),
					"interaction_id": float64(999), // must NOT be forwarded
				},
			})
			if err != nil {
				t.Fatalf("handler: %v", err)
			}
			cmd := <-gotCmd
			if cmd.Op != op {
				t.Errorf("want op=%s got %q", op, cmd.Op)
			}
			if cmd.Args["target_sim_id"] != float64(42) {
				t.Errorf("target_sim_id not forwarded: %v", cmd.Args)
			}
			if _, bad := cmd.Args["interaction_id"]; bad {
				t.Errorf("interaction_id must NOT be forwarded (verb owns the TTAB index): %v", cmd.Args)
			}
		})
	}
}

// TestSocialDeclarationsPresent asserts the six social ops are loadable from the embedded
// convention files and carry galtrader-style descriptions (prerequisite/effect/cost).
func TestSocialDeclarationsPresent(t *testing.T) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}
	byOp := map[string]*convention.Declaration{}
	for _, d := range decls {
		byOp[d.Operation] = d
	}
	for _, op := range []string{"speak", "be-friendly", "tell-joke", "flirt", "be-mean", "give-gift"} {
		d, ok := byOp[op]
		if !ok {
			t.Errorf("declaration for %q missing", op)
			continue
		}
		if d.Convention != "freeso-embodiment" {
			t.Errorf("%s: convention=%q", op, d.Convention)
		}
		if d.Description == "" {
			t.Errorf("%s: empty description", op)
		}
		// Galtrader-style: require at least 2 of Prerequisite/Effect/Cost. Sterile description
		// rule (Finding #1 from FreeSims).
		lower := strings.ToLower(d.Description)
		hits := 0
		for _, kw := range []string{"prerequisite", "effect", "cost"} {
			if strings.Contains(lower, kw) {
				hits++
			}
		}
		if hits < 2 {
			t.Errorf("%s: description must carry at least 2 of Prerequisite/Effect/Cost (got %d): %q",
				op, hits, d.Description)
		}
	}
}

// TestDirectedSocialVerifying_SocialFired: be-friendly IPC returns ok:true
// and a new action_queue entry appears for the target sim. Verdict must be
// queued or interaction-started (success path).
func TestDirectedSocialVerifying_SocialFired(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// The bot's directed-social ack carries callee_id (VM ObjectID of target avatar).
	// Post snapshot shows a new action_queue entry targeting that object.
	script := buildSnapshotScript("be-friendly", []map[string]any{
		{"ok": true, "payload": map[string]any{
			"queued":       true,
			"verb":         "be-friendly",
			"interaction":  3,
			"callee_id":    42,
			"target_sim_id": 1001,
		}},
	}, []any{}, []any{
		map[string]any{"interaction_id": 88, "name": "Be Friendly", "target_object_id": float64(42), "status": "queued"},
	})
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := verifyingDirectedSocialHandlerFast(ipc, "be-friendly")
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"target_sim_id": float64(1001),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	pay, _ := resp.Payload.(map[string]any)
	if pay == nil {
		t.Fatal("nil payload")
	}
	verdict, _ := pay["verdict"].(string)
	if verdict != "queued" && verdict != "interaction-started" {
		t.Errorf("want verdict=queued or interaction-started, got %q; payload=%v", verdict, pay)
	}
	if ok, _ := pay["ok"].(bool); !ok {
		t.Errorf("want ok=true, got %v", pay["ok"])
	}
}

// TestDirectedSocialVerifying_SilentDrop: be-friendly IPC returns ok:true
// but no new action_queue entry appears. Verdict must be silent-drop with
// "unavailable-interaction-no-event" hint.
func TestDirectedSocialVerifying_SilentDrop(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	script := buildSnapshotScript("be-friendly", []map[string]any{
		{"ok": true, "payload": map[string]any{
			"queued":    true,
			"verb":      "be-friendly",
			"callee_id": 42,
		}},
	}, []any{}, []any{} /* post queue still empty */)
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := verifyingDirectedSocialHandlerFast(ipc, "be-friendly")
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, _ := handler(ctx, &convention.Request{
		Args: map[string]any{
			"target_sim_id": float64(1001),
		},
	})
	pay, _ := resp.Payload.(map[string]any)
	if pay == nil {
		t.Fatal("nil payload")
	}
	if verdict := pay["verdict"]; verdict != "silent-drop" {
		t.Errorf("want verdict=silent-drop, got %q", verdict)
	}
	hints, _ := pay["hints"].([]string)
	if !containsHint(hints, "unavailable-interaction-no-event") {
		t.Errorf("want hint 'unavailable-interaction-no-event', got %v", hints)
	}
}

// TestDirectedSocialVerifying_BotRejected: bot returns ok:false (e.g. sim
// not in local VM). Handler surfaces bot-rejected without a settle/snapshot
// overhead.
func TestDirectedSocialVerifying_BotRejected(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	script := buildSnapshotScript("be-friendly", []map[string]any{
		{"ok": false, "error": "sim 9999 not in local VM"},
	}, []any{}, []any{})
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := verifyingDirectedSocialHandlerFast(ipc, "be-friendly")
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, _ := handler(ctx, &convention.Request{
		Args: map[string]any{
			"target_sim_id": float64(9999),
		},
	})
	pay, _ := resp.Payload.(map[string]any)
	if pay == nil {
		t.Fatal("nil payload")
	}
	if pay["verdict"] != "bot-rejected" {
		t.Errorf("want verdict=bot-rejected, got %q", pay["verdict"])
	}
	if ok, _ := pay["ok"].(bool); ok {
		t.Errorf("want ok=false, got true")
	}
}

// TestSocialExpectFn_IgnoresPreExistingEntries: entries that existed in pre
// must be ignored — only NEW entries count.
func TestSocialExpectFn_IgnoresPreExistingEntries(t *testing.T) {
	pre := lotSnapshot{
		actionQueue:  []ActionQueueEntry{{InteractionID: 10, Name: "Sleep", TargetObjectID: 42, Status: "running"}},
		objectsByPID: map[uint64]objectRef{},
	}
	post := lotSnapshot{
		actionQueue:  []ActionQueueEntry{{InteractionID: 10, Name: "Sleep", TargetObjectID: 42, Status: "running"}},
		objectsByPID: map[uint64]objectRef{},
	}
	ipcResp := &Response{Ok: true, Payload: []byte(`{"queued":true,"callee_id":42}`)}
	verdict, hints, _, ok := socialExpectFn(pre, post, nil, ipcResp)
	if verdict != "silent-drop" {
		t.Errorf("want silent-drop (pre entry not new), got %q", verdict)
	}
	if ok {
		t.Error("ok must be false for silent-drop")
	}
	if !containsHint(hints, "unavailable-interaction-no-event") {
		t.Errorf("want unavailable-interaction-no-event hint, got %v", hints)
	}
}

// TestSocialExpectFn_InteractionStartedStatus: new entry with status="running"
// must return verdict="interaction-started".
func TestSocialExpectFn_InteractionStartedStatus(t *testing.T) {
	pre := lotSnapshot{actionQueue: []ActionQueueEntry{}, objectsByPID: map[uint64]objectRef{}}
	post := lotSnapshot{
		actionQueue:  []ActionQueueEntry{{InteractionID: 77, Name: "Be Friendly", TargetObjectID: 42, Status: "running"}},
		objectsByPID: map[uint64]objectRef{},
	}
	ipcResp := &Response{Ok: true, Payload: []byte(`{"queued":true,"callee_id":42}`)}
	verdict, _, payload, ok := socialExpectFn(pre, post, nil, ipcResp)
	if verdict != "interaction-started" {
		t.Errorf("want interaction-started, got %q", verdict)
	}
	if !ok {
		t.Error("ok must be true for interaction-started")
	}
	if payload["action_uid"] != 77 {
		t.Errorf("action_uid = %v; want 77", payload["action_uid"])
	}
}

// TestSocialExpectFn_AllFiveVerbs: confirm socialExpectFn works for each of the
// five directed-social verbs (the ExpectFn is shared; this is a smoke test to
// ensure the callee_id extraction works identically across all five).
func TestSocialExpectFn_AllFiveVerbs(t *testing.T) {
	verbs := []string{"be-friendly", "tell-joke", "flirt", "be-mean", "give-gift"}
	for _, verb := range verbs {
		t.Run(verb, func(t *testing.T) {
			pre := lotSnapshot{actionQueue: []ActionQueueEntry{}, objectsByPID: map[uint64]objectRef{}}
			post := lotSnapshot{
				actionQueue: []ActionQueueEntry{
					{InteractionID: 55, Name: verb, TargetObjectID: 42, Status: "queued"},
				},
				objectsByPID: map[uint64]objectRef{},
			}
			ipcPayload, _ := json.Marshal(map[string]any{"queued": true, "verb": verb, "callee_id": 42})
			ipcResp := &Response{Ok: true, Payload: ipcPayload}
			verdict, _, _, ok := socialExpectFn(pre, post, nil, ipcResp)
			if verdict != "queued" {
				t.Errorf("%s: want verdict=queued, got %q", verb, verdict)
			}
			if !ok {
				t.Errorf("%s: want ok=true", verb)
			}
		})
	}
}

// TestDirectedSocialHandlersDispatchIPC_OldShape asserts the un-wrapped
// directedSocialHandler (used in older tests) still forwards the op correctly.
// This preserves the IPC shape contract that existed before W11.
func TestDirectedSocialHandlersDispatchIPC_OldShape(t *testing.T) {
	cases := []string{"be-friendly", "tell-joke", "flirt", "be-mean", "give-gift"}
	for _, op := range cases {
		op := op
		t.Run(op, func(t *testing.T) {
			fake := newFakeBotProcess()
			ipc := NewIPC(fake.bot)
			gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
				"kind": "response", "ok": true,
				"payload": map[string]any{"queued": true, "verb": op, "interaction": 3},
			})

			handler := directedSocialHandler(ipc, op)
			ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
			defer cancel()
			_, err := handler(ctx, &convention.Request{
				Args: map[string]any{
					"target_sim_id":  float64(42),
					"interaction_id": float64(999),
				},
			})
			if err != nil {
				t.Fatalf("handler: %v", err)
			}
			cmd := <-gotCmd
			if cmd.Op != op {
				t.Errorf("want op=%s got %q", op, cmd.Op)
			}
		})
	}
}

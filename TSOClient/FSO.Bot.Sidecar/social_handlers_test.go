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

/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"encoding/json"
	"fmt"
	"strings"
	"sync"
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
// with op="speak" carrying text + channel_id args, and that the handler returns
// ok=true with chunk_count=1 for short text.
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
	// Handler returns chunk_count=1 for sub-limit text.
	pay, _ := resp.Payload.(map[string]any)
	if pay == nil {
		t.Fatal("nil payload map")
	}
	if ok, _ := pay["ok"].(bool); !ok {
		t.Errorf("want ok=true, got %v", pay["ok"])
	}
	if cc, _ := pay["chunk_count"].(int); cc != 1 {
		t.Errorf("want chunk_count=1, got %v", pay["chunk_count"])
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
			"queued":        true,
			"verb":          "be-friendly",
			"interaction":   3,
			"callee_id":     42,
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

// TestSocialExpectFn_ObjectIDDriftFallback (sisters-shake-hands regression):
// the bot's IPC ack reports callee_id as the target VMAvatar's ObjectID at
// pie-menu lookup time; the engine commits the queue entry against a different
// ObjectID (an interaction-socket sub-object). When matched_name is present and
// matches a new entry's name (after stripping the path prefix), attribute it.
func TestSocialExpectFn_ObjectIDDriftFallback(t *testing.T) {
	pre := lotSnapshot{actionQueue: []ActionQueueEntry{}, objectsByPID: map[uint64]objectRef{}}
	post := lotSnapshot{
		actionQueue: []ActionQueueEntry{
			// Engine-committed entry: target_object_id differs from ack callee_id;
			// short name matches the trailing segment of matched_name.
			{InteractionID: 14, Name: "Hug", TargetObjectID: 101, Status: "queued"},
		},
		objectsByPID: map[uint64]objectRef{},
	}
	ipcResp := &Response{Ok: true, Payload: []byte(
		`{"queued":true,"verb":"be-friendly","interaction":61,"matched_name":"Nice/Hug","callee_id":98,"target_sim_id":3,"queue_mode":"queue","cancelled":2}`,
	)}
	verdict, _, payload, ok := socialExpectFn(pre, post, nil, ipcResp)
	if verdict != "queued" {
		t.Errorf("want verdict=queued (drift fallback), got %q", verdict)
	}
	if !ok {
		t.Error("want ok=true for queued")
	}
	if payload["matched_by"] != "matched_name" {
		t.Errorf("want matched_by=matched_name, got %v", payload["matched_by"])
	}
	if payload["target_object_id"] != 101 {
		t.Errorf("want target_object_id=101, got %v", payload["target_object_id"])
	}
}

// TestInteractExpectFn_SingleNewEntryFallback (sisters-shake-hands regression
// for interact-with): when the bot ack reports queued:true but no new queue
// entry matches callee_id strictly, and exactly one new non-idle entry exists,
// attribute it to this op.
func TestInteractExpectFn_SingleNewEntryFallback(t *testing.T) {
	pre := lotSnapshot{actionQueue: []ActionQueueEntry{}, objectsByPID: map[uint64]objectRef{}}
	post := lotSnapshot{
		actionQueue: []ActionQueueEntry{
			{InteractionID: 17, Name: "Greet", TargetObjectID: 101, Status: "running"},
		},
		objectsByPID: map[uint64]objectRef{},
	}
	args := map[string]any{"callee_id": 22, "interaction": 60}
	ipcResp := &Response{Ok: true, Payload: []byte(`{"queued":true,"callee_id":22,"interaction":60,"cancelled":2,"queue_mode":"preempt"}`)}
	verdict, _, payload, ok := interactExpectFn(pre, post, args, ipcResp)
	if verdict != "interaction-started" {
		t.Errorf("want interaction-started (single-new-entry fallback), got %q", verdict)
	}
	if !ok {
		t.Error("want ok=true for interaction-started")
	}
	if payload["matched_by"] != "single-new-entry" {
		t.Errorf("want matched_by=single-new-entry, got %v", payload["matched_by"])
	}
}

// TestInteractExpectFn_NoAckQueuedNoFallback: the single-new-entry fallback
// must NOT kick in when the bot ack does not confirm queued:true (e.g. the
// op was a no-op). Preserves silent-drop verdict on unrelated traffic.
func TestInteractExpectFn_NoAckQueuedNoFallback(t *testing.T) {
	pre := lotSnapshot{actionQueue: []ActionQueueEntry{}, objectsByPID: map[uint64]objectRef{}}
	post := lotSnapshot{
		actionQueue: []ActionQueueEntry{
			{InteractionID: 5, Name: "Cook", TargetObjectID: 88, Status: "queued"},
		},
		objectsByPID: map[uint64]objectRef{},
	}
	args := map[string]any{"callee_id": 17, "interaction": 0}
	// queued:false — bot did not actually dispatch; the new entry is unrelated.
	ipcResp := &Response{Ok: true, Payload: []byte(`{"queued":false,"callee_id":17}`)}
	verdict, _, _, ok := interactExpectFn(pre, post, args, ipcResp)
	if verdict != "silent-drop" {
		t.Errorf("want silent-drop (no fallback when not queued), got %q", verdict)
	}
	if ok {
		t.Error("want ok=false for silent-drop")
	}
}

// ---------------------------------------------------------------------------
// chunkText unit tests (automataisland-73d)
// ---------------------------------------------------------------------------

// TestChunkText_Short asserts text within speakChunkSize returns a single chunk.
func TestChunkText_Short(t *testing.T) {
	text := strings.Repeat("a", 500)
	chunks := chunkText(text, speakChunkSize)
	if len(chunks) != 1 {
		t.Errorf("want 1 chunk for 500-char text, got %d", len(chunks))
	}
	if chunks[0] != text {
		t.Errorf("single chunk must equal input")
	}
}

// TestChunkText_ExactLimit asserts text of exactly speakChunkSize runes returns a single chunk
// (boundary: 1000 runes, chunk_count=1).
func TestChunkText_ExactLimit(t *testing.T) {
	text := strings.Repeat("x", speakChunkSize)
	chunks := chunkText(text, speakChunkSize)
	if len(chunks) != 1 {
		t.Errorf("want 1 chunk for exactly-limit text (%d runes), got %d", speakChunkSize, len(chunks))
	}
}

// TestChunkText_OnePastLimit asserts text of speakChunkSize+1 runes is split into 2 chunks
// (boundary: 1001 runes, chunk_count=2).
func TestChunkText_OnePastLimit(t *testing.T) {
	// Build 1001-rune text with a space just inside the limit so the word-boundary
	// split fires at position 999.
	text := strings.Repeat("a", 999) + " b"
	if len([]rune(text)) != 1001 {
		t.Fatalf("precondition: text must be 1001 runes, got %d", len([]rune(text)))
	}
	chunks := chunkText(text, speakChunkSize)
	if len(chunks) != 2 {
		t.Errorf("want 2 chunks for 1001-rune text, got %d: %v", len(chunks), chunks)
	}
}

// TestChunkText_WordBoundary asserts chunks do not break mid-word and each is within limit.
func TestChunkText_WordBoundary(t *testing.T) {
	// 2500-rune text composed of ~5-char words separated by spaces.
	var sb strings.Builder
	for i := 0; sb.Len() < 2500; i++ {
		if i > 0 {
			sb.WriteRune(' ')
		}
		sb.WriteString(fmt.Sprintf("word%d", i))
	}
	text := string([]rune(sb.String())[:2500])
	chunks := chunkText(text, speakChunkSize)
	for i, c := range chunks {
		if strings.HasPrefix(c, " ") || strings.HasSuffix(c, " ") {
			t.Errorf("chunk %d has leading/trailing space: %q", i, c[:min(len(c), 20)])
		}
		if rc := len([]rune(c)); rc > speakChunkSize {
			t.Errorf("chunk %d exceeds limit: %d runes", i, rc)
		}
	}
}

// TestChunkText_2500Runes asserts a 2500-rune text with word boundaries produces 3 chunks.
func TestChunkText_2500Runes(t *testing.T) {
	// Build 2500-rune text with word boundaries inside each 1000-rune window.
	var sb strings.Builder
	for i := 0; sb.Len() < 2600; i++ {
		if i > 0 {
			sb.WriteRune(' ')
		}
		sb.WriteString(fmt.Sprintf("word%04d", i))
	}
	text := string([]rune(sb.String())[:2500])
	chunks := chunkText(text, speakChunkSize)
	if len(chunks) != 3 {
		t.Errorf("want 3 chunks for 2500-rune text, got %d", len(chunks))
	}
	// Reconstruct: joining chunks on spaces should recover original word list.
	joined := strings.Join(chunks, " ")
	origWords := strings.Fields(text)
	joinedWords := strings.Fields(joined)
	if len(origWords) != len(joinedWords) {
		t.Errorf("word count mismatch after round-trip: orig=%d joined=%d", len(origWords), len(joinedWords))
	}
}

// TestChunkText_HardSplit asserts a word longer than speakChunkSize is hard-split
// with a truncation marker.
func TestChunkText_HardSplit(t *testing.T) {
	text := strings.Repeat("a", speakChunkSize+50)
	chunks := chunkText(text, speakChunkSize)
	if len(chunks) < 2 {
		t.Fatalf("want >=2 chunks for overlong word, got %d", len(chunks))
	}
	// First chunk must end with truncation marker.
	firstRunes := []rune(chunks[0])
	if firstRunes[len(firstRunes)-1] != '…' {
		t.Errorf("first chunk missing truncation marker: %q", chunks[0][max(0, len(chunks[0])-4):])
	}
}

// ---------------------------------------------------------------------------
// speakHandler chunking integration tests (automataisland-73d)
//
// These tests use the fakeBotProcess + IPC layer — the full sidecar code path
// from convention.HandlerFunc through IPC.Send to the fake bot and back.
// No live FSO instance is required; the mock-scope is the bot's IPC channel
// only (the campfire and FSO server are not in scope for unit testing the
// speak handler directly).
//
// Test decision rationale (posted to swarm campfire): a live grid31337-test bot
// is the gold standard but requires a running FreeSO stack not available in
// this dispatch context. The fakeBotProcess exercises the identical IPC.Send
// path (same serialisation, same correlation logic, same context propagation)
// that the real bot uses. The observable behaviour — N sequential IPC commands,
// correct text per chunk, correct chunk_count in response — is fully verifiable
// without a live bot. Live-bot validation is covered by the integration test
// suite and operators' manual verification cadence.
// ---------------------------------------------------------------------------

// captureNCommands is a helper that reads n IPC commands from fake.stdinLines,
// responds to each with the provided per-command responses (indexed), and
// returns a channel that receives all commands in order.
func captureNCommands(t *testing.T, fake *fakeBotProcess, ipc *IPC, responses []map[string]any) <-chan []Command {
	t.Helper()
	n := len(responses)
	ch := make(chan []Command, 1)
	go func() {
		cmds := make([]Command, 0, n)
		for i := 0; i < n; i++ {
			select {
			case line := <-fake.stdinLines:
				var cmd Command
				if err := json.Unmarshal(line, &cmd); err != nil {
					t.Errorf("captureNCommands[%d]: unmarshal: %v", i, err)
					ch <- cmds
					return
				}
				cmds = append(cmds, cmd)
				resp := responses[i]
				resp["cmd_id"] = cmd.ID
				data, _ := json.Marshal(resp)
				ipc.Deliver(data)
			case <-time.After(5 * time.Second):
				t.Errorf("captureNCommands: command %d never arrived (got %d/%d)", i, len(cmds), n)
				ch <- cmds
				return
			}
		}
		ch <- cmds
	}()
	return ch
}

// TestSpeakHandler_500Chars asserts text of 500 chars sends one IPC command
// and returns chunk_count=1.
func TestSpeakHandler_500Chars(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	text := strings.Repeat("a", 500)

	responses := []map[string]any{
		{"kind": "response", "ok": true, "payload": map[string]any{"queued": true, "length": 500}},
	}
	gotCmds := captureNCommands(t, fake, ipc, responses)

	handler := speakHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{"text": text},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}

	cmds := <-gotCmds
	if len(cmds) != 1 {
		t.Errorf("want 1 IPC command for 500-char text, got %d", len(cmds))
	}
	if len(cmds) > 0 && cmds[0].Args["text"] != text {
		t.Errorf("IPC text mismatch")
	}

	pay, _ := resp.Payload.(map[string]any)
	if pay == nil {
		t.Fatal("nil payload")
	}
	if ok, _ := pay["ok"].(bool); !ok {
		t.Errorf("want ok=true, got %v", pay["ok"])
	}
	if cc, _ := pay["chunk_count"].(int); cc != 1 {
		t.Errorf("want chunk_count=1, got %v", pay["chunk_count"])
	}
}

// TestSpeakHandler_2500Chars asserts text of 2500 chars sends 3 IPC speak
// commands in correct order and returns chunk_count=3.
func TestSpeakHandler_2500Chars(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// 2500-rune text with word boundaries.
	var sb strings.Builder
	for i := 0; sb.Len() < 2600; i++ {
		if i > 0 {
			sb.WriteRune(' ')
		}
		sb.WriteString(fmt.Sprintf("word%04d", i))
	}
	text := string([]rune(sb.String())[:2500])

	expectedChunks := chunkText(text, speakChunkSize)
	if len(expectedChunks) != 3 {
		t.Fatalf("precondition: want 3 chunks, got %d", len(expectedChunks))
	}

	responses := []map[string]any{
		{"kind": "response", "ok": true, "payload": map[string]any{"queued": true}},
		{"kind": "response", "ok": true, "payload": map[string]any{"queued": true}},
		{"kind": "response", "ok": true, "payload": map[string]any{"queued": true}},
	}
	gotCmds := captureNCommands(t, fake, ipc, responses)

	handler := speakHandler(ipc)
	// Use a longer timeout to accommodate two 200ms inter-chunk delays.
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	var (
		handlerResp *convention.Response
		handlerErr  error
		wg          sync.WaitGroup
	)
	wg.Add(1)
	go func() {
		defer wg.Done()
		handlerResp, handlerErr = handler(ctx, &convention.Request{
			Args: map[string]any{"text": text},
		})
	}()

	cmds := <-gotCmds
	wg.Wait()

	if handlerErr != nil {
		t.Fatalf("handler: %v", handlerErr)
	}

	// Assert 3 IPC commands were issued in order.
	if len(cmds) != 3 {
		t.Fatalf("want 3 IPC speak commands, got %d", len(cmds))
	}
	for i, cmd := range cmds {
		if cmd.Op != "speak" {
			t.Errorf("cmd[%d]: want op=speak, got %q", i, cmd.Op)
		}
		if cmd.Args["text"] != expectedChunks[i] {
			t.Errorf("cmd[%d]: text mismatch\n  want: %q\n  got:  %q",
				i, expectedChunks[i], cmd.Args["text"])
		}
	}

	// Assert response shape.
	pay, _ := handlerResp.Payload.(map[string]any)
	if pay == nil {
		t.Fatal("nil payload")
	}
	if ok, _ := pay["ok"].(bool); !ok {
		t.Errorf("want ok=true, got %v", pay["ok"])
	}
	if cc, _ := pay["chunk_count"].(int); cc != 3 {
		t.Errorf("want chunk_count=3, got %v", pay["chunk_count"])
	}
}

// TestSpeakHandler_ChunkFailure asserts that when a mid-sequence IPC call
// fails (bot returns ok:false), the handler stops and returns ok=false with
// failed_chunk set.
func TestSpeakHandler_ChunkFailure(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// 2500-rune text → 3 chunks. Second chunk will fail.
	var sb strings.Builder
	for i := 0; sb.Len() < 2600; i++ {
		if i > 0 {
			sb.WriteRune(' ')
		}
		sb.WriteString(fmt.Sprintf("word%04d", i))
	}
	text := string([]rune(sb.String())[:2500])

	// Respond to first chunk ok, second chunk fails; third should never arrive.
	responses := []map[string]any{
		{"kind": "response", "ok": true, "payload": map[string]any{"queued": true}},
		{"kind": "response", "ok": false, "error": "sim not on lot"},
	}
	gotCmds := captureNCommands(t, fake, ipc, responses)

	handler := speakHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	var (
		handlerResp *convention.Response
		handlerErr  error
		wg          sync.WaitGroup
	)
	wg.Add(1)
	go func() {
		defer wg.Done()
		handlerResp, handlerErr = handler(ctx, &convention.Request{
			Args: map[string]any{"text": text},
		})
	}()

	cmds := <-gotCmds
	wg.Wait()

	if handlerErr != nil {
		t.Fatalf("handler: %v", handlerErr)
	}
	if len(cmds) != 2 {
		t.Errorf("want exactly 2 IPC commands before failure, got %d", len(cmds))
	}

	pay, _ := handlerResp.Payload.(map[string]any)
	if pay == nil {
		t.Fatal("nil payload")
	}
	if ok, _ := pay["ok"].(bool); ok {
		t.Errorf("want ok=false on chunk failure, got true")
	}
	if fc, _ := pay["failed_chunk"].(int); fc != 1 {
		t.Errorf("want failed_chunk=1, got %v", pay["failed_chunk"])
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

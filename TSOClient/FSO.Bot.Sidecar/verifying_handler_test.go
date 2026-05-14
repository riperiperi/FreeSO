/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"encoding/json"
	"reflect"
	"sort"
	"testing"
	"time"

	"github.com/campfire-net/campfire/pkg/convention"
)

// scriptedResponder feeds canned responses keyed by (op, call number) so a
// single handler invocation can see different pre / post snapshots. multi-
// AutoResponder (cross_level_test.go) keys only by op which collapses
// pre/post — insufficient here.
type scriptedResponder struct {
	t      *testing.T
	fake   *fakeBotProcess
	ipc    *IPC
	calls  map[string]int
	script map[string][]map[string]any // op → ordered list of responses
	recvd  chan Command
}

func newScriptedResponder(t *testing.T, fake *fakeBotProcess, ipc *IPC, script map[string][]map[string]any) *scriptedResponder {
	t.Helper()
	sr := &scriptedResponder{
		t:      t,
		fake:   fake,
		ipc:    ipc,
		calls:  map[string]int{},
		script: script,
		recvd:  make(chan Command, 32),
	}
	go sr.serve()
	return sr
}

func (sr *scriptedResponder) serve() {
	for line := range sr.fake.stdinLines {
		var cmd Command
		if err := json.Unmarshal(line, &cmd); err != nil {
			sr.t.Errorf("scriptedResponder: unmarshal: %v", err)
			continue
		}
		sr.recvd <- cmd
		queue, ok := sr.script[cmd.Op]
		var resp map[string]any
		if !ok || sr.calls[cmd.Op] >= len(queue) {
			resp = map[string]any{"ok": true, "payload": map[string]any{}}
		} else {
			resp = queue[sr.calls[cmd.Op]]
			sr.calls[cmd.Op]++
		}
		resp["cmd_id"] = cmd.ID
		resp["kind"] = "response"
		data, _ := json.Marshal(resp)
		sr.ipc.Deliver(data)
	}
}

// fastVerifyingConfig shortens the settle wait so unit tests don't take 1.5s
// each. The pure verdictResponse function (tested separately below) is the
// real correctness gate; this just exercises the IPC plumbing.
func fastVerifyingHandler(ipc *IPC, op string, allowedArgs ...string) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		return verifyingHandlerImpl(ctx, ipc, op, allowedArgs, fastTestConfig(), req)
	}
}

func fastDeleteHandler(ipc *IPC, op string, allowedArgs ...string) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		return deleteVerifyingHandlerImpl(ctx, ipc, op, allowedArgs, fastTestConfig(), req)
	}
}

func fastTestConfig() verifyingHandlerConfig {
	return verifyingHandlerConfig{
		settleWait:      10 * time.Millisecond,
		snapshotTimeout: 1 * time.Second,
		sendTimeout:     1 * time.Second,
	}
}

// TestPlacementVerifying_HappyPath: pre-snapshot shows no object at target,
// post-snapshot shows a new persist_id at the target tile, balance dropped by
// 200. Verdict: placed=true with object/cost details.
func TestPlacementVerifying_HappyPath(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	script := map[string][]map[string]any{
		"query-self": {
			{"ok": true, "payload": map[string]any{"balance": 10000}}, // pre
			{"ok": true, "payload": map[string]any{"balance": 9800}},  // post
		},
		"query-lot-objects": {
			{"ok": true, "payload": map[string]any{"objects": []any{
				// pre-existing: an unrelated object on level 1
				map[string]any{"object_id": 100, "persist_id": 16780000, "guid": 1234567, "guid_hex": "0x12D687", "x": 12, "y": 12, "level": 1, "dir": 0},
			}}},
			{"ok": true, "payload": map[string]any{"objects": []any{
				map[string]any{"object_id": 100, "persist_id": 16780000, "guid": 1234567, "guid_hex": "0x12D687", "x": 12, "y": 12, "level": 1, "dir": 0},
				// new one at target (tile 5,7 — subtile 80,112)
				map[string]any{"object_id": 101, "persist_id": 16780001, "guid": 1734088879, "guid_hex": "0x675C18AF", "x": 5, "y": 7, "level": 1, "dir": 0},
			}}},
		},
		"buy-object": {{"ok": true, "payload": map[string]any{"queued": true}}},
	}
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := fastVerifyingHandler(ipc, "buy-object", "guid", "x", "y", "level", "dir")
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"guid": float64(1734088879),
		"x":    float64(80),
		"y":    float64(112),
		"level": float64(1),
		"dir":  float64(0),
	}})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	pay := resp.Payload.(map[string]any)
	if pay["placed"] != true {
		t.Fatalf("expected placed=true, got %v (payload=%+v)", pay["placed"], pay)
	}
	if pay["verdict"] != "placed" {
		t.Errorf("verdict = %v; want placed", pay["verdict"])
	}
	if got, want := pay["persist_id"], uint64(16780001); got != want {
		t.Errorf("persist_id = %v; want %v", got, want)
	}
	if got, want := pay["object_id"], uint64(101); got != want {
		t.Errorf("object_id = %v; want %v", got, want)
	}
	if got, want := pay["cost"], int64(200); got != want {
		t.Errorf("cost = %v; want %v", got, want)
	}
	if got, want := pay["balance_before"], int64(10000); got != want {
		t.Errorf("balance_before = %v; want %v", got, want)
	}
	if got, want := pay["x"], 5; got != want {
		t.Errorf("x = %v; want %v (tile coord, not subtile)", got, want)
	}
	if got, want := pay["y"], 7; got != want {
		t.Errorf("y = %v; want %v", got, want)
	}
}

// TestPlacementVerifying_SilentDrop: bot returns ok:true / queued, but no
// object materializes and balance doesn't change. Verdict: placed=false with
// "no-budget-debit" hint.
func TestPlacementVerifying_SilentDrop(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	preObjects := []any{
		map[string]any{"object_id": 100, "persist_id": 16780000, "guid": 1234567, "x": 12, "y": 12, "level": 1, "dir": 0},
	}
	script := map[string][]map[string]any{
		"query-self": {
			{"ok": true, "payload": map[string]any{"balance": 10000}},
			{"ok": true, "payload": map[string]any{"balance": 10000}}, // unchanged
		},
		"query-lot-objects": {
			{"ok": true, "payload": map[string]any{"objects": preObjects}},
			{"ok": true, "payload": map[string]any{"objects": preObjects}}, // unchanged
		},
		"buy-object": {{"ok": true, "payload": map[string]any{"queued": true}}},
	}
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := fastVerifyingHandler(ipc, "buy-object", "guid", "x", "y", "level", "dir")
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, _ := handler(ctx, &convention.Request{Args: map[string]any{
		"guid":  float64(1734088879),
		"x":     float64(80),
		"y":     float64(112),
		"level": float64(1),
		"dir":   float64(0),
	}})
	pay := resp.Payload.(map[string]any)
	if pay["placed"] != false {
		t.Fatalf("expected placed=false, got %v", pay["placed"])
	}
	if pay["verdict"] != "silent-drop" {
		t.Errorf("verdict = %v; want silent-drop", pay["verdict"])
	}
	hints, _ := pay["hints"].([]string)
	if !containsHint(hints, "no-budget-debit") {
		t.Errorf("expected hint 'no-budget-debit', got %v", hints)
	}
	// IPC ack must be passed through.
	if _, ok := pay["ipc_ack"]; !ok {
		t.Error("expected ipc_ack in payload for diagnostics")
	}
}

// TestPlacementVerifying_BotRejected: bot returns ok:false (e.g. owner gate
// refused). Verdict: bot-rejected with the bot's error message — no settle,
// no post-snapshot.
func TestPlacementVerifying_BotRejected(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	script := map[string][]map[string]any{
		"query-self":        {{"ok": true, "payload": map[string]any{"balance": 10000}}},
		"query-lot-objects": {{"ok": true, "payload": map[string]any{"objects": []any{}}}},
		"buy-object":        {{"ok": false, "error": "caller is not lot owner (owner_id=2, me=5)", "payload": map[string]any{"owner_id": 2, "me": 5}}},
	}
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := fastVerifyingHandler(ipc, "buy-object", "guid", "x", "y", "level", "dir")
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, _ := handler(ctx, &convention.Request{Args: map[string]any{
		"guid":  float64(1734088879),
		"x":     float64(80),
		"y":     float64(112),
		"level": float64(1),
		"dir":   float64(0),
	}})
	pay := resp.Payload.(map[string]any)
	if pay["ok"] != false {
		t.Errorf("ok = %v; want false", pay["ok"])
	}
	if pay["verdict"] != "bot-rejected" {
		t.Errorf("verdict = %v; want bot-rejected", pay["verdict"])
	}
	if pay["error"] != "caller is not lot owner (owner_id=2, me=5)" {
		t.Errorf("error = %v; want caller-is-not-lot-owner shape", pay["error"])
	}
}

// TestPlacementVerifying_TileOccupiedHint: pre-snapshot shows an object
// already at the target tile, op returns ok:true but no new object appears.
// The hint set should include "tile-occupied".
func TestPlacementVerifying_TileOccupiedHint(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// tile (5,7), level 1 — same as target args below (subtile 80, 112).
	occupied := []any{
		map[string]any{"object_id": 99, "persist_id": 16780099, "guid": 1234567, "x": 5, "y": 7, "level": 1, "dir": 0},
	}
	script := map[string][]map[string]any{
		"query-self": {
			{"ok": true, "payload": map[string]any{"balance": 10000}},
			{"ok": true, "payload": map[string]any{"balance": 10000}},
		},
		"query-lot-objects": {
			{"ok": true, "payload": map[string]any{"objects": occupied}},
			{"ok": true, "payload": map[string]any{"objects": occupied}},
		},
		"buy-object": {{"ok": true, "payload": map[string]any{"queued": true}}},
	}
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := fastVerifyingHandler(ipc, "buy-object", "guid", "x", "y", "level", "dir")
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, _ := handler(ctx, &convention.Request{Args: map[string]any{
		"guid":  float64(1734088879),
		"x":     float64(80), // tile 5
		"y":     float64(112), // tile 7
		"level": float64(1),
		"dir":   float64(0),
	}})
	pay := resp.Payload.(map[string]any)
	if pay["placed"] != false {
		t.Fatalf("expected placed=false, got %v", pay["placed"])
	}
	hints, _ := pay["hints"].([]string)
	if !containsHint(hints, "tile-occupied") {
		t.Errorf("expected hint 'tile-occupied', got %v", hints)
	}
}

// TestVerdictResponse_BalanceChangedNoObject: the diff-only test for the
// rare case where the VM debited but rolled back without leaving a new
// object. Pure-function test of verdictResponse.
func TestVerdictResponse_BalanceChangedNoObject(t *testing.T) {
	pre := lotSnapshot{
		balance:      10000,
		objects:      nil,
		objectsByPID: map[uint64]objectRef{},
	}
	post := lotSnapshot{
		balance:      9500,
		objects:      nil, // no new objects
		objectsByPID: map[uint64]objectRef{},
	}
	args := map[string]any{"x": float64(80), "y": float64(112), "level": float64(1), "dir": float64(0)}
	ipcResp := &Response{Ok: true, Payload: json.RawMessage(`{"queued":true}`)}

	resp := verdictResponse("buy-object", args, pre, post, nil, nil, ipcResp)
	pay := resp.Payload.(map[string]any)
	if pay["placed"] != false {
		t.Fatalf("placed = %v; want false", pay["placed"])
	}
	hints, _ := pay["hints"].([]string)
	sort.Strings(hints)
	if !containsHint(hints, "balance-changed-no-object") {
		t.Errorf("expected hint 'balance-changed-no-object', got %v", hints)
	}
}

// TestVerdictResponse_MultiplePersistsDisambig: two new persist_ids appeared
// during the build window. We should prefer the one at the target tile.
func TestVerdictResponse_MultiplePersistsDisambig(t *testing.T) {
	pre := lotSnapshot{balance: 10000, objectsByPID: map[uint64]objectRef{}}
	post := lotSnapshot{
		balance: 9500,
		objects: []objectRef{
			{ObjectID: 100, PersistID: 16780100, Guid: 999, X: 3, Y: 3, Level: 1},
			{ObjectID: 101, PersistID: 16780101, Guid: 1734088879, X: 5, Y: 7, Level: 1},
		},
		objectsByPID: map[uint64]objectRef{
			16780100: {ObjectID: 100, PersistID: 16780100, Guid: 999, X: 3, Y: 3, Level: 1},
			16780101: {ObjectID: 101, PersistID: 16780101, Guid: 1734088879, X: 5, Y: 7, Level: 1},
		},
	}
	args := map[string]any{"x": float64(80), "y": float64(112), "level": float64(1), "dir": float64(0)}
	ipcResp := &Response{Ok: true, Payload: json.RawMessage(`{"queued":true}`)}

	resp := verdictResponse("buy-object", args, pre, post, nil, nil, ipcResp)
	pay := resp.Payload.(map[string]any)
	if pay["placed"] != true {
		t.Fatalf("placed = %v; want true", pay["placed"])
	}
	// Should pick the one at tile (5,7), not the unrelated one at (3,3).
	if got, want := pay["persist_id"], uint64(16780101); got != want {
		t.Errorf("persist_id = %v; want %v (the one at target tile)", got, want)
	}
	if got, want := pay["co_placed_count"], 1; got != want {
		t.Errorf("co_placed_count = %v; want 1 (the unrelated other new object)", got)
	}
}

// TestPlacementVerifying_PreSnapshotFails: query-self fails on pre-snapshot.
// We still try the build op (degraded mode) and surface a hint that the diff
// is untrustworthy. Verdict: silent-drop with "pre-snapshot-failed" hint.
func TestPlacementVerifying_PreSnapshotFails(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	script := map[string][]map[string]any{
		"query-self": {
			{"ok": false, "error": "bot is wedged"}, // pre fails
			{"ok": true, "payload": map[string]any{"balance": 10000}},
		},
		"query-lot-objects": {
			{"ok": true, "payload": map[string]any{"objects": []any{}}},
		},
		"buy-object": {{"ok": true, "payload": map[string]any{"queued": true}}},
	}
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := fastVerifyingHandler(ipc, "buy-object", "guid", "x", "y", "level", "dir")
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, _ := handler(ctx, &convention.Request{Args: map[string]any{
		"guid":  float64(1734088879),
		"x":     float64(80),
		"y":     float64(112),
		"level": float64(1),
		"dir":   float64(0),
	}})
	pay := resp.Payload.(map[string]any)
	hints, _ := pay["hints"].([]string)
	if !containsHint(hints, "pre-snapshot-failed") {
		t.Errorf("expected hint 'pre-snapshot-failed', got %v", hints)
	}
	if _, ok := pay["pre_snapshot_error"]; !ok {
		t.Errorf("expected pre_snapshot_error in payload")
	}
}

func containsHint(hints []string, want string) bool {
	for _, h := range hints {
		if h == want {
			return true
		}
	}
	return false
}

// TestSnapshot_BalanceAndObjects: directly exercises snapshot() to confirm
// json.Number parsing for both balance and object fields.
func TestSnapshot_BalanceAndObjects(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	script := map[string][]map[string]any{
		"query-self":        {{"ok": true, "payload": map[string]any{"balance": 12345}}},
		"query-lot-objects": {{"ok": true, "payload": map[string]any{"objects": []any{
			map[string]any{"object_id": 1, "persist_id": 100, "guid": 50, "guid_hex": "0x32", "x": 5, "y": 7, "level": 3, "dir": 0},
			map[string]any{"object_id": 2, "persist_id": 200, "guid": 60, "x": 9, "y": 11, "level": 3, "dir": 64},
		}}}},
	}
	_ = newScriptedResponder(t, fake, ipc, script)

	ctx, cancel := context.WithTimeout(context.Background(), 1*time.Second)
	defer cancel()
	snap, err := snapshot(ctx, ipc, 3, "", 500*time.Millisecond)
	if err != nil {
		t.Fatalf("snapshot: %v", err)
	}
	if snap.balance != 12345 {
		t.Errorf("balance = %d; want 12345", snap.balance)
	}
	if len(snap.objects) != 2 {
		t.Fatalf("len(objects) = %d; want 2", len(snap.objects))
	}
	if got, want := snap.objects[1].GuidHex, "0x3C"; got != want {
		t.Errorf("objects[1].GuidHex = %q; want %q (synthesized from guid=60=0x3C)", got, want)
	}
	if _, ok := snap.objectsByPID[100]; !ok {
		t.Error("objectsByPID missing pid=100")
	}
	if _, ok := snap.objectsByPID[200]; !ok {
		t.Error("objectsByPID missing pid=200")
	}
}

// TestGuidHexFromArgs spot-checks the small parser used to narrow the query-
// lot-objects filter.
func TestGuidHexFromArgs(t *testing.T) {
	cases := []struct {
		name string
		in   map[string]any
		want string
	}{
		{"present-int", map[string]any{"guid": 1734088879}, "0x675C18AF"},
		{"present-json-number", map[string]any{"guid": json.Number("1734088879")}, "0x675C18AF"},
		{"present-float64", map[string]any{"guid": float64(1734088879)}, "0x675C18AF"},
		{"present-string", map[string]any{"guid": "1734088879"}, "0x675C18AF"},
		{"present-hex-string", map[string]any{"guid": "0x675C18AF"}, "0x675C18AF"},
		{"missing", map[string]any{}, ""},
		{"zero", map[string]any{"guid": 0}, ""},
	}
	for _, tc := range cases {
		t.Run(tc.name, func(t *testing.T) {
			if got := guidHexFromArgs(tc.in); got != tc.want {
				t.Errorf("got %q; want %q", got, tc.want)
			}
		})
	}
}

// TestTileFromSubtileArg confirms the subtile → tile conversion the verdict
// path uses to compare against query-lot-objects (tile-unit) data.
func TestTileFromSubtileArg(t *testing.T) {
	cases := []struct {
		name      string
		args      map[string]any
		wantTile  int
		wantOk    bool
	}{
		{"subtile-80", map[string]any{"x": float64(80)}, 5, true},
		{"subtile-512", map[string]any{"x": float64(512)}, 32, true},
		{"missing", map[string]any{}, 0, false},
		{"non-numeric", map[string]any{"x": "abc"}, 0, false},
		{"int", map[string]any{"x": 160}, 10, true},
		{"json-number", map[string]any{"x": json.Number("256")}, 16, true},
	}
	for _, tc := range cases {
		t.Run(tc.name, func(t *testing.T) {
			gotTile, gotOk := tileFromSubtileArg(tc.args, "x")
			if gotTile != tc.wantTile || gotOk != tc.wantOk {
				t.Errorf("got (%d, %v); want (%d, %v)", gotTile, gotOk, tc.wantTile, tc.wantOk)
			}
		})
	}
}

// TestVerdictPayloadShape spot-checks that the verdict's payload keys remain
// stable across refactors. The agent contract is "verdict, placed, target,
// hints, balance_before, balance_after" — if those go missing or rename
// silently, an agent's parsing breaks. We grep the keys for presence.
func TestVerdictPayloadShape(t *testing.T) {
	pre := lotSnapshot{balance: 10000, objectsByPID: map[uint64]objectRef{}}
	post := lotSnapshot{balance: 10000, objectsByPID: map[uint64]objectRef{}}
	args := map[string]any{"x": float64(80), "y": float64(112), "level": float64(1), "dir": float64(0)}
	resp := verdictResponse("buy-object", args, pre, post, nil, nil, &Response{Ok: true})
	pay := resp.Payload.(map[string]any)
	required := []string{"ok", "placed", "verdict", "op", "target", "hints", "balance_before", "balance_after"}
	have := map[string]bool{}
	for k := range pay {
		have[k] = true
	}
	for _, k := range required {
		if !have[k] {
			t.Errorf("verdict payload missing key %q (have: %v)", k, sortedKeys(have))
		}
	}
}

func sortedKeys(m map[string]bool) []string {
	out := make([]string, 0, len(m))
	for k := range m {
		out = append(out, k)
	}
	sort.Strings(out)
	return out
}

// Smoke: defaultVerifyingConfig has sane non-zero settings, lest a refactor
// zero them and tests silently start passing on no-wait races.
func TestDefaultConfigSane(t *testing.T) {
	cfg := defaultVerifyingConfig()
	if cfg.settleWait <= 0 || cfg.settleWait > 5*time.Second {
		t.Errorf("settleWait = %v; want >0 and <=5s", cfg.settleWait)
	}
	if cfg.snapshotTimeout <= 0 {
		t.Error("snapshotTimeout must be positive")
	}
	if cfg.sendTimeout <= 0 {
		t.Error("sendTimeout must be positive")
	}
}

// ---- delete-object verifier tests ----

// TestDeleteVerifying_SingleTile_Succeeds: target persist_id is single-tile;
// first delete attempt removes it. Verdict: deleted=true, no retry.
func TestDeleteVerifying_SingleTile_Succeeds(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	preObjects := []any{
		map[string]any{"object_id": 100, "persist_id": 16780100, "guid": 1111, "x": 5, "y": 7, "level": 1, "dir": 0},
		map[string]any{"object_id": 101, "persist_id": 16780101, "guid": 2222, "x": 9, "y": 11, "level": 1, "dir": 0},
	}
	postObjects := []any{
		map[string]any{"object_id": 101, "persist_id": 16780101, "guid": 2222, "x": 9, "y": 11, "level": 1, "dir": 0},
	}
	script := map[string][]map[string]any{
		"query-self": {
			{"ok": true, "payload": map[string]any{"balance": 10000}},
			{"ok": true, "payload": map[string]any{"balance": 10300}}, // refund
		},
		"query-lot-objects": {
			{"ok": true, "payload": map[string]any{"objects": preObjects}},
			{"ok": true, "payload": map[string]any{"objects": postObjects}},
		},
		"delete-object": {{"ok": true, "payload": map[string]any{"queued": true}}},
	}
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := fastDeleteHandler(ipc, "delete-object", "target_object_id", "cleanup_all")
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, _ := handler(ctx, &convention.Request{Args: map[string]any{
		"target_object_id": float64(100),
	}})
	pay := resp.Payload.(map[string]any)
	if pay["deleted"] != true {
		t.Fatalf("deleted = %v; want true; payload=%+v", pay["deleted"], pay)
	}
	if pay["verdict"] != "deleted" {
		t.Errorf("verdict = %v; want deleted", pay["verdict"])
	}
	if pay["refund"] != int64(300) {
		t.Errorf("refund = %v; want 300", pay["refund"])
	}
	if _, ok := pay["retried_on_object_id"]; ok {
		t.Errorf("expected no retry for single-tile delete, got retry: %+v", pay)
	}
}

// TestDeleteVerifying_MultitileMaster_RetriesOnSubordinate: target is the
// master tile of a 3-tile multitile. First attempt no-ops (persist_id still
// present after settle). Handler retries with subordinate tile and succeeds.
func TestDeleteVerifying_MultitileMaster_RetriesOnSubordinate(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// persist_id 16780100 has three tiles: master object_id=100, subs 101 + 102
	tile := func(oid, pid uint64, x, y int) map[string]any {
		return map[string]any{
			"object_id": oid, "persist_id": pid, "guid": 9999,
			"x": x, "y": y, "level": 2, "dir": 0,
		}
	}
	preObjects := []any{tile(100, 16780100, 44, 45), tile(101, 16780100, 44, 46), tile(102, 16780100, 44, 47)}
	post1Objects := preObjects // unchanged after master delete (no-op)
	post2Objects := []any{}    // all gone after subordinate retry

	script := map[string][]map[string]any{
		"query-self": {
			{"ok": true, "payload": map[string]any{"balance": 10000}},
			{"ok": true, "payload": map[string]any{"balance": 10000}},
			{"ok": true, "payload": map[string]any{"balance": 10800}},
		},
		"query-lot-objects": {
			{"ok": true, "payload": map[string]any{"objects": preObjects}},
			{"ok": true, "payload": map[string]any{"objects": post1Objects}},
			{"ok": true, "payload": map[string]any{"objects": post2Objects}},
		},
		"delete-object": {
			{"ok": true, "payload": map[string]any{"queued": true}}, // master no-op
			{"ok": true, "payload": map[string]any{"queued": true}}, // subordinate succeeds
		},
	}
	received := newScriptedResponder(t, fake, ipc, script)

	handler := fastDeleteHandler(ipc, "delete-object", "target_object_id", "cleanup_all")
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, _ := handler(ctx, &convention.Request{Args: map[string]any{
		"target_object_id": float64(100), // master
	}})
	pay := resp.Payload.(map[string]any)
	if pay["deleted"] != true {
		t.Fatalf("deleted = %v; want true; payload=%+v", pay["deleted"], pay)
	}
	if pay["verdict"] != "deleted" {
		t.Errorf("verdict = %v; want deleted", pay["verdict"])
	}
	if pay["note"] != "retried-on-subordinate" {
		t.Errorf("note = %v; want retried-on-subordinate", pay["note"])
	}
	gotRetryID, _ := pay["retried_on_object_id"].(uint64)
	if gotRetryID != 101 && gotRetryID != 102 {
		t.Errorf("retried_on_object_id = %v; want 101 or 102 (a subordinate)", pay["retried_on_object_id"])
	}

	// Confirm both delete IPCs went out — the first against master, second
	// against a subordinate. handler() is synchronous so by the time it
	// returned all commands have been written to fake stdin and consumed by
	// scriptedResponder; we drain the channel of whatever's buffered.
	var cmds []Command
draining:
	for {
		select {
		case cmd := <-received.recvd:
			cmds = append(cmds, cmd)
		case <-time.After(100 * time.Millisecond):
			break draining
		}
	}
	deleteCmds := []Command{}
	for _, c := range cmds {
		if c.Op == "delete-object" {
			deleteCmds = append(deleteCmds, c)
		}
	}
	if len(deleteCmds) != 2 {
		t.Fatalf("delete-object dispatched %d times; want 2; all cmds=%+v", len(deleteCmds), cmds)
	}
	got0, _ := tryGetU64(deleteCmds[0].Args, "target_object_id")
	if got0 != 100 {
		t.Errorf("first delete target_object_id = %v; want 100 (master)", got0)
	}
	got1, _ := tryGetU64(deleteCmds[1].Args, "target_object_id")
	if got1 == 100 || got1 == 0 {
		t.Errorf("second delete target_object_id = %v; want a subordinate (101 or 102), not the master", got1)
	}
}

// TestDeleteVerifying_TotalFailure_NoSubordinate: persist_id is single-tile,
// first attempt no-ops, no sibling to retry against. Verdict: deleted=false
// with "no-siblings-to-retry" hint.
func TestDeleteVerifying_TotalFailure_NoSubordinate(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	preObjects := []any{
		map[string]any{"object_id": 100, "persist_id": 16780100, "guid": 1234, "x": 5, "y": 7, "level": 1, "dir": 0},
	}
	script := map[string][]map[string]any{
		"query-self": {
			{"ok": true, "payload": map[string]any{"balance": 10000}},
			{"ok": true, "payload": map[string]any{"balance": 10000}},
		},
		"query-lot-objects": {
			{"ok": true, "payload": map[string]any{"objects": preObjects}},
			{"ok": true, "payload": map[string]any{"objects": preObjects}},
		},
		"delete-object": {{"ok": true, "payload": map[string]any{"queued": true}}},
	}
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := fastDeleteHandler(ipc, "delete-object", "target_object_id", "cleanup_all")
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, _ := handler(ctx, &convention.Request{Args: map[string]any{"target_object_id": float64(100)}})
	pay := resp.Payload.(map[string]any)
	if pay["deleted"] != false {
		t.Fatalf("deleted = %v; want false (single-tile no-op, no retry possible); payload=%+v", pay["deleted"], pay)
	}
	hints, _ := pay["hints"].([]string)
	if !containsHint(hints, "no-siblings-to-retry") {
		t.Errorf("expected hint 'no-siblings-to-retry', got %v", hints)
	}
}

// TestDeleteVerifying_BotRejected: bot returns ok:false on the first delete
// (e.g. caller-is-not-lot-owner). Handler skips settle + retry and surfaces
// the rejection.
func TestDeleteVerifying_BotRejected(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	preObjects := []any{
		map[string]any{"object_id": 100, "persist_id": 16780100, "guid": 1234, "x": 5, "y": 7, "level": 1, "dir": 0},
	}
	script := map[string][]map[string]any{
		"query-self":        {{"ok": true, "payload": map[string]any{"balance": 10000}}},
		"query-lot-objects": {{"ok": true, "payload": map[string]any{"objects": preObjects}}},
		"delete-object":     {{"ok": false, "error": "caller is not lot owner (owner_id=2, me=5)"}},
	}
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := fastDeleteHandler(ipc, "delete-object", "target_object_id", "cleanup_all")
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, _ := handler(ctx, &convention.Request{Args: map[string]any{"target_object_id": float64(100)}})
	pay := resp.Payload.(map[string]any)
	if pay["ok"] != false {
		t.Fatalf("ok = %v; want false", pay["ok"])
	}
	if pay["verdict"] != "bot-rejected" {
		t.Errorf("verdict = %v; want bot-rejected", pay["verdict"])
	}
}

// TestDeleteVerifying_TargetNotFound: target_object_id not in pre-snapshot.
// We forward anyway (let the bot speak); if the bot accepts (ok:true) we
// trust it without trying to find siblings.
func TestDeleteVerifying_TargetNotFound(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	script := map[string][]map[string]any{
		"query-self":        {{"ok": true, "payload": map[string]any{"balance": 10000}}, {"ok": true, "payload": map[string]any{"balance": 10000}}},
		"query-lot-objects": {{"ok": true, "payload": map[string]any{"objects": []any{}}}, {"ok": true, "payload": map[string]any{"objects": []any{}}}},
		"delete-object":     {{"ok": true, "payload": map[string]any{"queued": true}}},
	}
	_ = newScriptedResponder(t, fake, ipc, script)

	handler := fastDeleteHandler(ipc, "delete-object", "target_object_id", "cleanup_all")
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, _ := handler(ctx, &convention.Request{Args: map[string]any{"target_object_id": float64(999)}})
	pay := resp.Payload.(map[string]any)
	if pay["deleted"] != true {
		t.Errorf("deleted = %v; want true (trust the bot when target not in pre-snapshot)", pay["deleted"])
	}
}

// TestTryGetU64 spot-checks the small parser used by delete-object.
func TestTryGetU64(t *testing.T) {
	cases := []struct {
		name string
		in   map[string]any
		want uint64
		ok   bool
	}{
		{"missing", map[string]any{}, 0, false},
		{"int", map[string]any{"k": 123}, 123, true},
		{"float64", map[string]any{"k": float64(456)}, 456, true},
		{"negative-int", map[string]any{"k": -1}, 0, false},
		{"json-number", map[string]any{"k": json.Number("789")}, 789, true},
		{"string-dec", map[string]any{"k": "12345"}, 12345, true},
		{"string-non-numeric", map[string]any{"k": "abc"}, 0, false},
	}
	for _, tc := range cases {
		t.Run(tc.name, func(t *testing.T) {
			got, ok := tryGetU64(tc.in, "k")
			if got != tc.want || ok != tc.ok {
				t.Errorf("got (%d, %v); want (%d, %v)", got, ok, tc.want, tc.ok)
			}
		})
	}
}

// Confirms reflect-based config struct hygiene — at least these fields exist.
// (Caught a typo'd field renaming once; cheap insurance.)
func TestVerifyingConfigStructShape(t *testing.T) {
	want := []string{"settleWait", "snapshotTimeout", "sendTimeout"}
	got := []string{}
	rt := reflect.TypeOf(verifyingHandlerConfig{})
	for i := 0; i < rt.NumField(); i++ {
		got = append(got, rt.Field(i).Name)
	}
	sort.Strings(got)
	sort.Strings(want)
	if !reflect.DeepEqual(got, want) {
		t.Errorf("config fields = %v; want %v", got, want)
	}
}

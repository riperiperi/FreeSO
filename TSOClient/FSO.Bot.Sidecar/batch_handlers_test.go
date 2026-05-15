/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"errors"
	"strings"
	"testing"

	"github.com/campfire-net/campfire/pkg/convention"
)

func mockOp(payload map[string]any, err error) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		if err != nil {
			return nil, err
		}
		return &convention.Response{Payload: payload}, nil
	}
}

func TestBatchBuild_EmptyOpsRejected(t *testing.T) {
	h := batchBuildHandlerWithMap(map[string]convention.HandlerFunc{})
	resp, _ := h(context.Background(), &convention.Request{Args: map[string]any{"ops": []any{}}})
	payload, _ := resp.Payload.(map[string]any)
	if ok, _ := payload["ok"].(bool); ok {
		t.Fatalf("empty ops should fail; got ok=true payload=%v", payload)
	}
	errMsg, _ := payload["error"].(string)
	if !strings.Contains(errMsg, "non-empty") {
		t.Errorf("error should mention 'non-empty'; got %q", errMsg)
	}
}

func TestBatchBuild_MissingOpsRejected(t *testing.T) {
	h := batchBuildHandlerWithMap(map[string]convention.HandlerFunc{})
	resp, _ := h(context.Background(), &convention.Request{Args: map[string]any{}})
	payload, _ := resp.Payload.(map[string]any)
	if ok, _ := payload["ok"].(bool); ok {
		t.Fatalf("missing ops should fail; got ok=true")
	}
}

func TestBatchBuild_UnknownOpRejected(t *testing.T) {
	calls := 0
	h := batchBuildHandlerWithMap(map[string]convention.HandlerFunc{
		"buy-object": mockOp(map[string]any{"ok": true, "verdict": "placed"}, nil),
		"counter": func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
			calls++
			return &convention.Response{Payload: map[string]any{"ok": true}}, nil
		},
	})
	resp, _ := h(context.Background(), &convention.Request{
		Args: map[string]any{"ops": []any{
			map[string]any{"op": "buy-object"},
			map[string]any{"op": "buy-object"},
			map[string]any{"op": "not-a-real-op"},
		}},
	})
	payload, _ := resp.Payload.(map[string]any)
	if ok, _ := payload["ok"].(bool); ok {
		t.Fatalf("unknown op should fail; got ok=true")
	}
	if calls != 0 {
		t.Errorf("pre-validation should run before any dispatch; got calls=%d", calls)
	}
	errMsg, _ := payload["error"].(string)
	if !strings.Contains(errMsg, "not-a-real-op") {
		t.Errorf("error should name the bad op; got %q", errMsg)
	}
}

func TestBatchBuild_AllSuccess(t *testing.T) {
	h := batchBuildHandlerWithMap(map[string]convention.HandlerFunc{
		"buy-object": mockOp(map[string]any{
			"ok": true, "verdict": "placed", "persist_id": 100,
		}, nil),
	})
	resp, _ := h(context.Background(), &convention.Request{
		Args: map[string]any{"ops": []any{
			map[string]any{"op": "buy-object", "guid": 0x17579980, "x": 160, "y": 240},
			map[string]any{"op": "buy-object", "guid": 0x17579980, "x": 160, "y": 256},
			map[string]any{"op": "buy-object", "guid": 0x17579980, "x": 160, "y": 272},
		}},
	})
	payload, _ := resp.Payload.(map[string]any)
	if ok, _ := payload["ok"].(bool); !ok {
		t.Fatalf("all-success batch should return ok=true; payload=%v", payload)
	}
	if count, _ := payload["count"].(int); count != 3 {
		t.Errorf("count=3 expected, got %v", payload["count"])
	}
	if _, halted := payload["halted_at"]; halted {
		t.Errorf("halted_at should be absent on full success; got %v", payload["halted_at"])
	}
	verdicts, _ := payload["verdicts"].([]any)
	if len(verdicts) != 3 {
		t.Fatalf("verdicts len=3 expected, got %d", len(verdicts))
	}
	for i, v := range verdicts {
		entry := v.(map[string]any)
		if idx, _ := entry["index"].(int); idx != i {
			t.Errorf("verdicts[%d].index = %v want %d", i, entry["index"], i)
		}
		if entry["op"] != "buy-object" {
			t.Errorf("verdicts[%d].op = %v want buy-object", i, entry["op"])
		}
		if entry["verdict"] != "placed" {
			t.Errorf("verdicts[%d].verdict = %v want placed", i, entry["verdict"])
		}
	}
}

func TestBatchBuild_StopOnFailure(t *testing.T) {
	thirdCalled := false
	verdictByCall := []map[string]any{
		{"ok": true, "verdict": "placed"},
		{"ok": true, "verdict": "silent-drop", "hints": []any{"tile-occupied"}},
	}
	callIdx := 0
	h := batchBuildHandlerWithMap(map[string]convention.HandlerFunc{
		"buy-object": func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
			if callIdx >= len(verdictByCall) {
				thirdCalled = true
				return &convention.Response{Payload: map[string]any{"ok": true, "verdict": "placed"}}, nil
			}
			p := verdictByCall[callIdx]
			callIdx++
			return &convention.Response{Payload: p}, nil
		},
	})
	resp, _ := h(context.Background(), &convention.Request{
		Args: map[string]any{"ops": []any{
			map[string]any{"op": "buy-object"},
			map[string]any{"op": "buy-object"},
			map[string]any{"op": "buy-object"},
		}},
	})
	payload, _ := resp.Payload.(map[string]any)
	if thirdCalled {
		t.Fatalf("third op should NOT run on stop-on-failure default")
	}
	if halted, _ := payload["halted_at"].(int); halted != 1 {
		t.Errorf("halted_at=1 expected, got %v", payload["halted_at"])
	}
	if reason, _ := payload["halt_reason"].(string); reason != "silent-drop" {
		t.Errorf("halt_reason=silent-drop expected, got %v", payload["halt_reason"])
	}
	verdicts, _ := payload["verdicts"].([]any)
	if len(verdicts) != 2 {
		t.Errorf("verdicts len=2 expected (success + failure), got %d", len(verdicts))
	}
}

func TestBatchBuild_ContinueOnFailure(t *testing.T) {
	verdictByCall := []map[string]any{
		{"ok": true, "verdict": "placed"},
		{"ok": true, "verdict": "silent-drop", "hints": []any{"tile-occupied"}},
		{"ok": true, "verdict": "placed"},
	}
	callIdx := 0
	h := batchBuildHandlerWithMap(map[string]convention.HandlerFunc{
		"buy-object": func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
			p := verdictByCall[callIdx]
			callIdx++
			return &convention.Response{Payload: p}, nil
		},
	})
	resp, _ := h(context.Background(), &convention.Request{
		Args: map[string]any{
			"ops": []any{
				map[string]any{"op": "buy-object"},
				map[string]any{"op": "buy-object"},
				map[string]any{"op": "buy-object"},
			},
			"continue_on_failure": true,
		},
	})
	payload, _ := resp.Payload.(map[string]any)
	if _, halted := payload["halted_at"]; halted {
		t.Errorf("halted_at should be absent with continue_on_failure=true")
	}
	verdicts, _ := payload["verdicts"].([]any)
	if len(verdicts) != 3 {
		t.Errorf("verdicts len=3 expected, got %d", len(verdicts))
	}
	if callIdx != 3 {
		t.Errorf("all 3 ops should have run; callIdx=%d", callIdx)
	}
}

func TestBatchBuild_OpStripped(t *testing.T) {
	var receivedArgs map[string]any
	h := batchBuildHandlerWithMap(map[string]convention.HandlerFunc{
		"buy-object": func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
			receivedArgs = req.Args
			return &convention.Response{Payload: map[string]any{"ok": true, "verdict": "placed"}}, nil
		},
	})
	_, _ = h(context.Background(), &convention.Request{
		Args: map[string]any{"ops": []any{
			map[string]any{"op": "buy-object", "guid": 391616896, "x": 160, "y": 240, "level": 1},
		}},
	})
	if _, hasOp := receivedArgs["op"]; hasOp {
		t.Errorf("'op' field should be stripped before forward; got %v", receivedArgs)
	}
	if receivedArgs["guid"] != 391616896 {
		t.Errorf("guid should pass through; got %v", receivedArgs["guid"])
	}
}

func TestBatchBuild_HandlerError(t *testing.T) {
	h := batchBuildHandlerWithMap(map[string]convention.HandlerFunc{
		"buy-object": mockOp(nil, errors.New("ipc timeout")),
	})
	resp, _ := h(context.Background(), &convention.Request{
		Args: map[string]any{"ops": []any{map[string]any{"op": "buy-object"}}},
	})
	payload, _ := resp.Payload.(map[string]any)
	if halted, _ := payload["halted_at"].(int); halted != 0 {
		t.Errorf("halted_at=0 expected, got %v", payload["halted_at"])
	}
	verdicts, _ := payload["verdicts"].([]any)
	entry := verdicts[0].(map[string]any)
	if ok, _ := entry["ok"].(bool); ok {
		t.Errorf("entry.ok should be false on handler error")
	}
	if e, _ := entry["error"].(string); e != "ipc timeout" {
		t.Errorf("error not propagated; got %v", entry["error"])
	}
}

func TestBatchBuild_VerdictlessSuccess(t *testing.T) {
	h := batchBuildHandlerWithMap(map[string]convention.HandlerFunc{
		"move-object": mockOp(map[string]any{"ok": true, "queued": true}, nil),
	})
	resp, _ := h(context.Background(), &convention.Request{
		Args: map[string]any{"ops": []any{map[string]any{"op": "move-object"}}},
	})
	payload, _ := resp.Payload.(map[string]any)
	if _, halted := payload["halted_at"]; halted {
		t.Errorf("verdictless ok=true response should be treated as success; got halt")
	}
}

func TestBatchBuild_AllowlistComposition(t *testing.T) {
	ops := buildBatchOps(nil)
	mustHave := []string{
		"buy-object", "place-from-inventory", "move-object", "delete-object",
		"send-to-inventory", "list-object-for-sale", "buy-listed-object", "upgrade-object",
		"place-wall", "paint-wall", "paint-floor", "paint-grass",
		"flatten-terrain", "raise-terrain", "set-roof",
		"change-environment", "change-lot-size",
	}
	for _, op := range mustHave {
		if _, ok := ops[op]; !ok {
			t.Errorf("batch allowlist missing %q", op)
		}
	}
	mustNotHave := []string{
		"search-catalog", "list-catalog-categories",
		"list-architecture-styles", "leave-build-buy",
	}
	for _, op := range mustNotHave {
		if _, ok := ops[op]; ok {
			t.Errorf("batch allowlist should NOT include read-only/non-mutating %q", op)
		}
	}
}

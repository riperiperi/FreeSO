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

// TestInstantMessageHandlerDispatchesIPC asserts the instant-message handler forwards the
// declared args (target_persist_id, text, color) to the bot via IPC with op="instant-message".
func TestInstantMessageHandlerDispatchesIPC(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{
			"queued":            true,
			"target_persist_id": float64(42),
			"self_target":       false,
			"text":              "hi",
			"length":            2,
			"ack_id":            "abc123",
		},
	})

	handler := instantMessageHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"target_persist_id": float64(42),
			"text":              "hi",
			"color":             float64(0xFF00FF00),
			// An arg not in the declaration's allow-list must NOT leak through.
			"spoof_from": float64(999),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	if resp == nil {
		t.Fatal("nil response")
	}
	cmd := <-gotCmd
	if cmd.Op != "instant-message" {
		t.Errorf("want op=instant-message got %q", cmd.Op)
	}
	if cmd.Args["target_persist_id"] != float64(42) {
		t.Errorf("target_persist_id not forwarded: %v", cmd.Args)
	}
	if cmd.Args["text"] != "hi" {
		t.Errorf("text not forwarded: %v", cmd.Args)
	}
	if cmd.Args["color"] != float64(0xFF00FF00) {
		t.Errorf("color not forwarded: %v", cmd.Args)
	}
	if _, bad := cmd.Args["spoof_from"]; bad {
		t.Errorf("spoof_from must NOT be forwarded (not in declaration): %v", cmd.Args)
	}
}

// TestIMDeclarationPresent asserts the instant-message declaration loads, targets the right
// family, and carries a galtrader-style description with at least two of Prerequisite/Effect/Cost.
func TestIMDeclarationPresent(t *testing.T) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}
	var d *convention.Declaration
	for _, x := range decls {
		if x.Operation == "instant-message" {
			d = x
			break
		}
	}
	if d == nil {
		t.Fatal("declaration for instant-message missing")
	}
	if d.Convention != "freeso-embodiment" {
		t.Errorf("convention=%q", d.Convention)
	}
	if d.Description == "" {
		t.Fatal("empty description")
	}
	lower := strings.ToLower(d.Description)
	hits := 0
	for _, kw := range []string{"prerequisite", "effect", "cost"} {
		if strings.Contains(lower, kw) {
			hits++
		}
	}
	if hits < 2 {
		t.Errorf("description must carry at least 2 of Prerequisite/Effect/Cost (got %d): %q", hits, d.Description)
	}
	// Required args sanity.
	seen := map[string]bool{}
	for _, a := range d.Args {
		seen[a.Name] = true
	}
	for _, need := range []string{"target_persist_id", "text"} {
		if !seen[need] {
			t.Errorf("declaration missing required arg %q", need)
		}
	}
}

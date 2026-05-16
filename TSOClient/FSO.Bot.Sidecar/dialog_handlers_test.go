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

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// TestRespondToDialogHandlerForwardsArgs asserts that a respond-to-dialog convention
// invocation produces an IPC command with op="respond-to-dialog" carrying dialog_id,
// response_kind, and integer_value. This is the outer Go-side veracity guarantee:
// the handler forwards args verbatim to the bot, which maps them to VMNetDialogResponseCmd.
func TestRespondToDialogHandlerForwardsArgs(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{
			"dialog_id":     "12345678",
			"response_kind": "integer",
			"response_code": 0,
			"response_text": "50",
		},
	})

	handler := respondToDialogHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	resp, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"dialog_id":     "12345678",
			"response_kind": "integer",
			"integer_value": float64(50),
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	if resp == nil {
		t.Fatal("nil response")
	}
	cmd := <-gotCmd
	if cmd.Op != "respond-to-dialog" {
		t.Errorf("want op=respond-to-dialog got %q", cmd.Op)
	}
	if cmd.Args["dialog_id"] != "12345678" {
		t.Errorf("dialog_id not forwarded: %v", cmd.Args)
	}
	if cmd.Args["response_kind"] != "integer" {
		t.Errorf("response_kind not forwarded: %v", cmd.Args)
	}
	if cmd.Args["integer_value"] != float64(50) {
		t.Errorf("integer_value not forwarded: %v", cmd.Args)
	}
	// string_value must NOT be in args when not provided.
	if _, bad := cmd.Args["string_value"]; bad {
		t.Error("string_value must not be forwarded when absent from request")
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil || payload["ok"] != true {
		t.Errorf("response payload missing ok=true: %v", resp.Payload)
	}
}

// TestRespondToDialogHandlerOkKind asserts response_kind=ok forwards correctly without
// integer_value or string_value.
func TestRespondToDialogHandlerOkKind(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{
			"dialog_id":     "999",
			"response_kind": "ok",
			"response_code": 0,
			"response_text": "",
		},
	})

	handler := respondToDialogHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	_, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"dialog_id":     "999",
			"response_kind": "ok",
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	cmd := <-gotCmd
	if cmd.Op != "respond-to-dialog" {
		t.Errorf("want op=respond-to-dialog got %q", cmd.Op)
	}
	if cmd.Args["response_kind"] != "ok" {
		t.Errorf("response_kind not forwarded: %v", cmd.Args)
	}
}

// TestRespondToDialogHandlerCancelKind asserts response_kind=cancel forwards correctly.
func TestRespondToDialogHandlerCancelKind(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	gotCmd := captureOneCommand(t, fake, ipc, map[string]any{
		"kind": "response", "ok": true,
		"payload": map[string]any{
			"dialog_id":     "42",
			"response_kind": "cancel",
			"response_code": 2,
			"response_text": "",
		},
	})

	handler := respondToDialogHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	_, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"dialog_id":     "42",
			"response_kind": "cancel",
		},
	})
	if err != nil {
		t.Fatalf("handler: %v", err)
	}
	cmd := <-gotCmd
	if cmd.Args["response_kind"] != "cancel" {
		t.Errorf("response_kind not forwarded: %v", cmd.Args)
	}
}

// TestRespondToDialogDeclarationPresent asserts the respond-to-dialog convention
// declaration exists in the embedded conventions/ directory and is well-formed.
func TestRespondToDialogDeclarationPresent(t *testing.T) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}
	var found *convention.Declaration
	for _, d := range decls {
		if d.Operation == "respond-to-dialog" {
			found = d
			break
		}
	}
	if found == nil {
		t.Fatal("respond-to-dialog declaration missing from conventions/")
	}
	if found.Convention != "freeso-embodiment" {
		t.Errorf("convention=%q want freeso-embodiment", found.Convention)
	}
	if found.Description == "" {
		t.Error("description must not be empty")
	}

	// Arg names: dialog_id, response_kind required; integer_value, string_value optional.
	argNames := make(map[string]bool)
	for _, a := range found.Args {
		argNames[a.Name] = true
	}
	for _, required := range []string{"dialog_id", "response_kind"} {
		if !argNames[required] {
			t.Errorf("declaration missing required arg %q", required)
		}
	}
	for _, optional := range []string{"integer_value", "string_value"} {
		if !argNames[optional] {
			t.Errorf("declaration missing optional arg %q", optional)
		}
	}
}

/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"fmt"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// RegisterDialogHandlers (freesoexperiment-849) wires the respond-to-dialog convention op.
//
// respond-to-dialog: the agent observed a dialog event in its perception stream
// (recent_events[] kind=='dialog') and now answers it by sending VMNetDialogResponseCmd
// through the bot's IPC channel.
//
// The op forwards dialog_id, response_kind, integer_value, and string_value to the C#
// bot handler (DialogHandlers.RespondToDialog), which maps response_kind to a
// ResponseCode byte and calls VMClientDriver.SendCommand(VMNetDialogResponseCmd).
//
// Declaration: conventions/respond-to-dialog.json
//
// Returns the count of registered ops (always 1 on success). A missing declaration is a
// hard error — this op is a freesoexperiment-849 deliverable and MUST ship with its declaration.
func RegisterDialogHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"respond-to-dialog": respondToDialogHandler(ipc),
	}

	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		return 0, fmt.Errorf("load declarations: %w", err)
	}
	byOp := make(map[string]*convention.Declaration, len(decls))
	for _, d := range decls {
		byOp[d.Operation] = d
	}

	started := 0
	for op, handler := range ops {
		decl, ok := byOp[op]
		if !ok {
			return started, fmt.Errorf("declaration for op %q missing (expected in conventions/%s.json)", op, op)
		}
		cf.Router.Register(decl, handler)
		started++
	}
	return started, nil
}

// respondToDialogHandler translates a respond-to-dialog convention invocation into an IPC
// command. Args forwarded verbatim (the bot validates them):
//
//	dialog_id      — string (uint64 encoded as string from perception.recent_events extras)
//	response_kind  — enum string: ok|cancel|integer|string
//	integer_value  — int, required when response_kind='integer'
//	string_value   — string, required when response_kind='string'
//
// The bot-side handler maps response_kind to VMNetDialogResponseCmd.ResponseCode (byte)
// and ResponseText per the existing VMDialogResult contract.
func respondToDialogHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "dialog_id", "response_kind", "integer_value", "string_value")
		return forwardIPC(ctx, ipc, "respond-to-dialog", args)
	}
}

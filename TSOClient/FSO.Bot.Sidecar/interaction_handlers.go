/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"fmt"

	"github.com/campfire-net/campfire/pkg/convention"
)

// RegisterInteractionHandlers (freesoexperiment-2a8) mirrors the movement-family
// pattern: one convention.Server per op, each emitting an IPC command to the
// C# bot and forwarding the bot's response as the convention fulfillment.
//
// Ops served:
//
//	interact-with       — VMNetInteractionCmd (queues a named pie-menu interaction)
//	cancel-interaction  — VMNetInteractionCancelCmd scoped to a specific action_uid
//	query-pie-menu      — local-VM introspection (no wire PDU)
//
// Returns the count of opened servers. A missing declaration is a hard error —
// these ops are a freesoexperiment-2a8 deliverable and MUST ship with their
// declarations.
func RegisterInteractionHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"interact-with":      interactWithHandler(ipc),
		"cancel-interaction": cancelInteractionHandler(ipc),
		"query-pie-menu":     queryPieMenuHandler(ipc),
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

// interactWithHandler — translates a convention invocation into an IPC command
// for the bot's InteractionHandlers.InteractWith, which emits VMNetInteractionCmd.
// Args: interaction (required), callee_id (required), param0, global.
func interactWithHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "interaction", "callee_id", "param0", "global", "queue_mode")
		return forwardIPC(ctx, ipc, "interact-with", args)
	}
}

// cancelInteractionHandler — scoped cancel by action_uid. The bot-side handler
// REQUIRES action_uid (unlike movement-family 'cancel' which sweeps broadly),
// so the sidecar just forwards verbatim and lets the bot enforce the contract.
func cancelInteractionHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "action_uid")
		return forwardIPC(ctx, ipc, "cancel-interaction", args)
	}
}

// queryPieMenuHandler — local-VM introspection; no outbound wire PDU. The bot
// computes the pie-menu under its tick lock and returns the interactions list
// as a sync response. Args: target_object_id | target_sim_id (one required),
// include_hidden, include_global.
func queryPieMenuHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "target_object_id", "target_sim_id", "include_hidden", "include_global")
		return forwardIPC(ctx, ipc, "query-pie-menu", args)
	}
}

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

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// RegisterMovementHandlers loads the movement-family convention declarations
// (walk-to, cancel) and wires a convention.Server per op that emits the
// corresponding IPC command to the bot and returns a convention.Response
// fulfillment.
//
// One Server per Declaration is the campfire framework's dispatch model
// (server.go: handlers map[string]HandlerFunc is per-Server, but
// dispatch only looks up s.decl.Operation). Each Server.Serve() runs in its
// own goroutine until ctx is cancelled.
//
// Returns the count of opened servers (zero if declarations are missing from
// the convention set, which we treat as a hard error — the movement verbs
// are a freesoexperiment-b9c deliverable and MUST be present).
func RegisterMovementHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"walk-to": walkToHandler(ipc),
		"cancel":  cancelHandler(ipc),
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

// walkToHandler translates a walk-to invocation into an IPC command and
// returns the bot's response payload as the convention.Response.
//
// Args forwarded directly (validated again client-side by the bot dispatcher):
//
//	x, y, level — absolute target
//	target_object_id — in-VM object to approach
//	target_sim_id — Sim persist_id to approach
//	interaction, param0 — optional PDU fields (default interaction=4 "Run Here")
//
// Cross-level navigation (freesoexperiment-81d): when target level != current
// level, the handler auto-detects stairs via query-nearby, queues a Climb-Stairs
// interact-with on the closest stair first (queue_mode=queue), then appends the
// original walk-to as a chained action (queue_mode=queue). If no stair is found,
// the handler refuses with reason=category:no-stair-path.
func walkToHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "x", "y", "level", "target_object_id", "target_sim_id", "interaction", "param0", "queue_mode")
		targetLevel, hasLevel := ExtractTargetLevel(args)
		if hasLevel && targetLevel > 0 {
			stair, err := findStairForCrossLevel(ctx, ipc, targetLevel, 0)
			if err != nil {
				return &convention.Response{
					Payload: map[string]any{
						"ok":     false,
						"reason": err.Error(),
						"error":  "cross-level navigation failed: " + err.Error(),
					},
				}, nil
			}
			if stair != nil {
				return queueStairThenDestination(ctx, ipc, stair, "walk-to", args)
			}
		}
		return forwardIPC(ctx, ipc, "walk-to", args)
	}
}

// cancelHandler translates cancel into VMNetInteractionCancelCmd. If
// action_uid is provided, cancels that specific queued action; otherwise the
// bot iterates every locally-visible queued action and cancels each.
func cancelHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "action_uid")
		return forwardIPC(ctx, ipc, "cancel", args)
	}
}

// forwardIPC is the common tail: send the command, translate the bot's
// response into a convention.Response. On bot-side failure (Ok=false), the
// fulfillment still goes out so the agent sees the failure — we do not
// convert to a transport error.
func forwardIPC(ctx context.Context, ipc *IPC, op string, args map[string]any) (*convention.Response, error) {
	resp, err := ipc.Send(ctx, op, args)
	if err != nil {
		return &convention.Response{
			Payload: map[string]any{"ok": false, "error": err.Error()},
		}, nil
	}
	// Pass bot payload through intact for the agent to inspect.
	var payload map[string]any
	if len(resp.Payload) > 0 {
		_ = json.Unmarshal(resp.Payload, &payload)
	}
	out := map[string]any{"ok": resp.Ok}
	if resp.Ok {
		out["payload"] = payload
	} else {
		out["error"] = resp.Error
	}
	return &convention.Response{
		Payload: out,
	}, nil
}

// pickArgs copies only the named keys from req.Args into a fresh map. This
// filters any convention-framework args (e.g. "message", "content") that the
// bot's dispatcher would reject with "unknown arg".
func pickArgs(in map[string]any, keys ...string) map[string]any {
	out := make(map[string]any, len(keys))
	for _, k := range keys {
		if v, ok := in[k]; ok {
			out[k] = v
		}
	}
	return out
}

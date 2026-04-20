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

// RegisterIMHandlers loads the IM-family convention declaration (instant-message only for now)
// and starts a convention.Server per op that forwards invocation to the bot via IPC.
//
// Pattern mirrors social_handlers.go / movement_handlers.go: one Server per Declaration, each
// served in its own goroutine until ctx is cancelled.
//
// The bot routes the InstantMessage PDU on the CITY Aries socket (not the lot socket). The
// server's MessagingHandler forwards successful IMs to the target's session with no SUCCESS_ACK
// back to the sender — the wire-level round-trip is observable instead via the perception
// stream's recent_events kind=im entries (incoming delivery or FAILURE_ACK).
func RegisterIMHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"instant-message": instantMessageHandler(ipc),
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

// instantMessageHandler translates instant-message invocations into a bot-IPC "instant-message"
// command. Required: target_persist_id, text. Optional: color. The bot writes the
// InstantMessage PDU on the city socket; wire-level delivery verification happens through the
// perception stream (inbound InstantMessage echo for self-IM, FAILURE_ACK for offline targets).
func instantMessageHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "target_persist_id", "text", "color")
		return forwardIPC(ctx, ipc, "instant-message", args)
	}
}

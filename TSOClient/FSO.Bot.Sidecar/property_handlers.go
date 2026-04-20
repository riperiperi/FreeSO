/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"time"
	"context"
	"fmt"
	"log"

	"github.com/campfire-net/campfire/pkg/convention"
)

// RegisterPropertyHandlers (freesoexperiment-9c5) wires the property verb family:
// add-roommate, evict-roommate, lock-lot, unlock-lot. Mirrors the movement/social
// per-op server pattern. Each handler is thin: it argument-picks, forwards to the
// C# bot over IPC, and returns the bot's correlated response as the convention
// fulfillment.
//
// pay-bills is NOT in this set. Per docs/design/verb-catalog.md:127, bills are
// automatic on quarter-day; no player action PDU exists. The UI's BillsButton is
// hard-disabled in UIHouseMode.cs:76. Dropping per item spec's "don't stub missing
// wire PDUs" rule.
//
// Owner gating is enforced in the C# bot handler (PropertyHandlers.cs CheckOwner),
// not here. The sidecar is a dumb forwarder — it has no VM view, no TSOState access,
// and no way to know who the owner is.
func RegisterPropertyHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"add-roommate":   simpleForwardingHandler(ipc, "add-roommate", "target_persist_id"),
		"evict-roommate": simpleForwardingHandler(ipc, "evict-roommate", "target_persist_id"),
		"lock-lot":       simpleForwardingHandler(ipc, "lock-lot"),
		"unlock-lot":     simpleForwardingHandler(ipc, "unlock-lot"),
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
		op := op // capture

		srv := convention.NewServer(cf.Client, decl).WithErrorHandler(func(err error) {

			log.Printf("handler[%s]: errFn: %v", op, err)

		}).WithPollInterval(10 * time.Second)
		srv.RegisterHandler(op, handler)
		go func(op string, srv *convention.Server) {
			log.Printf("handler[%s]: serving", op)
			// retry-on-subscription-drop: Serve exits cleanly on sqlite BUSY or similar;
			// keep restarting until ctx is cancelled.
			for {
				err := srv.Serve(ctx, cf.ID)
				if ctx.Err() != nil {
					break
				}
				if err != nil && err != context.Canceled {
					log.Printf("handler[%s]: serve err: %v (restarting)", op, err)
				} else {
					log.Printf("handler[%s]: serve returned (restarting)", op)
				}
				// small backoff so we don't hot-spin on a persistent failure
				select {
				case <-ctx.Done():
					return
				case <-time.After(200 * time.Millisecond):
				}
			}
			log.Printf("handler[%s]: stopped", op)
		}(op, srv)
		started++
	}
	return started, nil
}

// simpleForwardingHandler is a generic pick-and-forward handler. It takes a
// whitelist of arg names to pass through (variadic) and forwards to the given
// IPC op. Used for property-family ops because each is a thin argument-pass
// with no sidecar-side logic beyond forwarding.
func simpleForwardingHandler(ipc *IPC, op string, allowedArgs ...string) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, allowedArgs...)
		return forwardIPC(ctx, ipc, op, args)
	}
}

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

// RegisterBuildModeHandlers (freesoexperiment-41d) wires the build-buy-architecture verb
// family: place-wall, paint-wall, paint-floor, paint-grass, flatten-terrain, raise-terrain,
// set-roof, change-environment, change-lot-size, leave-build-buy. All thin argument-pass
// forwarding — the sidecar has no VM view, so owner gating lives in the C# handler
// (BuildModeHandlers.CheckLotOwner under RunUnderTickLock).
//
// PDU mapping (see BuildModeHandlers.cs for detail):
//   - six verbs ride VMNetArchitectureCmd (walls / paint / terrain / grass)
//   - set-roof           → VMNetSetRoofCmd
//   - change-environment → VMNetChangeEnvironmentCmd
//   - change-lot-size    → VMNetChangeLotSizeCmd
//   - leave-build-buy    → VMNetLeaveBuildBuyCmd
//
// Gating note: server-side Verify() enforces CanBuildTool (BuildBuyRoommate+ for most arch
// ops) and Owner (for change-lot-size and set-roof). The bot handler gates owner-only at
// the handler level for deterministic refuse — the server silently drops unauthorized
// commands (no negative-ACK) so a bot-side gate is the only way the agent gets a
// synchronous error.
func RegisterBuildModeHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"place-wall": simpleForwardingHandler(ipc, "place-wall",
			"x", "y", "level", "length", "direction", "pattern", "style", "remove"),
		"paint-wall": simpleForwardingHandler(ipc, "paint-wall",
			"x", "y", "level", "side", "pattern"),
		"paint-floor": simpleForwardingHandler(ipc, "paint-floor",
			"x", "y", "level", "width", "height", "pattern", "fill"),
		"paint-grass": simpleForwardingHandler(ipc, "paint-grass",
			"x", "y", "level", "pattern"),
		"flatten-terrain": simpleForwardingHandler(ipc, "flatten-terrain",
			"x", "y", "level", "width", "height"),
		"raise-terrain": simpleForwardingHandler(ipc, "raise-terrain",
			"x", "y", "level", "delta"),
		"set-roof": simpleForwardingHandler(ipc, "set-roof",
			"style", "pitch"),
		"change-environment": simpleForwardingHandler(ipc, "change-environment",
			"guids_to_add", "guids_to_clear"),
		"change-lot-size": simpleForwardingHandler(ipc, "change-lot-size",
			"lot_size", "lot_stories"),
		"leave-build-buy": simpleForwardingHandler(ipc, "leave-build-buy",
			"build"),
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

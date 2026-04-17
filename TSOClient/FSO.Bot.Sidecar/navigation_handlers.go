/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"fmt"
	"log"

	"github.com/campfire-net/campfire/pkg/convention"
)

// RegisterNavigationHandlers wires the navigation verb family
// (freesoexperiment-a61). Ops served:
//
//	go-home     — already-home check + deferred cross-lot transition
//	visit-lot   — deferred (HeadlessLotConnection has no reconnect primitive yet)
//	find-avatar — full wire PDU (FindAvatarRequest → FindAvatarResponse) on the
//	              city socket
//
// Dropped per catalog: find-lot — docs/design/verb-catalog.md says "do not
// declare as a separate convention op — subsumed by visit-lot". No
// convention JSON, no handler (item constraint: "If d87-0's catalog
// determines an op in this family has no wire path, DROP the op").
//
// Deferral honesty (per wave-2b standards): go-home and visit-lot emit
// `deferred:true` in the response payload when they cannot perform the real
// wire effect. The integration test hard-asserts presence of the marker —
// never "accept ok=true OR ok=false as evidence of success". A follow-up item
// (freesoexperiment-ca0) tracks the lot-transition implementation.
//
// Returns the count of opened servers.
func RegisterNavigationHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"go-home":     goHomeHandler(ipc),
		"visit-lot":   visitLotHandler(ipc),
		"find-avatar": findAvatarHandler(ipc),
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
		srv := convention.NewServer(cf.Client, decl)
		srv.RegisterHandler(op, handler)
		go func(op string, srv *convention.Server) {
			log.Printf("handler[%s]: serving", op)
			if err := srv.Serve(ctx, cf.ID); err != nil && err != context.Canceled {
				log.Printf("handler[%s]: serve err: %v", op, err)
			}
			log.Printf("handler[%s]: stopped", op)
		}(op, srv)
		started++
	}
	return started, nil
}

// goHomeHandler forwards the go-home query to the bot. No args. Bot-side
// handler compares TSOState.LotID against the avatar's home LotLocation and
// returns {already_home, current_lot_location, home_lot_location} — plus
// {deferred:true, deferred_reason} when the avatar is on a different lot and
// the cross-lot transition path is not yet implemented.
func goHomeHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		return forwardIPC(ctx, ipc, "go-home", map[string]any{})
	}
}

// visitLotHandler forwards the visit-lot request. Arg: target_lot_location
// (required; location code — NOT a DB lot_id; hex string "0x..." or decimal
// uint). The bot currently returns `ok:true, deferred:true` because
// HeadlessLotConnection lacks a reconnect primitive; agents should branch on
// the deferred marker. When the a61-transition follow-up lands, the same
// wire contract (ok=true + non-deferred payload) becomes populated without
// a client-visible shape change.
func visitLotHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "target_lot_location")
		return forwardIPC(ctx, ipc, "visit-lot", args)
	}
}

// findAvatarHandler forwards the find-avatar query. Arg: target_avatar_id
// (required; uint DB id — query-nearby's persist_id or any known avatar id).
// Bot sends FindAvatarRequest on the city socket, awaits FindAvatarResponse,
// returns {status, on_lot, lot_location, lot_location_raw, target_avatar_id}.
func findAvatarHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "target_avatar_id")
		return forwardIPC(ctx, ipc, "find-avatar", args)
	}
}

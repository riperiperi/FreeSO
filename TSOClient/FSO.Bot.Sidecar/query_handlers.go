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

// RegisterQueryHandlers wires the queries verb family (freesoexperiment-e9f):
// query-self, query-nearby, query-lot, query-relationships, query-inventory.
//
// Every op is a pull-on-demand local-VM query on the bot side — no outbound
// PDU. The sidecar forwards the invocation through IPC and relays the bot's
// structured payload back as a convention.Response. Response shapes are
// stable and documented in each handler's doc comment; agents can consume
// them without knowing the underlying VM schema.
//
// Same serve-goroutine pattern as RegisterMovementHandlers — one
// convention.Server per op, each running until ctx cancels.
func RegisterQueryHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"query-self":          querySelfHandler(ipc),
		"query-nearby":        queryNearbyHandler(ipc),
		"query-lot":           queryLotHandler(ipc),
		"query-relationships": queryRelationshipsHandler(ipc),
		"query-inventory":     queryInventoryHandler(ipc),
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

// querySelfHandler forwards a parameter-less self-snapshot query. The bot
// returns a payload containing: persist_id, name, shard, position{x,y,level,direction},
// animation_raw, action_queue[], motives{hunger,comfort,energy,...}, skills{...},
// balance. Pure projection of the avatar's current VM state.
func querySelfHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		return forwardIPC(ctx, ipc, "query-self", map[string]any{})
	}
}

// queryNearbyHandler forwards the nearby-entities query. Optional arg:
// radius_tiles (default 20). Returns {nearby_objects:[], nearby_sims:[],
// radius_tiles}.
func queryNearbyHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "radius_tiles")
		return forwardIPC(ctx, ipc, "query-nearby", args)
	}
}

// queryLotHandler forwards the lot-metadata query. No args. Returns lot_id,
// name, shard, owner_is_me, owner_id, roommates[], build_roommates[],
// category_id, category, size_tiles, floors, neighborhood_id, other_avatars.
// Pulled from VMTSOLotState — no PDU.
func queryLotHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		return forwardIPC(ctx, ipc, "query-lot", map[string]any{})
	}
}

// queryRelationshipsHandler forwards the relationship-dump query. No args.
// Returns {relationships: [{with_persist_id, with_name, friendly, daily}, ...]}.
// Keyed off VMAvatar.MeToObject (the same data the perception stream uses).
func queryRelationshipsHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		return forwardIPC(ctx, ipc, "query-relationships", map[string]any{})
	}
}

// queryInventoryHandler forwards the inventory query. Returns
// {items:[], deferred:true, deferred_reason:"..."} because DataService inventory
// feed is not wired into FSO.Server.Clients (tracked as freesoexperiment-1a9).
// Shape is stable so agents can poll this now; when -1a9 closes, items[] starts
// populating without a contract change.
func queryInventoryHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		return forwardIPC(ctx, ipc, "query-inventory", map[string]any{})
	}
}

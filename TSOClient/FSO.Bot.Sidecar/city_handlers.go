/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"fmt"
	"strings"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// RegisterCityHandlers (freesoexperiment-ded) wires the city verb family:
// view-bulletin, post-bulletin, vote, nominate, view-neighborhood. All five
// ride the city Aries socket (not the lot socket); all are thin argument-pass
// forwarding handlers like property/im. The C# bot owns correlation (FIFO for
// bulletin/nhood — they have no wire correlator; SendingAvatarID for
// DataServiceWrapperPDU). The sidecar has no city-side state.
//
// Election-gated ops (vote, nominate) deterministically refuse with
// ELECTION_OVER on workshop's current DB state (no active election_cycle_id).
// That IS the test's wire-level-effect verification lever.
//
// vote and nominate support name-primary resolution (freesoexperiment-17f,
// I0-5): --target_avatar_name resolves against the naming store (kind=sim)
// and overrides --target_persist_id. This keeps civic ops consistent with
// go-to / visit-lot name-primary convention.
func RegisterCityHandlers(ctx context.Context, cf *Campfire, ipc *IPC, store *MemoryStore) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"view-bulletin":     simpleForwardingHandler(ipc, "view-bulletin", "neighborhood_id"),
		"post-bulletin":     simpleForwardingHandler(ipc, "post-bulletin", "subject", "body", "neighborhood_id", "lot_id"),
		"vote":              voteHandler(ipc, store),
		"nominate":          nominateHandler(ipc, store),
		"view-neighborhood": simpleForwardingHandler(ipc, "view-neighborhood", "neighborhood_id"),
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

// voteHandler: name-primary vote (freesoexperiment-17f, I0-5).
//
// Resolution priority:
//  1. If --target_avatar_name is present: resolve in naming store (must be kind=sim).
//     Resolved persist_id overwrites target_persist_id in the forwarded args.
//  2. Otherwise: use --target_persist_id directly (hex or decimal).
//
// Returns ok:false immediately if target_avatar_name is supplied but unresolvable.
// Returns ok:false if neither target_avatar_name nor target_persist_id is provided.
// Forwards {target_persist_id, neighborhood_id} to the bot on success.
func voteHandler(ipc *IPC, store *MemoryStore) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args, resolveErr := resolveElectionTarget(req.Args, store, "vote")
		if resolveErr != nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": resolveErr.Error()},
			}, nil
		}
		return forwardIPC(ctx, ipc, "vote", args)
	}
}

// nominateHandler: name-primary nominate (freesoexperiment-17f, I0-5).
// Same resolution logic as voteHandler.
func nominateHandler(ipc *IPC, store *MemoryStore) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args, resolveErr := resolveElectionTarget(req.Args, store, "nominate")
		if resolveErr != nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": resolveErr.Error()},
			}, nil
		}
		return forwardIPC(ctx, ipc, "nominate", args)
	}
}

// resolveElectionTarget resolves the target for election ops (vote, nominate)
// applying the I0-5 name-primary invariant.
//
// If target_avatar_name is present: look it up in the naming store. Entry must
// exist and have kind="sim" with a non-zero target_sim_id. The resolved value
// is placed into target_persist_id in the returned args map.
//
// If target_avatar_name is absent: use target_persist_id directly.
//
// Returns ok args map (target_persist_id + neighborhood_id) or an error.
func resolveElectionTarget(inArgs map[string]any, store *MemoryStore, opName string) (map[string]any, error) {
	out := make(map[string]any)

	// Forward neighborhood_id if present.
	if v, ok := inArgs["neighborhood_id"]; ok {
		out["neighborhood_id"] = v
	}

	// Step 1: attempt name-primary resolution.
	if avatarName, ok := inArgs["target_avatar_name"].(string); ok && strings.TrimSpace(avatarName) != "" {
		avatarName = strings.TrimSpace(avatarName)
		entry := lookupName(store, avatarName)
		if entry == nil {
			return nil, fmt.Errorf("%s: target_avatar_name %q is not bound in the naming store — use 'name --as %q --target_sim_id <persist_id>' to bind it first", opName, avatarName, avatarName)
		}
		kind, _ := entry["kind"].(string)
		if kind != "sim" {
			return nil, fmt.Errorf("%s: target_avatar_name %q has kind %q, not \"sim\" — election ops require a sim name binding (use 'name --as %q --target_sim_id <persist_id>')", opName, avatarName, kind, avatarName)
		}
		persistID, ok := numericArg(entry, "target_sim_id")
		if !ok || persistID == 0 {
			return nil, fmt.Errorf("%s: target_avatar_name %q is a sim binding but has no valid target_sim_id — re-bind with 'name --as %q --target_sim_id <persist_id>'", opName, avatarName, avatarName)
		}
		out["target_persist_id"] = persistID
		return out, nil
	}

	// Step 2: fallback to target_persist_id.
	persistIDRaw, ok := inArgs["target_persist_id"]
	if !ok || persistIDRaw == nil {
		return nil, fmt.Errorf("%s: one of --target_avatar_name or --target_persist_id is required", opName)
	}
	persistID, ok := numericArg(inArgs, "target_persist_id")
	if !ok || persistID == 0 {
		return nil, fmt.Errorf("%s: target_persist_id must be a non-zero integer", opName)
	}
	out["target_persist_id"] = persistID
	return out, nil
}

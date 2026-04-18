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

// RegisterBuyModeHandlers (freesoexperiment-304) wires the build-buy-catalog verb family:
// buy-object, place-from-inventory, move-object, delete-object, send-to-inventory,
// list-object-for-sale, buy-listed-object, upgrade-object. All thin argument-pass
// forwarding — the sidecar has no VM view, so owner/target gating lives in the C# handler.
//
// Plus find-cheap-catalog-guid, a test-support helper (not in the verb catalog) that reads
// the bot's local Content.WorldCatalog and returns the cheapest user-placeable objects.
// Used by the integration test to pick a throwaway GUID at runtime without hardcoding one.
//
// OQ-8 (docs/design/verb-catalog.md:145): there is no build/buy mode-entry PDU. Gating is
// server-side inside each VMNet*Cmd.Verify() via PlatformState.Validator.GetPurchaseMode.
// On denial the server silently drops the command (no error PDU back) — the agent observes
// the effect (or absence) via the next perception frame.
func RegisterBuyModeHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"buy-object": simpleForwardingHandler(ipc, "buy-object",
			"guid", "x", "y", "level", "dir", "mode", "target_upgrade_level"),
		"place-from-inventory": simpleForwardingHandler(ipc, "place-from-inventory",
			"object_persist_id", "x", "y", "level", "dir", "mode"),
		"move-object": simpleForwardingHandler(ipc, "move-object",
			"target_object_id", "x", "y", "level", "dir"),
		"delete-object": simpleForwardingHandler(ipc, "delete-object",
			"target_object_id", "cleanup_all"),
		"send-to-inventory": simpleForwardingHandler(ipc, "send-to-inventory",
			"target_object_persist_id"),
		"list-object-for-sale": simpleForwardingHandler(ipc, "list-object-for-sale",
			"target_object_persist_id", "new_price"),
		"buy-listed-object": simpleForwardingHandler(ipc, "buy-listed-object",
			"target_object_persist_id"),
		"upgrade-object": simpleForwardingHandler(ipc, "upgrade-object",
			"target_object_persist_id", "target_upgrade_level"),
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

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

// RegisterBuyModeHandlers (freesoexperiment-304) wires the build-buy-catalog verb family:
// buy-object, place-from-inventory, move-object, delete-object, send-to-inventory,
// list-object-for-sale, buy-listed-object, upgrade-object. All thin argument-pass
// forwarding — the sidecar has no VM view, so owner/target gating lives in the C# handler.
//
// Also: list-catalog-categories (category index, no args) and search-catalog
// (freesoexperiment-281a: name/category/tier/min_price/max_price/limit filters; tier bins
// computed at bot boot from live catalog P33/P67; limit clamped server-side to 200).
//
// OQ-8 (docs/design/verb-catalog.md:145): there is no build/buy mode-entry PDU. Gating is
// server-side inside each VMNet*Cmd.Verify() via PlatformState.Validator.GetPurchaseMode.
// On denial the server silently drops the command (no error PDU back) — the agent observes
// the effect (or absence) via the next perception frame.
func RegisterBuyModeHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := BuyModeOps(ipc)

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

// BuyModeOps returns the build-buy-catalog op handler map. Extracted so
// batch-build (freesoexperiment-e5e) can dispatch into the same handler set
// without re-implementing the verifying wrappers — every entry in a batch
// gets the same structured verdict (placed/silent-drop/bot-rejected, hints,
// balance_before/after) the single-op call would return.
func BuyModeOps(ipc *IPC) map[string]convention.HandlerFunc {
	return map[string]convention.HandlerFunc{
		"list-catalog-categories": simpleForwardingHandler(ipc, "list-catalog-categories"),
		"search-catalog": simpleForwardingHandler(ipc, "search-catalog",
			"name", "category", "tier", "min_price", "max_price", "limit", "guid_hex"),
		"buy-object": placementVerifyingHandler(ipc, "buy-object",
			"guid", "x", "y", "level", "dir", "mode", "target_upgrade_level"),
		"place-from-inventory": placementVerifyingHandler(ipc, "place-from-inventory",
			"object_persist_id", "x", "y", "level", "dir", "mode"),
		"move-object": simpleForwardingHandler(ipc, "move-object",
			"target_object_id", "x", "y", "level", "dir"),
		"delete-object": deleteVerifyingHandler(ipc, "delete-object",
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
}

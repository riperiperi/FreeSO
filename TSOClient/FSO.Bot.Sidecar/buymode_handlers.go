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
	ops := map[string]convention.HandlerFunc{
		"list-catalog-categories": simpleForwardingHandler(ipc, "list-catalog-categories"),
		// search-catalog (freesoexperiment-281a): optional filter args forwarded as-is to
		// the C# handler which applies them. All args are optional — the sidecar is a strict
		// forwarder; filtering logic lives in BuyModeHandlers.FindCheapCatalogGuid.
		"search-catalog": simpleForwardingHandler(ipc, "search-catalog",
			"name", "category", "tier", "min_price", "max_price", "limit"),
		// buy-object & place-from-inventory ride placementVerifyingHandler so
		// callers get a structured {placed, persist_id, cost, hints} verdict
		// instead of the ok:true / queued:true ack that conflates "bot received"
		// with "VM placed" (OQ-8 silent-drop, see verifying_handler.go).
		"buy-object": placementVerifyingHandler(ipc, "buy-object",
			"guid", "x", "y", "level", "dir", "mode", "target_upgrade_level"),
		"place-from-inventory": placementVerifyingHandler(ipc, "place-from-inventory",
			"object_persist_id", "x", "y", "level", "dir", "mode"),
		"move-object": simpleForwardingHandler(ipc, "move-object",
			"target_object_id", "x", "y", "level", "dir"),
		// delete-object rides deleteVerifyingHandler to (a) surface a structured
		// {deleted:true|false} verdict instead of the silent OQ-8 ack, and
		// (b) auto-retry against a subordinate tile when the caller targets a
		// multitile master (master-tile no-op, freesoexperiment-850).
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

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
	"log"

	"github.com/campfire-net/campfire/pkg/convention"
)

// RegisterNavigationHandlers wires the navigation verb family
// (freesoexperiment-a61 / freesoexperiment-e66). Ops served:
//
//	go-home     — already-home check + deferred cross-lot transition
//	visit-lot   — name-primary with hex fallback (freesoexperiment-e66):
//	              resolves --my_name → lot_location, probes via bot-cmd:probe-lot,
//	              writes next-lot, then issues bot-cmd:exit to trigger supervised
//	              relaunch at the target lot.
//	find-avatar — full wire PDU (FindAvatarRequest → FindAvatarResponse) on the
//	              city socket
//
// Dropped per catalog: find-lot — docs/design/verb-catalog.md says "do not
// declare as a separate convention op — subsumed by visit-lot". No
// convention JSON, no handler (item constraint: "If d87-0's catalog
// determines an op in this family has no wire path, DROP the op").
//
// Returns the count of opened servers.
func RegisterNavigationHandlers(ctx context.Context, cf *Campfire, ipc *IPC, botCmds *BotCmdPump, store *MemoryStore) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"go-home":     goHomeHandler(ipc),
		"visit-lot":   visitLotHandler(ipc, botCmds, store),
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
		cf.Router.Register(decl, handler)
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

// visitLotHandler implements the visit-lot convention with name-primary
// resolution and hex fallback (freesoexperiment-e66, I0-5).
//
// Wire flow (Q3, cross-lot-first-class.md §Q3):
//  1. Resolve target_lot_location:
//     a. If --my_name is present: look up in the naming store. Entry must have
//        kind=="lot" with a non-empty lot_location field. If not found or wrong
//        kind → ok:false.
//     b. Otherwise: use --target_lot_location directly (hex "0x..." or decimal).
//  2. Issue bot-cmd:probe-lot to verify the target lot is reachable.
//     FOUND → proceed. NOT_OPEN / error → ok:false (caller can retry later).
//  3. WriteNextLot(lotLocation) — supervisor reads this AFTER bot exit.
//     INVARIANT: WriteNextLot must complete before bot-cmd:exit is dispatched.
//     The supervisor reads next-lot after bot exit; an async write or
//     post-exit write means the supervisor sees stale/missing state and
//     the cross-lot transition silently uses the default lot.
//  4. Issue bot-cmd:bot-exit-request. Bot disconnects cleanly; supervisor
//     detects exit-0, reads next-lot, relaunches at target lot.
//
// Both --my_name and --target_lot_location call shapes are mandatory (I0-5,
// wire-protocol contract). Dropping either shape is escalation-required.
func visitLotHandler(ipc *IPC, botCmds *BotCmdPump, store *MemoryStore) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		// Step 1: resolve lot_location.
		lotLocation, resolveErr := resolveLotLocation(req.Args, store)
		if resolveErr != nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": resolveErr.Error()},
			}, nil
		}

		// Step 2: probe the target lot to confirm it is reachable.
		// lot_location is a decimal string; convert to uint32 for the probe-lot arg.
		var lotLocUint uint64
		if _, err := fmt.Sscanf(lotLocation, "%d", &lotLocUint); err != nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": fmt.Sprintf("parse lot_location %q: %v", lotLocation, err)},
			}, nil
		}

		// If botCmds is nil (no bot subprocess, e.g. --no-bot mode), fall back
		// gracefully with a deferred marker rather than panicking.
		if botCmds == nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":              false,
					"error":           "visit-lot: bot-cmd channel not available (--no-bot mode or botCmds not wired)",
					"deferred":        true,
					"lot_location":    lotLocation,
				},
			}, nil
		}

		probeReply, probeErr := botCmds.Send(ctx, "probe-lot", map[string]any{
			"lot_location": lotLocUint,
		})
		if probeErr != nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": fmt.Sprintf("probe-lot failed: %v", probeErr)},
			}, nil
		}
		if !probeReply.Ok {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": fmt.Sprintf("probe-lot error: %s", probeReply.Error)},
			}, nil
		}
		// Parse status and lot_id from probe reply.
		var probeData struct {
			Status string `json:"status"`
			LotID  uint32 `json:"lot_id"`
		}
		if len(probeReply.Data) > 0 {
			if err := json.Unmarshal(probeReply.Data, &probeData); err != nil {
				log.Printf("visit-lot: probe-lot data parse error: %v (treating as UNKNOWN)", err)
			}
		}
		if probeData.Status != "FOUND" {
			return &convention.Response{
				Payload: map[string]any{
					"ok":           false,
					"error":        fmt.Sprintf("lot not reachable: probe status=%q", probeData.Status),
					"probe_status": probeData.Status,
					"lot_location": lotLocation,
				},
			}, nil
		}

		// Step 3: INVARIANT — WriteNextLot must complete before bot-cmd:exit.
		// Server-side LotAdmit / LotRefuse is the access gate for community lots
		// (freesoexperiment-f34: dropped sidecar-local community-access.json gate).
		// The supervisor reads next-lot after bot exit; writing after exit means
		// stale/missing state → cross-lot transition silently uses the default lot.
		if err := WriteNextLot(lotLocation); err != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("write next-lot: %v", err),
				},
			}, nil
		}

		// Step 4: issue bot-exit-request. Bot disconnects cleanly; supervisor
		// detects exit-0, reads next-lot, relaunches at target lot.
		exitReply, exitErr := botCmds.Send(ctx, "bot-exit-request", nil)
		if exitErr != nil {
			// next-lot is already written; supervisor will still pick it up when
			// the bot eventually exits (or is killed). Log but return ok:true so
			// the agent doesn't retry the whole sequence.
			log.Printf("visit-lot: bot-exit-request error (next-lot written, supervisor will handle): %v", exitErr)
			return &convention.Response{
				Payload: map[string]any{
					"ok":           true,
					"lot_location": lotLocation,
					"note":         "bot-exit-request timed out; transition queued via next-lot",
				},
			}, nil
		}
		if !exitReply.Ok {
			log.Printf("visit-lot: bot-exit-request rejected: %s (next-lot written)", exitReply.Error)
		}

		return &convention.Response{
			Payload: map[string]any{
				"ok":           true,
				"lot_location": lotLocation,
				"probe_status": probeData.Status,
			},
		}, nil
	}
}

// resolveLotLocation resolves the target lot location from request args.
// Priority: --my_name (naming store, kind=="lot") → --target_lot_location.
// Returns a canonical decimal uint32 string on success, or an error.
func resolveLotLocation(args map[string]any, store *MemoryStore) (string, error) {
	if myName, ok := args["my_name"].(string); ok && myName != "" {
		entry := lookupName(store, myName)
		if entry == nil {
			return "", fmt.Errorf("name %q is not bound (use list-names to see current bindings, or 'name --as %q --lot_location 0x<hex>' to bind it)", myName, myName)
		}
		kind, _ := entry["kind"].(string)
		if kind != "lot" {
			return "", fmt.Errorf("name %q has kind %q, not \"lot\" — visit-lot requires a lot binding (use 'name --as %q --lot_location 0x<hex>')", myName, kind, myName)
		}
		ll, _ := entry["lot_location"].(string)
		if ll == "" {
			return "", fmt.Errorf("name %q has kind=\"lot\" but missing lot_location (re-bind with 'name --as %q --lot_location 0x<hex>')", myName, myName)
		}
		return ll, nil
	}

	// Fallback: --target_lot_location (hex or decimal string).
	rawLL, ok := args["target_lot_location"]
	if !ok || rawLL == nil {
		return "", fmt.Errorf("one of --my_name or --target_lot_location is required")
	}
	ll, err := coerceLotLocation(rawLL)
	if err != nil {
		return "", fmt.Errorf("target_lot_location: %w", err)
	}
	return ll, nil
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

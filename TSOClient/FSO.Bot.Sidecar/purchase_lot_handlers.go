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
	"time"

	"github.com/campfire-net/campfire/pkg/convention"
)

// RegisterPurchaseLotHandlers wires the purchase-lot convention op
// (freesoexperiment-eaa). Implements the full purchase flow:
//
//  1. Pre-check: bot-cmd:probe-road validates that the target tile has road
//     bits (CLAUDE.md gotcha 12: (GetRoad(x,y) & 0xF) != 0 required).
//  2. Pre-check: sidecar reads owned-lots.json; if non-empty and allow_move
//     is not true, refuse ALREADY_OWNS (prevents silent §§§ double-purchase).
//  3. Dispatch bot-cmd:purchase-lot → C# issues PurchaseLotRequest on the
//     city Aries socket, awaits PurchaseLotResponse, replies with
//     {ok, lot_id, new_funds, status, reason}.
//  4. On success: update owned-lots.json with the new entry (I0-7 habitation
//     block zeroed — body must eat/sleep/use-toilet to mark habitable).
//
// ESCALATION RISK: if Alphaville's fso_neighborhoods has flag & 1 set
// (NHOOD_RESERVED), every non-mayor purchase returns NHOOD_RESERVED from the
// server. The bot handler surfaces that reason verbatim; the agent (or
// orchestrator) must detect it and escalate so an operator can clear the flag.
// Do NOT silently retry — NHOOD_RESERVED is a permanent gate until cleared.
//
// No direct DB queries here — the sidecar has no DB credentials. All state
// mutations go through the C# bot (road-bits validation, PurchaseLotRequest)
// or persona-state files (owned-lots.json).
func RegisterPurchaseLotHandlers(ctx context.Context, cf *Campfire, botCmds *BotCmdPump) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"purchase-lot": purchaseLotHandler(botCmds),
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

// purchaseLotHandler implements the purchase-lot convention.
//
// Required args:
//   - target_lot_location (hex "0x..." or decimal uint32): the packed tile
//     coordinate. MapCoordinates.Pack(x,y) = x<<16|y per CLAUDE.md gotcha 11.
//   - name (string): lot name, validated by the server (max 64 chars).
//
// Optional args:
//   - start_fresh (bool, default false): pass to C# — ignored by most purchases
//     but present for mayor-mode community lot placement.
//   - allow_move (bool, default false): if true, skip the ALREADY_OWNS check.
//     Set when the agent explicitly wants to re-purchase / move.
func purchaseLotHandler(botCmds *BotCmdPump) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := req.Args

		// --- Step 1: resolve and validate target_lot_location ---
		rawLL, ok := args["target_lot_location"]
		if !ok || rawLL == nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": "target_lot_location required (hex 0x... or decimal uint32)"},
			}, nil
		}
		locationStr, err := coerceLotLocation(rawLL)
		if err != nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": fmt.Sprintf("target_lot_location: %v", err)},
			}, nil
		}

		// Decode X,Y from the packed location (x = high 16 bits, y = low 16 bits).
		// CLAUDE.md gotcha 11: MapCoordinates.Pack(x,y) = x<<16|y
		var loc uint64
		if _, serr := fmt.Sscanf(locationStr, "%d", &loc); serr != nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": fmt.Sprintf("parse location %q: %v", locationStr, serr)},
			}, nil
		}
		locX := uint16((loc >> 16) & 0xFFFF)
		locY := uint16(loc & 0xFFFF)

		// --- Step 2: validate lot name ---
		name, _ := args["name"].(string)
		if name == "" {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": "name required"},
			}, nil
		}
		if len(name) > 64 {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": "name > 64 chars (server cap)"},
			}, nil
		}

		// --- Step 3: optional args ---
		startFresh := false
		if v, ok := args["start_fresh"].(bool); ok {
			startFresh = v
		}
		allowMove := false
		if v, ok := args["allow_move"].(bool); ok {
			allowMove = v
		}

		// --- Step 4: ALREADY_OWNS pre-check ---
		if !allowMove {
			existing, rerr := ReadOwnedLots()
			if rerr != nil {
				log.Printf("purchase-lot: read owned-lots.json: %v (treating as empty)", rerr)
			} else if len(existing) > 0 {
				// Agent already owns a lot. Return ALREADY_OWNS refuse so the
				// agent can decide whether to use allow_move=true (which triggers
				// server-side move cost) or visit the existing lot instead.
				ownerOf := existing[0].LocationHex
				if len(existing) > 0 {
					ownerOf = existing[len(existing)-1].LocationHex
				}
				return &convention.Response{
					Payload: map[string]any{
						"ok":                false,
						"error":             "ALREADY_OWNS",
						"reason":            "ALREADY_OWNS",
						"owned_lot_location": ownerOf,
						"hint":              "set allow_move=true to override (server will charge move fee); or use visit-lot to return to existing home",
					},
				}, nil
			}
		}

		// --- Step 5: bot-cmd:probe-road — validate road bits before purchase ---
		// CLAUDE.md gotcha 12: lot placement requires road bits on the lot tile
		// itself: (GetRoad(x,y) & 0xF) != 0.
		if botCmds == nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":     false,
					"error":  "purchase-lot: bot-cmd channel not available (--no-bot mode or botCmds not wired)",
					"deferred": true,
				},
			}, nil
		}

		roadReply, roadErr := botCmds.Send(ctx, "probe-road", map[string]any{
			"x": uint64(locX),
			"y": uint64(locY),
		})
		if roadErr != nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": fmt.Sprintf("probe-road failed: %v", roadErr)},
			}, nil
		}
		if !roadReply.Ok {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": fmt.Sprintf("probe-road error: %s", roadReply.Error)},
			}, nil
		}
		// Parse road probe reply — expect {road_bits, has_road, x, y}.
		var roadData struct {
			HasRoad  bool   `json:"has_road"`
			RoadBits uint8  `json:"road_bits"`
		}
		if len(roadReply.Data) > 0 {
			if perr := json.Unmarshal(roadReply.Data, &roadData); perr != nil {
				log.Printf("purchase-lot: probe-road data parse error: %v (treating road as unknown)", perr)
			}
		}
		if !roadData.HasRoad {
			return &convention.Response{
				Payload: map[string]any{
					"ok":        false,
					"error":     "NO_ROAD_ACCESS",
					"reason":    "NO_ROAD_ACCESS",
					"road_bits": int(roadData.RoadBits),
					"x":         int(locX),
					"y":         int(locY),
					"hint":      "The target tile must have road bits (GetRoad(x,y) & 0xF) != 0; pick a tile adjacent to a road",
				},
			}, nil
		}

		// --- Step 6: bot-cmd:purchase-lot ---
		purchaseReply, purchaseErr := botCmds.Send(ctx, "purchase-lot", map[string]any{
			"location_x":  uint64(locX),
			"location_y":  uint64(locY),
			"name":        name,
			"start_fresh": startFresh,
		})
		if purchaseErr != nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": fmt.Sprintf("purchase-lot cmd failed: %v", purchaseErr)},
			}, nil
		}
		if !purchaseReply.Ok {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": fmt.Sprintf("purchase-lot refused: %s", purchaseReply.Error)},
			}, nil
		}

		// Parse purchase reply: {ok, lot_id, new_funds, status, reason}
		var purchaseData struct {
			LotID     int64  `json:"lot_id"`
			NewFunds  int64  `json:"new_funds"`
			Status    string `json:"status"`
			Reason    string `json:"reason,omitempty"`
		}
		if len(purchaseReply.Data) > 0 {
			if perr := json.Unmarshal(purchaseReply.Data, &purchaseData); perr != nil {
				log.Printf("purchase-lot: purchase data parse error: %v", perr)
			}
		}

		// Check for server-side failure (bot returns ok=true but status=FAILED).
		if purchaseData.Status == "FAILED" {
			return &convention.Response{
				Payload: map[string]any{
					"ok":     false,
					"error":  purchaseData.Reason,
					"reason": purchaseData.Reason,
					"status": purchaseData.Status,
					"hint":   escalationHint(purchaseData.Reason),
				},
			}, nil
		}

		// --- Step 7: update owned-lots.json (I0-7 habitation block) ---
		locationHex := fmt.Sprintf("0x%08X", loc)
		entry := OwnedLotEntry{
			Name:        name,
			LocationHex: locationHex,
			PurchasedAt: time.Now().UnixMilli(),
			Habitation:  OwnedLotHabitation{}, // zeroed — habitation watcher populates
			IsHabitable: false,
		}
		if werr := AppendOwnedLot(entry); werr != nil {
			// Non-fatal: purchase succeeded server-side. Log and continue so the
			// agent sees the success response. The agent or orchestrator can
			// re-read owned-lots.json on the next session to recover the entry.
			log.Printf("purchase-lot: update owned-lots.json: %v (non-fatal — purchase succeeded)", werr)
		}

		return &convention.Response{
			Payload: map[string]any{
				"ok":           true,
				"lot_id":       purchaseData.LotID,
				"new_funds":    purchaseData.NewFunds,
				"status":       purchaseData.Status,
				"location_hex": locationHex,
				"x":            int(locX),
				"y":            int(locY),
				"name":         name,
				"road_bits":    int(roadData.RoadBits),
			},
		}, nil
	}
}

// escalationHint returns a human-readable hint for specific server-side failure
// reasons. NHOOD_RESERVED is the escalation risk called out in the item spec.
func escalationHint(reason string) string {
	switch reason {
	case "NHOOD_RESERVED":
		return "ESCALATE: Alphaville fso_neighborhoods.flag & 1 is set (NHOOD_RESERVED). An operator must run: UPDATE fso_neighborhoods SET flag = flag & ~1 WHERE neighborhood_id=1; before non-mayor purchases will succeed."
	case "INSUFFICIENT_FUNDS":
		return "Avatar balance is below the lot price. Use query-self to check current balance."
	case "LOT_TAKEN", "LOCATION_TAKEN":
		return "The target tile is already occupied. Use a different target_lot_location."
	case "NAME_TAKEN":
		return "Lot name is already in use. Choose a different name."
	default:
		return ""
	}
}

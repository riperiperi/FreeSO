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
	"os"
	"strconv"
	"strings"

	"github.com/campfire-net/campfire/pkg/convention"
)

// RegisterInteractionHandlers (freesoexperiment-2a8 / freesoexperiment-2ac) mirrors
// the movement-family pattern: one convention.Server per op, each emitting an IPC
// command to the C# bot and forwarding the bot's response as the convention fulfillment.
//
// Ops served:
//
//	interact-with       — VMNetInteractionCmd (queues a named pie-menu interaction);
//	                      special-cases object_type="bulletin_board" (freesoexperiment-2ac):
//	                      gates on FREESO_COMMUNITY_LOT_ID env, issues probe-bulletin
//	                      bot-cmd via BotCmdPump, emits bulletin_listing payload.
//	cancel-interaction  — VMNetInteractionCancelCmd scoped to a specific action_uid
//	query-pie-menu      — local-VM introspection (no wire PDU)
//
// botCmds is required for the bulletin_board path (probe-bulletin bot-cmd). It
// may be nil only in --no-bot mode or tests that don't exercise that path; in
// that case the bulletin_board branch returns ok:false with a deferred marker.
//
// Returns the count of opened servers. A missing declaration is a hard error —
// these ops are a freesoexperiment-2a8 deliverable and MUST ship with their
// declarations.
func RegisterInteractionHandlers(ctx context.Context, cf *Campfire, ipc *IPC, botCmds *BotCmdPump) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"interact-with":      interactWithHandler(ipc, botCmds),
		"queue-interactions": simpleForwardingHandler(ipc, "queue-interactions", "interactions", "queue_mode"),
		"cancel-interaction": cancelInteractionHandler(ipc),
		"query-pie-menu":     queryPieMenuHandler(ipc),
		"query-action-queue": simpleForwardingHandler(ipc, "query-action-queue", "include_idle"),
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

// interactWithHandler — translates a convention invocation into an IPC command
// for the bot's InteractionHandlers.InteractWith, which emits VMNetInteractionCmd.
// Args: interaction (required), callee_id (required), param0, global.
//
// Special case (freesoexperiment-2ac): when object_type="bulletin_board" is
// present, the handler bypasses the VM-interaction path and instead:
//   - Checks FREESO_COMMUNITY_LOT_ID env: if unset or 0, refuses NO_AFFORDANCE.
//   - Issues probe-bulletin bot-cmd via BotCmdPump.
//   - Returns ok:true with kind="bulletin_listing" and the messages array.
func interactWithHandler(ipc *IPC, botCmds *BotCmdPump) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		// Bulletin-board special path (freesoexperiment-2ac).
		if objectType, _ := req.Args["object_type"].(string); objectType == "bulletin_board" {
			return bulletinBoardHandler(ctx, botCmds)
		}
		args := pickArgs(req.Args, "interaction", "callee_id", "param0", "global", "queue_mode")
		return forwardIPC(ctx, ipc, "interact-with", args)
	}
}

// bulletinBoardHandler implements the bulletin_board branch of interact-with.
//
// Affordance gate: mirrors PerceptionAugmentor.isCommunityLotByID — reads
// FREESO_COMMUNITY_LOT_ID env (decimal lot_id). If unset or 0, the body is not
// on a community lot and there is no bulletin board to interact with.
//
// On success: issues probe-bulletin bot-cmd, returns bulletin_listing payload.
// The C# bot issues BulletinRequest{Type=GET_MESSAGES, TargetNHood=1} and
// resolves on the next BulletinResponse.Messages slice.
func bulletinBoardHandler(ctx context.Context, botCmds *BotCmdPump) (*convention.Response, error) {
	// Gate: FREESO_COMMUNITY_LOT_ID must be set and non-zero.
	if !hasBulletinBoardAffordance() {
		return &convention.Response{
			Payload: map[string]any{
				"ok":     false,
				"reason": "NO_AFFORDANCE",
				"error":  "NO_AFFORDANCE: not on a community lot with a bulletin board (FREESO_COMMUNITY_LOT_ID not set or 0)",
			},
		}, nil
	}

	if botCmds == nil {
		return &convention.Response{
			Payload: map[string]any{
				"ok":       false,
				"reason":   "NO_BOT",
				"error":    "bulletin_board: bot-cmd channel not available (--no-bot mode or botCmds not wired)",
				"deferred": true,
			},
		}, nil
	}

	reply, err := botCmds.Send(ctx, "probe-bulletin", map[string]any{
		"neighborhood_id": 1,
	})
	if err != nil {
		return &convention.Response{
			Payload: map[string]any{
				"ok":    false,
				"error": fmt.Sprintf("probe-bulletin failed: %v", err),
			},
		}, nil
	}
	if !reply.Ok {
		return &convention.Response{
			Payload: map[string]any{
				"ok":    false,
				"error": fmt.Sprintf("probe-bulletin refused: %s", reply.Error),
			},
		}, nil
	}

	// Parse the bulletin messages from the bot's reply data.
	var bulletinData struct {
		Messages []json.RawMessage `json:"messages"`
		Count    int               `json:"count"`
	}
	if len(reply.Data) > 0 {
		if perr := json.Unmarshal(reply.Data, &bulletinData); perr != nil {
			// Data parse failure is non-fatal — return what we got.
			return &convention.Response{
				Payload: map[string]any{
					"ok":   true,
					"kind": "bulletin_listing",
					"raw":  string(reply.Data),
				},
			}, nil
		}
	}

	return &convention.Response{
		Payload: map[string]any{
			"ok":       true,
			"kind":     "bulletin_listing",
			"messages": bulletinData.Messages,
			"count":    bulletinData.Count,
		},
	}, nil
}

// hasBulletinBoardAffordance returns true when FREESO_COMMUNITY_LOT_ID is set
// to a non-zero decimal integer. This mirrors the PerceptionAugmentor's
// isCommunityLotByID check: both use the same env var, ensuring the sidecar
// handler and the perception augmentor agree on affordance state.
//
// NOTE: this checks env only — it does NOT read live perception. The community
// lot is identified by env var set at sidecar startup (or in the test harness
// via t.Setenv). FREESO_COMMUNITY_LOT_ID is not set in any chart.toml yet
// (filed as freesoexperiment-a3b); tests must set it explicitly.
func hasBulletinBoardAffordance() bool {
	raw := strings.TrimSpace(os.Getenv("FREESO_COMMUNITY_LOT_ID"))
	if raw == "" {
		return false
	}
	id, err := strconv.ParseInt(raw, 10, 64)
	if err != nil || id == 0 {
		return false
	}
	return true
}

// cancelInteractionHandler — scoped cancel by action_uid. The bot-side handler
// REQUIRES action_uid (unlike movement-family 'cancel' which sweeps broadly),
// so the sidecar just forwards verbatim and lets the bot enforce the contract.
func cancelInteractionHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "action_uid")
		return forwardIPC(ctx, ipc, "cancel-interaction", args)
	}
}

// queryPieMenuHandler — local-VM introspection; no outbound wire PDU. The bot
// computes the pie-menu under its tick lock and returns the interactions list
// as a sync response. Args: target_object_id | target_sim_id (one required),
// include_hidden, include_global.
func queryPieMenuHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "target_object_id", "target_sim_id", "include_hidden", "include_global")
		return forwardIPC(ctx, ipc, "query-pie-menu", args)
	}
}

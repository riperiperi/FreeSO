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
	"strings"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// RegisterChargenHandlers wires the chargen (character-creation) convention
// operation (freesoexperiment-92a): create-avatar.
//
// create-avatar drives the FSO engine's CreateASim flow:
//   - Sidecar validates required args (first_name, last_name, gender,
//     head_guid, body_guid) and rejects missing/malformed inputs early.
//   - On valid args, forwards to the bot via bot-cmd:create-avatar (BotCmdPump
//     channel — city-socket operation, not a lot-VM command).
//   - Bot (in --chargen-mode) sends RSGZWrapperPDU on the city Voltron socket,
//     awaits CreateASimResponse, and replies via the bot-cmd-reply channel.
//   - Sidecar translates the reply into a convention.Response:
//       ok:true, avatar_id:<new_persist_id>
//       ok:false, reason:"category:<name-taken|invalid-guid|engine-refused>"
//
// Wire path: cf $CF create-avatar ... → sidecar → bot-cmd:create-avatar →
//   RSGZWrapperPDU → FSO.Server RegistrationHandler → CreateASimResponse →
//   bot-cmd-reply → convention.Response fulfills.
//
// Requires the bot to be launched with --chargen-mode. In normal (lot-joined)
// mode the bot-cmd:create-avatar cmd returns ok:false with reason:session-already-bound.
//
// Returns the count of registered ops. Missing declaration is a hard error.
func RegisterChargenHandlers(ctx context.Context, cf *Campfire, botCmds *BotCmdPump) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"create-avatar": createAvatarHandler(botCmds),
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

// createAvatarHandler sends bot-cmd:create-avatar to the bot (in chargen mode)
// and surfaces the CreateASimResponse result as a convention response.
//
// Arg validation is performed here (sidecar tier) for the required fields and
// structural shape. Content validation (GUID existence, name uniqueness) is
// engine-side and surfaced via the reason field.
//
// Required args:
//
//	first_name (string)  — must be non-empty
//	last_name  (string)  — must be non-empty
//	gender     (string)  — "M" or "F" (case-insensitive)
//	head_guid  (uint)    — high 32 bits of TSO head OutfitID; must be > 0
//	body_guid  (uint)    — high 32 bits of TSO body OutfitID; must be > 0
//
// Optional args:
//
//	description (string) — avatar bio; defaults to ""
//	skin_tone   (string) — "light" | "medium" | "dark"; defaults to "light"
func createAvatarHandler(botCmds *BotCmdPump) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := req.Args

		// --- Required arg validation (sidecar tier) ---

		firstName, _ := args["first_name"].(string)
		if strings.TrimSpace(firstName) == "" {
			return errResp("create-avatar", "first_name is required and must be non-empty")
		}

		lastName, _ := args["last_name"].(string)
		if strings.TrimSpace(lastName) == "" {
			return errResp("create-avatar", "last_name is required and must be non-empty")
		}

		genderStr, _ := args["gender"].(string)
		genderStr = strings.ToUpper(strings.TrimSpace(genderStr))
		if genderStr != "M" && genderStr != "F" {
			return errResp("create-avatar", "gender must be 'M' or 'F'")
		}

		headGUID, guidErr := coerceUint64(args["head_guid"])
		if guidErr != nil || headGUID == 0 {
			return errResp("create-avatar", "head_guid is required and must be a non-zero uint32 (high 32 bits of TSO OutfitID)")
		}

		bodyGUID, guidErr := coerceUint64(args["body_guid"])
		if guidErr != nil || bodyGUID == 0 {
			return errResp("create-avatar", "body_guid is required and must be a non-zero uint32 (high 32 bits of TSO OutfitID)")
		}

		// Optional args with defaults.
		description, _ := args["description"].(string)

		skinTone, _ := args["skin_tone"].(string)
		skinTone = strings.ToLower(strings.TrimSpace(skinTone))
		if skinTone == "" {
			skinTone = "light"
		}
		if skinTone != "light" && skinTone != "medium" && skinTone != "dark" {
			return errResp("create-avatar", "skin_tone must be 'light', 'medium', or 'dark'")
		}

		// --- Forward to bot via BotCmdPump ---
		if botCmds == nil {
			return errResp("create-avatar", "bot-cmd channel not available (--no-bot mode or botCmds not wired)")
		}

		botArgs := map[string]any{
			"first_name":  firstName,
			"last_name":   lastName,
			"gender":      genderStr,
			"head_guid":   headGUID,
			"body_guid":   bodyGUID,
			"description": description,
			"skin_tone":   skinTone,
		}

		reply, err := botCmds.Send(ctx, "create-avatar", botArgs)
		if err != nil {
			return errResp("create-avatar", fmt.Sprintf("bot-cmd send failed: %v", err))
		}

		if !reply.Ok {
			// Bot-cmd transport failure (e.g. bot not in chargen mode, send
			// timeout). This is a sidecar/infrastructure error — use "error"
			// (consistent with errResp and all other handler families).
			// Distinct from engine-tier semantic categories (see below).
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": reply.Error,
				},
			}, nil
		}

		// Parse the bot-cmd-reply data to extract avatar_id.
		var replyData struct {
			AvatarID uint32 `json:"avatar_id"`
			Status   string `json:"status"`
			Reason   string `json:"reason,omitempty"`
		}
		if len(reply.Data) > 0 {
			if parseErr := json.Unmarshal(reply.Data, &replyData); parseErr != nil {
				return errResp("create-avatar", fmt.Sprintf("parse bot-cmd reply: %v", parseErr))
			}
		}

		if replyData.Status != "SUCCESS" {
			// Engine-tier refusal (e.g. "category:name-taken", "category:invalid-guid").
			// The "reason" field carries a semantic category code that agent code
			// may pattern-match on (distinct from the free-form "error" field used
			// for sidecar/infrastructure failures above).
			return &convention.Response{
				Payload: map[string]any{
					"ok":     false,
					"reason": replyData.Reason,
					"status": replyData.Status,
				},
			}, nil
		}

		return &convention.Response{
			Payload: map[string]any{
				"ok":        true,
				"avatar_id": replyData.AvatarID,
				"status":    replyData.Status,
			},
		}, nil
	}
}

// Note: errResp and coerceUint64 are declared in helpers.go and shared across
// all handler families in this package.

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

// RegisterAvatarHandlers wires the avatar-family convention operations
// (freesoexperiment-b88): change-outfit + change-description.
//
// Both ops forward the raw JSON args over IPC to the C# bot, which does the
// argument validation, wire-PDU construction, and sends the command to the
// FSO server:
//
//   - change-outfit     → VMNetSetOutfitCmd (lot Aries socket, sync ACK).
//   - change-description → DataServiceWrapperPDU with Avatar_Description field
//                          sync (city Aries socket, DB row UPDATEd
//                          synchronously in ServerAvatarProvider.PersistMutation).
//
// Returns the count of opened servers. Missing declarations are a hard error —
// the b88 deliverable requires both ops live.
func RegisterAvatarHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"change-outfit":      changeOutfitHandler(ipc),
		"change-description": changeDescriptionHandler(ipc),
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

// changeOutfitHandler forwards outfit_id + optional scope to the bot.
// Args forwarded (bot validates):
//
//	outfit_id (required uint64)
//	scope     (optional string; default "dynamic-daywear")
func changeOutfitHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "outfit_id", "scope")
		return forwardIPC(ctx, ipc, "change-outfit", args)
	}
}

// changeDescriptionHandler forwards the new description text to the bot. The
// bot validates length ≤ 500 (server cap) and non-empty, then writes the
// DataServiceWrapperPDU on its city session.
func changeDescriptionHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "text")
		return forwardIPC(ctx, ipc, "change-description", args)
	}
}

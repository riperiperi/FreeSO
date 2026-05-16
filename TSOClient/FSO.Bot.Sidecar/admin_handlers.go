/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"fmt"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// RegisterAdminHandlers (freesoexperiment-3df) wires the admin verb family:
// kick-user, ban-user, ipban-user, archive-approve-user, archive-reject-user,
// admin-chat-command. Thin forwarders — the C# bot (AdminHandlers.cs) enforces
// the is_admin gate against the caller's in-VM VMTSOAvatarPermissions so the
// refuse path is deterministic. The sidecar has no VM view and cannot check
// admin status independently.
//
// cheat-command (catalog row 110) is NOT registered here. Per the catalog
// note: the ! prefix is UICheatHandler client-side only — there is no wire
// path to the server. Delete semantics are covered by build-buy-catalog's
// delete-object op when that family ships. Dropped per item-spec rule
// "if catalog lists ops that don't have a clean wire path, drop them with a
// note — do NOT stub."
//
// Arg schemas (bot-side contract in AdminHandlers.cs):
//
//	kick-user            { target_persist_id: uint }  — avatar_id
//	ban-user             { target_persist_id: uint }  — avatar_id
//	ipban-user           { target_persist_id: uint }  — avatar_id
//	archive-approve-user { target_user_id:    uint }  — user_id (NOT avatar_id)
//	archive-reject-user  { target_user_id:    uint }  — user_id (NOT avatar_id)
//	admin-chat-command   { command: string, args?: string }
//	                     — command ∈ {ban, banip, unban, banlist, builder,
//	                       admin, roomie, visitor, close, qtrday, setjob,
//	                       trace, reload, time, tickrate, speed, tuning,
//	                       fixall, testcollision}
//
// Ground-source verification (per tests/integration/verb-admin.sh):
//   - ban-user: fso_users.is_banned = 1 on target after call; reset to 0 in teardown.
//   - ipban-user: same + fso_bans row for target's last_ip (non-loopback).
//   - kick-user: target session closes (best-effort; no persistent DB mutation).
//   - archive-approve-user: fso_users.is_verified = 1.
//   - archive-reject-user: target session closed via VerificationNotification.
//   - admin-chat-command /banlist: response ok=true + a Generic chat event appears
//     listing current bans (visible in perception recent_events).
func RegisterAdminHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"kick-user":            simpleForwardingHandler(ipc, "kick-user", "target_persist_id"),
		"ban-user":             simpleForwardingHandler(ipc, "ban-user", "target_persist_id"),
		"ipban-user":           simpleForwardingHandler(ipc, "ipban-user", "target_persist_id"),
		"archive-approve-user": simpleForwardingHandler(ipc, "archive-approve-user", "target_user_id"),
		"archive-reject-user":  simpleForwardingHandler(ipc, "archive-reject-user", "target_user_id"),
		"admin-chat-command":   simpleForwardingHandler(ipc, "admin-chat-command", "command", "args"),
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

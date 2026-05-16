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

// RegisterSocialHandlers loads the social-family convention declarations (speak, be-friendly,
// tell-joke, flirt, be-mean, give-gift) and starts one convention.Server per op that forwards
// each invocation to the bot via IPC and returns the correlated convention.Response.
//
// Pattern mirrors movement_handlers.go: one Server per Declaration, each served in its own
// goroutine until ctx is cancelled. A missing declaration is a hard error — these six JSONs
// are shipped with the same checkout as this Go file.
//
// speak stays on the fast-path (forwardIPC) — it emits VMNetChatCmd, not VMNetInteractionCmd,
// so there is no action_queue effect to verify.
//
// The five directed socials (be-friendly, tell-joke, flirt, be-mean, give-gift) are wrapped
// with verifyingHandlerWithExpect (freesoexperiment-596 / W11). They dispatch VMNetInteractionCmd
// via pie-menu alias resolution, so the same action_queue diff that verify interact-with applies.
func RegisterSocialHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"speak":       speakHandler(ipc),
		"be-friendly": verifyingDirectedSocialHandler(ipc, "be-friendly"),
		"tell-joke":   verifyingDirectedSocialHandler(ipc, "tell-joke"),
		"flirt":       verifyingDirectedSocialHandler(ipc, "flirt"),
		"be-mean":     verifyingDirectedSocialHandler(ipc, "be-mean"),
		"give-gift":   verifyingDirectedSocialHandler(ipc, "give-gift"),
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

// speakHandler translates speak invocations into a bot-IPC "speak" command. Required: text.
// Optional: channel_id (default 0). The bot ACKs with {queued:true, text, channel_id, length};
// the wire-level effect (VMChatEvent echo) is observable by the caller via the perception
// stream's recent_events kind=chat entries.
func speakHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "text", "channel_id")
		return forwardIPC(ctx, ipc, "speak", args)
	}
}

// directedSocialHandler translates a pre-parameterized directed social (be-friendly, tell-joke,
// flirt, be-mean, give-gift) into the matching bot-IPC op. The bot resolves the target's
// pie-menu by name alias and emits VMNetInteractionCmd with the correct TTAB index. Required:
// target_sim_id OR target_object_id. No interaction_id — the bot picks it.
//
// This is the DELIBERATE divergence from interact-with: interact-with takes a caller-supplied
// interaction id; the directed socials here hide the id behind the verb. Shared VMNet encoding
// lives on the C# side (SocialHandlers.DirectedSocial).
//
// NOTE: Not used by RegisterSocialHandlers (which uses verifyingDirectedSocialHandler below).
// Kept for backwards-compatible use in tests that verify the IPC dispatch shape only.
func directedSocialHandler(ipc *IPC, op string) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "target_sim_id", "target_object_id", "queue_mode")
		return forwardIPC(ctx, ipc, op, args)
	}
}

// verifyingDirectedSocialHandler wraps a directed-social op (be-friendly, tell-joke, flirt,
// be-mean, give-gift) with verifyingHandlerWithExpect (freesoexperiment-596 / W11).
//
// The bot dispatches VMNetInteractionCmd (same wire command as interact-with and
// go-to-with-interaction), so the action_queue diff in socialExpectFn applies directly.
// The settle window, snapshot, and verdict shapes are identical to interact-with — only the
// ExpectFn changes (socialExpectFn extracts callee_id from the bot's IPC ack).
func verifyingDirectedSocialHandler(ipc *IPC, op string) convention.HandlerFunc {
	return verifyingHandlerWithExpect(
		ipc,
		op,
		[]string{"target_sim_id", "target_object_id", "queue_mode"},
		0,   // snapshotLevel: diff action_queue, not objects
		"",  // snapshotGuidHex: not used
		nil, // extendedPollTrigger: disabled (action_queue changes sync)
		socialExpectFn,
		defaultSocialVerifyingConfig(),
	)
}

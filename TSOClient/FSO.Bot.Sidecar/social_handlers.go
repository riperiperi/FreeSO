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

// RegisterSocialHandlers loads the social-family convention declarations (speak, be-friendly,
// tell-joke, flirt, be-mean, give-gift) and starts one convention.Server per op that forwards
// each invocation to the bot via IPC and returns the correlated convention.Response.
//
// Pattern mirrors movement_handlers.go: one Server per Declaration, each served in its own
// goroutine until ctx is cancelled. A missing declaration is a hard error — these six JSONs
// are shipped with the same checkout as this Go file.
func RegisterSocialHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"speak":       speakHandler(ipc),
		"be-friendly": directedSocialHandler(ipc, "be-friendly"),
		"tell-joke":   directedSocialHandler(ipc, "tell-joke"),
		"flirt":       directedSocialHandler(ipc, "flirt"),
		"be-mean":     directedSocialHandler(ipc, "be-mean"),
		"give-gift":   directedSocialHandler(ipc, "give-gift"),
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
func directedSocialHandler(ipc *IPC, op string) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, "target_sim_id", "target_object_id")
		return forwardIPC(ctx, ipc, op, args)
	}
}

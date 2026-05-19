/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"fmt"
	"strings"
	"time"
	"unicode/utf8"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// RegisterSocialHandlers loads the social-family convention declarations (speak, be-friendly,
// tell-joke, flirt, be-mean, give-gift) and starts one convention.Server per op that forwards
// each invocation to the bot via IPC and returns the correlated convention.Response.
//
// Pattern mirrors movement_handlers.go: one Server per Declaration, each served in its own
// goroutine until ctx is cancelled. A missing declaration is a hard error — these six JSONs
// are shipped with the same checkout as this Go file.
//
// speak uses speakHandler (chunking-aware): it emits VMNetChatCmd, not VMNetInteractionCmd,
// so there is no action_queue effect to verify. Long texts are split into ≤1000-rune chunks
// with 200ms inter-chunk delays to preserve FSO message ordering.
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

// speakChunkSize is the maximum rune count per FSO chat message. FSO's chat
// packet parser silently truncates messages longer than 1024 bytes; keeping
// chunks at 1000 runes provides a comfortable margin for multi-byte characters.
const speakChunkSize = 1000

// speakChunkDelay is the inter-chunk delay injected between sequential IPC
// speak commands to preserve message ordering on the FSO server.
const speakChunkDelay = 200 * time.Millisecond

// speakHandler translates speak invocations into one or more bot-IPC "speak"
// commands. Required: text. Optional: channel_id (default 0).
//
// If text fits in speakChunkSize runes, a single IPC command is issued and the
// bot's ACK payload is forwarded with an additional chunk_count=1 field.
//
// If text exceeds speakChunkSize, it is split into chunks at the last word
// boundary (space) at or before position speakChunkSize. If a single word
// exceeds speakChunkSize, it is hard-split at speakChunkSize with a "…"
// truncation marker appended. Chunks are sent sequentially with speakChunkDelay
// between them. The combined response reports ok=true with chunk_count=N when
// ALL chunks are delivered; if any chunk fails the response reports ok=false
// with the failing chunk index in failed_chunk.
//
// The wire-level effect (VMChatEvent echo) is observable by the caller via the
// perception stream's recent_events kind=chat entries, one entry per chunk.
func speakHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		raw, _ := req.Args["text"].(string)
		if raw == "" {
			// No text: forward as-is so the bot can return its own error shape.
			args := pickArgs(req.Args, "text", "channel_id")
			return forwardIPC(ctx, ipc, "speak", args)
		}

		chunks := chunkText(raw, speakChunkSize)
		channelID := req.Args["channel_id"]

		for i, chunk := range chunks {
			if i > 0 {
				select {
				case <-time.After(speakChunkDelay):
				case <-ctx.Done():
					return &convention.Response{
						Payload: map[string]any{
							"ok":           false,
							"error":        "context cancelled between chunks",
							"failed_chunk": i,
						},
					}, nil
				}
			}

			args := map[string]any{"text": chunk}
			if channelID != nil {
				args["channel_id"] = channelID
			}

			resp, err := ipc.Send(ctx, "speak", args)
			if err != nil {
				return &convention.Response{
					Payload: map[string]any{
						"ok":           false,
						"error":        err.Error(),
						"failed_chunk": i,
					},
				}, nil
			}
			if !resp.Ok {
				return &convention.Response{
					Payload: map[string]any{
						"ok":           false,
						"error":        resp.Error,
						"failed_chunk": i,
					},
				}, nil
			}
		}

		return &convention.Response{
			Payload: map[string]any{
				"ok":          true,
				"chunk_count": len(chunks),
			},
		}, nil
	}
}

// chunkText splits text into chunks of at most maxRunes runes each. Splits
// prefer the last space at or before maxRunes (word boundary). If no space
// exists within the allowed range (single overlong word), the chunk is
// hard-split at maxRunes and a "…" marker is appended.
//
// Text that is empty or fits entirely within maxRunes is returned as a single
// chunk (no allocation beyond the slice header).
func chunkText(text string, maxRunes int) []string {
	if maxRunes <= 0 {
		maxRunes = speakChunkSize
	}
	if utf8.RuneCountInString(text) <= maxRunes {
		return []string{text}
	}

	var chunks []string
	remaining := text
	for len(remaining) > 0 {
		runes := []rune(remaining)
		if len(runes) <= maxRunes {
			chunks = append(chunks, remaining)
			break
		}

		// Find last space at or before maxRunes.
		candidate := string(runes[:maxRunes])
		splitAt := strings.LastIndex(candidate, " ")

		if splitAt > 0 {
			// Split at word boundary: keep trimmed chunk, skip the space.
			chunks = append(chunks, strings.TrimRight(string(runes[:splitAt]), " "))
			remaining = strings.TrimLeft(string(runes[splitAt:]), " ")
		} else {
			// Hard split: single word exceeds maxRunes. Truncate with marker.
			chunks = append(chunks, string(runes[:maxRunes])+"…")
			remaining = string(runes[maxRunes:])
		}
	}
	return chunks
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

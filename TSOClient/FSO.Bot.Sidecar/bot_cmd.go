/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"log"
	"sync"
	"time"

	"github.com/google/uuid"
)

// BotCmdPump is the sidecar-side pump for the bot-cmd IPC envelope
// (freesoexperiment-0de). It writes {"kind":"bot-cmd","cmd":"...","correlation_id":
// "<uuid>","args":{...}} lines to the bot's stdin and correlates replies observed
// on bot stdout (kind=="bot-cmd-reply") back to waiting callers via correlation ID.
//
// This is a separate layer from the existing IPC (ipc.go) which uses the
// {"id":"...","op":"..."} / {"kind":"response","cmd_id":"..."} envelope for VM-command
// RPCs. The bot-cmd envelope is reserved for city-socket operations that must run
// inside the C# bot (probe-lot, bot-exit-request) and cannot be issued from the sidecar
// directly (sidecar has no city socket of its own).
//
// Wire shapes:
//
//	sidecar→bot (stdin): {"kind":"bot-cmd","cmd":"probe-lot","correlation_id":"<uuid>","args":{"lot_location":12345}}
//	bot→sidecar (stdout): {"kind":"bot-cmd-reply","correlation_id":"<uuid>","ok":true,"data":{"status":"FOUND"}}
//	                       {"kind":"bot-cmd-reply","correlation_id":"<uuid>","ok":false,"error":"timeout"}
//
// Load-bearing for cross-lot transition (freesoexperiment-ca0, freesoexperiment-0de).
type BotCmdPump struct {
	bot *BotProcess

	mu      sync.Mutex
	pending map[string]chan *BotCmdReply
}

// BotCmdRequest is the wire shape written to bot stdin.
type BotCmdRequest struct {
	Kind          string         `json:"kind"`           // always "bot-cmd"
	Cmd           string         `json:"cmd"`            // "probe-lot", "bot-exit-request", ...
	CorrelationID string         `json:"correlation_id"` // uuid
	Args          map[string]any `json:"args,omitempty"`
}

// BotCmdReply is the wire shape read from bot stdout (kind="bot-cmd-reply").
type BotCmdReply struct {
	Kind          string          `json:"kind"`           // "bot-cmd-reply"
	CorrelationID string          `json:"correlation_id"` // echoed from request
	Ok            bool            `json:"ok"`
	Data          json.RawMessage `json:"data,omitempty"`
	Error         string          `json:"error,omitempty"`
}

// NewBotCmdPump constructs a BotCmdPump over a BotProcess. The caller must
// route bot-cmd-reply frames from bot stdout to BotCmdPump.Deliver via the
// bridge layer.
func NewBotCmdPump(bot *BotProcess) *BotCmdPump {
	return &BotCmdPump{
		bot:     bot,
		pending: make(map[string]chan *BotCmdReply),
	}
}

// Send writes a bot-cmd to bot stdin, waits for the correlated reply, and
// returns it. If ctx expires before a reply arrives, Send returns
// context.DeadlineExceeded and the pending slot is reclaimed.
//
// The default timeout when ctx has no deadline is 15 seconds — longer than
// the IPC default (10s) because city-socket round-trips (FindLotRequest) can
// take up to 10s on a loaded server, and we want margin for the bot to reply.
func (p *BotCmdPump) Send(ctx context.Context, cmd string, args map[string]any) (*BotCmdReply, error) {
	if p == nil || p.bot == nil {
		return nil, errors.New("bot-cmd: not wired to a bot process")
	}
	if cmd == "" {
		return nil, errors.New("bot-cmd: cmd is required")
	}
	if _, ok := ctx.Deadline(); !ok {
		var cancel context.CancelFunc
		ctx, cancel = context.WithTimeout(ctx, 15*time.Second)
		defer cancel()
	}

	corrID := uuid.NewString()
	req := BotCmdRequest{
		Kind:          "bot-cmd",
		Cmd:           cmd,
		CorrelationID: corrID,
		Args:          args,
	}
	line, err := json.Marshal(req)
	if err != nil {
		return nil, fmt.Errorf("bot-cmd marshal %s: %w", cmd, err)
	}

	ch := make(chan *BotCmdReply, 1)
	p.mu.Lock()
	p.pending[corrID] = ch
	p.mu.Unlock()

	// Reclaim the pending slot on every return path.
	defer func() {
		p.mu.Lock()
		delete(p.pending, corrID)
		p.mu.Unlock()
	}()

	if err := p.bot.WriteStdin(line); err != nil {
		return nil, fmt.Errorf("bot-cmd write stdin (%s): %w", cmd, err)
	}

	select {
	case reply := <-ch:
		if reply == nil {
			return nil, errors.New("bot-cmd: nil reply")
		}
		return reply, nil
	case <-ctx.Done():
		return nil, fmt.Errorf("bot-cmd await %s (corr=%s): %w", cmd, corrID, ctx.Err())
	}
}

// UpdateBot hot-swaps the underlying BotProcess. Called by the supervisor loop
// after relaunching the bot so all in-flight Send callers on the old bot surface
// a write error and the new bot is used for subsequent calls.
func (p *BotCmdPump) UpdateBot(bot *BotProcess) {
	if p == nil {
		return
	}
	p.mu.Lock()
	p.bot = bot
	p.mu.Unlock()
}

// Deliver routes a raw stdout bot-cmd-reply frame to the pending caller, if any.
// Unknown correlation IDs are dropped with a log — we do not crash on stragglers.
func (p *BotCmdPump) Deliver(line []byte) {
	if p == nil {
		return
	}
	var reply BotCmdReply
	if err := json.Unmarshal(line, &reply); err != nil {
		log.Printf("bot-cmd: malformed reply frame: %v", err)
		return
	}
	if reply.CorrelationID == "" {
		log.Printf("bot-cmd: reply missing correlation_id (dropped)")
		return
	}
	p.mu.Lock()
	ch, ok := p.pending[reply.CorrelationID]
	p.mu.Unlock()
	if !ok {
		log.Printf("bot-cmd: unknown correlation_id %s (stray reply)", reply.CorrelationID)
		return
	}
	// Non-blocking: buffer of 1, first delivery wins.
	select {
	case ch <- &reply:
	default:
	}
}

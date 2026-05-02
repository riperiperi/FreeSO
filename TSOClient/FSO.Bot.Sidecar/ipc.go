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

// IPC is the sidecar-side bot command channel (freesoexperiment-b9c). Sends
// NDJSON command frames to the bot's stdin and correlates responses observed
// on the bot's stdout (via the bridge layer) back to the caller via
// correlation ID.
//
// Channel choice rationale: see CommandDispatcher.cs §Design decision.
// Same rationale applies to the Go side — stdin is the inbound pipe the
// sidecar already owns as parent of the bot subprocess, so correlated
// request/response rides on top of an existing file descriptor the subprocess
// lifecycle already manages.
//
// Load-bearing for 13 follow-on verb-family items (d87-d-*). A handler in
// campfire.go's convention.Server calls Send(), awaits a Response, and turns
// it into a convention.Response fulfillment.
type IPC struct {
	bot *BotProcess

	mu      sync.Mutex
	pending map[string]chan *Response
}

// Command is the wire shape written to bot stdin.
type Command struct {
	ID              string         `json:"id"`
	Op              string         `json:"op"`
	Args            map[string]any `json:"args,omitempty"`
	ExpectResponse  bool           `json:"expect_response"`
}

// Response is the wire shape read from bot stdout (kind="response"). Only one
// of Payload or Error is set depending on Ok.
type Response struct {
	CmdID   string          `json:"cmd_id"`
	Ok      bool            `json:"ok"`
	Payload json.RawMessage `json:"payload,omitempty"`
	Error   string          `json:"error,omitempty"`
}

// NewIPC constructs an IPC value over a BotProcess. The caller must wire
// response frames (kind="response" on stdout) back to IPC.Deliver via the
// bridge layer.
func NewIPC(bot *BotProcess) *IPC {
	return &IPC{
		bot:     bot,
		pending: make(map[string]chan *Response),
	}
}

// Send writes a command to bot stdin, waits for the correlated response, and
// returns it. If ctx expires before a response arrives, Send returns
// context.DeadlineExceeded and the pending slot is reclaimed.
//
// The default timeout when ctx has no deadline is 10 seconds — long enough for
// the worst case of the VM queue being full and the bot retrying, short
// enough to surface a stuck bot. Callers wanting different timeouts should
// set one on ctx.
func (i *IPC) Send(ctx context.Context, op string, args map[string]any) (*Response, error) {
	if i == nil {
		return nil, errors.New("ipc: not wired to a bot process")
	}
	// Snapshot i.bot under i.mu to avoid a data race with UpdateBot(), which
	// writes i.bot under the same lock. We read once into a local variable and
	// release the lock before any I/O so we don't hold the lock across the
	// blocking WriteStdin call.
	i.mu.Lock()
	bot := i.bot
	i.mu.Unlock()
	if bot == nil {
		return nil, errors.New("ipc: not wired to a bot process")
	}
	if op == "" {
		return nil, errors.New("ipc: op is required")
	}
	if _, ok := ctx.Deadline(); !ok {
		var cancel context.CancelFunc
		ctx, cancel = context.WithTimeout(ctx, 10*time.Second)
		defer cancel()
	}

	id := uuid.NewString()
	cmd := Command{
		ID:             id,
		Op:             op,
		Args:           args,
		ExpectResponse: true,
	}
	line, err := json.Marshal(cmd)
	if err != nil {
		return nil, fmt.Errorf("marshal cmd %s: %w", op, err)
	}

	ch := make(chan *Response, 1)
	i.mu.Lock()
	i.pending[id] = ch
	i.mu.Unlock()

	// Ensure the slot is reclaimed on every return path, not just timeout.
	defer func() {
		i.mu.Lock()
		delete(i.pending, id)
		i.mu.Unlock()
	}()

	if err := bot.WriteStdin(line); err != nil {
		return nil, fmt.Errorf("ipc write stdin (%s): %w", op, err)
	}

	select {
	case resp := <-ch:
		if resp == nil {
			return nil, errors.New("ipc: nil response")
		}
		return resp, nil
	case <-ctx.Done():
		return nil, fmt.Errorf("ipc await %s (id=%s): %w", op, id, ctx.Err())
	}
}

// UpdateBot hot-swaps the underlying BotProcess. Called by the supervisor loop
// after relaunching the bot so all registered handlers automatically route
// subsequent IPC.Send calls to the new process without re-registration.
// Any in-flight Send callers on the old bot will receive a write error
// (stdin closed) and surface it as a convention response error — the agent
// retries cleanly.
func (i *IPC) UpdateBot(bot *BotProcess) {
	if i == nil {
		return
	}
	i.mu.Lock()
	i.bot = bot
	i.mu.Unlock()
}

// Deliver routes a raw stdout response frame to the pending caller, if any.
// Unknown cmd_ids are dropped with a log — we don't crash on stragglers.
func (i *IPC) Deliver(line []byte) {
	if i == nil {
		return
	}
	var resp Response
	if err := json.Unmarshal(line, &resp); err != nil {
		log.Printf("ipc: malformed response frame: %v", err)
		return
	}
	if resp.CmdID == "" {
		log.Printf("ipc: response missing cmd_id (dropped)")
		return
	}
	i.mu.Lock()
	ch, ok := i.pending[resp.CmdID]
	i.mu.Unlock()
	if !ok {
		log.Printf("ipc: unknown cmd_id %s (stray response)", resp.CmdID)
		return
	}
	// Non-blocking send — buffer of 1 means first delivery wins; duplicates
	// dropped.
	select {
	case ch <- &resp:
	default:
	}
}

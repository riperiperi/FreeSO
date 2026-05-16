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
	"strings"
	"sync"
	"time"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
	"github.com/campfire-net/campfire/cf-protocol/protocol"
)

// Router is a single-Subscribe dispatcher for all freeso-embodiment convention
// operations on a campfire. Replaces N convention.Server goroutines (one per op)
// with one Subscribe loop + an in-process op-name → handler map.
//
// Why this exists: 103 concurrent Subscribe goroutines × 500ms poll × per-poll
// filesystem-sync saturated the campfire's single-writer SQLite, causing
// SQLITE_BUSY cascades and silent handler death (each handler's Serve loop
// surfaces sub.Err() via a no-op errFn and returns ctx.Err()=nil). One
// subscriber means: one filesystem sync per poll, one cursor to advance, no
// per-handler restart bookkeeping, ~50× less DB load. See
// docs/design/embodiment-runs/run6-20260420T1633Z/observation.md "Known issues".
//
// What it intentionally does NOT do: validateArgs (the campfire convention
// library's strict allow-listing). Handlers are responsible for type coercion
// and required-arg checks — they already do this defensively (see e.g.
// nameHandler returning errResp on empty `as`). Skipping validateArgs trades
// schema-strictness for keeping all handler logic reachable; the prior
// per-server validation was killing handlers on phantom requests anyway.
type Router struct {
	mu       sync.RWMutex
	handlers map[string]convention.HandlerFunc
	decls    map[string]*convention.Declaration

	pollInterval time.Duration
}

// NewRouter creates a router with a default poll interval. 250ms is fast
// enough that build-loop agents stop noticing the receive-side wait and
// cheap enough that the SQLite read cost is still negligible (our store
// holds ~1000 messages per session — read-with-tag is an indexed lookup).
// Previously 2s, which combined with the cf-client's 2s await poll yielded
// 5+ sec round-trips even for trivial ops; halving each side gets us to a
// regime where per-op cost is dominated by the handler, not the transport.
func NewRouter() *Router {
	return &Router{
		handlers:     make(map[string]convention.HandlerFunc, 128),
		decls:        make(map[string]*convention.Declaration, 128),
		pollInterval: 250 * time.Millisecond,
	}
}

// Register binds an op name to a handler. Subsequent Register calls for the
// same op name overwrite. Safe to call before Serve starts; not safe to call
// concurrently with Serve.
func (r *Router) Register(decl *convention.Declaration, handler convention.HandlerFunc) {
	r.mu.Lock()
	defer r.mu.Unlock()
	r.handlers[decl.Operation] = handler
	r.decls[decl.Operation] = decl
}

// Count returns the number of registered ops. For startup logging.
func (r *Router) Count() int {
	r.mu.RLock()
	defer r.mu.RUnlock()
	return len(r.handlers)
}

// Serve subscribes once for all freeso:* operation tags and dispatches each
// matching message to the registered handler. Blocks until ctx is cancelled.
// On subscription error, logs and reconnects with a 200ms backoff (the same
// retry pattern the per-handler loop used, just centralized).
func (r *Router) Serve(ctx context.Context, cf *Campfire) {
	for {
		err := r.serveOnce(ctx, cf)
		if ctx.Err() != nil {
			log.Printf("router: stopped (%d ops)", r.Count())
			return
		}
		if err != nil {
			log.Printf("router: serve err: %v (restarting)", err)
		} else {
			log.Printf("router: serve returned (restarting)")
		}
		select {
		case <-ctx.Done():
			return
		case <-time.After(200 * time.Millisecond):
		}
	}
}

// routerSubscribeRequest builds the SubscribeRequest the Router uses. Extracted
// from serveOnce so unit tests can assert configuration (ExcludeTags presence,
// poll interval, cursor floor) without spinning up a real campfire client.
//
// afterTimestamp is the cursor floor — Subscribe returns only messages with
// timestamp > afterTimestamp. Pass 0 to replay all history (default cf
// behaviour, not what we want); pass time.Now().UnixNano() at boot to skip
// historical replay (what we want — a request from N seconds ago when we
// were down is stale, the requester long-since timed out).
func (r *Router) routerSubscribeRequest(campfireID string, afterTimestamp int64) protocol.SubscribeRequest {
	return protocol.SubscribeRequest{
		CampfireID: campfireID,
		// Process only messages that arrive AFTER we're up. SubscribeRequest's
		// docstring (cf-protocol/protocol/subscribe.go): "When 0, all existing
		// messages are returned first, then new ones are streamed." With 47k
		// historical messages on our body-campfire, AfterTimestamp=0 means the
		// initial sync-and-filter scans every one. Boot-time-now eliminates
		// that scan and the stale-request replay.
		AfterTimestamp: afterTimestamp,
		// Match every freeso-embodiment op tag in one subscription.
		TagPrefixes: []string{"freeso:"},
		// Defensive excludes:
		//   - "fulfills" — our own response messages carry this; without exclusion
		//     the Router would re-ingest them as new requests.
		//   - "convention:operation" — declaration broadcasts at startup, not
		//     invocations.
		//   - "freeso:perception", "freeso:dialog", "freeso:system" — these
		//     are bridge broadcasts FROM the bot TO the campfire. The Router's
		//     freeso:* prefix matches them; without exclusion every perception
		//     tick (1Hz × N sims) produces a `router: no handler registered
		//     for op "perception"` convention:error message that floods the
		//     campfire and pollutes cf reads. The fix is opt-out by event
		//     family, not by op-name allowlist, so future event families
		//     added in bridges.go inherit the protection.
		ExcludeTags: []string{
			"fulfills",
			"convention:operation",
			"freeso:perception",
			"freeso:dialog",
			"freeso:system",
		},
		PollInterval: r.pollInterval,
	}
}

func (r *Router) serveOnce(ctx context.Context, cf *Campfire) error {
	// Cursor floor = "now". A restart of the Router (either at sidecar boot
	// or after a reconnect-backoff) skips any historical messages and only
	// processes new requests. Requests older than restart-time are by
	// definition stale — the caller's cf await would have timed out already.
	floor := time.Now().UnixNano()
	sub := cf.Client.Subscribe(ctx, r.routerSubscribeRequest(cf.ID, floor))
	for msg := range sub.Messages() {
		r.dispatch(ctx, cf, msg)
	}
	return sub.Err()
}

// dispatch identifies the op from msg.Tags, looks up the handler, builds the
// convention.Request, calls the handler, and sends the fulfillment. All errors
// are logged and surfaced to the caller as a fulfills-tagged error message.
func (r *Router) dispatch(ctx context.Context, cf *Campfire, msg protocol.Message) {
	op := opFromTags(msg.Tags)
	if op == "" {
		// Tag prefix matched but no recognizable freeso:<op> tag — skip.
		return
	}
	r.mu.RLock()
	handler, hOk := r.handlers[op]
	r.mu.RUnlock()
	if !hOk {
		// No handler registered for this op. Possible if a client invokes an
		// op the sidecar doesn't expose. Send back an error so the caller
		// fails fast instead of timing out.
		_ = sendFulfillsError(cf, msg.ID, fmt.Errorf("router: no handler registered for op %q", op))
		return
	}

	// Permissive arg parsing: empty payload → nil args, otherwise decode JSON
	// object into map[string]any. Handlers do their own coercion.
	var args map[string]any
	if len(msg.Payload) > 0 {
		if err := json.Unmarshal(msg.Payload, &args); err != nil {
			_ = sendFulfillsError(cf, msg.ID, fmt.Errorf("dispatch[%s]: parse payload: %w", op, err))
			return
		}
	}

	req := &convention.Request{
		MessageID:  msg.ID,
		Sender:     msg.Sender,
		CampfireID: cf.ID,
		Args:       args,
		Tags:       msg.Tags,
	}

	resp, herr := handler(ctx, req)
	if herr != nil {
		log.Printf("dispatch[%s]: handler err on msg %s: %v", op, msg.ID, herr)
		_ = sendFulfillsError(cf, msg.ID, herr)
		return
	}
	if resp == nil {
		// Some handlers intentionally return (nil, nil) — fire-and-forget. Skip the send.
		return
	}
	if err := sendFulfillsResponse(cf, msg.ID, resp); err != nil {
		log.Printf("dispatch[%s]: send response: %v (msg %s)", op, err, msg.ID)
	}
}

// opFromTags extracts the operation name from a message's tags. Looks for the
// first tag of shape "freeso:<op>". Returns "" if none found.
func opFromTags(tags []string) string {
	for _, t := range tags {
		if strings.HasPrefix(t, "freeso:") {
			return strings.TrimPrefix(t, "freeso:")
		}
	}
	return ""
}

// sendFulfillsResponse mirrors convention.Server.sendResponse: tag with
// "fulfills", reference the request via Antecedents, and JSON-encode the
// payload. We rebuild this here because convention.Server.sendResponse is
// unexported and we're not going through that codepath.
func sendFulfillsResponse(cf *Campfire, requestMsgID string, resp *convention.Response) error {
	var payload []byte
	if resp.Payload != nil {
		var err error
		payload, err = json.Marshal(resp.Payload)
		if err != nil {
			return fmt.Errorf("marshal payload: %w", err)
		}
	}
	tags := append([]string{"fulfills"}, resp.Tags...)
	_, err := cf.Client.Send(protocol.SendRequest{
		CampfireID:  cf.ID,
		Payload:     payload,
		Tags:        tags,
		Antecedents: []string{requestMsgID},
	})
	return err
}

// sendFulfillsError is the error counterpart. Mirrors convention.Server.sendErrorResponse.
func sendFulfillsError(cf *Campfire, requestMsgID string, handlerErr error) error {
	payload, _ := json.Marshal(map[string]string{"error": handlerErr.Error()})
	_, err := cf.Client.Send(protocol.SendRequest{
		CampfireID:  cf.ID,
		Payload:     payload,
		Tags:        []string{"fulfills", "convention:error"},
		Antecedents: []string{requestMsgID},
	})
	return err
}

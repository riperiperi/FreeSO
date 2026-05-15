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
	"strconv"
	"strings"
	"time"

	"github.com/campfire-net/campfire/pkg/convention"
)

// ExpectFn is the verdict callback for verifyingHandlerWithExpect.
//
// Implementations receive the pre- and post-snapshots, the filtered arg map,
// and the IPC response from the forwarded op. They return:
//
//   - verdict  — a short identifier string (e.g. "placed", "silent-drop", "deleted").
//   - hints    — a list of short identifiable hints for the caller to switch on.
//   - payload  — the full structured response payload (merged into the response).
//   - ok       — true on success (placed / deleted / etc.), false on failure.
//
// The framework handles pre/post snapshot acquisition, settle sleep, extended-
// poll loop, and bot-rejection before invoking ExpectFn. ExpectFn is only
// called when the IPC op returned ok:true and settle + optional polls are done.
type ExpectFn func(pre, post lotSnapshot, args map[string]any, ipcResp *Response) (verdict string, hints []string, payload map[string]any, ok bool)

// verifyingHandlerWithExpect is the generic snapshot-diff framework shared by
// placementVerifyingHandler and deleteVerifyingHandler (and any future verifying
// handlers). It handles:
//
//  1. Pre-snapshot (non-fatal on error — op proceeds in degraded mode).
//  2. Forward the op.
//  3. Bot-rejection fast-path: surfaces the error and returns immediately.
//  4. Settle sleep (cfg.settleWait).
//  5. Post-snapshot.
//  6. Extended-poll loop: if the initial post shows balance changed but the
//     expect predicate isn't satisfied yet, re-polls up to cfg.extendedPollTimeout.
//  7. Calls expect(pre, post, args, ipcResp) and merges the returned payload
//     into the convention.Response.
//
// The snapshotLevel and snapshotGuidHex parameters control what snapshot() reads;
// callers pass the level and GUID relevant to their op (0/"" for a full-level dump).
//
// The extendedPollTrigger controls when the extended-poll loop fires: it should
// return true when the post-snapshot shows evidence that the op was partially
// applied (e.g. balance changed) but the verdict isn't conclusive yet.
// A nil trigger disables extended polling.
func verifyingHandlerWithExpect(
	ipc *IPC,
	op string,
	allowedArgs []string,
	snapshotLevel int,
	snapshotGuidHex string,
	extendedPollTrigger func(pre, post lotSnapshot) bool,
	expect ExpectFn,
	cfg verifyingHandlerConfig,
) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := pickArgs(req.Args, allowedArgs...)
		t0 := time.Now()
		log.Printf("verifying-handler[%s]: start (snapshot level=%d)", op, snapshotLevel)

		// 1. Pre-snapshot (non-fatal).
		pre, preErr := snapshot(ctx, ipc, snapshotLevel, snapshotGuidHex, cfg.snapshotTimeout)
		log.Printf("verifying-handler[%s]: pre-snapshot done balance=%d objects=%d err=%v elapsed=%s",
			op, pre.balance, len(pre.objects), preErr, time.Since(t0))

		// 2. Forward the op.
		sendCtx, sendCancel := context.WithTimeout(ctx, cfg.sendTimeout)
		tSend := time.Now()
		ipcResp, err := ipc.Send(sendCtx, op, args)
		sendCancel()
		log.Printf("verifying-handler[%s]: forward done ipc.ok=%v err=%v elapsed=%s",
			op, ipcResp != nil && ipcResp.Ok, err, time.Since(tSend))
		if err != nil {
			return &convention.Response{Payload: map[string]any{
				"ok":      false,
				"verdict": "ipc-error",
				"error":   err.Error(),
				"op":      op,
			}}, nil
		}

		// 3. Bot-side rejection fast-path.
		if !ipcResp.Ok {
			out := map[string]any{
				"ok":      false,
				"verdict": "bot-rejected",
				"error":   ipcResp.Error,
				"op":      op,
			}
			if len(ipcResp.Payload) > 0 {
				var p map[string]any
				if err := json.Unmarshal(ipcResp.Payload, &p); err == nil {
					out["payload"] = p
				}
			}
			return &convention.Response{Payload: out}, nil
		}

		// 4. Settle.
		select {
		case <-time.After(cfg.settleWait):
		case <-ctx.Done():
			return ctxCancelledResponse(op, ctx), nil
		}

		// 5. Post-snapshot.
		tPost := time.Now()
		post, postErr := snapshot(ctx, ipc, snapshotLevel, snapshotGuidHex, cfg.snapshotTimeout)
		log.Printf("verifying-handler[%s]: post-snapshot done balance=%d objects=%d err=%v elapsed=%s",
			op, post.balance, len(post.objects), postErr, time.Since(tPost))

		// 6. Extended-poll loop.
		//
		// When the trigger fires (e.g. balance changed but no new persist_id),
		// re-poll up to extendedPollTimeout before falling through to expect().
		// This covers the two-phase server-side apply (Verify debits, Execute
		// spawns on a later tick) pattern observed in both buy-object and
		// delete-object.
		resolvedAfterPoll := false
		pollCount := 0
		if cfg.extendedPollTimeout > 0 && extendedPollTrigger != nil &&
			preErr == nil && postErr == nil && extendedPollTrigger(pre, post) {

			pollDeadline := time.Now().Add(cfg.extendedPollTimeout)
			for time.Now().Before(pollDeadline) {
				if !sleepRespectCtx(ctx, cfg.extendedPollInterval) {
					return ctxCancelledResponse(op, ctx), nil
				}
				pollCount++
				tPoll := time.Now()
				postPolled, pollErr := snapshot(ctx, ipc, snapshotLevel, snapshotGuidHex, cfg.snapshotTimeout)
				log.Printf("verifying-handler[%s]: extended-poll #%d balance=%d objects=%d err=%v elapsed=%s",
					op, pollCount, postPolled.balance, len(postPolled.objects), pollErr, time.Since(tPoll))
				if pollErr != nil {
					break
				}
				post = postPolled
				// Stop polling as soon as the trigger no longer fires — the
				// delta we were waiting for has landed.
				if !extendedPollTrigger(pre, post) {
					resolvedAfterPoll = true
					log.Printf("verifying-handler[%s]: extended-poll resolved after %d polls (%s past settle)",
						op, pollCount, time.Since(tPost))
					break
				}
			}
			if !resolvedAfterPoll {
				log.Printf("verifying-handler[%s]: extended-poll exhausted after %d polls (%s); falling through",
					op, pollCount, time.Since(tPost))
			}
		}

		// 7. Invoke expect and merge the result into a response.
		verdict, hints, extraPayload, _ := expect(pre, post, args, ipcResp)
		out := map[string]any{
			"op":      op,
			"verdict": verdict,
		}
		for k, v := range extraPayload {
			out[k] = v
		}

		// Merge snapshot errors: add the error string fields, and inject
		// hint tokens so callers can switch on them without parsing the
		// error string. These are appended AFTER expect() so the ExpectFn
		// does not need to thread preErr/postErr through the ExpectFn
		// signature (which is fixed by the spec).
		if preErr != nil {
			out["pre_snapshot_error"] = preErr.Error()
			hints = append(hints, "pre-snapshot-failed")
		}
		if postErr != nil {
			out["post_snapshot_error"] = postErr.Error()
			hints = append(hints, "post-snapshot-failed")
		}

		if len(hints) > 0 {
			// Merge with any hints the ExpectFn already placed in extraPayload
			// (e.g. verdictResponse sets "hints" for the silent-drop path).
			if existing, ok := out["hints"].([]string); ok {
				// ExpectFn already set hints — append ours without duplicating.
				seen := make(map[string]bool, len(existing))
				for _, h := range existing {
					seen[h] = true
				}
				merged := append([]string{}, existing...)
				for _, h := range hints {
					if !seen[h] {
						merged = append(merged, h)
						seen[h] = true
					}
				}
				out["hints"] = merged
			} else {
				out["hints"] = hints
			}
		}

		if resolvedAfterPoll {
			out["resolved_after_poll"] = true
			out["poll_count"] = pollCount
		}
		log.Printf("verifying-handler[%s]: done verdict=%s total_elapsed=%s", op, verdict, time.Since(t0))
		return &convention.Response{Payload: out}, nil
	}
}

// placementVerifyingHandler is buy-object's / place-from-inventory's robust
// successor for the build-buy verb family.
//
// The underlying VM has no negative-ACK for rejected build commands: it
// silently drops the placement and the agent observes the absence via the
// next perception frame (OQ-8, see buymode_handlers.go:27). That means
// simpleForwardingHandler returns ok:true / queued:true for *every* IPC the
// bot accepts, regardless of whether the placement actually materialized.
// Agents that trust the ack burn tokens guessing why their "successful" buys
// don't appear.
//
// This handler does the diff the agent would otherwise do:
//
//  1. Pre-snapshot: query-self (for balance) + query-lot-objects (for objects
//     at the target level, optionally filtered to the requested catalog GUID).
//  2. Forward the build op (buy-object / place-from-inventory).
//  3. If the bot itself rejected (ok:false), return that verdict immediately —
//     no point waiting for a VM tick that won't come.
//  4. Settle: brief sleep while the VM applies the command and the next
//     perception tick lands. Default 1500ms (FSO_PERCEPTION_HZ=1).
//  5. Post-snapshot: same two reads.
//  6. Diff by persist_id. A new persist_id on the level → placed=true with
//     full {object_id, persist_id, guid, x, y, level, dir, cost} payload. No
//     new persist_id → placed=false with structured hints (tile-occupied,
//     no-budget-debit, ipc-rejected, etc).
//
// Cost: 4 extra IPC round-trips per build (2 pre, 2 post) — all bot-local
// reads, sub-100ms each. The total wall time per buy-object is roughly:
//     pre-snapshot (200ms) + IPC send (100ms) + settle (1500ms) + post (200ms)
//   ≈ 2 seconds per placement, with a structured yes/no verdict.
//
// Compared to the agent's previous loop (issue buy-object, await 30s timeout,
// fire query-lot-objects to verify, retry with different dir, repeat) this
// caps token cost per placement at the IPC response shape — no exploratory
// querying.
//
// Use for: buy-object, place-from-inventory. Other build-buy ops have
// different verdict shapes — move-object (position changed), delete-object
// (persist_id absent), upgrade-object (upgrade level changed) — and warrant
// their own helpers if/when robustified.
func placementVerifyingHandler(ipc *IPC, op string, allowedArgs ...string) convention.HandlerFunc {
	cfg := defaultVerifyingConfig()
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		return verifyingHandlerImpl(ctx, ipc, op, allowedArgs, cfg, req)
	}
}

// verifyingHandlerImpl is the inner implementation, parameterised on config so
// tests can shrink settleWait without changing production defaults.
// It delegates to verifyingHandlerWithExpect using the placement-specific
// ExpectFn (verdictResponse) and trigger (balance dropped + no new persist_id).
func verifyingHandlerImpl(ctx context.Context, ipc *IPC, op string, allowedArgs []string, cfg verifyingHandlerConfig, req *convention.Request) (*convention.Response, error) {
	level := intArg(req.Args, "level", 1)

	// placementExpect wraps verdictResponse as an ExpectFn. Snapshot errors
	// are NOT passed to verdictResponse because the generic framework injects
	// the error string fields and "pre/post-snapshot-failed" hints after
	// expect() returns.
	placementExpect := func(pre, post lotSnapshot, args map[string]any, ipcResp *Response) (string, []string, map[string]any, bool) {
		resp := verdictResponse(op, args, pre, post, nil, nil, ipcResp)
		pay := resp.Payload.(map[string]any)
		verdict, _ := pay["verdict"].(string)
		placed, _ := pay["placed"].(bool)
		return verdict, nil, pay, placed
	}

	// placementTrigger: balance fell but no new persist_id has landed yet.
	// Fires when the VM's two-phase apply (Verify debits, Execute spawns) has
	// completed the debit but not the spawn.
	placementTrigger := func(pre, post lotSnapshot) bool {
		return newPersists(pre, post) == 0 && post.balance < pre.balance
	}

	handler := verifyingHandlerWithExpect(ipc, op, allowedArgs, level, "", placementTrigger, placementExpect, cfg)
	return handler(ctx, req)
}

// newPersists counts the persist_ids present in post but not pre. Skips
// zero PIDs — those are entities that haven't been registered with the global
// link yet and would race the diff anyway.
func newPersists(pre, post lotSnapshot) int {
	n := 0
	for _, o := range post.objects {
		if o.PersistID == 0 {
			continue
		}
		if _, existed := pre.objectsByPID[o.PersistID]; !existed {
			n++
		}
	}
	return n
}

// verifyingHandlerConfig tunes the verdict pipeline. Exposed as a type so
// tests can override (and a future env-var path can re-tune without code
// changes).
type verifyingHandlerConfig struct {
	// settleWait is the pause between forwarding the op and the FIRST post-
	// snapshot. Must cover at least one VM tick (~33ms) and one perception
	// emission cycle (1s at FSO_PERCEPTION_HZ=1). Default 1500ms.
	settleWait time.Duration

	// snapshotTimeout caps the duration of one pre/post snapshot. Snapshots
	// are cheap local-VM reads; this exists so a stuck bot does not hang the
	// build pipeline. Default 5s.
	snapshotTimeout time.Duration

	// sendTimeout caps the build op forward. Independent of snapshot timeout
	// because the build IPC can legitimately block on the lot tick queue.
	// Default 10s.
	sendTimeout time.Duration

	// extendedPollTimeout is the additional wall-clock budget the verdict path
	// will burn polling for the spawn delta when the initial post-snapshot
	// shows balance dropped but no new persist_id. Buy-object is two-phase
	// server-side (VMNetBuyObjectCmd.Verify debits via async transaction and
	// requeues, then Execute spawns on a LATER tick), so the balance-change
	// delta routinely arrives before the spawn delta. Without this poll, the
	// initial settleWait races the spawn and we false-negative real placements
	// as "balance-changed-no-object" — money gone, no object reported, agent
	// thinks the placement failed when it succeeded. Default 5s, polled at
	// extendedPollInterval.
	extendedPollTimeout time.Duration

	// extendedPollInterval is how often the extended-poll path re-snapshots
	// while waiting for the spawn delta. Default 400ms — small enough that
	// most races resolve in 1–2 polls, large enough to avoid hammering the
	// bot's tick lock.
	extendedPollInterval time.Duration
}

func defaultVerifyingConfig() verifyingHandlerConfig {
	return verifyingHandlerConfig{
		// Settle: VM applies the command on its tick (~33ms); perception fires
		// at FSO_PERCEPTION_HZ (default 1 Hz). 1500ms gives at least one tick
		// of slack.
		settleWait: 1500 * time.Millisecond,
		// Snapshot: query-self / query-lot-objects are local-VM reads that take
		// <500ms on a healthy bot. Cap at 3s so a busy bot doesn't push the
		// total handler runtime past the cf client's default 30s await on
		// busy lots. The handler does 4 snapshot reads (pre + post = 2×2) so
		// the worst case is 12s of snapshot time, leaving 18s headroom.
		snapshotTimeout: 3 * time.Second,
		// Build IPC send timeout — independent of snapshot cap; build commands
		// can legitimately block on the lot tick queue.
		sendTimeout: 10 * time.Second,
		// Extended poll: covers the gap between balance-change delta and
		// spawn delta. Empirically the spawn lands within 1-3 ticks of the
		// transaction callback; 5s is generous headroom under load.
		extendedPollTimeout:  5 * time.Second,
		extendedPollInterval: 400 * time.Millisecond,
	}
}

// objectRef is the slim shape extracted from query-lot-objects responses.
// We collapse to one entry per persist_id (multitiles already collapse server-
// side in the C# handler).
type objectRef struct {
	ObjectID  uint64
	PersistID uint64
	Guid      uint64
	GuidHex   string
	X         int
	Y         int
	Level     int
	Dir       int
}

// ActionQueueEntry is one item from the avatar's interaction queue as returned
// by query-self. The C# handler (QueryHandlers.cs:82) emits {interaction_id,
// name, target_object_id, status} for each queued action. W7a (freesoexperiment-177)
// confirmed action_queue is already on the wire; this struct parses it so
// snapshot() callers (including the future interact-with verifier, W8) can diff
// queue depth before and after an interaction request.
type ActionQueueEntry struct {
	InteractionID  int    `json:"interaction_id"`
	Name           string `json:"name"`
	TargetObjectID int    `json:"target_object_id"`
	Status         string `json:"status"`
}

// lotSnapshot is one moment of bot-observable lot state, scoped to the level
// and (optionally) catalog GUID the placement targets.
type lotSnapshot struct {
	balance     int64
	actionQueue []ActionQueueEntry
	objects      []objectRef
	objectsByPID map[uint64]objectRef // index for fast diff
}

// snapshot performs the pre/post lot read using two parallel-able IPC calls.
// We serialize them today to keep the bot's command-dispatch contract simple
// (one in-flight per family) — easily parallelizable later if it shows up in
// profiles.
func snapshot(ctx context.Context, ipc *IPC, level int, guidHex string, timeout time.Duration) (lotSnapshot, error) {
	snap := lotSnapshot{objectsByPID: map[uint64]objectRef{}}

	// balance via query-self
	selfCtx, selfCancel := context.WithTimeout(ctx, timeout)
	selfResp, err := ipc.Send(selfCtx, "query-self", map[string]any{})
	selfCancel()
	if err != nil {
		return snap, fmt.Errorf("query-self: %w", err)
	}
	if !selfResp.Ok {
		return snap, fmt.Errorf("query-self bot-rejected: %s", selfResp.Error)
	}
	var selfPayload struct {
		Balance     json.Number        `json:"balance"`
		ActionQueue []ActionQueueEntry `json:"action_queue"`
	}
	if err := json.Unmarshal(selfResp.Payload, &selfPayload); err == nil {
		if b, perr := selfPayload.Balance.Int64(); perr == nil {
			snap.balance = b
		}
		snap.actionQueue = selfPayload.ActionQueue
	}

	// objects via query-lot-objects
	objArgs := map[string]any{}
	if level > 0 {
		objArgs["level"] = level
	}
	if guidHex != "" {
		objArgs["guid_hex"] = guidHex
	}
	objCtx, objCancel := context.WithTimeout(ctx, timeout)
	objResp, err := ipc.Send(objCtx, "query-lot-objects", objArgs)
	objCancel()
	if err != nil {
		return snap, fmt.Errorf("query-lot-objects: %w", err)
	}
	if !objResp.Ok {
		return snap, fmt.Errorf("query-lot-objects bot-rejected: %s", objResp.Error)
	}
	var objPayload struct {
		Objects []struct {
			ObjectID  json.Number `json:"object_id"`
			PersistID json.Number `json:"persist_id"`
			Guid      json.Number `json:"guid"`
			GuidHex   string      `json:"guid_hex"`
			X         json.Number `json:"x"`
			Y         json.Number `json:"y"`
			Level     json.Number `json:"level"`
			Dir       json.Number `json:"dir"`
		} `json:"objects"`
	}
	if err := json.Unmarshal(objResp.Payload, &objPayload); err != nil {
		return snap, fmt.Errorf("decode query-lot-objects: %w", err)
	}
	for _, o := range objPayload.Objects {
		ref := objectRef{
			ObjectID:  numToU64(o.ObjectID),
			PersistID: numToU64(o.PersistID),
			Guid:      numToU64(o.Guid),
			GuidHex:   o.GuidHex,
			X:         int(numToU64(o.X)),
			Y:         int(numToU64(o.Y)),
			Level:     int(numToU64(o.Level)),
			Dir:       int(numToU64(o.Dir)),
		}
		// If guid_hex is empty but guid is present, synthesize it.
		if ref.GuidHex == "" && ref.Guid != 0 {
			ref.GuidHex = fmt.Sprintf("0x%X", ref.Guid)
		}
		snap.objects = append(snap.objects, ref)
		snap.objectsByPID[ref.PersistID] = ref
	}
	return snap, nil
}

// verdictResponse synthesizes the structured verdict from a snapshot diff.
// Pure function — easy to test directly.
func verdictResponse(op string, args map[string]any, pre, post lotSnapshot, preErr, postErr error, ipcResp *Response) *convention.Response {
	// Find new persist_ids in post that weren't in pre.
	var newObjects []objectRef
	for _, o := range post.objects {
		if _, existed := pre.objectsByPID[o.PersistID]; !existed && o.PersistID != 0 {
			newObjects = append(newObjects, o)
		}
	}

	// Happy path: one new object on the targeted level. Confidence is highest
	// when the new object matches the target tile and (for buy-object) the
	// catalog GUID.
	if len(newObjects) > 0 {
		obj := pickPlacedObject(newObjects, args)
		out := map[string]any{
			"ok":             true,
			"placed":         true,
			"verdict":        "placed",
			"op":             op,
			"object_id":      obj.ObjectID,
			"persist_id":     obj.PersistID,
			"x":              obj.X,
			"y":              obj.Y,
			"level":          obj.Level,
			"dir":            obj.Dir,
			"balance_before": pre.balance,
			"balance_after":  post.balance,
		}
		if obj.Guid != 0 {
			out["guid"] = obj.Guid
		}
		if obj.GuidHex != "" {
			out["guid_hex"] = obj.GuidHex
		}
		// Cost = pre - post; clamp to >= 0 since pre/post can race for unrelated
		// debits (rent, mail, etc.) that we don't try to disentangle.
		if pre.balance >= post.balance {
			out["cost"] = pre.balance - post.balance
		}
		// If more than one new object showed up, the caller deserves to know —
		// might mean a parallel build by another agent leaked in.
		if len(newObjects) > 1 {
			out["co_placed_count"] = len(newObjects) - 1
		}
		return &convention.Response{Payload: out}
	}

	// No new object → silent-drop. Build a hint set so the caller can act
	// instead of guessing.
	hints := buildHints(args, pre, post, preErr, postErr)
	out := map[string]any{
		"ok":      true,
		"placed":  false,
		"verdict": "silent-drop",
		"reason":  "VM accepted the IPC ack but no new object materialized after settle window — upstream FSO drops invalid build commands silently (OQ-8).",
		"op":      op,
		"target": map[string]any{
			"x":     args["x"],
			"y":     args["y"],
			"level": args["level"],
			"dir":   args["dir"],
		},
		"hints":          hints,
		"balance_before": pre.balance,
		"balance_after":  post.balance,
	}
	if preErr != nil {
		out["pre_snapshot_error"] = preErr.Error()
	}
	if postErr != nil {
		out["post_snapshot_error"] = postErr.Error()
	}
	// Pass the original bot ack through (it might carry queue or arg-echo info
	// useful for diagnosis).
	if ipcResp != nil && len(ipcResp.Payload) > 0 {
		var p map[string]any
		if err := json.Unmarshal(ipcResp.Payload, &p); err == nil {
			out["ipc_ack"] = p
		}
	}
	return &convention.Response{Payload: out}
}

// pickPlacedObject prefers the object whose tile coords match the target.
// For buy-object the args are in SUBTILE units (1/16-tile) while object dumps
// are in TILE units; divide by 16 before comparing.
func pickPlacedObject(candidates []objectRef, args map[string]any) objectRef {
	if len(candidates) == 1 {
		return candidates[0]
	}
	tileX, hasX := tileFromSubtileArg(args, "x")
	tileY, hasY := tileFromSubtileArg(args, "y")
	if !hasX || !hasY {
		return candidates[0]
	}
	for _, o := range candidates {
		if o.X == tileX && o.Y == tileY {
			return o
		}
	}
	return candidates[0]
}

// buildHints turns observable state diffs into actionable suggestions for the
// caller. Each hint is a short identifier the caller can switch on; humans
// can read the list and recognize the failure mode without doing their own
// diff.
func buildHints(args map[string]any, pre, post lotSnapshot, preErr, postErr error) []string {
	hints := []string{}
	if preErr != nil {
		hints = append(hints, "pre-snapshot-failed")
	}
	if postErr != nil {
		hints = append(hints, "post-snapshot-failed")
	}
	if post.balance == pre.balance {
		hints = append(hints, "no-budget-debit")
	} else if post.balance < pre.balance {
		// Money moved but no new object showed up. Either we missed it (rare —
		// settle is generous) or the VM debited then rolled back (FSO refunds
		// failed placements per buy-object.json description).
		hints = append(hints, "balance-changed-no-object")
	}
	tileX, hasX := tileFromSubtileArg(args, "x")
	tileY, hasY := tileFromSubtileArg(args, "y")
	level := intArg(args, "level", 1)
	if hasX && hasY {
		for _, o := range pre.objects {
			if o.X == tileX && o.Y == tileY && o.Level == level {
				hints = append(hints, "tile-occupied")
				break
			}
		}
	}
	return hints
}

// tileFromSubtileArg reads a subtile-coord arg (as accepted by buy-object) and
// returns the tile value. Returns (0,false) if the arg is missing or not a
// number.
func tileFromSubtileArg(args map[string]any, key string) (int, bool) {
	raw, ok := args[key]
	if !ok {
		return 0, false
	}
	sub, ok := anyToInt(raw)
	if !ok {
		return 0, false
	}
	return sub / 16, true
}

// intArg reads an int-valued arg with a default. Accepts json.Number, int,
// int64, float64, and numeric strings.
func intArg(args map[string]any, key string, def int) int {
	raw, ok := args[key]
	if !ok {
		return def
	}
	if v, ok := anyToInt(raw); ok {
		return v
	}
	return def
}

func anyToInt(v any) (int, bool) {
	switch x := v.(type) {
	case int:
		return x, true
	case int64:
		return int(x), true
	case float64:
		return int(x), true
	case json.Number:
		if i, err := x.Int64(); err == nil {
			return int(i), true
		}
		if f, err := x.Float64(); err == nil {
			return int(f), true
		}
	case string:
		// Accept hex or decimal.
		if strings.HasPrefix(x, "0x") || strings.HasPrefix(x, "0X") {
			if i, err := strconv.ParseInt(x[2:], 16, 64); err == nil {
				return int(i), true
			}
		}
		if i, err := strconv.ParseInt(x, 10, 64); err == nil {
			return int(i), true
		}
	}
	return 0, false
}

func numToU64(n json.Number) uint64 {
	if n == "" {
		return 0
	}
	if i, err := n.Int64(); err == nil {
		if i < 0 {
			return 0
		}
		return uint64(i)
	}
	if f, err := n.Float64(); err == nil {
		if f < 0 {
			return 0
		}
		return uint64(f)
	}
	return 0
}

// deleteVerifyingHandler is delete-object's robust successor (freesoexperiment-850).
//
// Two bugs we paper over here:
//
//  1. Same OQ-8 silent-drop as placement ops — the VM has no negative-ACK for
//     rejected destructive commands. simpleForwardingHandler returns ok:true /
//     queued:true regardless.
//  2. Multitile master-tile no-op: when target_object_id is the *base* tile
//     of a multitile group (e.g. the 2x3 Castle bed, the 3-tile drawer set),
//     the bot's IPC returns ok:true / queued:true but the delete silently
//     no-ops in the VM. Calling delete-object against a *subordinate* tile
//     of the same persist_id executes correctly. Discovered empirically
//     during freesoexperiment-83c cleanup. Root cause is in the C# bot's
//     delete dispatch (suspected: VMNetDeleteObjectCmd.Verify rejects when
//     called on a multitile base tile in some condition) — we work around
//     it here so callers see one clean op.
//
// Algorithm:
//   - Pre-snapshot: full level dump (no GUID filter, since we don't know the
//     target's GUID up front). Find the target_object_id, record its
//     persist_id and all sibling object_ids belonging to the same persist_id.
//   - Forward delete-object.
//   - Settle (1500ms — same VM tick / perception cadence as placement).
//   - Post-snapshot: is the persist_id still present?
//       no   → deleted=true, return refund / final state.
//       yes  → if pre-snapshot showed multiple tiles for that persist_id, pick
//              a subordinate tile's object_id and retry delete-object. Settle
//              again. Re-check.
//       yes after retry → deleted=false, silent-drop, hints={multitile-no-op-after-retry,
//                          tile-occupied?, ...}. Surface for caller diagnosis.
//
// Cost: 1 extra delete IPC in the multitile-master case (free in the common
// single-tile case). Plus the same 2-snapshot overhead as placement.
//
// Use for: delete-object. send-to-inventory has its own semantics (object
// goes to inventory, not vanishes from the world) so it's not wrapped here
// yet — TODO if we hit similar silent-drop patterns there.
func deleteVerifyingHandler(ipc *IPC, op string, allowedArgs ...string) convention.HandlerFunc {
	cfg := defaultVerifyingConfig()
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		return deleteVerifyingHandlerImpl(ctx, ipc, op, allowedArgs, cfg, req)
	}
}

func deleteVerifyingHandlerImpl(ctx context.Context, ipc *IPC, op string, allowedArgs []string, cfg verifyingHandlerConfig, req *convention.Request) (*convention.Response, error) {
	args := pickArgs(req.Args, allowedArgs...)
	targetID, hasTarget := tryGetU64(args, "target_object_id")
	log.Printf("delete-verifying[%s]: start target_object_id=%d", op, targetID)

	// resolvePIDAndSiblings extracts the target's persist_id and its sibling
	// tiles from a pre-snapshot. Called by both the trigger and the ExpectFn to
	// keep the logic co-located with the data it needs.
	resolvePIDAndSiblings := func(pre lotSnapshot) (pid uint64, siblings []objectRef) {
		if !hasTarget {
			return 0, nil
		}
		for _, o := range pre.objects {
			if o.ObjectID == targetID {
				pid = o.PersistID
				break
			}
		}
		if pid != 0 {
			for _, o := range pre.objects {
				if o.PersistID == pid && o.ObjectID != targetID {
					siblings = append(siblings, o)
				}
			}
		}
		return pid, siblings
	}

	// Extended-poll trigger for delete: refund landed (balance went up) but the
	// target's (oid, pid) tuple is still present. The entity-removal delta
	// arrives on a later tick than the refund transaction.
	deleteTrigger := func(pre, post lotSnapshot) bool {
		pid, _ := resolvePIDAndSiblings(pre)
		return objectTuplePresent(post, targetID, pid) && post.balance > pre.balance
	}

	// deleteExpect implements the verdict logic, including the multitile-master
	// retry. It captures ctx, ipc, cfg, op, args, targetID from the outer scope
	// so the ExpectFn signature (defined in the spec) can remain context-free.
	deleteExpect := func(pre, post lotSnapshot, _ map[string]any, ipcResp *Response) (string, []string, map[string]any, bool) {
		targetPID, siblings := resolvePIDAndSiblings(pre)
		log.Printf("delete-verifying[%s]: expect entry target_pid=%d siblings=%d", op, targetPID, len(siblings))

		if targetPID == 0 || !objectTuplePresent(post, targetID, targetPID) {
			// Either we couldn't resolve the persist_id in pre (so we trust the
			// bot's ok:true at face value), or the target's (oid, pid) tuple is
			// gone — success.
			resp, _ := deleteVerdict(op, args, pre, post, true, "", 0, ipcResp, nil, nil)
			pay := resp.Payload.(map[string]any)
			return "deleted", nil, pay, true
		}

		// Target's tuple still present after settle + optional poll.
		// Multitile master-tile no-op is the most common cause when we have
		// known siblings. Try the first subordinate.
		if len(siblings) == 0 {
			resp, _ := deleteVerdict(op, args, pre, post, false, "no-siblings-to-retry", 0, ipcResp, nil, nil)
			pay := resp.Payload.(map[string]any)
			return "silent-drop", []string{"no-siblings-to-retry"}, pay, false
		}

		sub := siblings[0]
		retryArgs := map[string]any{
			"target_object_id": sub.ObjectID,
		}
		if v, ok := args["cleanup_all"]; ok {
			retryArgs["cleanup_all"] = v
		}
		log.Printf("delete-verifying[%s]: master no-op suspected; retrying on subordinate object_id=%d (persist_id=%d)", op, sub.ObjectID, sub.PersistID)
		resp2, err := sendDelete(ctx, ipc, op, retryArgs, cfg.sendTimeout)
		if err != nil {
			pay := map[string]any{
				"ok":              false,
				"verdict":         "retry-ipc-error",
				"error":           err.Error(),
				"op":              op,
				"first_attempt":   ipcResp.Payload,
				"retry_target_id": sub.ObjectID,
			}
			return "retry-ipc-error", nil, pay, false
		}
		if !resp2.Ok {
			pay := map[string]any{
				"ok":              false,
				"verdict":         "retry-bot-rejected",
				"error":           resp2.Error,
				"op":              op,
				"retry_target_id": sub.ObjectID,
			}
			return "retry-bot-rejected", nil, pay, false
		}

		if !sleepRespectCtx(ctx, cfg.settleWait) {
			// Context cancelled during retry settle — surface as ctx-cancelled.
			pay := map[string]any{
				"ok":      false,
				"verdict": "ctx-cancelled",
				"error":   ctx.Err().Error(),
				"op":      op,
			}
			return "ctx-cancelled", nil, pay, false
		}

		post2, post2Err := snapshot(ctx, ipc, 0, "", cfg.snapshotTimeout)
		log.Printf("delete-verifying[%s]: post2 objects=%d err=%v", op, len(post2.objects), post2Err)

		if !objectTuplePresent(post2, targetID, targetPID) {
			resp, _ := deleteVerdict(op, args, pre, post2, true, "retried-on-subordinate", sub.ObjectID, resp2, nil, post2Err)
			pay := resp.Payload.(map[string]any)
			return "deleted", nil, pay, true
		}

		resp, _ := deleteVerdict(op, args, pre, post2, false, "multitile-no-op-after-retry", sub.ObjectID, resp2, nil, post2Err)
		pay := resp.Payload.(map[string]any)
		return "silent-drop", []string{"multitile-no-op-after-retry"}, pay, false
	}

	handler := verifyingHandlerWithExpect(ipc, op, allowedArgs, 0, "", deleteTrigger, deleteExpect, cfg)
	return handler(ctx, req)
}

// sendDelete is a wrapper around ipc.Send for delete-family ops with a fresh
// per-attempt deadline.
func sendDelete(ctx context.Context, ipc *IPC, op string, args map[string]any, sendTimeout time.Duration) (*Response, error) {
	sendCtx, cancel := context.WithTimeout(ctx, sendTimeout)
	defer cancel()
	return ipc.Send(sendCtx, op, args)
}

// persistPresent reports whether the snapshot still contains the persist_id.
func persistPresent(snap lotSnapshot, pid uint64) bool {
	_, ok := snap.objectsByPID[pid]
	return ok
}

// objectTuplePresent reports whether the snapshot contains an entry with both
// the given object_id AND persist_id. Stricter than persistPresent — handles
// the multitile partial-delete case where a master tile is gone but a
// subordinate sibling promoted into the BaseObject slot under a different
// object_id (the shared persist_id is "still present" in the broader sense,
// but the caller's specifically-named oid is gone).
func objectTuplePresent(snap lotSnapshot, oid, pid uint64) bool {
	for _, o := range snap.objects {
		if o.ObjectID == oid && o.PersistID == pid {
			return true
		}
	}
	return false
}

// sleepRespectCtx waits for the given duration but returns false if ctx is
// cancelled first. Returns true on normal completion.
func sleepRespectCtx(ctx context.Context, d time.Duration) bool {
	select {
	case <-time.After(d):
		return true
	case <-ctx.Done():
		return false
	}
}

func ctxCancelledResponse(op string, ctx context.Context) *convention.Response {
	return &convention.Response{Payload: map[string]any{
		"ok":      false,
		"verdict": "ctx-cancelled",
		"error":   ctx.Err().Error(),
		"op":      op,
	}}
}

// botRejectedPayload formats the structured "bot said no" response. Extracted
// so placementVerifyingHandler and deleteVerifyingHandler share the shape.
func botRejectedPayload(op string, resp *Response) map[string]any {
	out := map[string]any{
		"ok":      false,
		"verdict": "bot-rejected",
		"error":   resp.Error,
		"op":      op,
	}
	if len(resp.Payload) > 0 {
		var p map[string]any
		if err := json.Unmarshal(resp.Payload, &p); err == nil {
			out["payload"] = p
		}
	}
	return out
}

// deleteVerdict builds the final structured response from a delete attempt.
func deleteVerdict(op string, args map[string]any, pre, post lotSnapshot, deleted bool, mode string, retriedID uint64, ipcResp *Response, preErr, postErr error) (*convention.Response, error) {
	targetID, _ := tryGetU64(args, "target_object_id")
	if deleted {
		out := map[string]any{
			"ok":               true,
			"deleted":          true,
			"verdict":          "deleted",
			"op":               op,
			"target_object_id": targetID,
			"balance_before":   pre.balance,
			"balance_after":    post.balance,
		}
		// Refund clamping mirrors placement: a refund returns funds to balance,
		// so post > pre is expected for a successful delete. We don't clamp
		// negative because that would hide unexpected debits.
		if post.balance >= pre.balance {
			out["refund"] = post.balance - pre.balance
		}
		if mode != "" {
			out["note"] = mode
		}
		if retriedID != 0 {
			out["retried_on_object_id"] = retriedID
		}
		return &convention.Response{Payload: out}, nil
	}

	hints := []string{}
	if mode != "" {
		hints = append(hints, mode)
	}
	if preErr != nil {
		hints = append(hints, "pre-snapshot-failed")
	}
	if postErr != nil {
		hints = append(hints, "post-snapshot-failed")
	}
	out := map[string]any{
		"ok":               true,
		"deleted":          false,
		"verdict":          "silent-drop",
		"reason":           "VM accepted the IPC ack but the persist_id was still present after retry — upstream FSO drops invalid destructive commands silently (OQ-8).",
		"op":               op,
		"target_object_id": targetID,
		"hints":            hints,
		"balance_before":   pre.balance,
		"balance_after":    post.balance,
	}
	if preErr != nil {
		out["pre_snapshot_error"] = preErr.Error()
	}
	if postErr != nil {
		out["post_snapshot_error"] = postErr.Error()
	}
	if retriedID != 0 {
		out["retried_on_object_id"] = retriedID
	}
	if ipcResp != nil && len(ipcResp.Payload) > 0 {
		var p map[string]any
		if err := json.Unmarshal(ipcResp.Payload, &p); err == nil {
			out["ipc_ack"] = p
		}
	}
	return &convention.Response{Payload: out}, nil
}

// tryGetU64 reads a uint64-valued arg. Returns (0, false) if absent or
// non-numeric. delete-object's target_object_id is uint64.
func tryGetU64(args map[string]any, key string) (uint64, bool) {
	raw, ok := args[key]
	if !ok {
		return 0, false
	}
	switch x := raw.(type) {
	case uint64:
		return x, true
	case int:
		if x < 0 {
			return 0, false
		}
		return uint64(x), true
	case int64:
		if x < 0 {
			return 0, false
		}
		return uint64(x), true
	case float64:
		if x < 0 {
			return 0, false
		}
		return uint64(x), true
	case json.Number:
		if i, err := x.Int64(); err == nil && i >= 0 {
			return uint64(i), true
		}
	case string:
		if i, err := strconv.ParseUint(x, 10, 64); err == nil {
			return i, true
		}
	}
	return 0, false
}

// guidHexFromArgs extracts a hex-formatted GUID from the build args. buy-object
// takes guid as a decimal integer; we format it as the 0xHEX form
// query-lot-objects expects to narrow its filter. Returns "" if guid is
// absent or unparseable — the snapshot then reads all guids on the level,
// which is correct but slower.
func guidHexFromArgs(args map[string]any) string {
	raw, ok := args["guid"]
	if !ok {
		return ""
	}
	v, ok := anyToInt(raw)
	if !ok || v == 0 {
		return ""
	}
	return fmt.Sprintf("0x%X", uint64(v))
}

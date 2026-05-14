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
func verifyingHandlerImpl(ctx context.Context, ipc *IPC, op string, allowedArgs []string, cfg verifyingHandlerConfig, req *convention.Request) (*convention.Response, error) {
	args := pickArgs(req.Args, allowedArgs...)
	level := intArg(args, "level", 1)
	guidHex := guidHexFromArgs(args)
	t0 := time.Now()
	log.Printf("verifying-handler[%s]: start (target level=%d guid_hex=%s)", op, level, guidHex)

	// 1. Pre-snapshot. A snapshot failure is non-fatal — we still try the
	//    op so a wedged query-* path doesn't block builds outright. We
	//    just flag it in the verdict so the agent knows the diff is
	//    untrustworthy.
	pre, preErr := snapshot(ctx, ipc, level, guidHex, cfg.snapshotTimeout)
	log.Printf("verifying-handler[%s]: pre-snapshot done balance=%d objects=%d err=%v elapsed=%s", op, pre.balance, len(pre.objects), preErr, time.Since(t0))

	// 2. Forward the op.
	sendCtx, sendCancel := context.WithTimeout(ctx, cfg.sendTimeout)
	tSend := time.Now()
	ipcResp, err := ipc.Send(sendCtx, op, args)
	sendCancel()
	log.Printf("verifying-handler[%s]: forward done ipc.ok=%v err=%v elapsed=%s", op, ipcResp != nil && ipcResp.Ok, err, time.Since(tSend))
	if err != nil {
		return &convention.Response{Payload: map[string]any{
			"ok":      false,
			"verdict": "ipc-error",
			"error":   err.Error(),
			"op":      op,
		}}, nil
	}

	// 3. Bot-side rejection (e.g. caller-is-not-lot-owner gate). The bot
	//    returned a structured error — surface it cleanly and skip the
	//    settle/diff dance.
	if !ipcResp.Ok {
		out := map[string]any{
			"ok":      false,
			"verdict": "bot-rejected",
			"error":   ipcResp.Error,
			"op":      op,
		}
		// If the bot included a payload (e.g. owner_id/me for the
		// not-lot-owner case), pass it through.
		if len(ipcResp.Payload) > 0 {
			var payload map[string]any
			if err := json.Unmarshal(ipcResp.Payload, &payload); err == nil {
				out["payload"] = payload
			}
		}
		return &convention.Response{Payload: out}, nil
	}

	// 4. Settle. The VM applies the command on its next tick (~33ms) and
	//    perception emits at FSO_PERCEPTION_HZ (default 1 Hz). A settle
	//    of 1500ms gives the VM at least one tick and reduces the
	//    chance that the post-snapshot races the placement.
	select {
	case <-time.After(cfg.settleWait):
	case <-ctx.Done():
		return &convention.Response{Payload: map[string]any{
			"ok":      false,
			"verdict": "ctx-cancelled",
			"error":   ctx.Err().Error(),
			"op":      op,
		}}, nil
	}

	// 5. Post-snapshot.
	tPost := time.Now()
	post, postErr := snapshot(ctx, ipc, level, guidHex, cfg.snapshotTimeout)
	log.Printf("verifying-handler[%s]: post-snapshot done balance=%d objects=%d err=%v elapsed=%s", op, post.balance, len(post.objects), postErr, time.Since(tPost))

	// 6. Diff & verdict.
	resp := verdictResponse(op, args, pre, post, preErr, postErr, ipcResp)
	log.Printf("verifying-handler[%s]: done total_elapsed=%s", op, time.Since(t0))
	return resp, nil
}

// verifyingHandlerConfig tunes the verdict pipeline. Exposed as a type so
// tests can override (and a future env-var path can re-tune without code
// changes).
type verifyingHandlerConfig struct {
	// settleWait is the pause between forwarding the op and the post-snapshot.
	// Must cover at least one VM tick (~33ms) and one perception emission cycle
	// (1s at FSO_PERCEPTION_HZ=1). Default 1500ms.
	settleWait time.Duration

	// snapshotTimeout caps the duration of one pre/post snapshot. Snapshots
	// are cheap local-VM reads; this exists so a stuck bot does not hang the
	// build pipeline. Default 5s.
	snapshotTimeout time.Duration

	// sendTimeout caps the build op forward. Independent of snapshot timeout
	// because the build IPC can legitimately block on the lot tick queue.
	// Default 10s.
	sendTimeout time.Duration
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

// lotSnapshot is one moment of bot-observable lot state, scoped to the level
// and (optionally) catalog GUID the placement targets.
type lotSnapshot struct {
	balance      int64
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
		Balance json.Number `json:"balance"`
	}
	if err := json.Unmarshal(selfResp.Payload, &selfPayload); err == nil {
		if b, perr := selfPayload.Balance.Int64(); perr == nil {
			snap.balance = b
		}
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
	t0 := time.Now()
	log.Printf("delete-verifying[%s]: start target_object_id=%d", op, targetID)

	// Pre-snapshot: full level dump. We need the multitile sibling list, so we
	// take a wide snapshot rather than guid-narrowing.
	pre, preErr := snapshot(ctx, ipc, 0, "", cfg.snapshotTimeout)
	log.Printf("delete-verifying[%s]: pre-snapshot done objects=%d err=%v elapsed=%s", op, len(pre.objects), preErr, time.Since(t0))

	// Find the target — get its persist_id and all sibling object_ids on the
	// same persist_id.
	var targetPID uint64
	var siblings []objectRef
	if hasTarget {
		for _, o := range pre.objects {
			if o.ObjectID == targetID {
				targetPID = o.PersistID
				break
			}
		}
		if targetPID != 0 {
			for _, o := range pre.objects {
				if o.PersistID == targetPID && o.ObjectID != targetID {
					siblings = append(siblings, o)
				}
			}
		}
	}
	log.Printf("delete-verifying[%s]: resolved target_pid=%d siblings=%d", op, targetPID, len(siblings))

	// First-attempt forward.
	resp1, err := sendDelete(ctx, ipc, op, args, cfg.sendTimeout)
	if err != nil {
		return &convention.Response{Payload: map[string]any{
			"ok":      false,
			"verdict": "ipc-error",
			"error":   err.Error(),
			"op":      op,
		}}, nil
	}
	if !resp1.Ok {
		out := botRejectedPayload(op, resp1)
		return &convention.Response{Payload: out}, nil
	}

	// Settle.
	if !sleepRespectCtx(ctx, cfg.settleWait) {
		return ctxCancelledResponse(op, ctx), nil
	}

	// Post-snapshot: is the persist_id gone?
	post1, post1Err := snapshot(ctx, ipc, 0, "", cfg.snapshotTimeout)
	log.Printf("delete-verifying[%s]: post1 objects=%d err=%v elapsed=%s", op, len(post1.objects), post1Err, time.Since(t0))

	if targetPID == 0 || !persistPresent(post1, targetPID) {
		// Either we couldn't resolve the persist_id in pre (so we trust the
		// bot's ok:true at face value), or it's gone — success.
		return deleteVerdict(op, args, pre, post1, true, "", 0, resp1, preErr, post1Err)
	}

	// First attempt didn't take. Multitile master-tile no-op is the most
	// common cause when we have known siblings. Try the first subordinate.
	if len(siblings) == 0 {
		return deleteVerdict(op, args, pre, post1, false, "no-siblings-to-retry", 0, resp1, preErr, post1Err)
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
		// Retry transport failure — surface it with the original pre-snapshot
		// context so caller can act.
		return &convention.Response{Payload: map[string]any{
			"ok":              false,
			"verdict":         "retry-ipc-error",
			"error":           err.Error(),
			"op":              op,
			"first_attempt":   resp1.Payload,
			"retry_target_id": sub.ObjectID,
		}}, nil
	}
	if !resp2.Ok {
		out := map[string]any{
			"ok":              false,
			"verdict":         "retry-bot-rejected",
			"error":           resp2.Error,
			"op":              op,
			"retry_target_id": sub.ObjectID,
		}
		return &convention.Response{Payload: out}, nil
	}

	if !sleepRespectCtx(ctx, cfg.settleWait) {
		return ctxCancelledResponse(op, ctx), nil
	}

	post2, post2Err := snapshot(ctx, ipc, 0, "", cfg.snapshotTimeout)
	log.Printf("delete-verifying[%s]: post2 objects=%d err=%v elapsed=%s", op, len(post2.objects), post2Err, time.Since(t0))

	if !persistPresent(post2, targetPID) {
		return deleteVerdict(op, args, pre, post2, true, "retried-on-subordinate", sub.ObjectID, resp2, preErr, post2Err)
	}

	return deleteVerdict(op, args, pre, post2, false, "multitile-no-op-after-retry", sub.ObjectID, resp2, preErr, post2Err)
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

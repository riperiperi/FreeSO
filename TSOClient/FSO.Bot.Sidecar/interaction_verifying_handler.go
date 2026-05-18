/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"encoding/json"
	"log"
	"time"
)

// pieMenuQueryTimeout caps the query-pie-menu IPC call in the pre-check.
// The query is a local-VM tick-lock acquisition — it should complete in well
// under 1s on a healthy bot. We cap at 3s to match the snapshot IPC cap used
// by the verifying handler, while staying short enough that a stuck bot
// doesn't materially delay the rejection path.
const pieMenuQueryTimeout = 3 * time.Second

// pieMenuPreCheckResult is returned by queryPieMenuPreCheck.
type pieMenuPreCheckResult struct {
	// refused is true when the pre-check found the matching entry with
	// available!=true. The caller must return refusalResp immediately and NOT
	// forward the IPC.
	refused     bool
	refusalResp map[string]any

	// infraFailed is true when the query-pie-menu IPC itself failed (network,
	// timeout, bot rejected). Per spec the caller falls through and lets the
	// verifying handler proceed normally — infrastructure failure must NOT block.
	infraFailed bool
	infraErr    string
}

// queryPieMenuPreCheck issues a query-pie-menu IPC for callee_id and looks up
// the entry matching interactionID. Returns a pieMenuPreCheckResult describing
// what to do next:
//
//   - refused=true  → the engine says available=false for this interaction;
//     return refusalResp immediately without consuming a verifier slot.
//   - infraFailed=true → the IPC itself failed (or entry not found); fall
//     through to the verifying handler. Fail-open: infrastructure glitches
//     must NOT block the caller.
//   - neither       → the interaction is available; proceed normally.
//
// Delegates to queryPieMenu (cross_level.go) for the IPC call, reusing its
// timeout (pieMenuQueryTimeout, 3s) and error handling.
func queryPieMenuPreCheck(ctx context.Context, ipc *IPC, calleeID int, interactionID int) pieMenuPreCheckResult {
	qctx, cancel := context.WithTimeout(ctx, pieMenuQueryTimeout)
	defer cancel()

	entries, err := queryPieMenu(qctx, ipc, int64(calleeID))
	if err != nil {
		log.Printf("pie-menu-pre-check: IPC error callee_id=%d interaction=%d: %v (falling through)", calleeID, interactionID, err)
		return pieMenuPreCheckResult{infraFailed: true, infraErr: err.Error()}
	}

	// Find the matching entry by TTAB id (int64 in pieMenuEntry).
	for _, entry := range entries {
		if entry.ID != int64(interactionID) {
			continue
		}
		if !entry.Available {
			// Engine says this interaction is unavailable. Refuse immediately.
			log.Printf("pie-menu-pre-check: REFUSING callee_id=%d interaction=%d available=false gates=%v", calleeID, interactionID, entry.Gates)
			gates := entry.Gates
			if gates == nil {
				gates = []string{}
			}
			return pieMenuPreCheckResult{
				refused: true,
				refusalResp: map[string]any{
					"ok":        false,
					"verdict":   "bot-rejected",
					"reason":    "interaction unavailable",
					"available": entry.Available,
					"gates":     gates,
					"callee_id": calleeID,
				},
			}
		}
		// available=true → proceed.
		log.Printf("pie-menu-pre-check: OK callee_id=%d interaction=%d available=true", calleeID, interactionID)
		return pieMenuPreCheckResult{}
	}

	// Entry not found in the pie menu at all — treat as fall-through (the bot's
	// IPC-level validation will catch a truly invalid interaction ID).
	log.Printf("pie-menu-pre-check: interaction=%d not found in pie-menu for callee_id=%d (falling through)", interactionID, calleeID)
	return pieMenuPreCheckResult{infraFailed: true, infraErr: "interaction not found in pie-menu"}
}

// interactExpectFn is the ExpectFn for interact-with. It diffs the action_queue
// between the pre- and post-snapshots to determine whether the interaction was
// accepted by the VM.
//
// Verdict logic:
//
//  1. Find a new entry in post.actionQueue not present in pre.actionQueue where
//     TargetObjectID == callee_id.
//     - If found with status="running" → verdict "interaction-started" (the VM
//       has already begun executing the interaction within the settle window).
//     - If found with any other status → verdict "queued".
//  2. If no such entry is found → verdict "silent-drop".
//     Possible hints:
//     - "unavailable-interaction-no-event" — the VM did not enqueue the
//       interaction. This is the most common cause: the TTAB check tree rejected
//       the interaction (target out of range, incompatible state, busy refusing).
//     - "target-out-of-range" — callee_id not seen in any snapshot data (cannot
//       confirm it's on the lot). Appended alongside "unavailable-interaction-no-event".
//
// callee_id matching: the snapshot's ActionQueueEntry.TargetObjectID is an int
// (cast from uint64 in the C# handler). callee_id arrives as float64 from JSON.
// We use anyToInt for robust comparison.
//
// The ExpectFn signature is fixed by the verifyingHandlerWithExpect spec —
// pre, post, args, ipcResp are all provided by the framework.
func interactExpectFn(pre, post lotSnapshot, args map[string]any, ipcResp *Response) (verdict string, hints []string, payload map[string]any, ok bool) {
	calleeID := intArg(args, "callee_id", -1)

	// Collect pre action_queue UIDs for fast lookup so we only count NEW entries.
	preUIDs := make(map[int]bool, len(pre.actionQueue))
	for _, e := range pre.actionQueue {
		preUIDs[e.InteractionID] = true
	}

	// Scan post.actionQueue for new entries targeting callee_id. Strict-match
	// first; on miss, fall back to single-new-entry attribution (see below).
	var newEntries []ActionQueueEntry
	for _, e := range post.actionQueue {
		if preUIDs[e.InteractionID] {
			continue
		}
		if calleeID >= 0 && e.TargetObjectID == calleeID {
			out := map[string]any{
				"ok":               true,
				"action_uid":       e.InteractionID,
				"target_object_id": e.TargetObjectID,
				"interaction_name": e.Name,
				"status":           e.Status,
				"callee_id":        calleeID,
			}
			if e.Status == "running" {
				out["verdict"] = "interaction-started"
				return "interaction-started", nil, out, true
			}
			out["verdict"] = "queued"
			return "queued", nil, out, true
		}
		newEntries = append(newEntries, e)
	}

	// Fallback: when the bot's VMNetInteractionCmd dispatch reports ok=true but
	// no new entry matches callee_id strictly, the engine has committed the
	// queue entry against an interaction-socket sub-object whose ObjectID differs
	// from the caller-supplied callee_id. If the bot ack confirms queued:true
	// AND exactly one new non-idle entry exists in post, attribute it to this op.
	// The queued:true gate keeps unrelated traffic (e.g. another agent's social
	// landing in the same tick) from being mis-attributed when our IPC was a no-op.
	ackQueued := false
	if ipcResp != nil && len(ipcResp.Payload) > 0 {
		var ack struct {
			Queued bool `json:"queued"`
		}
		if err := json.Unmarshal(ipcResp.Payload, &ack); err == nil {
			ackQueued = ack.Queued
		}
	}
	if ackQueued && len(newEntries) == 1 {
		e := newEntries[0]
		out := map[string]any{
			"ok":               true,
			"action_uid":       e.InteractionID,
			"target_object_id": e.TargetObjectID,
			"interaction_name": e.Name,
			"status":           e.Status,
			"callee_id":        calleeID,
			"matched_by":       "single-new-entry",
		}
		if e.Status == "running" {
			out["verdict"] = "interaction-started"
			return "interaction-started", nil, out, true
		}
		out["verdict"] = "queued"
		return "queued", nil, out, true
	}

	// No new queue entry → silent-drop.
	h := []string{"unavailable-interaction-no-event"}

	// Append target-out-of-range hint if callee_id was not seen in either snapshot.
	if calleeID >= 0 && !calleeIDInObjects(pre, calleeID) && !calleeIDInObjects(post, calleeID) {
		h = append(h, "target-out-of-range")
	}

	out := map[string]any{
		"ok":        false,
		"verdict":   "silent-drop",
		"reason":    "VM accepted the IPC ack but no new action_queue entry appeared after settle window — the interaction was silently rejected (TTAB check failure, target out of range, or incompatible state).",
		"callee_id": calleeID,
		"hints":     h,
	}
	// Pass the IPC ack through for diagnostics (mirrors buy-object / delete-object pattern).
	if ipcResp != nil && len(ipcResp.Payload) > 0 {
		var p map[string]any
		if err := json.Unmarshal(ipcResp.Payload, &p); err == nil {
			out["ipc_ack"] = p
		}
	}
	return "silent-drop", h, out, false
}

// calleeIDInObjects returns true if any object in the snapshot has ObjectID == id.
// Used to detect whether the target object is on the lot at all (range hint).
func calleeIDInObjects(snap lotSnapshot, id int) bool {
	target := uint64(id) //nolint:gosec // id is validated > 0 by caller
	for _, o := range snap.objects {
		if o.ObjectID == target {
			return true
		}
	}
	return false
}

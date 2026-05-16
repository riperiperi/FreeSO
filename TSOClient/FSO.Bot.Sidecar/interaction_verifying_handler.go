/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import "encoding/json"

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

	// Scan post.actionQueue for new entries targeting callee_id.
	for _, e := range post.actionQueue {
		if preUIDs[e.InteractionID] {
			// This entry existed before — not caused by our interact-with.
			continue
		}
		if calleeID >= 0 && e.TargetObjectID != calleeID {
			// New entry, but targeting a different object.
			continue
		}
		// New entry with matching target → success.
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

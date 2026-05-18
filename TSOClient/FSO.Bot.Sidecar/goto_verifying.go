/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"encoding/json"
	"strings"
)

// gotoInteractionExpect is the ExpectFn for go-to when --interaction is set
// (W11, freesoexperiment-596). The bot resolves the interaction name to a TTAB
// id internally and dispatches VMNetInteractionCmd — the same wire command as
// interact-with. We therefore diff action_queue exactly as interactExpectFn
// does, but extract the callee object id from the IPC response payload
// (picked_object_id) rather than from a caller-supplied arg.
//
// Verdict logic:
//
//  1. Parse picked_object_id from ipcResp.Payload (the bot's "walk-and-do"
//     response). If the response's mode is not "walk-and-do" or the field is
//     absent, fall through to pre/post action_queue diff without callee filter.
//  2. Find a new entry in post.actionQueue not present in pre.actionQueue where
//     TargetObjectID == picked_object_id (if known).
//     - status="running" → verdict "interaction-started".
//     - any other status → verdict "queued".
//  3. No new entry → verdict "silent-drop".
//     Hints:
//     - "unavailable-interaction-no-event" — VM did not enqueue.
//     - "target-out-of-range" — picked_object_id not in either snapshot.
//
// The ExpectFn signature is fixed by the verifyingHandlerWithExpect spec.
func gotoInteractionExpect(pre, post lotSnapshot, args map[string]any, ipcResp *Response) (verdict string, hints []string, payload map[string]any, ok bool) {
	// Extract picked_object_id from the bot's walk-and-do IPC ack.
	calleeID := -1 // -1 = unknown
	if ipcResp != nil && len(ipcResp.Payload) > 0 {
		var ack struct {
			Mode           string `json:"mode"`
			PickedObjectID int    `json:"picked_object_id"`
		}
		if err := json.Unmarshal(ipcResp.Payload, &ack); err == nil && ack.Mode == "walk-and-do" && ack.PickedObjectID > 0 {
			calleeID = ack.PickedObjectID
		}
	}

	// Collect pre action_queue UIDs for fast lookup.
	preUIDs := make(map[int]bool, len(pre.actionQueue))
	for _, e := range pre.actionQueue {
		preUIDs[e.InteractionID] = true
	}

	// Scan post.actionQueue for new entries targeting calleeID.
	for _, e := range post.actionQueue {
		if preUIDs[e.InteractionID] {
			continue
		}
		if calleeID >= 0 && e.TargetObjectID != calleeID {
			continue
		}
		// New entry with matching target → success.
		out := map[string]any{
			"ok":               true,
			"action_uid":       e.InteractionID,
			"target_object_id": e.TargetObjectID,
			"interaction_name": e.Name,
			"status":           e.Status,
		}
		if calleeID >= 0 {
			out["callee_id"] = calleeID
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

	// Append target-out-of-range hint when we know the callee and it isn't visible.
	if calleeID >= 0 && !calleeIDInObjects(pre, calleeID) && !calleeIDInObjects(post, calleeID) {
		h = append(h, "target-out-of-range")
	}

	out := map[string]any{
		"ok":      false,
		"verdict": "silent-drop",
		"reason":  "VM accepted the go-to IPC ack but no new action_queue entry appeared after settle window — the walk-and-do interaction was silently rejected (target out of range, incompatible state, or no matching pie-menu entry).",
		"hints":   h,
	}
	if calleeID >= 0 {
		out["callee_id"] = calleeID
	}
	if ipcResp != nil && len(ipcResp.Payload) > 0 {
		var p map[string]any
		if err := json.Unmarshal(ipcResp.Payload, &p); err == nil {
			out["ipc_ack"] = p
		}
	}
	return "silent-drop", h, out, false
}

// socialExpectFn is the ExpectFn for the directed-social verbs (be-friendly,
// tell-joke, flirt, be-mean, give-gift). The bot resolves the verb to a
// TTAB id via pie-menu alias matching and dispatches VMNetInteractionCmd
// (SocialHandlers.DirectedSocial). The action_queue diff is therefore
// identical to interact-with / go-to-with-interaction.
//
// callee_id is extracted from args["target_sim_id"] or args["target_object_id"],
// falling back to the bot's IPC ack field "callee_id" (ObjectID of the target
// avatar, as opposed to its persist_id in target_sim_id).
//
// Verdict logic mirrors interactExpectFn:
//   - new entry, status="running"  → "interaction-started"
//   - new entry, other status      → "queued"
//   - no new entry                 → "silent-drop"
func socialExpectFn(pre, post lotSnapshot, args map[string]any, ipcResp *Response) (verdict string, hints []string, payload map[string]any, ok bool) {
	// The directed-social IPC response carries "callee_id" (the VM ObjectID of
	// the target avatar). Use that first — it's the most direct link to
	// action_queue TargetObjectID. Fall back to probing the args.
	calleeID := -1
	if ipcResp != nil && len(ipcResp.Payload) > 0 {
		var ack struct {
			CalleeID int `json:"callee_id"`
		}
		if err := json.Unmarshal(ipcResp.Payload, &ack); err == nil && ack.CalleeID > 0 {
			calleeID = ack.CalleeID
		}
	}
	// matchedName from the bot ack: "Nice/Hug", "Romance/Blow a Kiss". The queue
	// entry's name is the short variant ("Hug", "Blow a Kiss") because the engine
	// commits the resolved leaf interaction. Fall back to suffix-match when the
	// callee_id strict match misses (observed: bot ack callee_id is the target
	// VMAvatar ObjectID; the queue entry's TargetObjectID is the spawned
	// interaction socket sub-object, a different ObjectID).
	matchedName := ""
	if ipcResp != nil && len(ipcResp.Payload) > 0 {
		var ack struct {
			MatchedName string `json:"matched_name"`
		}
		if err := json.Unmarshal(ipcResp.Payload, &ack); err == nil {
			matchedName = ack.MatchedName
		}
	}

	// Collect pre action_queue UIDs for fast lookup.
	preUIDs := make(map[int]bool, len(pre.actionQueue))
	for _, e := range pre.actionQueue {
		preUIDs[e.InteractionID] = true
	}

	// Two passes: strict callee_id match first (preserves prior behavior),
	// then matched_name suffix-match across non-idle new entries.
	var fallback *ActionQueueEntry
	for i := range post.actionQueue {
		e := post.actionQueue[i]
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
		// Track first new entry whose name appears as the trailing segment of
		// matched_name ("Nice/Hug" → "Hug"; "Romance/Blow a Kiss" → "Blow a Kiss").
		if fallback == nil && matchedName != "" && e.Name != "" {
			if matchedName == e.Name || strings.HasSuffix(matchedName, "/"+e.Name) {
				ent := e
				fallback = &ent
			}
		}
	}

	if fallback != nil {
		out := map[string]any{
			"ok":                true,
			"action_uid":        fallback.InteractionID,
			"target_object_id":  fallback.TargetObjectID,
			"interaction_name":  fallback.Name,
			"status":            fallback.Status,
			"matched_by":        "matched_name",
			"matched_name":      matchedName,
		}
		if calleeID >= 0 {
			out["callee_id"] = calleeID
		}
		if fallback.Status == "running" {
			out["verdict"] = "interaction-started"
			return "interaction-started", nil, out, true
		}
		out["verdict"] = "queued"
		return "queued", nil, out, true
	}

	// No new queue entry → silent-drop.
	h := []string{"unavailable-interaction-no-event"}

	if calleeID >= 0 && !calleeIDInObjects(pre, calleeID) && !calleeIDInObjects(post, calleeID) {
		h = append(h, "target-out-of-range")
	}

	out := map[string]any{
		"ok":      false,
		"verdict": "silent-drop",
		"reason":  "VM accepted the directed-social IPC ack but no new action_queue entry appeared after settle window — the interaction was silently rejected (target out of range, incompatible state, or no matching pie-menu entry).",
		"hints":   h,
	}
	if calleeID >= 0 {
		out["callee_id"] = calleeID
	}
	if ipcResp != nil && len(ipcResp.Payload) > 0 {
		var p map[string]any
		if err := json.Unmarshal(ipcResp.Payload, &p); err == nil {
			out["ipc_ack"] = p
		}
	}
	return "silent-drop", h, out, false
}

// defaultGotoInteractVerifyingConfig returns a verifyingHandlerConfig for
// go-to-with-interaction. Extended polling is disabled for the same reason as
// interact-with: action_queue changes appear synchronously within one VM tick.
func defaultGotoInteractVerifyingConfig() verifyingHandlerConfig {
	cfg := defaultVerifyingConfig()
	cfg.extendedPollTimeout = 0
	return cfg
}

// defaultSocialVerifyingConfig returns a verifyingHandlerConfig for directed
// social verbs. Same as gotoInteract: no extended polling.
func defaultSocialVerifyingConfig() verifyingHandlerConfig {
	cfg := defaultVerifyingConfig()
	cfg.extendedPollTimeout = 0
	return cfg
}

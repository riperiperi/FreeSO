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
	"math"
	"strings"

	"github.com/campfire-net/campfire/pkg/convention"
)

// crossLevelResult is returned by findStairForCrossLevel when a stair is required.
// ObjectID is the IPC object_id of the closest stair. Level is the stair's
// floor (always between current and target floor). DistanceTiles is the
// distance from the avatar's current position.
type crossLevelResult struct {
	ObjectID     int64
	Level        int64
	DistanceTiles float64
}

// ExtractTargetLevel returns the target floor level from walk-to or go-to args.
// Walk-to args carry level directly as a top-level key; go-to args carry it
// inside a location sub-map. Returns (level, true) when found, (1, false)
// otherwise (caller should treat false as "level not specified" — same-level
// navigation, no stair check needed).
func ExtractTargetLevel(args map[string]any) (int64, bool) {
	// Direct level key (walk-to style).
	if v, ok := args["level"]; ok {
		if lv, ok2 := coerceInt64(v); ok2 && lv > 0 {
			return lv, true
		}
	}
	// Nested location map (go-to style: {x, y, level}).
	if loc, ok := args["location"]; ok {
		if locMap, ok2 := loc.(map[string]any); ok2 {
			if v, ok3 := locMap["level"]; ok3 {
				if lv, ok4 := coerceInt64(v); ok4 && lv > 0 {
					return lv, true
				}
			}
		}
	}
	return 1, false
}

// queueStairThenDestination issues two IPC commands:
//  1. interact-with on the stair (queue_mode=queue) — traverses the portal.
//  2. destinationOp (walk-to or go-to) with queue_mode=queue — arrives at the target.
//
// Both are issued with queue_mode=queue so they chain behind any existing action.
// The second command always has queue_mode overridden to "queue" regardless of
// what the original request specified, so it does not cancel the stair traversal.
//
// Returns the combined response indicating cross-level navigation was initiated.
func queueStairThenDestination(ctx context.Context, ipc *IPC, stair *crossLevelResult, destinationOp string, destArgs map[string]any) (*convention.Response, error) {
	// Step 1: find the climb interaction ID (best-effort; 0 is the portal default).
	climbID := climbInteractionID(ctx, ipc, stair.ObjectID)

	// Step 2: queue the Climb-Stairs interact-with. We use queue_mode="queue" so
	// it enqueues behind any running action but does not cancel the whole queue.
	climbResp, climbErr := ipc.Send(ctx, "interact-with", map[string]any{
		"callee_id":  stair.ObjectID,
		"interaction": climbID,
		"queue_mode": "queue",
	})
	if climbErr != nil {
		return &convention.Response{
			Payload: map[string]any{
				"ok":     false,
				"reason": "category:no-stair-path",
				"error":  "climb-stairs IPC failed: " + climbErr.Error(),
			},
		}, nil
	}
	if climbResp != nil && !climbResp.Ok {
		return &convention.Response{
			Payload: map[string]any{
				"ok":     false,
				"reason": "category:no-stair-path",
				"error":  "climb-stairs refused: " + climbResp.Error,
			},
		}, nil
	}

	// Step 3: queue the destination walk/go with queue_mode=queue so it runs
	// AFTER Climb-Stairs completes, not in parallel.
	chainedArgs := make(map[string]any, len(destArgs)+1)
	for k, v := range destArgs {
		chainedArgs[k] = v
	}
	chainedArgs["queue_mode"] = "queue"

	destResp, destErr := ipc.Send(ctx, destinationOp, chainedArgs)
	if destErr != nil {
		return &convention.Response{
			Payload: map[string]any{
				"ok":     false,
				"error":  destinationOp + " IPC failed after stair queue: " + destErr.Error(),
			},
		}, nil
	}

	// Build merged response. Unpack the destination payload so the agent sees
	// both the cross_level metadata AND the standard queued/ok fields.
	var destPayload map[string]any
	if destResp != nil && len(destResp.Payload) > 0 {
		_ = json.Unmarshal(destResp.Payload, &destPayload)
	}

	result := map[string]any{
		"ok":                destResp != nil && destResp.Ok,
		"cross_level":       true,
		"stair_object_id":   stair.ObjectID,
		"stair_level":       stair.Level,
		"stair_distance_tiles": stair.DistanceTiles,
		"climb_interaction": climbID,
	}
	if destPayload != nil {
		for k, v := range destPayload {
			if _, exists := result[k]; !exists {
				result[k] = v
			}
		}
	}
	if destResp != nil && !destResp.Ok {
		result["error"] = destResp.Error
	}
	return &convention.Response{Payload: result}, nil
}

// coerceInt64 converts common JSON numeric types (float64, int, int64, json.Number)
// to int64. Returns (0, false) for non-numeric values.
func coerceInt64(v any) (int64, bool) {
	switch t := v.(type) {
	case int64:
		return t, true
	case int:
		return int64(t), true
	case float64:
		return int64(t), true
	case json.Number:
		n, err := t.Int64()
		if err != nil {
			f, err2 := t.Float64()
			if err2 != nil {
				return 0, false
			}
			return int64(f), true
		}
		return n, true
	default:
		return 0, false
	}
}

// querySelfLevel issues a query-self IPC call and returns the avatar's current
// floor level (1-based). Returns (0, err) on IPC failure or missing data.
func querySelfLevel(ctx context.Context, ipc *IPC) (int64, error) {
	resp, err := ipc.Send(ctx, "query-self", map[string]any{})
	if err != nil {
		return 0, fmt.Errorf("query-self: %w", err)
	}
	if !resp.Ok {
		return 0, fmt.Errorf("query-self refused: %s", resp.Error)
	}
	// Payload shape: {"persist_id":..., "position":{"x","y","level",...}, ...}
	var payload struct {
		Position struct {
			Level float64 `json:"level"`
		} `json:"position"`
	}
	if err := json.Unmarshal(resp.Payload, &payload); err != nil {
		return 0, fmt.Errorf("query-self parse: %w", err)
	}
	return int64(payload.Position.Level), nil
}

// nearbyObject is the minimal projection of a query-nearby nearby_objects entry.
type nearbyObject struct {
	ObjectID      int64   `json:"object_id"`
	Name          string  `json:"name"`
	DistanceTiles float64 `json:"distance_tiles"`
	Position      struct {
		Level float64 `json:"level"`
	} `json:"position"`
}

// queryNearbyObjects issues a query-nearby IPC call and returns the raw
// nearby_objects slice. radiusTiles <= 0 uses the bot default (20 tiles).
func queryNearbyObjects(ctx context.Context, ipc *IPC, radiusTiles float64) ([]nearbyObject, error) {
	args := map[string]any{}
	if radiusTiles > 0 {
		args["radius_tiles"] = radiusTiles
	}
	resp, err := ipc.Send(ctx, "query-nearby", args)
	if err != nil {
		return nil, fmt.Errorf("query-nearby: %w", err)
	}
	if !resp.Ok {
		return nil, fmt.Errorf("query-nearby refused: %s", resp.Error)
	}
	var payload struct {
		NearbyObjects []nearbyObject `json:"nearby_objects"`
	}
	if err := json.Unmarshal(resp.Payload, &payload); err != nil {
		return nil, fmt.Errorf("query-nearby parse: %w", err)
	}
	return payload.NearbyObjects, nil
}

// isStairObject returns true when the object name looks like a stair portal.
// The heuristic matches any name that contains "stair" (case-insensitive).
// Stairs in FSO are OBJDType.Portal=8 (OBJD.cs:23) and always include "Stair"
// in their user-facing name (catalog entries: stair.piff, stair2.piff,
// stair3.piff, stair4.piff, stairsspiral.piff). The C# bot does not emit
// OBJDType in the perception stream so name-matching is the only viable sidecar
// heuristic without a C# engine change.
func isStairObject(name string) bool {
	return strings.Contains(strings.ToLower(name), "stair")
}

// pieMenuEntry is a minimal projection of one interact-with pie-menu entry,
// as returned by query-pie-menu. ID is the TTAB index to pass to interact-with.
type pieMenuEntry struct {
	ID   int64  `json:"id"`
	Name string `json:"name"`
}

// queryPieMenu issues a query-pie-menu IPC call for the given object and
// returns the raw interactions slice. Returns nil on failure (caller falls back
// to interaction 0).
func queryPieMenu(ctx context.Context, ipc *IPC, objectID int64) ([]pieMenuEntry, error) {
	resp, err := ipc.Send(ctx, "query-pie-menu", map[string]any{
		"target_object_id": objectID,
	})
	if err != nil {
		return nil, fmt.Errorf("query-pie-menu: %w", err)
	}
	if !resp.Ok {
		return nil, fmt.Errorf("query-pie-menu refused: %s", resp.Error)
	}
	var payload struct {
		Interactions []pieMenuEntry `json:"interactions"`
	}
	if err := json.Unmarshal(resp.Payload, &payload); err != nil {
		return nil, fmt.Errorf("query-pie-menu parse: %w", err)
	}
	return payload.Interactions, nil
}

// climbInteractionID returns the TTAB index for a "climb" or "use" interaction
// on a stair object. We query the pie menu and prefer any entry whose name
// contains "climb" (case-insensitive), then fall back to the first available
// entry, then to 0 (the canonical default portal interaction).
func climbInteractionID(ctx context.Context, ipc *IPC, stairObjectID int64) int64 {
	entries, err := queryPieMenu(ctx, ipc, stairObjectID)
	if err != nil || len(entries) == 0 {
		return 0
	}
	for _, e := range entries {
		if strings.Contains(strings.ToLower(e.Name), "climb") {
			return e.ID
		}
	}
	// No "climb" entry found — use the first listed interaction (portal
	// objects typically only expose one: the portal traversal function).
	return entries[0].ID
}

// findStairForCrossLevel is the cross-level navigation decision: query the
// avatar's current level, compare against targetLevel, and if they differ
// locate the closest stair object. Returns:
//
//   - (nil, nil)          — same-level navigation, no stair check needed.
//   - (result, nil)       — cross-level: stair found, caller should queue it first.
//   - (nil, errNoStair)   — cross-level: no stair found; caller should refuse.
//
// errNoStair.Error() returns the category:no-stair-path token consumed by the
// callers to build the convention.Response.Fail shape.
//
// targetLevel must be > 0 (caller validates). radiusTiles controls the
// nearby-objects search radius; <= 0 uses the bot default (20 tiles).
func findStairForCrossLevel(ctx context.Context, ipc *IPC, targetLevel int64, radiusTiles float64) (*crossLevelResult, error) {
	currentLevel, err := querySelfLevel(ctx, ipc)
	if err != nil {
		// If we can't get current level, let the original command through
		// unchanged (best-effort). Don't block navigation on a level query
		// failure.
		return nil, nil //nolint:nilerr // intentional: degraded path
	}

	if currentLevel == 0 {
		// Avatar not yet placed in world — pass through.
		return nil, nil
	}

	if currentLevel == targetLevel {
		// Same floor — no stair logic needed.
		return nil, nil
	}

	// Cross-level requested. Find the closest stair object.
	objects, nearErr := queryNearbyObjects(ctx, ipc, radiusTiles)
	if nearErr != nil {
		// Can't enumerate objects — refuse with no-stair-path so the agent
		// knows navigation failed, rather than silently sending a walk command
		// that the engine will route incorrectly.
		return nil, fmt.Errorf("category:no-stair-path (query-nearby failed: %v)", nearErr)
	}

	var best *crossLevelResult
	bestDist := math.MaxFloat64
	for _, obj := range objects {
		if !isStairObject(obj.Name) {
			continue
		}
		if obj.DistanceTiles < bestDist {
			bestDist = obj.DistanceTiles
			best = &crossLevelResult{
				ObjectID:     obj.ObjectID,
				Level:        int64(obj.Position.Level),
				DistanceTiles: obj.DistanceTiles,
			}
		}
	}
	if best == nil {
		return nil, fmt.Errorf("category:no-stair-path")
	}
	return best, nil
}

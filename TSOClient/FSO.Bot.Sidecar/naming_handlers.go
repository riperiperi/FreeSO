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
	"strconv"
	"strings"

	"github.com/campfire-net/campfire/pkg/convention"
)

// The naming family is a thin layer over the sidecar's in-memory store
// (MemoryStore). Agents bind human-readable names to VM targets once and then
// reference them in subsequent go-to / walk-to calls via --my_name, removing
// the need to carry ObjectIDs or coordinates in working memory.
//
// Storage shape: key = "name:<lowercased name>", value = JSON
//   {"name": <original case>, "kind": "object"|"sim"|"location", "note": "...",
//    "target_object_id": <int>, "target_sim_id": <int>, "x"/"y"/"level": <int>}
//
// Case-insensitive on lookup, original case preserved on display. The sidecar
// store is session-scoped (lost on sidecar restart); persistent names across
// sessions are an identity-continuation (-38a) follow-up.

const namePrefix = "name:"

// RegisterNamingHandlers wires name, forget, and list-names as convention.Server
// instances on cf. Returns the count of opened servers; errors hard if any
// declaration is missing.
func RegisterNamingHandlers(ctx context.Context, cf *Campfire, store *MemoryStore) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"name":       nameHandler(store),
		"forget":     forgetHandler(store),
		"list-names": listNamesHandler(store),
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

// nameHandler: binds <as> → one of target_object_id | target_sim_id | location | lot_location.
//
// Args:
//
//	as (string, required)           — the agent's chosen name. Case-insensitive
//	                                  on lookup.
//	target_object_id (int)          — ObjectID to bind the name to
//	target_sim_id (int)             — Sim persist id to bind
//	location (json {x,y,level})     — tile coords to bind (a spot on a lot)
//	lot_location (string)           — lot location code (hex "0x..." or decimal uint32)
//	                                  to bind; used by visit-lot --my_name resolution.
//	note (string, optional)         — free-form note stored alongside
//
// Exactly one of target_object_id, target_sim_id, location, or lot_location
// must be present. A name is overwritten if it already exists.
func nameHandler(store *MemoryStore) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		as, _ := req.Args["as"].(string)
		as = strings.TrimSpace(as)
		if as == "" {
			return errResp("name", "as (the chosen name) is required")
		}

		entry := map[string]any{"name": as}
		if v, ok := req.Args["note"].(string); ok && v != "" {
			entry["note"] = v
		}

		// Exactly one target selector.
		count := 0
		if v, ok := numericArg(req.Args, "target_object_id"); ok && v != 0 {
			entry["kind"] = "object"
			entry["target_object_id"] = v
			count++
		}
		if v, ok := numericArg(req.Args, "target_sim_id"); ok && v != 0 {
			entry["kind"] = "sim"
			entry["target_sim_id"] = v
			count++
		}
		if locRaw, ok := req.Args["location"]; ok && locRaw != nil {
			loc, err := coerceLocation(locRaw)
			if err != nil {
				return errResp("name", "location: "+err.Error())
			}
			entry["kind"] = "location"
			entry["x"] = loc.X
			entry["y"] = loc.Y
			entry["level"] = loc.Level
			count++
		}
		if llRaw, ok := req.Args["lot_location"]; ok && llRaw != nil {
			ll, err := coerceLotLocation(llRaw)
			if err != nil {
				return errResp("name", "lot_location: "+err.Error())
			}
			entry["kind"] = "lot"
			entry["lot_location"] = ll
			count++
		}
		if count == 0 {
			return errResp("name", "one of target_object_id, target_sim_id, location, or lot_location is required")
		}
		if count > 1 {
			return errResp("name", "only one of target_object_id, target_sim_id, location, or lot_location may be set")
		}

		encoded, err := json.Marshal(entry)
		if err != nil {
			return errResp("name", "encode: "+err.Error())
		}
		store.Store(namePrefix+strings.ToLower(as), encoded)
		return &convention.Response{
			Payload: map[string]any{"ok": true, "name": as, "entry": entry},
		}, nil
	}
}

// forgetHandler: removes a name binding.
//
// MemoryStore does not expose Delete, so "forget" writes a tombstone — an empty
// JSON object. listNames and the my_name resolver treat empty/missing entries
// the same way. (This keeps MemoryStore.Store contract minimal; a proper delete
// is a small follow-up if the tombstone pattern proves awkward.)
func forgetHandler(store *MemoryStore) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		name, _ := req.Args["name"].(string)
		name = strings.TrimSpace(name)
		if name == "" {
			return errResp("forget", "name is required")
		}
		key := namePrefix + strings.ToLower(name)
		prior, found := store.Load(key)
		if !found || isTombstone(prior) {
			return &convention.Response{
				Payload: map[string]any{"ok": true, "found": false, "name": name},
			}, nil
		}
		store.Store(key, json.RawMessage("{}"))
		return &convention.Response{
			Payload: map[string]any{"ok": true, "found": true, "name": name},
		}, nil
	}
}

// listNamesHandler returns all current (non-tombstoned) name bindings.
// No paging; session-scoped store is small enough that the full list fits in
// one response.
func listNamesHandler(store *MemoryStore) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		names := listNames(store)
		return &convention.Response{
			Payload: map[string]any{"ok": true, "count": len(names), "names": names},
		}, nil
	}
}

// listNames iterates the store and returns live name entries.
// Exported-by-package for reuse by the go-to my_name resolver.
func listNames(store *MemoryStore) []map[string]any {
	var out []map[string]any
	store.m.Range(func(k, v any) bool {
		key, ok := k.(string)
		if !ok || !strings.HasPrefix(key, namePrefix) {
			return true
		}
		raw, ok := v.(json.RawMessage)
		if !ok || isTombstone(raw) {
			return true
		}
		var decoded map[string]any
		if err := json.Unmarshal(raw, &decoded); err == nil && len(decoded) > 0 {
			out = append(out, decoded)
		}
		return true
	})
	return out
}

// lookupName returns the stored entry for name (case-insensitive), or nil if
// not found or tombstoned. Used by the go-to --my_name path.
func lookupName(store *MemoryStore, name string) map[string]any {
	raw, found := store.Load(namePrefix + strings.ToLower(strings.TrimSpace(name)))
	if !found || isTombstone(raw) {
		return nil
	}
	var decoded map[string]any
	if err := json.Unmarshal(raw, &decoded); err != nil || len(decoded) == 0 {
		return nil
	}
	return decoded
}

// isTombstone detects the empty-object JSON we write on forget.
func isTombstone(raw json.RawMessage) bool {
	s := strings.TrimSpace(string(raw))
	return s == "" || s == "null" || s == "{}"
}

// numericArg coerces both numeric and string-numeric args to int64 for
// target_object_id / target_sim_id. Convention framework may deliver floats.
func numericArg(args map[string]any, key string) (int64, bool) {
	v, ok := args[key]
	if !ok || v == nil {
		return 0, false
	}
	switch x := v.(type) {
	case int:
		return int64(x), true
	case int64:
		return x, true
	case float64:
		return int64(x), true
	case json.Number:
		if i, err := x.Int64(); err == nil {
			return i, true
		}
	case string:
		var i int64
		_, err := fmt.Sscan(x, &i)
		return i, err == nil
	}
	return 0, false
}

type locationArg struct {
	X     int64
	Y     int64
	Level int64
}

// coerceLocation accepts {x, y, level} as a JSON object (map[string]any) or a
// pre-decoded struct-ish shape. Level defaults to 1.
func coerceLocation(raw any) (locationArg, error) {
	var loc locationArg
	var m map[string]any
	switch v := raw.(type) {
	case map[string]any:
		m = v
	case string:
		// Convention framework may deliver as a JSON string.
		if err := json.Unmarshal([]byte(v), &m); err != nil {
			return loc, fmt.Errorf("not a JSON object: %w", err)
		}
	default:
		return loc, fmt.Errorf("expected JSON object with x/y/level")
	}
	x, okx := numericArg(m, "x")
	y, oky := numericArg(m, "y")
	if !okx || !oky {
		return loc, fmt.Errorf("x and y (tile units) are required")
	}
	loc.X = x
	loc.Y = y
	if lv, ok := numericArg(m, "level"); ok && lv != 0 {
		loc.Level = lv
	} else {
		loc.Level = 1
	}
	return loc, nil
}

func errResp(op, msg string) (*convention.Response, error) {
	return &convention.Response{
		Payload: map[string]any{"ok": false, "error": msg},
	}, nil
}

// coerceLotLocation accepts a lot location code as a hex string ("0x..."),
// decimal uint string, or a numeric value (float64/int64/etc.) and normalises
// it to a canonical decimal uint32 string. The supervisor loop stores and reads
// this value as a plain decimal string in the next-lot file; FSO_LOT_LOCATION
// also accepts decimal. Hex-with-0x-prefix is accepted for agent ergonomics
// (agents read lot_location from perception as hex-formatted strings).
//
// Returns an error if the value cannot be parsed or is zero (a zero location
// code is never a valid lot — it means "not on a lot").
func coerceLotLocation(raw any) (string, error) {
	var u uint64
	switch v := raw.(type) {
	case string:
		s := strings.TrimSpace(v)
		if strings.HasPrefix(s, "0x") || strings.HasPrefix(s, "0X") {
			n, err := strconv.ParseUint(s[2:], 16, 32)
			if err != nil {
				return "", fmt.Errorf("hex parse %q: %w", s, err)
			}
			u = n
		} else {
			n, err := strconv.ParseUint(s, 10, 32)
			if err != nil {
				return "", fmt.Errorf("decimal parse %q: %w", s, err)
			}
			u = n
		}
	case float64:
		if v < 0 || v > 0xFFFFFFFF {
			return "", fmt.Errorf("lot_location %v out of uint32 range", v)
		}
		u = uint64(v)
	case int64:
		if v < 0 {
			return "", fmt.Errorf("lot_location %v must be non-negative", v)
		}
		u = uint64(v)
	case int:
		if v < 0 {
			return "", fmt.Errorf("lot_location %v must be non-negative", v)
		}
		u = uint64(v)
	case json.Number:
		n, err := strconv.ParseUint(v.String(), 10, 32)
		if err != nil {
			return "", fmt.Errorf("json.Number parse %q: %w", v.String(), err)
		}
		u = n
	default:
		return "", fmt.Errorf("unexpected type %T for lot_location", raw)
	}
	if u == 0 {
		return "", fmt.Errorf("lot_location 0 is not a valid lot (means 'not on a lot')")
	}
	return strconv.FormatUint(u, 10), nil
}

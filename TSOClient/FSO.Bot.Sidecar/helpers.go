/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"fmt"

	"github.com/campfire-net/campfire/pkg/convention"
)

// errResp returns a convention.Response with ok=false and the given error
// message. It is the canonical error helper for all handler families; callers
// should not construct ok=false payloads inline.
//
// Wire-protocol note: the error field carries free-form diagnostic text for
// sidecar-tier failures (arg validation, bot-cmd channel unavailable, parse
// errors). Engine-tier semantic categories (e.g. "category:name-taken") are
// surfaced in the separate "reason" field (see chargen_handlers.go).
func errResp(op, msg string) (*convention.Response, error) {
	return &convention.Response{
		Payload: map[string]any{"ok": false, "error": msg},
	}, nil
}

// coerceUint64 converts a convention arg value to uint64.
// Convention framework delivers JSON numbers as float64; callers may also pass
// strings or native integer types.
func coerceUint64(v any) (uint64, error) {
	switch val := v.(type) {
	case float64:
		if val < 0 {
			return 0, fmt.Errorf("negative value %v cannot be uint64", val)
		}
		return uint64(val), nil
	case int:
		if val < 0 {
			return 0, fmt.Errorf("negative value %v cannot be uint64", val)
		}
		return uint64(val), nil
	case int64:
		if val < 0 {
			return 0, fmt.Errorf("negative value %v cannot be uint64", val)
		}
		return uint64(val), nil
	case uint64:
		return val, nil
	case string:
		var u uint64
		if _, err := fmt.Sscanf(val, "%d", &u); err != nil {
			return 0, fmt.Errorf("cannot parse %q as uint64: %w", val, err)
		}
		return u, nil
	default:
		return 0, fmt.Errorf("cannot convert %T to uint64", v)
	}
}

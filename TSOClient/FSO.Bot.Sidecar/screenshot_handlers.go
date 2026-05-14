/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"math"
	"net/http"
	"os"
	"path/filepath"
	"strconv"
	"strings"
	"sync"
	"time"

	"github.com/campfire-net/campfire/pkg/convention"
)

// RegisterScreenshotHandlers (freesoexperiment-0a2) wires the take-screenshot
// operation. This operation is a sidecar + HTTP proxy hybrid:
//
//   - Sidecar checks roommate-or-mayor authority on the current lot via IPC
//     (using the same query-lot pattern as other permission checks).
//   - On pass, HTTP POSTs the screenshot request to localhost:9101/render
//     (the headless renderer service).
//   - Saves the PNG to /tmp/embody-$RUN/screenshots/ and returns the path.
//   - Rate limits: 1 per soul per 10s, 10 per lot per minute.
//
// augmentor is shared with the bridge goroutine; it caches the latest
// mayor_status from the C# bot's perception ticks so the handler can
// check mayor authority without an extra IPC round-trip.
//
// Returns count of handlers registered. Missing declaration is an error.
func RegisterScreenshotHandlers(ctx context.Context, cf *Campfire, ipc *IPC, augmentor *PerceptionAugmentor) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"take-screenshot": takeScreenshotHandler(ipc, augmentor),
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

// screenshotRateLimiter enforces per-soul and per-lot rate limits.
// - soul: 1 per 10 seconds
// - lot: 10 per minute
type screenshotRateLimiter struct {
	mu       sync.Mutex
	souls    map[string][]time.Time // sender ID -> timestamps of requests in the 10s window
	lots     map[string][]time.Time  // lot ID -> timestamps of requests in the 60s window
}

var rateLimiter = &screenshotRateLimiter{
	souls: make(map[string][]time.Time),
	lots:  make(map[string][]time.Time),
}

// checkRateLimit returns true if the request is allowed, false if rate-limited.
// soulID is the sender, lotID is the current lot (from query-lot).
// Cleans up old timestamps as a side effect.
func (rl *screenshotRateLimiter) check(soulID, lotID string) bool {
	rl.mu.Lock()
	defer rl.mu.Unlock()

	now := time.Now()

	// Soul limit: 1 per 10 seconds
	if soulTs, ok := rl.souls[soulID]; ok {
		// Remove timestamps older than 10s
		cutoff := now.Add(-10 * time.Second)
		var recent []time.Time
		for _, ts := range soulTs {
			if ts.After(cutoff) {
				recent = append(recent, ts)
			}
		}
		rl.souls[soulID] = recent

		// If we have a recent request, deny
		if len(recent) > 0 {
			return false
		}
	}

	// Lot limit: 10 per 60 seconds
	if lotTs, ok := rl.lots[lotID]; ok {
		// Remove timestamps older than 60s
		cutoff := now.Add(-60 * time.Second)
		var recent []time.Time
		for _, ts := range lotTs {
			if ts.After(cutoff) {
				recent = append(recent, ts)
			}
		}
		rl.lots[lotID] = recent

		// If we already have 10, deny
		if len(recent) >= 10 {
			return false
		}
	}

	// Request is allowed: record the timestamp
	rl.souls[soulID] = append(rl.souls[soulID], now)
	rl.lots[lotID] = append(rl.lots[lotID], now)

	return true
}

// parseLotLocation converts a lot location string (hex "0x..." or decimal) to
// a uint32 suitable for the renderer's JSON payload. Returns 0 and error on
// failure. The renderer expects a numeric uint32, not a hex string.
func parseLotLocation(s string) (uint32, error) {
	s = strings.TrimSpace(s)
	if s == "" {
		return 0, fmt.Errorf("empty lot_location string")
	}
	lower := strings.ToLower(s)
	var n uint64
	var err error
	if strings.HasPrefix(lower, "0x") {
		n, err = strconv.ParseUint(lower[2:], 16, 32)
	} else {
		n, err = strconv.ParseUint(s, 10, 32)
	}
	if err != nil {
		return 0, fmt.Errorf("parse lot_location %q: %w", s, err)
	}
	return uint32(n), nil
}

// mapZoom maps the declaration's zoom vocabulary (small/medium/large) to the
// renderer's accepted enum values (far/med/near). The renderer's default is
// "far"; freesoexperiment-b85 found that "medium" was being forwarded verbatim,
// which caused the renderer to fall back to its default and silently ignore
// the zoom arg. We keep the user-facing vocabulary friendly and translate here.
func mapZoom(z string) string {
	switch z {
	case "small":
		return "far"
	case "medium":
		return "med"
	case "large":
		return "near"
	// Pass renderer-native values through unchanged so callers who know the
	// renderer's enum can use it directly.
	case "far", "med", "near":
		return z
	default:
		return "far"
	}
}

// takeScreenshotHandler processes a take-screenshot request.
// Steps:
//  1. Call query-lot to check roommate/owner/mayor status and get lot_id
//  2. Rate-limit check (soul + lot)
//  3. Resolve lot_location (for renderer when FSO_DB_URL is unset)
//  4. HTTP POST to renderer with correct body (freesoexperiment-b85)
//  5. Stream PNG from renderer to /tmp/embody-$RUN/screenshots/
//  6. Return {path, width, height, age_sec}
func takeScreenshotHandler(ipc *IPC, augmentor *PerceptionAugmentor) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		// Step 1: Query lot to check permissions and get lot_id.
		lotResp, err := forwardIPC(ctx, ipc, "query-lot", map[string]any{})
		if err != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("query-lot failed: %v", err),
				},
			}, nil
		}

		// Parse the lot response to get permissions.
		// forwardIPC wraps the bot's payload as {"ok":true,"payload":{...}}.
		// We need the inner payload map that contains lot_id, owner_is_me, etc.
		outerPayload, ok := lotResp.Payload.(map[string]any)
		if !ok {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": "query-lot returned unexpected payload type",
				},
			}, nil
		}
		// Drill into the nested "payload" key from forwardIPC's wrapper.
		lotPayload, ok := outerPayload["payload"].(map[string]any)
		if !ok {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("query-lot inner payload missing or wrong type: %v", outerPayload),
				},
			}, nil
		}

		// freesoexperiment-d49: correct auth gate.
		// Auth = owner OR roommate OR mayor.
		// Prior bug: `!ownerIsMe && len(roommates) == 0` — wrong semantics; denied
		// any non-owner on a lot with no roommates, and ignored mayor status entirely.
		ownerIsMe, _ := lotPayload["owner_is_me"].(bool)
		isRoommate, _ := lotPayload["is_roommate"].(bool)

		// Mayor status comes from the augmentor's cache (populated each perception
		// tick by the C# bot via PerceptionProjector.cs). This avoids an extra IPC
		// round-trip and is consistent with the civic handler pattern.
		var isMayor bool
		if augmentor != nil {
			isMayor = augmentor.LatestMayorStatus().IsMayor
		}

		if !ownerIsMe && !isRoommate && !isMayor {
			return &convention.Response{
				Payload: map[string]any{
					"ok":       false,
					"reason":   "NOT_PERMITTED",
					"error":    "not-permitted: you must be the owner, a roommate, or the mayor to take screenshots",
					"category": "not-permitted",
				},
			}, nil
		}

		// Extract lot_id for rate-limit keying.
		lotID := fmt.Sprintf("%v", lotPayload["lot_id"])

		// Step 2: Rate-limit check.
		if !rateLimiter.check(req.Sender, lotID) {
			return &convention.Response{
				Payload: map[string]any{
					"ok":       false,
					"reason":   "RATE_LIMITED",
					"error":    "rate limited: 1 screenshot per 10 seconds per soul, 10 per minute per lot",
					"category": "rate-limited",
				},
			}, nil
		}

		// Step 3: Extract and validate renderer arguments.
		level := int64(1)
		if lvl, ok := req.Args["level"].(float64); ok {
			level = int64(lvl)
		}
		angle := "iso-ne"
		if a, ok := req.Args["angle"].(string); ok {
			angle = a
		}
		zoom := "medium"
		if z, ok := req.Args["zoom"].(string); ok {
			zoom = z
		}
		// freesoexperiment-b85: map declaration zoom vocab → renderer enum.
		rendererZoom := mapZoom(zoom)

		roofless := false
		if r, ok := req.Args["roofless"].(bool); ok {
			roofless = r
		}

		// Step 4: Resolve lot_location.
		// The renderer needs lot_location (packed uint32 = x<<16 | y) to locate the
		// FSOV save on disk when FSO_DB_URL is unset (the default in our setup).
		// Prefer the value already in lotPayload — the bot's query-lot returns the
		// engine's VMTSOLotState.LotID, which IS the packed location (NOT the DB
		// primary key from fso_lots). This avoids a misleading DB lookup that would
		// fail with "Could not find lot_id=<location>" on residential lots where the
		// engine LotID ≠ DB lot_id (freesoexperiment-884: previously the handler
		// only filled lot_location from owned-lots.json, which was absent in the
		// integration test harness and bot-with-no-home-lot cases).
		//
		// owned-lots.json remains a fallback, but the inline path is the primary.
		var lotLocationUint uint32
		if loc, ok := lotPayload["lot_id"]; ok {
			switch v := loc.(type) {
			case float64:
				if v > 0 && v <= math.MaxUint32 {
					lotLocationUint = uint32(v)
				}
			case int:
				if v > 0 {
					lotLocationUint = uint32(v)
				}
			case int64:
				if v > 0 && v <= math.MaxUint32 {
					lotLocationUint = uint32(v)
				}
			}
		}
		if lotLocationUint == 0 {
			// Fallback: owned-lots.json (the persona's home lot). This is the original
			// path; kept as a safety net for callers without a populated lotPayload.
			lotLocationHex, locErr := ReadHomeLotFromOwnedLots()
			if locErr == nil && lotLocationHex != "" {
				if parsed, perr := parseLotLocation(lotLocationHex); perr == nil {
					lotLocationUint = parsed
				} else {
					fmt.Printf("screenshot: parse lot_location %q: %v (renderer will use DB lookup)\n", lotLocationHex, perr)
				}
			}
		}

		// Step 5: HTTP POST to renderer.
		// freesoexperiment-b85: prior bug had `_ = payloadBody` discarding the
		// marshalled body and passing nil to client.Post, so the renderer received
		// an empty body and responded {"error":"empty body"}.
		rendererURL := "http://localhost:9101/render"
		payload := map[string]any{
			"shard":    "Alphaville",
			"lot_id":   lotPayload["lot_id"],
			"level":    level,
			"angle":    angle,
			"zoom":     rendererZoom,
			"roofless": roofless,
		}
		if lotLocationUint != 0 {
			payload["lot_location"] = lotLocationUint
		}

		payloadBody, marshalErr := json.Marshal(payload)
		if marshalErr != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("marshal renderer payload: %v", marshalErr),
				},
			}, nil
		}

		client := &http.Client{Timeout: 30 * time.Second}
		resp, err := client.Post(rendererURL, "application/json", bytes.NewReader(payloadBody))
		if err != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("renderer POST failed: %v", err),
				},
			}, nil
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			// Try to read error body for diagnostic context.
			errBody, _ := io.ReadAll(io.LimitReader(resp.Body, 512))
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("renderer returned status %d: %s", resp.StatusCode, string(errBody)),
				},
			}, nil
		}

		// Step 6: Parse the renderer's JSON response.
		// The renderer returns {"path":"<cache-path>","width":W,"height":H,"age_sec":N}.
		// The PNG is already on disk at the returned path; we copy it to SCREENSHOT_DIR
		// so agents have a stable location independent of the renderer's cache layout.
		bodyBytes, err := io.ReadAll(resp.Body)
		if err != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("read renderer response: %v", err),
				},
			}, nil
		}
		var renderResp struct {
			Path   string  `json:"path"`
			Width  int     `json:"width"`
			Height int     `json:"height"`
			AgeSec float64 `json:"age_sec"`
			Error  string  `json:"error"`
		}
		if err := json.Unmarshal(bodyBytes, &renderResp); err != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": func() string {
					preview := len(bodyBytes)
					if preview > 256 {
						preview = 256
					}
					return fmt.Sprintf("parse renderer response: %v (body: %s)", err, string(bodyBytes[:preview]))
				}(),
				},
			}, nil
		}
		if renderResp.Error != "" {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("renderer error: %s", renderResp.Error),
				},
			}, nil
		}
		if renderResp.Path == "" {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": "renderer returned empty path",
				},
			}, nil
		}

		// Copy from renderer cache to SCREENSHOT_DIR for agent-visible stable path.
		screenshotDir := os.Getenv("SCREENSHOT_DIR")
		if screenshotDir == "" {
			screenshotDir = "/tmp/embody-demo/screenshots"
		}
		if err := os.MkdirAll(screenshotDir, 0o700); err != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("mkdir screenshots: %v", err),
				},
			}, nil
		}

		filename := filepath.Join(screenshotDir, fmt.Sprintf("screenshot-%d-%s.png", time.Now().UnixMilli(), angle))

		// Copy the rendered PNG from the renderer's cache path to our screenshots dir.
		srcData, err := os.ReadFile(renderResp.Path)
		if err != nil {
			// If the cache path is not accessible (renderer on different host), fall
			// through to save the JSON body as a debug artifact. In practice the renderer
			// and sidecar share the same filesystem.
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("read rendered PNG from cache %s: %v", renderResp.Path, err),
				},
			}, nil
		}
		if err := os.WriteFile(filename, srcData, 0o600); err != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("save screenshot: %v", err),
				},
			}, nil
		}

		// Step 7: Return success.
		return &convention.Response{
			Payload: map[string]any{
				"ok":      true,
				"path":    filename,
				"width":   renderResp.Width,
				"height":  renderResp.Height,
				"age_sec": renderResp.AgeSec,
				"size":    len(srcData),
			},
		}, nil
	}
}

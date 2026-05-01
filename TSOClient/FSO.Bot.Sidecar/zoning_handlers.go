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
	"os"
	"path/filepath"
	"sync"
	"time"

	"github.com/campfire-net/campfire/pkg/convention"
)

// RegisterZoningHandlers (freesoexperiment-409) wires the set-zoning civic op.
//
// Design: zoning is a sidecar-local overlay. The sidecar maintains
// /home/baron/projects/freeso-civic/zoning.json (shared across all sidecars on
// workshop — single-machine deployment). The purchase-lot handler calls
// CheckZoningViolation before issuing the PurchaseLotRequest to the C# bot.
//
// Authorization: mayor-only, same checkMayorAuthorization path as civic-access.
//
// Escalation risk (Risk #7): zoning.json is a single shared file on workshop.
// All sidecars read it at purchase-lot time. Writes are protected by an
// in-process mutex. If concurrent writes occur (multiple mayor sidecars, which
// is not a realistic scenario at research scale), we log a warning and the last
// write wins. Multi-writer coordination is NOT implemented here — escalate if
// concurrent mayor sessions occur.
const zoningFilePath = "/home/baron/projects/freeso-civic/zoning.json"

// zoningMu guards concurrent writes to zoning.json in-process.
// Across processes (multiple sidecars on the same machine), we accept
// last-write-wins (Risk #7 accepted at research scale).
var zoningMu sync.Mutex

// ValidZoneTypes is the set of allowed zone_type values.
var ValidZoneTypes = map[string]bool{
	"residential": true,
	"commercial":  true,
	"park":        true,
}

// ZoneEntry is one record in zoning.json.
type ZoneEntry struct {
	ZoneType  string `json:"zone_type"`  // "residential", "commercial", "park"
	X1        uint32 `json:"x1"`
	Y1        uint32 `json:"y1"`
	X2        uint32 `json:"x2"`
	Y2        uint32 `json:"y2"`
	ZoneName  string `json:"zone_name,omitempty"`
	CreatedAt int64  `json:"created_at"` // unix ms
}

// RegisterZoningHandlers wires the set-zoning convention op.
func RegisterZoningHandlers(ctx context.Context, cf *Campfire, botCmds *BotCmdPump) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"set-zoning": setZoningHandler(botCmds),
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

// setZoningHandler implements the set-zoning convention.
//
// Flow:
//  1. Validate zone_type, x1/y1/x2/y2.
//  2. Verify caller is mayor.
//  3. Append zone to /home/baron/projects/freeso-civic/zoning.json.
//  4. Return {ok, zone_type, x1, y1, x2, y2, zone_count}.
func setZoningHandler(botCmds *BotCmdPump) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := req.Args

		// --- Step 1: validate zone_type ---
		zoneType, _ := args["zone_type"].(string)
		if zoneType == "" {
			return &convention.Response{
				Payload: map[string]any{
					"ok":     false,
					"error":  "zone_type required ('residential', 'commercial', or 'park')",
					"reason": "INVALID_ZONE_TYPE",
				},
			}, nil
		}
		if !ValidZoneTypes[zoneType] {
			return &convention.Response{
				Payload: map[string]any{
					"ok":     false,
					"error":  "INVALID_ZONE_TYPE",
					"reason": "INVALID_ZONE_TYPE",
					"hint":   "zone_type must be 'residential', 'commercial', or 'park'",
				},
			}, nil
		}

		// --- Step 2: validate bounds ---
		x1, err1 := coerceUint64(args["x1"])
		y1, err2 := coerceUint64(args["y1"])
		x2, err3 := coerceUint64(args["x2"])
		y2, err4 := coerceUint64(args["y2"])
		if err1 != nil || err2 != nil || err3 != nil || err4 != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":     false,
					"error":  "INVALID_BOUNDS",
					"reason": "INVALID_BOUNDS",
					"hint":   "x1, y1, x2, y2 must all be provided as non-negative integers",
				},
			}, nil
		}
		if x2 < x1 || y2 < y1 {
			return &convention.Response{
				Payload: map[string]any{
					"ok":     false,
					"error":  "INVALID_BOUNDS",
					"reason": "INVALID_BOUNDS",
					"hint":   "x2 must be >= x1 and y2 must be >= y1",
				},
			}, nil
		}

		// --- Step 3: optional zone_name ---
		zoneName, _ := args["zone_name"].(string)

		// --- Step 4: mayor authorization ---
		isMayor, authErr := checkMayorAuthorization(ctx, botCmds)
		if authErr != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("mayor check failed: %v", authErr),
				},
			}, nil
		}
		if !isMayor {
			return &convention.Response{
				Payload: map[string]any{
					"ok":     false,
					"error":  "NO_AUTH",
					"reason": "NO_AUTH",
					"hint":   "Only the neighborhood mayor can set zoning. Your avatar must have mayor_nhood > 0.",
				},
			}, nil
		}

		// --- Step 5: append zone to zoning.json ---
		zoningMu.Lock()
		defer zoningMu.Unlock()

		zones, readErr := readZoning()
		if readErr != nil {
			// Non-fatal: treat as empty overlay and continue.
			log.Printf("set-zoning: read zoning.json: %v (treating as empty)", readErr)
			zones = []ZoneEntry{}
		}

		zone := ZoneEntry{
			ZoneType:  zoneType,
			X1:        uint32(x1),
			Y1:        uint32(y1),
			X2:        uint32(x2),
			Y2:        uint32(y2),
			ZoneName:  zoneName,
			CreatedAt: time.Now().UnixMilli(),
		}
		zones = append(zones, zone)

		if writeErr := writeZoning(zones); writeErr != nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": fmt.Sprintf("write zoning.json: %v", writeErr)},
			}, nil
		}

		return &convention.Response{
			Payload: map[string]any{
				"ok":         true,
				"zone_type":  zoneType,
				"x1":         uint32(x1),
				"y1":         uint32(y1),
				"x2":         uint32(x2),
				"y2":         uint32(y2),
				"zone_name":  zoneName,
				"zone_count": len(zones),
			},
		}, nil
	}
}

// readZoning reads zoning.json. Returns an empty slice when the file does not
// exist (no zones defined yet).
func readZoning() ([]ZoneEntry, error) {
	data, err := os.ReadFile(zoningFilePath)
	if err != nil {
		if os.IsNotExist(err) {
			return []ZoneEntry{}, nil
		}
		return nil, fmt.Errorf("read zoning.json: %w", err)
	}
	var zones []ZoneEntry
	if err := json.Unmarshal(data, &zones); err != nil {
		return nil, fmt.Errorf("parse zoning.json: %w", err)
	}
	return zones, nil
}

// writeZoning atomically writes zones to zoningFilePath. Creates the directory
// if necessary.
func writeZoning(zones []ZoneEntry) error {
	dir := filepath.Dir(zoningFilePath)
	if err := os.MkdirAll(dir, 0o755); err != nil {
		return fmt.Errorf("mkdir civic dir: %w", err)
	}
	data, err := json.MarshalIndent(zones, "", "  ")
	if err != nil {
		return fmt.Errorf("marshal zoning: %w", err)
	}
	tmp := zoningFilePath + ".tmp"
	if err := os.WriteFile(tmp, data, 0o644); err != nil {
		return fmt.Errorf("write zoning.json tmp: %w", err)
	}
	if err := os.Rename(tmp, zoningFilePath); err != nil {
		return fmt.Errorf("rename zoning.json: %w", err)
	}
	return nil
}

// CheckZoningViolation inspects the zoning overlay to determine whether a
// purchase at the given (x, y) tile with the given lotType is permitted.
//
// Returns (false, "") if no zone covers the tile or the lot type matches.
// Returns (true, reason) if a zone prohibits the purchase.
//
// Called by the purchase-lot handler before issuing PurchaseLotRequest.
// lotType "" is treated as "residential" (default lot category in FSO).
//
// Park zones prohibit all lot purchases unconditionally.
// Residential/commercial zones: the lotType must match. On workshop, all
// player-purchased lots are "residential" by default — only mayor-placed lots
// via start_fresh=true can be "community"/"commercial". For simplicity, we
// treat lotType "" and "residential" as equivalent.
func CheckZoningViolation(x, y uint16, lotType string) (violated bool, reason string) {
	zoningMu.Lock()
	defer zoningMu.Unlock()

	zones, err := readZoning()
	if err != nil {
		// If we can't read the zoning file, log and allow (fail-open).
		log.Printf("CheckZoningViolation: read zoning.json: %v (fail-open)", err)
		return false, ""
	}

	if lotType == "" {
		lotType = "residential"
	}

	for _, z := range zones {
		if x < uint16(z.X1) || x > uint16(z.X2) || y < uint16(z.Y1) || y > uint16(z.Y2) {
			continue // tile not in this zone
		}
		// Tile is within this zone.
		switch z.ZoneType {
		case "park":
			reason := "ZONING_VIOLATION"
			if z.ZoneName != "" {
				reason = fmt.Sprintf("ZONING_VIOLATION: tile is within park zone %q — no lot purchases allowed", z.ZoneName)
			}
			return true, reason
		case "residential":
			if lotType != "residential" {
				return true, fmt.Sprintf("ZONING_VIOLATION: tile is within residential zone — only residential lots allowed (got %q)", lotType)
			}
		case "commercial":
			if lotType != "commercial" {
				return true, fmt.Sprintf("ZONING_VIOLATION: tile is within commercial zone — only commercial lots allowed (got %q)", lotType)
			}
		}
	}
	return false, ""
}

// nowUnixMs returns the current time in unix milliseconds.
// Shared between tax_handlers.go and civic_access_handlers.go.
func nowUnixMs() int64 {
	return time.Now().UnixMilli()
}

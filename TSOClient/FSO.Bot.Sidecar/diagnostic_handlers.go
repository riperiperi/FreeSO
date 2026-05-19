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
	"time"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// RegisterDiagnosticHandlers wires the body__report-liveness and
// body__verify-perception diagnostic conventions (Component 9 — A6 resolution).
//
// Why these exist: Trunk F bans Bash from the talent's allowlist. Without
// diagnostic conventions the talent cannot distinguish "broadcast bridge
// broken" from "no perceptions recently." These two ops close the gap
// without re-enabling Bash.
//
//   - report-liveness: pure local read (SidecarHealth + BotProcess + Router).
//     Returns {sidecar_uptime_sec, bot_connected, bridge_active,
//     perception_tick_count_last_60s, handler_count, lot_id,
//     last_perception_ts}. Answerable even when the bot is wedged.
//
//   - verify-perception: emits a SYNTHETIC tick (synthetic:true flag) onto
//     the body campfire, then polls until the message appears in the local
//     store. Does NOT touch the real FSO server. Returns {tick_seen, ts}.
//
// Both ops are declared in conventions/ and registered at boot (frozen
// registry invariant — Component 10). No runtime promotion.
func RegisterDiagnosticHandlers(ctx context.Context, cf *Campfire, bot *BotProcess, health *SidecarHealth) (int, error) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		return 0, fmt.Errorf("load declarations: %w", err)
	}

	registered := 0
	for _, d := range decls {
		switch d.Operation {
		case "report-liveness":
			cf.Router.Register(d, makeLivenessHandler(bot, health, cf))
			registered++
		case "verify-perception":
			cf.Router.Register(d, makeVerifyPerceptionHandler(cf))
			registered++
		}
	}
	if registered != 2 {
		return registered, fmt.Errorf("expected 2 diagnostic declarations (report-liveness, verify-perception), found %d — check conventions/ directory", registered)
	}
	return registered, nil
}

// makeLivenessHandler returns a HandlerFunc for body__report-liveness.
// The handler is a pure local read — no IPC, no campfire poll, no file I/O.
// Runs in the Router's dispatch goroutine; must not block.
func makeLivenessHandler(bot *BotProcess, health *SidecarHealth, cf *Campfire) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		snap := health.LivenessSnapshotNow(bot, cf.Router.Count())
		return &convention.Response{Payload: snap}, nil
	}
}

// makeVerifyPerceptionHandler returns a HandlerFunc for body__verify-perception.
//
// Protocol:
//  1. Emit a synthetic perception tick to the body campfire with synthetic:true.
//  2. Poll the local campfire store for the message for up to wait_seconds.
//  3. Return {tick_seen: bool, ts: int} where ts is the unix-ms the message
//     was observed (0 if not seen).
//
// Synthetic tick does NOT reach the real FSO server — it is a pure
// sidecar-side broadcast that exercises the sidecar→campfire write path only.
// Agents use this to verify the broadcast path is live without waiting for a
// real game-world perception event.
func makeVerifyPerceptionHandler(cf *Campfire) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		// Parse wait_seconds from args. Default 5.
		waitSec := 5
		if req.Args != nil {
			if v, ok := req.Args["wait_seconds"]; ok {
				switch n := v.(type) {
				case float64:
					waitSec = int(n)
				case json.Number:
					if i, err := n.Int64(); err == nil {
						waitSec = int(i)
					}
				case int:
					waitSec = n
				case int64:
					waitSec = int(n)
				}
				if waitSec < 1 {
					waitSec = 1
				}
				if waitSec > 30 {
					waitSec = 30
				}
			}
		}

		// Build a synthetic perception tick. Use a unique correlation token
		// so the poll step can identify this specific synthetic tick even if
		// real perception events are also in-flight.
		corrToken := fmt.Sprintf("verify-perception-%d", time.Now().UnixNano())
		synthPayload, err := json.Marshal(map[string]any{
			"kind":      "perception",
			"synthetic": true,
			"token":     corrToken,
			"t":         time.Now().UnixMilli(),
		})
		if err != nil {
			return nil, fmt.Errorf("marshal synthetic tick: %w", err)
		}

		// Emit the synthetic tick. BroadcastEvent tags it freeso:perception
		// (with FREESO_BROADCAST_PERCEPTION=1 behaviour bypassed — we send
		// directly via the campfire client so the diagnostic always works
		// regardless of the FREESO_BROADCAST_PERCEPTION gate). We use the
		// campfire Client's Send directly for the same reason: the synthetic
		// tick must appear in the local store for the poll to find it.
		sentAt := time.Now()
		if err := cf.BroadcastEvent("perception", synthPayload, ""); err != nil {
			log.Printf("verify-perception: broadcast synthetic tick: %v", err)
			// Non-fatal: continue to poll; tick_seen will be false if the
			// send failed.
		}

		// Poll the campfire for the synthetic tick. We read all messages tagged
		// freeso:perception from AfterTimestamp = sentAt − 1s (a small margin
		// so we catch messages that arrived just before our sentAt wall-clock).
		deadline := time.Now().Add(time.Duration(waitSec) * time.Second)
		tickSeen := false
		var observedTs int64

		for time.Now().Before(deadline) {
			select {
			case <-ctx.Done():
				goto done
			default:
			}
			msgs, readErr := cf.readSyntheticTick(corrToken, sentAt)
			if readErr != nil {
				log.Printf("verify-perception: poll: %v", readErr)
			} else if msgs {
				tickSeen = true
				observedTs = time.Now().UnixMilli()
				break
			}
			select {
			case <-ctx.Done():
				goto done
			case <-time.After(200 * time.Millisecond):
			}
		}
	done:
		return &convention.Response{Payload: map[string]any{
			"tick_seen": tickSeen,
			"ts":        observedTs,
		}}, nil
	}
}

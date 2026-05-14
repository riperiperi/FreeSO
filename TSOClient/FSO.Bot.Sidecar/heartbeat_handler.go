/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"fmt"

	"github.com/campfire-net/campfire/pkg/convention"
)

// RegisterHeartbeatHandler wires the freeso:heartbeat convention. heartbeat
// is the "is anything alive over there?" check agents call when a longer
// freeso:* op times out at the cf await layer.
//
// Why it exists: cf await times out at 30s and returns "convention X
// response timed out" with no diagnostic context. The agent has no way to
// tell whether the sidecar process is dead, the bot mid-restart, perception
// stale, or the lot just slow. heartbeat answers all four cheaply.
//
// Implementation: pure in-memory read of SidecarHealth + IPC counters. No
// bot IPC, no campfire poll, no file access. The handler returns even when
// the bot is wedged — which is exactly when an agent wants an answer.
//
// Call signature: `cf $CF heartbeat` — no args.
//
// Response shape (HealthSnapshot in health.go):
//   - sidecar_uptime_ms        — how long this sidecar has been up
//   - bot_running              — bool, bot subprocess alive
//   - bot_pid                  — pid or 0
//   - have_perception          — bool, any perception ever
//   - perception_count         — running counter
//   - last_perception_unix_ms  — wall-clock of last tick
//   - last_perception_ms_ago   — convenience; -1 if never
//   - sim_id                   — avatar persist_id once known
//   - position {level,x,y}     — last known
//   - on_lot                   — last perception carried a lot payload
//   - pending_ipc_count        — in-flight IPC commands (queue depth)
//   - ops_registered           — total convention ops the router knows about
//
// Common diagnostic patterns:
//   - bot_running=false               → bot crashed; supervisor is relaunching
//   - bot_running=true, perception_count=0 → bot logging in / waiting for VM
//   - last_perception_ms_ago > 10000   → bot wedged or session-evicted
//   - pending_ipc_count > 5            → command queue draining slowly
//   - on_lot=false after settle        → bot bounced off lot or lot is full
func RegisterHeartbeatHandler(ctx context.Context, cf *Campfire, bot *BotProcess, ipc *IPC, health *SidecarHealth) (int, error) {
	handler := func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		snap := health.Snapshot(bot, ipc, cf.Router.Count())
		// Hand the struct directly — json marshals the json tags. Reusing the
		// shape that production state observers (logs, dashboards) will read.
		return &convention.Response{Payload: snap}, nil
	}

	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		return 0, fmt.Errorf("load declarations: %w", err)
	}
	for _, d := range decls {
		if d.Operation == "heartbeat" {
			cf.Router.Register(d, handler)
			return 1, nil
		}
	}
	return 0, fmt.Errorf("declaration for op %q missing (expected in conventions/heartbeat.json)", "heartbeat")
}

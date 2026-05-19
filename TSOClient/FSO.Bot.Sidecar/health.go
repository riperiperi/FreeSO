/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"sync"
	"sync/atomic"
	"time"
)

// SidecarHealth is a tiny in-memory snapshot of "is this sidecar/bot pair
// alive and tracking?" — designed so a caller who hits a cf timeout on a
// freeso:* op can ask `heartbeat` and learn what failed.
//
// Without this, a 30-second cf await timeout returns a single line that says
// "convention X response timed out after 30000ms" — useless for diagnosis. Was
// the sidecar restarting? Was the bot wedged? Was perception stale? The old
// answer was "spawn another agent and poke around for ten minutes." The new
// answer is "call heartbeat, read the structured state."
//
// Three lock-free counters (atomic), one short critical section for the last
// perception position. Reads are cheap so the heartbeat handler can fire
// without blocking the bridge goroutine even on hot lots.
//
// recentPerceptionTimes is a 128-slot ring buffer of UnixNano timestamps for
// the most recent 128 perception ticks. Used by LivenessSnapshotNow to count
// ticks within the last 60 seconds without storing a full sliding window.
// Protected by mu (same mutex as simID/lastPos/onLot).
type SidecarHealth struct {
	startTime time.Time

	// perceptionCount: total perception events observed from the bot since
	// the sidecar started. Useful as a freshness signal — if this number is
	// stable across two heartbeats spaced 2s apart, the bot is not emitting.
	perceptionCount atomic.Uint64

	// lastPerceptionUnixMs: wall-clock ms of the most recent perception
	// event. Zero if none yet observed.
	lastPerceptionUnixMs atomic.Int64

	// bot identity, populated once by the bridge when the first perception
	// frame's avatar.persist_id appears.
	mu       sync.RWMutex
	simID    string
	lastPos  *perceptionPosition // last known position (level, x, y). nil if never observed.
	onLot    bool                // most recent perception payload contained "lot" data
	lotID    int64               // last known lot_id (0 = unknown). Updated by RecordPerceptionWithLot.

	// recentPerceptionTimes: ring buffer of the last 128 perception tick
	// timestamps (UnixNano). Protected by mu. recentHead is the write index.
	recentPerceptionTimes [128]int64
	recentHead            int
}

type perceptionPosition struct {
	Level int     `json:"level"`
	X     float64 `json:"x"`
	Y     float64 `json:"y"`
}

// NewSidecarHealth returns a fresh health tracker. Call AT sidecar boot.
func NewSidecarHealth() *SidecarHealth {
	return &SidecarHealth{startTime: time.Now()}
}

// RecordPerception is called by the bridge layer for each perception line.
// Cheap; called from the bridge goroutine which is the single perception
// reader. Delegates to RecordPerceptionWithLot with lotID=0.
func (h *SidecarHealth) RecordPerception(simID string, pos *perceptionPosition, onLot bool) {
	h.RecordPerceptionWithLot(simID, pos, onLot, 0)
}

// RecordPerceptionWithLot records a perception event with a known lot_id.
// lotID=0 means unknown; the existing lotID is preserved (sticky). This is
// called by the bridge layer when the perception line carries "lot.lot_id".
func (h *SidecarHealth) RecordPerceptionWithLot(simID string, pos *perceptionPosition, onLot bool, lotID int64) {
	if h == nil {
		return
	}
	h.perceptionCount.Add(1)
	nowMs := time.Now().UnixMilli()
	h.lastPerceptionUnixMs.Store(nowMs)
	h.mu.Lock()
	if simID != "" {
		h.simID = simID
	}
	if pos != nil {
		h.lastPos = pos
	}
	h.onLot = onLot
	if lotID != 0 {
		h.lotID = lotID
	}
	// Advance ring buffer.
	h.recentPerceptionTimes[h.recentHead] = time.Now().UnixNano()
	h.recentHead = (h.recentHead + 1) % 128
	h.mu.Unlock()
}

// LivenessSnapshot is the wire shape returned by the body__report-liveness
// diagnostic convention (Component 9 — A6 resolution). Fields are a superset
// of HealthSnapshot oriented towards "is the broadcast bridge alive?" rather
// than "which operation timed out?"
type LivenessSnapshot struct {
	SidecarUptimeSec            int64 `json:"sidecar_uptime_sec"`
	BotConnected                bool  `json:"bot_connected"`
	BridgeActive                bool  `json:"bridge_active"`
	PerceptionTickCountLast60s  int   `json:"perception_tick_count_last_60s"`
	HandlerCount                int   `json:"handler_count"`
	LotID                       int64 `json:"lot_id"`
	LastPerceptionTs            int64 `json:"last_perception_ts"`
}

// LivenessSnapshotNow builds a LivenessSnapshot from the current tracker state.
// bot may be nil (--no-bot mode). opsRegistered is the router's handler count.
func (h *SidecarHealth) LivenessSnapshotNow(bot *BotProcess, opsRegistered int) LivenessSnapshot {
	snap := LivenessSnapshot{
		HandlerCount: opsRegistered,
	}
	if h == nil {
		return snap
	}
	now := time.Now()
	snap.SidecarUptimeSec = int64(now.Sub(h.startTime).Seconds())
	lastMs := h.lastPerceptionUnixMs.Load()
	snap.LastPerceptionTs = lastMs
	snap.BridgeActive = lastMs > 0
	if bot != nil {
		snap.BotConnected = bot.Pid() > 0
	}
	// Count ticks in the ring buffer within the last 60 seconds.
	cutoff := now.Add(-60 * time.Second).UnixNano()
	h.mu.RLock()
	snap.LotID = h.lotID
	count := 0
	for _, t := range h.recentPerceptionTimes {
		if t > cutoff {
			count++
		}
	}
	h.mu.RUnlock()
	snap.PerceptionTickCountLast60s = count
	return snap
}

// HealthSnapshot is the wire shape returned by the heartbeat convention.
type HealthSnapshot struct {
	SidecarUptimeMs   int64               `json:"sidecar_uptime_ms"`
	PerceptionCount   uint64              `json:"perception_count"`
	LastPerceptionMs  int64               `json:"last_perception_unix_ms"`
	LastPerceptionAgo int64               `json:"last_perception_ms_ago"`
	HavePerception    bool                `json:"have_perception"`
	SimID             string              `json:"sim_id,omitempty"`
	Position          *perceptionPosition `json:"position,omitempty"`
	OnLot             bool                `json:"on_lot"`
	BotRunning        bool                `json:"bot_running"`
	BotPid            int                 `json:"bot_pid,omitempty"`
	PendingIPCCount   int                 `json:"pending_ipc_count"`
	OpsRegistered     int                 `json:"ops_registered"`
}

// Snapshot reads the current state with cheap locking. Safe to call from any
// goroutine, including the heartbeat handler.
func (h *SidecarHealth) Snapshot(bot *BotProcess, ipc *IPC, opsRegistered int) HealthSnapshot {
	if h == nil {
		return HealthSnapshot{}
	}
	now := time.Now()
	lastMs := h.lastPerceptionUnixMs.Load()
	snap := HealthSnapshot{
		SidecarUptimeMs:  now.Sub(h.startTime).Milliseconds(),
		PerceptionCount:  h.perceptionCount.Load(),
		LastPerceptionMs: lastMs,
		HavePerception:   lastMs > 0,
		OpsRegistered:    opsRegistered,
	}
	if lastMs > 0 {
		snap.LastPerceptionAgo = now.UnixMilli() - lastMs
	} else {
		snap.LastPerceptionAgo = -1
	}
	h.mu.RLock()
	snap.SimID = h.simID
	if h.lastPos != nil {
		// Copy out to avoid handing the caller a pointer into our mutex-
		// protected state.
		p := *h.lastPos
		snap.Position = &p
	}
	snap.OnLot = h.onLot
	h.mu.RUnlock()
	if bot != nil {
		snap.BotPid = bot.Pid()
		snap.BotRunning = snap.BotPid > 0
	}
	if ipc != nil {
		snap.PendingIPCCount = ipc.PendingCount()
	}
	return snap
}

/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"encoding/json"
	"log"
	"os"
	"strconv"
	"time"
)

// Bridges owns the goroutines that forward bot NDJSON events to the campfire.
// One bridge per event family (perception, dialog, system). A single stdout
// reader fans events out to family bridges so the line order and sim-id
// extraction stay in lockstep.
type Bridges struct {
	cf      *Campfire
	bot     *BotProcess
	ipc     *IPC
	botCmds *BotCmdPump // freesoexperiment-0de: routes bot-cmd-reply frames

	// SimID is the avatar persist_id of this bot; populated from the first
	// perception event that carries one, then stable. Used as the sim:<id>
	// tag on broadcasts.
	simID string

	// habWatcher observes perception events to track I0-7 habitation flags.
	// Nil disables habitation tracking (e.g. --no-bot mode).
	habWatcher *HabitationWatcher

	// augmentor enriches perception ticks with sidecar-side fields before
	// forwarding to the campfire (freesoexperiment-ef1: home_lot, civic
	// affordances, mayor_status). Nil disables augmentation (e.g. --no-bot mode).
	augmentor *PerceptionAugmentor

	// journal writes perception events to the persona's journal directory
	// (freesoexperiment-f6d). Constructed once at bridge creation and shared
	// across the bridge's lifetime. When disabled (FSO_USER unset or XDG dirs
	// unavailable), Write is a no-op so the bridge continues running.
	journal *JournalWriter

	// health tracks perception cadence + last avatar position for the
	// heartbeat convention. Constructed once at sidecar bringup and shared
	// across bot relaunches (perception count is cumulative across the
	// sidecar's lifetime; agents reading heartbeat want "is the bot alive
	// right now," which a re-launched bot still answers correctly via the
	// latest perception timestamp).
	health *SidecarHealth
}

// NewBridges constructs a Bridges value. Call Run(ctx) once to start the
// fan-out; it returns when the bot stdout channel closes or ctx is cancelled.
// ipc may be nil in tests that don't exercise the command channel.
// augmentor may be nil to disable perception augmentation.
func NewBridges(cf *Campfire, bot *BotProcess, ipc *IPC, augmentor *PerceptionAugmentor) *Bridges {
	return &Bridges{cf: cf, bot: bot, ipc: ipc, habWatcher: NewHabitationWatcher(), augmentor: augmentor, journal: NewJournalWriter()}
}

// NewBridgesWithBotCmd constructs a Bridges value with the BotCmdPump wired.
// Used by the main supervisor loop when probe-lot / bot-exit-request are in use.
// augmentor is shared with civic convention handlers for mayor status reads.
func NewBridgesWithBotCmd(cf *Campfire, bot *BotProcess, ipc *IPC, botCmds *BotCmdPump, augmentor *PerceptionAugmentor) *Bridges {
	return &Bridges{cf: cf, bot: bot, ipc: ipc, botCmds: botCmds, habWatcher: NewHabitationWatcher(), augmentor: augmentor, journal: NewJournalWriter()}
}

// WithHealth attaches a SidecarHealth tracker. May be nil to disable health
// recording. Returns the receiver for chained construction.
func (b *Bridges) WithHealth(h *SidecarHealth) *Bridges {
	if b != nil {
		b.health = h
	}
	return b
}

// eventEnvelope is the loose shape we parse from bot stdout. We keep it a map
// because the rest of the payload is opaque to the sidecar — it passes through
// to the campfire intact.
type eventEnvelope struct {
	Kind  string          `json:"kind"`
	T     int64           `json:"t,omitempty"`
	Ts    int64           `json:"ts,omitempty"`
	Avatar json.RawMessage `json:"avatar,omitempty"`
	Payload json.RawMessage `json:"payload,omitempty"`
}

// Run drives the stdout fan-out until the bot exits or ctx is cancelled. On
// exit, broadcasts a final freeso:system bot-exited event so agents do not
// hang waiting for the next perception.
func (b *Bridges) Run(ctx context.Context) {
	log.Printf("bridges: starting")
	lines := b.bot.Lines()

	for {
		select {
		case <-ctx.Done():
			b.emitBotExited("ctx-cancelled")
			return
		case line, ok := <-lines:
			if !ok {
				b.emitBotExited("stdout-closed")
				return
			}
			b.handle(line)
		}
	}
}

// handle parses one NDJSON line and forwards it to the campfire with the
// appropriate tags. Lines that don't parse or have an unknown kind are logged
// and dropped — we do not crash the sidecar on malformed bot output.
func (b *Bridges) handle(line []byte) {
	if len(line) == 0 {
		return
	}
	var env eventEnvelope
	if err := json.Unmarshal(line, &env); err != nil {
		log.Printf("bridge: malformed line (dropped, %d bytes): %v", len(line), err)
		return
	}
	if env.Kind == "" {
		log.Printf("bridge: line missing 'kind' (dropped)")
		return
	}

	// Capture the avatar id the first time we see it. Perception events carry
	// "avatar": {"persist_id": <n>, ...}. Once captured, it does not change
	// for the life of the sidecar (one bot = one avatar).
	if b.simID == "" {
		if id := extractPersistID(env.Avatar); id != "" {
			b.simID = id
			log.Printf("bridge: captured avatar sim_id=%s", id)
		}
	}

	// Response frames are IPC-internal — they correlate to a specific sidecar-
	// issued command and MUST be routed through IPC.Deliver, not broadcast to
	// the campfire. Convention handlers that invoked the command turn the
	// response into a convention.Response fulfillment themselves.
	if env.Kind == "response" {
		if b.ipc != nil {
			b.ipc.Deliver(line)
		} else {
			log.Printf("bridge: response frame received but no IPC wired (dropped)")
		}
		return
	}

	// bot-cmd-reply frames (freesoexperiment-0de) are routed to the BotCmdPump.
	// These carry correlation IDs for city-socket ops (probe-lot, bot-exit-request)
	// that are distinct from the VM-command IPC envelope.
	if env.Kind == "bot-cmd-reply" {
		if b.botCmds != nil {
			b.botCmds.Deliver(line)
		} else {
			log.Printf("bridge: bot-cmd-reply received but no BotCmdPump wired (dropped)")
		}
		return
	}

	tag := env.Kind
	switch env.Kind {
	case "perception", "dialog", "system":
		// ok
	default:
		log.Printf("bridge: unknown kind %q (broadcasting as freeso:%s)", env.Kind, env.Kind)
	}

	// Habitation watcher: observe every perception tick to track I0-7 flags.
	// Non-perception kinds are passed through to ObservePerception which checks
	// the kind field and returns immediately — no overhead.
	if env.Kind == "perception" && b.habWatcher != nil {
		b.habWatcher.ObservePerception(line)
	}

	// Health tracker: record perception freshness + last position so the
	// heartbeat convention can answer "is the bot alive and on a lot?"
	// without an IPC round-trip. Runs on every perception tick.
	if env.Kind == "perception" && b.health != nil {
		pos := extractPerceptionPosition(line)
		onLot := perceptionHasLot(line)
		lotID := extractPerceptionLotID(line)
		b.health.RecordPerceptionWithLot(b.simID, pos, onLot, lotID)
	}

	// Perception augmentor: enrich perception ticks with sidecar-side fields
	// (home_lot, civic affordances, mayor_status) before forwarding to the
	// campfire. The habitation watcher runs BEFORE augmentation so it sees the
	// raw line and can update owned-lots.json; the augmentor then reads the
	// freshly-updated is_habitable when it builds home_lot. Order matters.
	// (freesoexperiment-ef1)
	if env.Kind == "perception" && b.augmentor != nil {
		line = b.augmentor.AugmentPerception(line)
	}

	// Journal perception ticks to the persona's run-scoped journal directory
	// (freesoexperiment-f6d). Journaling happens AFTER augmentation so the
	// persisted entry includes all sidecar-side enrichments. Write is a no-op
	// when the journal is disabled (FSO_USER unset, or RUN_ID unset → /tmp
	// fallback used instead of XDG). Errors are logged but not fatal so a
	// disk-full condition does not crash the sidecar.
	if env.Kind == "perception" && b.journal != nil {
		if jerr := b.journal.Write("perception", string(line)); jerr != nil {
			log.Printf("bridge: journal perception: %v", jerr)
		}
	}

	// Perception is a stream, not a coordination event. Broadcasting it
	// at FSO_PERCEPTION_HZ × N sims grew our body-campfire to 47k
	// messages, which made every cf Read run a 30s filesystem scan in
	// syncIfFilesystem. The journal writer above captures the same data
	// with fsync; agents who want perception should read from there.
	//
	// Gated for opt-in compatibility: set FREESO_BROADCAST_PERCEPTION=1
	// to restore the legacy broadcast. dialog and system events stay on
	// the campfire because they are infrequent and genuinely
	// coordination-shaped (bot-exited, dialog prompts).
	if env.Kind == "perception" && !broadcastPerceptionEnabled() {
		return
	}

	if err := b.cf.BroadcastEvent(tag, line, b.simID); err != nil {
		log.Printf("bridge: broadcast %s: %v", tag, err)
	}
}

// broadcastPerceptionEnabled reports whether the legacy "broadcast every
// perception tick to the campfire" behaviour should be restored. Off by
// default. Set FREESO_BROADCAST_PERCEPTION=1 (or "true") to opt back in.
//
// Read on every perception tick — kept cheap (single os.Getenv + small
// string check). For a sidecar processing perception at 1Hz this is
// nanoseconds; no need to cache.
func broadcastPerceptionEnabled() bool {
	v := os.Getenv("FREESO_BROADCAST_PERCEPTION")
	return v == "1" || v == "true"
}

// emitBotExited is called once on shutdown so agents see a final marker.
// It is best-effort; if the broadcast fails the sidecar is already tearing
// down and the agent will observe lack of perception as an implicit signal.
func (b *Bridges) emitBotExited(reason string) {
	evt := map[string]any{
		"kind": "system",
		"t":    time.Now().UnixMilli(),
		"payload": map[string]any{
			"event":  "bot-exited",
			"reason": reason,
		},
	}
	data, _ := json.Marshal(evt)
	if err := b.cf.BroadcastEvent("system", data, b.simID); err != nil {
		log.Printf("bridge: emit bot-exited: %v", err)
	} else {
		log.Printf("bridge: emitted bot-exited (%s)", reason)
	}
}

// extractPersistID pulls avatar.persist_id out of a raw perception "avatar"
// object. Returns "" if absent. We accept either a numeric id or a string id;
// the FSO perception emitter uses snake_case integers today.
func extractPersistID(raw json.RawMessage) string {
	if len(raw) == 0 {
		return ""
	}
	var shape struct {
		PersistID json.Number `json:"persist_id"`
	}
	dec := json.NewDecoder(bytesReader(raw))
	dec.UseNumber()
	if err := dec.Decode(&shape); err != nil {
		return ""
	}
	s := shape.PersistID.String()
	if s == "" || s == "0" {
		return ""
	}
	// Validate it at least looks numeric.
	if _, err := strconv.ParseInt(s, 10, 64); err != nil {
		return ""
	}
	return s
}

// extractPerceptionPosition decodes the avatar.position out of a perception
// frame for the health tracker. Returns nil if absent or malformed — the
// health snapshot then reports HavePerception=true but Position=nil, which
// callers read as "bot alive but position unknown right now."
func extractPerceptionPosition(line []byte) *perceptionPosition {
	var env struct {
		Avatar struct {
			Position *struct {
				Level json.Number `json:"level"`
				X     json.Number `json:"x"`
				Y     json.Number `json:"y"`
			} `json:"position"`
		} `json:"avatar"`
	}
	dec := json.NewDecoder(bytesReader(line))
	dec.UseNumber()
	if err := dec.Decode(&env); err != nil {
		return nil
	}
	if env.Avatar.Position == nil {
		return nil
	}
	level, _ := env.Avatar.Position.Level.Int64()
	x, _ := env.Avatar.Position.X.Float64()
	y, _ := env.Avatar.Position.Y.Float64()
	return &perceptionPosition{Level: int(level), X: x, Y: y}
}

// perceptionHasLot reports whether the perception line contained a non-null
// "lot" field. We use this as the heartbeat snapshot's OnLot flag — agents
// asking heartbeat want a quick yes/no without parsing the lot payload.
func perceptionHasLot(line []byte) bool {
	var env struct {
		Lot json.RawMessage `json:"lot"`
	}
	if err := json.Unmarshal(line, &env); err != nil {
		return false
	}
	if len(env.Lot) == 0 {
		return false
	}
	// `null` is 4 bytes — treat as absent.
	s := string(env.Lot)
	return s != "null"
}

// extractPerceptionLotID extracts the lot_id from a perception line's "lot"
// field. Returns 0 if absent or unparseable.
func extractPerceptionLotID(line []byte) int64 {
	var env struct {
		Lot struct {
			LotID int64 `json:"lot_id"`
		} `json:"lot"`
	}
	if err := json.Unmarshal(line, &env); err != nil {
		return 0
	}
	return env.Lot.LotID
}

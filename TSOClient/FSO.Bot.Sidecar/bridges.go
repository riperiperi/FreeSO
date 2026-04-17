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
	"strconv"
	"time"
)

// Bridges owns the goroutines that forward bot NDJSON events to the campfire.
// One bridge per event family (perception, dialog, system). A single stdout
// reader fans events out to family bridges so the line order and sim-id
// extraction stay in lockstep.
type Bridges struct {
	cf  *Campfire
	bot *BotProcess
	ipc *IPC

	// SimID is the avatar persist_id of this bot; populated from the first
	// perception event that carries one, then stable. Used as the sim:<id>
	// tag on broadcasts.
	simID string
}

// NewBridges constructs a Bridges value. Call Run(ctx) once to start the
// fan-out; it returns when the bot stdout channel closes or ctx is cancelled.
// ipc may be nil in tests that don't exercise the command channel.
func NewBridges(cf *Campfire, bot *BotProcess, ipc *IPC) *Bridges {
	return &Bridges{cf: cf, bot: bot, ipc: ipc}
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

	tag := env.Kind
	switch env.Kind {
	case "perception", "dialog", "system":
		// ok
	default:
		log.Printf("bridge: unknown kind %q (broadcasting as freeso:%s)", env.Kind, env.Kind)
	}

	if err := b.cf.BroadcastEvent(tag, line, b.simID); err != nil {
		log.Printf("bridge: broadcast %s: %v", tag, err)
	}
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

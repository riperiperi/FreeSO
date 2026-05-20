/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"fmt"
	"os"
	"time"

	"github.com/3dl-dev/freeso-sidecar/readiness"
	"github.com/campfire-net/campfire/cf-protocol/protocol"
)

// readiness_adapters.go wires the in-package types (Campfire, Router,
// SidecarHealth) to the minimal interfaces the readiness sub-package
// consumes. Keeping the adapters here (not in readiness/) avoids the
// readiness package taking a dependency on the sidecar's protocol.Client or
// embedded campfire SDK — keeps it pure and unit-testable.

// campfirePublisher adapts *Campfire to readiness.Publisher.
type campfirePublisher struct{ cf *Campfire }

func (p *campfirePublisher) Send(payload []byte, tags []string, antecedents []string) (string, error) {
	msg, err := p.cf.Client.Send(protocol.SendRequest{
		CampfireID:  p.cf.ID,
		Payload:     payload,
		Tags:        tags,
		Antecedents: antecedents,
	})
	if err != nil {
		return "", err
	}
	if msg == nil {
		return "", fmt.Errorf("campfirePublisher: nil message from Send")
	}
	return msg.ID, nil
}

// routerHandlers adapts *Router to readiness.HandlerCounter.
type routerHandlers struct{ r *Router }

func (h *routerHandlers) Count() int { return h.r.Count() }

// augmentorChargen adapts *PerceptionAugmentor to readiness.ChargenWatcher.
// Safe to construct with nil augmentor — IsChargenMode returns false, which
// causes chargen:ready to fulfill immediately at boot (correct for --no-bot mode
// and any mode where chargen is not applicable).
type augmentorChargen struct{ a *PerceptionAugmentor }

func (c *augmentorChargen) IsChargenMode() bool {
	if c.a == nil {
		return false
	}
	return c.a.IsChargenMode()
}

func (c *augmentorChargen) IsChargenPending() bool {
	if c.a == nil {
		return false
	}
	return c.a.IsChargenPending()
}

func (c *augmentorChargen) LastAvatarID() uint32 {
	if c.a == nil {
		return 0
	}
	return c.a.LastAvatarID()
}

// healthPerception adapts *SidecarHealth to readiness.PerceptionWatcher.
type healthPerception struct{ h *SidecarHealth }

func (p *healthPerception) HavePerceptionOnLot() bool {
	if p.h == nil {
		return false
	}
	p.h.mu.RLock()
	defer p.h.mu.RUnlock()
	return p.h.onLot
}

func (p *healthPerception) LastPerceptionUnixMs() int64 {
	if p.h == nil {
		return 0
	}
	return p.h.lastPerceptionUnixMs.Load()
}

func (p *healthPerception) LotID() int64 {
	// SidecarHealth tracks lot_id via peer item automataisland-2e8 (added
	// alongside RecordPerceptionWithLot for the report-liveness convention).
	// On a branch where that work has not yet landed, the field is absent
	// and we report 0; the payload still carries broadcast_bridge_verified
	// + first_perception_ts which are the load-bearing world:ready signals.
	// Wave-2 merge will surface the real lot_id automatically.
	return 0
}

// liveBridgeVerifier publishes one synthetic perception event to the body cf
// and reads it back to confirm the bridge surface is functional. This is
// world:ready gate (4) — the explicit "broadcast_bridge_verified=true" proof.
//
// The verification is deliberately end-to-end (Send → store → Read) so a
// FREESO_BROADCAST_PERCEPTION=0 environment OR a SQLite-stalled writer OR a
// crashed bridge goroutine all fail the same way. We do NOT attempt to set
// FREESO_BROADCAST_PERCEPTION here — the convention is that the operator
// (or test) sets it; the sidecar reports what is observable.
//
// One quirk: bridges.go's broadcastPerceptionEnabled() gate is checked on
// the perception path FROM the bot stdout. Our synthetic verification does
// NOT go through that path; it sends directly via Publisher. The gate we
// care about here is "can the campfire surface accept and surface back our
// own writes" — which is the irreducible "the body cf is alive" probe.
//
// For the FREESO_BROADCAST_PERCEPTION=0 case we rely on the world:gone
// heartbeat: no real-bot perception will broadcast → perception_last stays
// at zero → world:gone fulfils after the threshold. That is what the
// NEGATIVE test asserts.
type liveBridgeVerifier struct {
	cf *Campfire
}

func (v *liveBridgeVerifier) Verify(ctx context.Context) error {
	if v.cf == nil || v.cf.Client == nil {
		return fmt.Errorf("liveBridgeVerifier: nil campfire client")
	}
	// Synthetic perception probe: tag readiness:probe so the Router excludes
	// it (handlers see only freeso:* prefixes; readiness:* is invisible to
	// them) and so a tag-based Read can find it.
	probeID := fmt.Sprintf("readiness-probe-%d", time.Now().UnixNano())
	payload := []byte(fmt.Sprintf(`{"kind":"readiness:probe","probe_id":%q,"sent_unix_ms":%d}`, probeID, time.Now().UnixMilli()))
	sendMsg, err := v.cf.Client.Send(protocol.SendRequest{
		CampfireID: v.cf.ID,
		Payload:    payload,
		Tags:       []string{"readiness:probe"},
	})
	if err != nil {
		return fmt.Errorf("send probe: %w", err)
	}
	if sendMsg == nil {
		return fmt.Errorf("send probe: nil message")
	}
	// Read back with the same tag, bounded by ctx. Re-poll until the message
	// shows up in the store or ctx expires.
	deadline := time.Now().Add(5 * time.Second)
	if d, ok := ctx.Deadline(); ok && d.Before(deadline) {
		deadline = d
	}
	for time.Now().Before(deadline) {
		select {
		case <-ctx.Done():
			return ctx.Err()
		default:
		}
		resp, rErr := v.cf.Client.Read(protocol.ReadRequest{
			CampfireID: v.cf.ID,
			Tags:       []string{"readiness:probe"},
			SkipSync:   true, // our own write is already in local store
		})
		if rErr != nil {
			return fmt.Errorf("read probe: %w", rErr)
		}
		for _, msg := range resp.Messages {
			if msg.ID == sendMsg.ID {
				return nil
			}
		}
		time.Sleep(100 * time.Millisecond)
	}
	return fmt.Errorf("probe %s not readable from body cf within deadline", probeID)
}

// startReadiness publishes the four wake-readiness futures, runs the boot
// smoke test, and spawns the gate goroutines. Returns the Futures handle so
// the caller can fulfill identity:resolved at the right moment.
//
// On error the sidecar should not fail — the futures are best-effort; a
// missing future means talents will time out on await rather than learn the
// world is ready, which is recoverable.
//
// declaredOps is the count of declared ops; declaredOpNames is the ordered
// slice of their names (used by the skill referential integrity audit).
// The skills directory is read from FREESO_SKILLS_DIR; empty means no audit.
// augmentor may be nil (--no-bot mode) — chargen:ready fulfills immediately.
func startReadiness(ctx context.Context, cf *Campfire, router *Router, health *SidecarHealth, augmentor *PerceptionAugmentor, declaredOps int, declaredOpNames []string) *readiness.Futures {
	pub := &campfirePublisher{cf: cf}
	handlers := &routerHandlers{r: router}
	perception := &healthPerception{h: health}
	bridge := &liveBridgeVerifier{cf: cf}
	chargen := &augmentorChargen{a: augmentor}
	skillsDir := os.Getenv("FREESO_SKILLS_DIR")
	f := readiness.New(pub, handlers, perception, bridge, chargen, declaredOps, declaredOpNames, skillsDir, cf.PublicKeyHex, 0)
	if err := f.PublishAtBoot(ctx); err != nil {
		// Logged but non-fatal — see godoc rationale.
		// (Sidecar continues; talents simply won't have the futures to await.)
		return nil
	}
	// Run the one-shot boot smoke test. The result gates world:ready
	// permanently — a failed smoke test blocks fulfillment for this lifetime.
	// RunSmokeAtBoot must run AFTER PublishAtBoot (futures exist) and BEFORE
	// RunGates (the gate goroutine reads smokeOK on first tick).
	f.RunSmokeAtBoot()
	f.RunGates(ctx)
	return f
}

/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

// Package readiness pre-publishes the four wake-readiness futures on the body
// campfire at sidecar boot and fulfills them as preconditions land.
//
// Component 2 of the embodiment-runtime design (embodiment-runtime-design.md
// § Component 2 — Wake-readiness future graph). The talent's first MCP tool
// call is campfire_await(body, world:ready); a paired await on world:gone
// surfaces lot death / bridge failure / perception stoppage as an escalation
// trigger. identity:resolved fulfills when the persona is admitted to the body
// cf + has auto-joined the home cf. chargen:ready fulfills when bot has created
// (or already has) an FSO avatar — talents in chargen mode await this future
// before proceeding to lot-join mode.
//
// All four futures are pre-published at boot so a talent who reconnects sees
// them regardless of current fulfillment state. Stale futures from a prior
// sidecar boot (same persona, persona-stable body cf) are NOT actively swept
// here — re-publication supersedes via antecedent threading at the consumer.
//
// Design constraints (see item automataisland-5a4):
//   - world:gone heartbeat MUST run in a separate goroutine so a stuck bridge
//     never blocks the perception path.
//   - world:ready fulfillment requires all four gates to be green:
//       (1) handler_count == declared_op_count   — convention registry green
//       (2) bot has joined the configured lot    — perception with lot != null
//       (3) first lot-level perception broadcast — observed
//       (4) FREESO_BROADCAST_PERCEPTION=1 verified live — bridge wrote our
//           synthetic tick into the body cf store
//   - world:gone fulfills when perception ticks stop for >threshold,
//     bot disconnects, or the bridge fails.
//   - chargen:ready fulfills when avatar creation completes (or already existed
//     at boot). Pre-published unconditionally so talents in chargen mode can
//     await it without a race against sidecar startup.
//
// The futures package keeps NO sidecar-internal references: it consumes
// minimal interfaces (Publisher, HandlerCounter, PerceptionWatcher,
// BridgeVerifier, ChargenWatcher) so it can be unit tested without dragging in
// the campfire SDK or running a real bot.
package readiness

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"os"
	"strconv"
	"sync"
	"time"
)

// Reserved tag values frozen at cf-protocol 1.0. We use the strings directly
// (vs. importing protocol.TagFuture) so this package stays free of the
// campfire SDK; callers wire the values via the Publisher interface anyway.
const (
	tagFuture    = "future"
	tagFulfills  = "fulfills"
	TagWorldReady       = "world:ready"
	TagWorldGone        = "world:gone"
	TagIdentityResolved = "identity:resolved"
	TagChargenReady     = "chargen:ready"
)

// Default world:gone heartbeat threshold. Overridable via
// FREESO_WORLD_GONE_THRESHOLD_SEC.
const defaultWorldGoneThresholdSec = 30

// Publisher is the minimal send-surface the readiness package needs. It
// returns the published message ID so the caller can later thread fulfillment
// messages via Antecedents. Tags must include "future" for the antecedent
// future-message; "fulfills" for the fulfillment message.
type Publisher interface {
	Send(payload []byte, tags []string, antecedents []string) (msgID string, err error)
}

// HandlerCounter reports the number of currently-registered convention
// handler ops. Used as the world:ready gate (1): handler_count must equal
// declaredOps before fulfillment.
type HandlerCounter interface {
	Count() int
}

// PerceptionWatcher reports the live perception state. Used as the
// world:ready gates (2)+(3) and the world:gone perception-stale check.
//   - HavePerceptionOnLot returns true once at least one perception frame
//     with a non-null "lot" field has been observed.
//   - LastPerceptionUnixMs returns 0 if no perception ever, else the unix
//     millisecond timestamp of the most recent perception tick.
//   - LotID returns the lot_id from the most recent lot-bearing perception
//     (0 if unknown).
type PerceptionWatcher interface {
	HavePerceptionOnLot() bool
	LastPerceptionUnixMs() int64
	LotID() int64
}

// BridgeVerifier verifies world:ready gate (4): publishes one synthetic
// perception tick and confirms the bridge wrote it to the body cf store.
// Returns nil on verification success. Implementations should be idempotent
// and bounded; a failed verification means the broadcast bridge is dead
// (FREESO_BROADCAST_PERCEPTION=0 / handler unregistered / SQLite locked).
type BridgeVerifier interface {
	Verify(ctx context.Context) error
}

// ChargenWatcher reports chargen state for the chargen:ready future.
//
//   - IsChargenMode returns true when the bot was launched with --chargen-mode.
//     When false, chargen:ready fulfills immediately at boot with
//     already_existed=true (no chargen needed for this persona).
//   - IsChargenPending returns true when the bot is in chargen-mode AND no
//     avatar persist_id has been observed yet (avatar not yet created).
//     Transitions to false once avatar creation completes; that transition
//     is the fulfillment trigger.
//   - LastAvatarID returns the avatar_id from the completed chargen, or 0 if
//     chargen has not completed yet.
type ChargenWatcher interface {
	IsChargenMode() bool
	IsChargenPending() bool
	LastAvatarID() uint32
}

// Futures owns the four pre-published wake-readiness future messages and the
// goroutines that fulfill them.
//
// Lifecycle:
//   New(...) → PublishAtBoot(ctx) → RunSmokeAtBoot() → RunGates(ctx) (background) → Close()
//
// PublishAtBoot is synchronous; it returns once all four futures have message
// IDs. RunSmokeAtBoot runs the one-shot boot smoke test (handler-count parity
// + skill referential integrity); its result permanently gates world:ready —
// a failed smoke test prevents fulfillment for the sidecar's lifetime.
// RunGates is non-blocking — it spawns the gate-check goroutines and
// returns immediately so the sidecar boot path stays unblocked.
type Futures struct {
	pub             Publisher
	handlers        HandlerCounter
	perception      PerceptionWatcher
	bridge          BridgeVerifier
	chargen         ChargenWatcher // may be nil when chargen watcher not available
	declaredOps     int
	declaredOpNames []string // op name list; used by the smoke test skill audit
	skillsDir       string   // skill markdown dir to audit; empty = skip
	personaPubkey   string
	worldGoneSecs   int

	mu                    sync.Mutex
	worldReadyMsgID       string
	worldGoneMsgID        string
	identityResolvedMsgID string
	chargenReadyMsgID     string
	worldReadyDone        bool
	worldGoneDone         bool
	identityResolvedDone  bool
	chargenReadyDone      bool // idempotency guard — set once when chargen:ready fulfills
	smokeOK               bool // set once by RunSmokeAtBoot; gates world:ready permanently

	// gatePollInterval controls how often RunGates polls the world:ready
	// preconditions. 500ms balances responsiveness against churn.
	gatePollInterval time.Duration
}

// New constructs a Futures. declaredOps is the number of convention
// declarations published at boot (LoadDeclarations result); the world:ready
// gate (1) requires handlers.Count() to reach this number. personaPubkey is
// included in the world:ready payload for downstream consumers.
//
// declaredOpNames is the ordered list of declared operation names from
// conventions/*.json; used by the smoke test's skill referential integrity
// audit. May be nil — only the count gate runs if the slice is empty.
//
// skillsDir is the path to the directory containing skill markdown files to
// audit for convention references. Empty string means no skill audit is
// performed (common in dev environments without a legion jail configured).
//
// chargen is the ChargenWatcher for the chargen:ready future. Nil means
// no chargen mode detection — chargen:ready will still be published at boot
// but FulfillChargenReady must be called manually by the caller.
//
// worldGoneSecs of 0 selects the FREESO_WORLD_GONE_THRESHOLD_SEC env var (or
// the default 30s if absent or unparseable).
func New(pub Publisher, handlers HandlerCounter, perception PerceptionWatcher, bridge BridgeVerifier, chargen ChargenWatcher, declaredOps int, declaredOpNames []string, skillsDir string, personaPubkey string, worldGoneSecs int) *Futures {
	if worldGoneSecs <= 0 {
		worldGoneSecs = resolveWorldGoneThreshold()
	}
	return &Futures{
		pub:              pub,
		handlers:         handlers,
		perception:       perception,
		bridge:           bridge,
		chargen:          chargen,
		declaredOps:      declaredOps,
		declaredOpNames:  declaredOpNames,
		skillsDir:        skillsDir,
		personaPubkey:    personaPubkey,
		worldGoneSecs:    worldGoneSecs,
		gatePollInterval: 500 * time.Millisecond,
	}
}

// PublishAtBoot pre-publishes the four futures on the body cf. Returns the
// first error encountered. Callers should treat any error as fatal for the
// sidecar — without the futures, talents will hang on their first await.
func (f *Futures) PublishAtBoot(ctx context.Context) error {
	if err := ctx.Err(); err != nil {
		return err
	}

	wrID, err := f.publishFuture(TagWorldReady, map[string]any{
		"description": "fulfilled when sidecar is ready: handlers green, bot on lot, perception flowing, bridge verified",
	})
	if err != nil {
		return fmt.Errorf("publish world:ready future: %w", err)
	}

	wgID, err := f.publishFuture(TagWorldGone, map[string]any{
		"description":            "fulfilled when world becomes unobservable: perception stops, bot exits, or bridge fails",
		"threshold_sec":          f.worldGoneSecs,
	})
	if err != nil {
		return fmt.Errorf("publish world:gone future: %w", err)
	}

	irID, err := f.publishFuture(TagIdentityResolved, map[string]any{
		"description": "fulfilled when persona admitted to body cf and home cf auto-join completes",
	})
	if err != nil {
		return fmt.Errorf("publish identity:resolved future: %w", err)
	}

	crID, err := f.publishFuture(TagChargenReady, map[string]any{
		"description": "fulfilled when avatar creation completes (chargen mode) or avatar already exists (normal mode)",
	})
	if err != nil {
		return fmt.Errorf("publish chargen:ready future: %w", err)
	}

	f.mu.Lock()
	f.worldReadyMsgID = wrID
	f.worldGoneMsgID = wgID
	f.identityResolvedMsgID = irID
	f.chargenReadyMsgID = crID
	f.mu.Unlock()

	log.Printf("readiness: published futures world:ready=%s world:gone=%s identity:resolved=%s chargen:ready=%s (threshold_sec=%d)",
		shortID(wrID), shortID(wgID), shortID(irID), shortID(crID), f.worldGoneSecs)
	return nil
}

// publishFuture sends a single future message tagged ["future", semanticTag]
// with no antecedents (this IS the future; fulfillments thread back to it).
func (f *Futures) publishFuture(semanticTag string, payload map[string]any) (string, error) {
	data, err := json.Marshal(payload)
	if err != nil {
		return "", fmt.Errorf("marshal payload: %w", err)
	}
	return f.pub.Send(data, []string{tagFuture, semanticTag}, nil)
}

// FutureIDs returns the message IDs of the four pre-published futures. For
// inspection by tests / diagnostics. Returns empty strings if PublishAtBoot
// has not run.
func (f *Futures) FutureIDs() (worldReady, worldGone, identityResolved, chargenReady string) {
	f.mu.Lock()
	defer f.mu.Unlock()
	return f.worldReadyMsgID, f.worldGoneMsgID, f.identityResolvedMsgID, f.chargenReadyMsgID
}

// RunSmokeAtBoot executes the one-shot boot smoke test and stores the result.
// It must be called AFTER all Register*Handlers calls have completed (so
// handlers.Count() is final) and BEFORE RunGates (so the result is visible to
// the world:ready gate before it starts polling).
//
// A failed smoke test permanently blocks world:ready for this sidecar
// lifetime — the sidecar does not exit, diagnostic ops remain available, and
// world:gone still arms normally. Only a clean sidecar restart can clear the
// smoke-failed state.
//
// FREESO_SKIP_SMOKE_TEST=1 causes RunSmokeAtBoot to mark the smoke as passed
// and return immediately (dev/CI iteration path only; never set in production
// charts).
func (f *Futures) RunSmokeAtBoot() SmokeResult {
	result := RunSmokeTest(f.handlers.Count(), f.declaredOpNames, f.skillsDir)
	f.mu.Lock()
	f.smokeOK = result.Passed
	f.mu.Unlock()
	return result
}

// SmokeOK reports whether the boot smoke test passed. Always false until
// RunSmokeAtBoot has been called.
func (f *Futures) SmokeOK() bool {
	f.mu.Lock()
	defer f.mu.Unlock()
	return f.smokeOK
}

// RunGates spawns background goroutines: one polls the world:ready
// preconditions and fulfills when all four gates are green; the second runs
// the world:gone heartbeat and fulfills when perception goes stale; the third
// monitors chargen state and fulfills chargen:ready when the avatar is
// created (or immediately if no chargen is needed). Returns immediately. All
// goroutines exit on ctx.Done.
//
// FulfillIdentityResolved is the caller's responsibility (called from main
// after admit + home-join completes); the persistent goroutines here only
// own world:ready, world:gone, and chargen:ready.
func (f *Futures) RunGates(ctx context.Context) {
	go f.runWorldReadyGate(ctx)
	go f.runWorldGoneHeartbeat(ctx)
	go f.runChargenReadyGate(ctx)
}

// runWorldReadyGate polls the four preconditions every gatePollInterval until
// all four are green, then fulfills world:ready with the design payload.
// Exits on ctx.Done or after fulfillment (single-shot).
//
// Smoke test gate (pre-condition): if RunSmokeAtBoot recorded a failure,
// this goroutine exits immediately without ever polling — world:ready will
// never fulfill for this sidecar lifetime. The operator must restart.
func (f *Futures) runWorldReadyGate(ctx context.Context) {
	// Smoke gate: permanent failure if smoke test did not pass.
	f.mu.Lock()
	smokeOK := f.smokeOK
	f.mu.Unlock()
	if !smokeOK {
		log.Printf("readiness: world:ready permanently blocked — boot smoke test failed (restart sidecar to retry)")
		return
	}

	ticker := time.NewTicker(f.gatePollInterval)
	defer ticker.Stop()
	var bridgeVerified bool
	for {
		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
			// Gate (1): handler registry green.
			if f.handlers.Count() < f.declaredOps {
				continue
			}
			// Gates (2)+(3): bot joined lot AND first perception observed.
			if !f.perception.HavePerceptionOnLot() {
				continue
			}
			// Gate (4): broadcast bridge verified live. Run once; if it ever
			// passes, it stays passed for this sidecar lifetime (a transient
			// broadcast failure later is the world:gone gate's job).
			if !bridgeVerified {
				vctx, cancel := context.WithTimeout(ctx, 10*time.Second)
				vErr := f.bridge.Verify(vctx)
				cancel()
				if vErr != nil {
					// Keep polling — bridge may come up later (e.g. router
					// finishes initialising). Logged sparsely to avoid noise.
					log.Printf("readiness: bridge verify pending: %v", vErr)
					continue
				}
				bridgeVerified = true
			}
			// All four gates green — fulfill.
			if err := f.fulfillWorldReady(); err != nil {
				log.Printf("readiness: fulfill world:ready: %v", err)
				// Retry on next tick — fulfillment is idempotent at the
				// caller via fulfilled-flag short-circuit.
				continue
			}
			return
		}
	}
}

// fulfillWorldReady sends the fulfillment message with the design payload.
// Idempotent: once worldReadyDone is set, subsequent calls are no-ops.
func (f *Futures) fulfillWorldReady() error {
	f.mu.Lock()
	if f.worldReadyDone {
		f.mu.Unlock()
		return nil
	}
	futureID := f.worldReadyMsgID
	f.mu.Unlock()

	if futureID == "" {
		return fmt.Errorf("world:ready future not yet published")
	}
	payload := map[string]any{
		"lot_id":                    f.perception.LotID(),
		"persona_pubkey":            f.personaPubkey,
		"handler_count":             f.handlers.Count(),
		"first_perception_ts":       f.perception.LastPerceptionUnixMs(),
		"broadcast_bridge_verified": true,
	}
	data, err := json.Marshal(payload)
	if err != nil {
		return fmt.Errorf("marshal: %w", err)
	}
	_, err = f.pub.Send(data, []string{tagFulfills, TagWorldReady}, []string{futureID})
	if err != nil {
		return err
	}
	f.mu.Lock()
	f.worldReadyDone = true
	f.mu.Unlock()
	log.Printf("readiness: world:ready fulfilled (lot_id=%d handler_count=%d)",
		f.perception.LotID(), f.handlers.Count())
	return nil
}

// runWorldGoneHeartbeat polls perception freshness every second. When the
// last perception is older than worldGoneSecs, fulfills world:gone with the
// design payload {reason, last_perception_ts, sidecar_state}. Single-shot.
//
// Lives in its own goroutine so it cannot starve the perception bridge.
func (f *Futures) runWorldGoneHeartbeat(ctx context.Context) {
	ticker := time.NewTicker(1 * time.Second)
	defer ticker.Stop()
	thresholdMs := int64(f.worldGoneSecs) * 1000
	startMs := time.Now().UnixMilli()
	for {
		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
			last := f.perception.LastPerceptionUnixMs()
			nowMs := time.Now().UnixMilli()
			// Two stall modes:
			//   (a) Perception has never arrived AND we've been up longer
			//       than the threshold — bridge never came up.
			//   (b) Perception arrived once but the most recent tick is
			//       older than the threshold — perception went silent.
			var stalled bool
			var reason string
			if last == 0 {
				if nowMs-startMs > thresholdMs {
					stalled = true
					reason = fmt.Sprintf("no perception within %ds of sidecar boot — bridge may be down or bot never joined lot", f.worldGoneSecs)
				}
			} else if nowMs-last > thresholdMs {
				stalled = true
				reason = fmt.Sprintf("perception stopped %dms ago (threshold %ds) — bot disconnected, lot died, or bridge failed", nowMs-last, f.worldGoneSecs)
			}
			if !stalled {
				continue
			}
			if err := f.fulfillWorldGone(reason, last); err != nil {
				log.Printf("readiness: fulfill world:gone: %v", err)
				continue
			}
			return
		}
	}
}

// fulfillWorldGone sends the fulfillment message with the design payload.
// Idempotent.
func (f *Futures) fulfillWorldGone(reason string, lastPerceptionMs int64) error {
	f.mu.Lock()
	if f.worldGoneDone {
		f.mu.Unlock()
		return nil
	}
	futureID := f.worldGoneMsgID
	f.mu.Unlock()

	if futureID == "" {
		return fmt.Errorf("world:gone future not yet published")
	}
	state := "stalled"
	if lastPerceptionMs == 0 {
		state = "never-started"
	}
	payload := map[string]any{
		"reason":              reason,
		"last_perception_ts":  lastPerceptionMs,
		"sidecar_state":       state,
	}
	data, err := json.Marshal(payload)
	if err != nil {
		return fmt.Errorf("marshal: %w", err)
	}
	_, err = f.pub.Send(data, []string{tagFulfills, TagWorldGone}, []string{futureID})
	if err != nil {
		return err
	}
	f.mu.Lock()
	f.worldGoneDone = true
	f.mu.Unlock()
	log.Printf("readiness: world:gone fulfilled (reason=%q)", reason)
	return nil
}

// FulfillIdentityResolved sends the identity:resolved fulfillment. Called by
// the sidecar boot path once persona admit + home cf auto-join completes.
// Idempotent: a second call returns nil without re-sending (matches the
// worldReadyDone / worldGoneDone guard pattern).
func (f *Futures) FulfillIdentityResolved() error {
	f.mu.Lock()
	if f.identityResolvedDone {
		f.mu.Unlock()
		return nil
	}
	futureID := f.identityResolvedMsgID
	f.mu.Unlock()
	if futureID == "" {
		return fmt.Errorf("identity:resolved future not yet published")
	}
	payload := map[string]any{
		"persona_pubkey": f.personaPubkey,
	}
	data, err := json.Marshal(payload)
	if err != nil {
		return fmt.Errorf("marshal: %w", err)
	}
	if _, err := f.pub.Send(data, []string{tagFulfills, TagIdentityResolved}, []string{futureID}); err != nil {
		return err
	}
	f.mu.Lock()
	f.identityResolvedDone = true
	f.mu.Unlock()
	return nil
}

// runChargenReadyGate monitors chargen state and fulfills chargen:ready when
// the avatar exists. Two paths:
//
//   - Not in chargen mode (chargen watcher absent or IsChargenMode=false):
//     chargen:ready is fulfilled immediately at boot with already_existed=true.
//     This covers the normal lot-joined persona who has no chargen to do.
//
//   - In chargen mode (IsChargenMode=true, IsChargenPending=true):
//     polls until IsChargenPending transitions to false (avatar created),
//     then fulfills chargen:ready with avatar_id payload.
//
//   - In chargen mode but avatar already exists at boot (IsChargenMode=true,
//     IsChargenPending=false): fulfills immediately with already_existed=true.
//     This handles a sidecar restart when chargen completed in a prior run.
//
// Single-shot: exits after fulfillment or ctx.Done.
func (f *Futures) runChargenReadyGate(ctx context.Context) {
	// Determine chargen mode state at gate entry.
	inChargenMode := f.chargen != nil && f.chargen.IsChargenMode()

	if !inChargenMode {
		// Not a chargen boot — fulfill immediately (persona already has an avatar).
		if err := f.FulfillChargenReady(0, true); err != nil {
			log.Printf("readiness: fulfill chargen:ready (not-chargen-mode): %v", err)
		}
		return
	}

	// Check whether avatar already existed at boot (e.g. sidecar restart after
	// successful chargen in a prior run).
	if !f.chargen.IsChargenPending() {
		avatarID := f.chargen.LastAvatarID()
		if err := f.FulfillChargenReady(avatarID, true); err != nil {
			log.Printf("readiness: fulfill chargen:ready (avatar-pre-exists): %v", err)
		}
		return
	}

	// Chargen mode, avatar not yet created — poll until IsChargenPending flips.
	ticker := time.NewTicker(f.gatePollInterval)
	defer ticker.Stop()
	for {
		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
			if f.chargen.IsChargenPending() {
				// Still waiting for avatar creation.
				continue
			}
			// IsChargenPending flipped to false — chargen completed.
			avatarID := f.chargen.LastAvatarID()
			if err := f.FulfillChargenReady(avatarID, false); err != nil {
				log.Printf("readiness: fulfill chargen:ready (chargen-completed): %v", err)
				// Retry on next tick — fulfillment is idempotent via chargenReadyDone guard.
				continue
			}
			return
		}
	}
}

// FulfillChargenReady sends the chargen:ready fulfillment. Called automatically
// by runChargenReadyGate, or manually by callers who manage chargen state outside
// the ChargenWatcher interface.
//
// avatarID is the ID of the created avatar (0 when already_existed=true and
// the avatar_id is not available via the watcher). alreadyExisted=true means
// no chargen was needed — the persona already had an avatar at sidecar boot.
//
// Idempotent: once chargenReadyDone is set, subsequent calls return nil without
// re-sending (wave2-closeout idempotency pattern).
func (f *Futures) FulfillChargenReady(avatarID uint32, alreadyExisted bool) error {
	f.mu.Lock()
	if f.chargenReadyDone {
		f.mu.Unlock()
		return nil
	}
	futureID := f.chargenReadyMsgID
	f.mu.Unlock()

	if futureID == "" {
		return fmt.Errorf("chargen:ready future not yet published")
	}
	payload := map[string]any{
		"avatar_id":       avatarID,
		"already_existed": alreadyExisted,
	}
	data, err := json.Marshal(payload)
	if err != nil {
		return fmt.Errorf("marshal: %w", err)
	}
	_, err = f.pub.Send(data, []string{tagFulfills, TagChargenReady}, []string{futureID})
	if err != nil {
		return err
	}
	f.mu.Lock()
	f.chargenReadyDone = true
	f.mu.Unlock()
	log.Printf("readiness: chargen:ready fulfilled (avatar_id=%d already_existed=%v)", avatarID, alreadyExisted)
	return nil
}

// resolveWorldGoneThreshold reads FREESO_WORLD_GONE_THRESHOLD_SEC and returns
// the parsed value, falling back to the default on absence or parse error.
func resolveWorldGoneThreshold() int {
	v := os.Getenv("FREESO_WORLD_GONE_THRESHOLD_SEC")
	if v == "" {
		return defaultWorldGoneThresholdSec
	}
	n, err := strconv.Atoi(v)
	if err != nil || n <= 0 {
		log.Printf("readiness: invalid FREESO_WORLD_GONE_THRESHOLD_SEC=%q — falling back to %ds", v, defaultWorldGoneThresholdSec)
		return defaultWorldGoneThresholdSec
	}
	return n
}

func shortID(id string) string {
	if len(id) <= 12 {
		return id
	}
	return id[:12] + "…"
}

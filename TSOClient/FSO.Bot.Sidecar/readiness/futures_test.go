/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package readiness

import (
	"context"
	"encoding/json"
	"fmt"
	"sync"
	"sync/atomic"
	"testing"
	"time"
)

// recordedSend captures one Publisher.Send call for assertion.
type recordedSend struct {
	MsgID       string
	Payload     []byte
	Tags        []string
	Antecedents []string
}

// fakePublisher records every Send and assigns a synthetic message ID. Safe
// for concurrent use across the gate goroutines.
type fakePublisher struct {
	mu       sync.Mutex
	sends    []recordedSend
	nextID   int
	failNext bool // when true, the next Send returns an error and clears the flag
}

func newFakePublisher() *fakePublisher { return &fakePublisher{} }

func (f *fakePublisher) Send(payload []byte, tags []string, antecedents []string) (string, error) {
	f.mu.Lock()
	defer f.mu.Unlock()
	if f.failNext {
		f.failNext = false
		return "", fmt.Errorf("fakePublisher: injected failure")
	}
	f.nextID++
	id := fmt.Sprintf("msg-%d", f.nextID)
	// Defensive copies so callers mutating the slices don't corrupt our record.
	tagsCopy := append([]string(nil), tags...)
	antCopy := append([]string(nil), antecedents...)
	payloadCopy := append([]byte(nil), payload...)
	f.sends = append(f.sends, recordedSend{MsgID: id, Payload: payloadCopy, Tags: tagsCopy, Antecedents: antCopy})
	return id, nil
}

func (f *fakePublisher) snapshot() []recordedSend {
	f.mu.Lock()
	defer f.mu.Unlock()
	out := make([]recordedSend, len(f.sends))
	copy(out, f.sends)
	return out
}

// fakeHandlers reports a dynamic handler count.
type fakeHandlers struct{ n atomic.Int64 }

func (h *fakeHandlers) set(n int)   { h.n.Store(int64(n)) }
func (h *fakeHandlers) Count() int  { return int(h.n.Load()) }

// fakePerception is a settable PerceptionWatcher.
type fakePerception struct {
	mu              sync.Mutex
	haveOnLot       bool
	lastPerceptionMs int64
	lotID           int64
}

func (p *fakePerception) set(haveOnLot bool, lastMs int64, lotID int64) {
	p.mu.Lock()
	defer p.mu.Unlock()
	p.haveOnLot = haveOnLot
	p.lastPerceptionMs = lastMs
	p.lotID = lotID
}

func (p *fakePerception) HavePerceptionOnLot() bool {
	p.mu.Lock()
	defer p.mu.Unlock()
	return p.haveOnLot
}
func (p *fakePerception) LastPerceptionUnixMs() int64 {
	p.mu.Lock()
	defer p.mu.Unlock()
	return p.lastPerceptionMs
}
func (p *fakePerception) LotID() int64 {
	p.mu.Lock()
	defer p.mu.Unlock()
	return p.lotID
}

// fakeBridge succeeds or fails on demand.
type fakeBridge struct {
	mu        sync.Mutex
	shouldErr bool
}

func (b *fakeBridge) setErr(e bool) {
	b.mu.Lock()
	defer b.mu.Unlock()
	b.shouldErr = e
}

func (b *fakeBridge) Verify(ctx context.Context) error {
	b.mu.Lock()
	defer b.mu.Unlock()
	if b.shouldErr {
		return fmt.Errorf("fakeBridge: synthetic failure")
	}
	return nil
}

// ----- Tests -----

// TestPublishAtBoot_PublishesThreeFutures: the spec says three future messages
// MUST be on the body cf within boot, each tagged ["future", <semantic>].
func TestPublishAtBoot_PublishesThreeFutures(t *testing.T) {
	pub := newFakePublisher()
	h := &fakeHandlers{}
	p := &fakePerception{}
	b := &fakeBridge{}
	f := New(pub, h, p, b, 5, "deadbeef", 30)

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	if err := f.PublishAtBoot(ctx); err != nil {
		t.Fatalf("PublishAtBoot: %v", err)
	}

	sends := pub.snapshot()
	if len(sends) != 3 {
		t.Fatalf("want 3 future sends at boot, got %d: %+v", len(sends), sends)
	}

	want := map[string]bool{TagWorldReady: false, TagWorldGone: false, TagIdentityResolved: false}
	for _, s := range sends {
		if !containsTag(s.Tags, tagFuture) {
			t.Errorf("future send %s missing 'future' tag; tags=%v", s.MsgID, s.Tags)
		}
		if len(s.Antecedents) != 0 {
			t.Errorf("future send %s should have NO antecedents (it IS the future); got %v", s.MsgID, s.Antecedents)
		}
		for tag := range want {
			if containsTag(s.Tags, tag) {
				want[tag] = true
			}
		}
	}
	for tag, ok := range want {
		if !ok {
			t.Errorf("no future send had tag %q", tag)
		}
	}

	wr, wg, ir := f.FutureIDs()
	if wr == "" || wg == "" || ir == "" {
		t.Errorf("FutureIDs after PublishAtBoot: empty id in (%q,%q,%q)", wr, wg, ir)
	}
}

// TestRunGates_WorldReady_FulfillsWhenAllGatesGreen: the gate must fulfill
// world:ready exactly once when handler_count == declaredOps AND
// HavePerceptionOnLot=true AND Bridge.Verify=nil. Payload must carry all four
// fields from the design spec.
func TestRunGates_WorldReady_FulfillsWhenAllGatesGreen(t *testing.T) {
	pub := newFakePublisher()
	h := &fakeHandlers{}
	p := &fakePerception{}
	b := &fakeBridge{}
	f := New(pub, h, p, b, 3, "abc123", 30)
	f.gatePollInterval = 20 * time.Millisecond

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()
	if err := f.PublishAtBoot(ctx); err != nil {
		t.Fatalf("PublishAtBoot: %v", err)
	}
	f.RunGates(ctx)

	// Gates start RED — should not fulfill yet.
	time.Sleep(120 * time.Millisecond)
	if hasFulfillment(pub.snapshot(), TagWorldReady) {
		t.Fatal("world:ready fulfilled while gates still RED — regression")
	}

	// Turn all four gates GREEN.
	h.set(3)
	p.set(true, time.Now().UnixMilli(), 16318812)

	// Wait for fulfillment.
	deadline := time.Now().Add(2 * time.Second)
	for time.Now().Before(deadline) {
		if hasFulfillment(pub.snapshot(), TagWorldReady) {
			break
		}
		time.Sleep(20 * time.Millisecond)
	}
	sends := pub.snapshot()
	ful := findFulfillment(sends, TagWorldReady)
	if ful == nil {
		t.Fatalf("world:ready did not fulfill within 2s of all gates green; sends=%+v", sends)
	}

	// Payload shape: design spec keys.
	var got map[string]any
	if err := json.Unmarshal(ful.Payload, &got); err != nil {
		t.Fatalf("unmarshal payload: %v\n%s", err, string(ful.Payload))
	}
	for _, key := range []string{"lot_id", "persona_pubkey", "handler_count", "first_perception_ts", "broadcast_bridge_verified"} {
		if _, ok := got[key]; !ok {
			t.Errorf("payload missing key %q; got %v", key, got)
		}
	}
	if v, ok := got["broadcast_bridge_verified"].(bool); !ok || !v {
		t.Errorf("broadcast_bridge_verified want true; got %v", got["broadcast_bridge_verified"])
	}
	if v, ok := got["persona_pubkey"].(string); !ok || v != "abc123" {
		t.Errorf("persona_pubkey want abc123; got %v", got["persona_pubkey"])
	}
}

// TestRunGates_WorldReady_DoesNotFulfillWhenBridgeFails: NEGATIVE half of the
// gate pair. With perception flowing and handlers green BUT the bridge
// verifier returning error, world:ready MUST NOT fulfill.
func TestRunGates_WorldReady_DoesNotFulfillWhenBridgeFails(t *testing.T) {
	pub := newFakePublisher()
	h := &fakeHandlers{}
	p := &fakePerception{}
	b := &fakeBridge{}
	b.setErr(true) // bridge perpetually fails
	f := New(pub, h, p, b, 2, "xyz", 30)
	f.gatePollInterval = 20 * time.Millisecond

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()
	if err := f.PublishAtBoot(ctx); err != nil {
		t.Fatalf("PublishAtBoot: %v", err)
	}
	f.RunGates(ctx)

	// Set the other three gates GREEN.
	h.set(2)
	p.set(true, time.Now().UnixMilli(), 17)

	// Wait beyond several gate-poll intervals + bridge-verify cycles.
	time.Sleep(500 * time.Millisecond)

	if hasFulfillment(pub.snapshot(), TagWorldReady) {
		t.Fatal("world:ready fulfilled with bridge failing — broadcast_bridge_verified gate is not enforced")
	}
}

// TestRunGates_WorldGone_FulfillsOnPerceptionStall: with the threshold set to
// 1s, after 1.5s of no perception, world:gone MUST fulfill with the design
// payload including a reason mentioning bridge / perception stoppage.
func TestRunGates_WorldGone_FulfillsOnPerceptionStall(t *testing.T) {
	pub := newFakePublisher()
	h := &fakeHandlers{}
	p := &fakePerception{}
	b := &fakeBridge{}
	// 1s threshold for fast test.
	f := New(pub, h, p, b, 5, "k", 1)
	f.gatePollInterval = 20 * time.Millisecond

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()
	if err := f.PublishAtBoot(ctx); err != nil {
		t.Fatalf("PublishAtBoot: %v", err)
	}
	f.RunGates(ctx)

	// Wait beyond threshold + heartbeat interval.
	deadline := time.Now().Add(4 * time.Second)
	for time.Now().Before(deadline) {
		if hasFulfillment(pub.snapshot(), TagWorldGone) {
			break
		}
		time.Sleep(50 * time.Millisecond)
	}
	ful := findFulfillment(pub.snapshot(), TagWorldGone)
	if ful == nil {
		t.Fatalf("world:gone did not fulfill within 4s of perception stall")
	}
	var got map[string]any
	if err := json.Unmarshal(ful.Payload, &got); err != nil {
		t.Fatalf("unmarshal payload: %v", err)
	}
	for _, key := range []string{"reason", "last_perception_ts", "sidecar_state"} {
		if _, ok := got[key]; !ok {
			t.Errorf("world:gone payload missing key %q; got %v", key, got)
		}
	}
	if v, _ := got["reason"].(string); v == "" {
		t.Error("world:gone reason is empty")
	}
}

// TestRunGates_WorldGone_DoesNotFulfillWhilePerceptionFresh: world:gone must
// NOT fulfill when perception is flowing within the threshold window.
func TestRunGates_WorldGone_DoesNotFulfillWhilePerceptionFresh(t *testing.T) {
	pub := newFakePublisher()
	h := &fakeHandlers{}
	p := &fakePerception{}
	b := &fakeBridge{}
	f := New(pub, h, p, b, 5, "k", 1) // 1s threshold
	f.gatePollInterval = 20 * time.Millisecond

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()
	if err := f.PublishAtBoot(ctx); err != nil {
		t.Fatalf("PublishAtBoot: %v", err)
	}
	f.RunGates(ctx)

	// Keep perception fresh in a separate goroutine.
	stop := make(chan struct{})
	defer close(stop)
	go func() {
		t := time.NewTicker(200 * time.Millisecond)
		defer t.Stop()
		for {
			select {
			case <-stop:
				return
			case <-t.C:
				p.set(true, time.Now().UnixMilli(), 1)
			}
		}
	}()

	// Run for 2.5s — well beyond the 1s threshold.
	time.Sleep(2500 * time.Millisecond)
	if hasFulfillment(pub.snapshot(), TagWorldGone) {
		t.Fatal("world:gone fulfilled while perception was fresh — heartbeat falsely triggered")
	}
}

// TestFulfillIdentityResolved: sends the fulfillment threading the
// identity:resolved future as antecedent.
func TestFulfillIdentityResolved(t *testing.T) {
	pub := newFakePublisher()
	h := &fakeHandlers{}
	p := &fakePerception{}
	b := &fakeBridge{}
	f := New(pub, h, p, b, 1, "pk", 30)

	ctx := context.Background()
	if err := f.PublishAtBoot(ctx); err != nil {
		t.Fatalf("PublishAtBoot: %v", err)
	}
	_, _, irID := f.FutureIDs()
	if err := f.FulfillIdentityResolved(); err != nil {
		t.Fatalf("FulfillIdentityResolved: %v", err)
	}
	ful := findFulfillment(pub.snapshot(), TagIdentityResolved)
	if ful == nil {
		t.Fatal("identity:resolved fulfillment not sent")
	}
	if len(ful.Antecedents) != 1 || ful.Antecedents[0] != irID {
		t.Errorf("fulfillment antecedents want [%s]; got %v", irID, ful.Antecedents)
	}
}

// TestResolveWorldGoneThreshold_EnvOverride: the env var is honoured when
// set to a positive integer.
func TestResolveWorldGoneThreshold_EnvOverride(t *testing.T) {
	t.Setenv("FREESO_WORLD_GONE_THRESHOLD_SEC", "7")
	got := resolveWorldGoneThreshold()
	if got != 7 {
		t.Errorf("env=7 want 7; got %d", got)
	}
	t.Setenv("FREESO_WORLD_GONE_THRESHOLD_SEC", "notanumber")
	if v := resolveWorldGoneThreshold(); v != defaultWorldGoneThresholdSec {
		t.Errorf("bad env value want default %d; got %d", defaultWorldGoneThresholdSec, v)
	}
	t.Setenv("FREESO_WORLD_GONE_THRESHOLD_SEC", "")
	if v := resolveWorldGoneThreshold(); v != defaultWorldGoneThresholdSec {
		t.Errorf("empty env want default %d; got %d", defaultWorldGoneThresholdSec, v)
	}
}

// ----- Helpers -----

func containsTag(tags []string, want string) bool {
	for _, t := range tags {
		if t == want {
			return true
		}
	}
	return false
}

func hasFulfillment(sends []recordedSend, semanticTag string) bool {
	return findFulfillment(sends, semanticTag) != nil
}

func findFulfillment(sends []recordedSend, semanticTag string) *recordedSend {
	for i := range sends {
		s := &sends[i]
		if containsTag(s.Tags, tagFulfills) && containsTag(s.Tags, semanticTag) {
			return s
		}
	}
	return nil
}

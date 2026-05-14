/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"testing"

	"github.com/campfire-net/campfire/pkg/protocol"
)

// Tests for the idempotent-declaration-publish path. The campfire was
// accumulating 113 new declaration messages on every sidecar restart,
// inflating the cf-client-visible op list (340+ ops at peak — 3x the real
// surface). The dedup path reads existing convention:operation broadcasts,
// hashes by op-name + canonical JSON, and skips publication when the
// content is byte-identical.

// TestHashBytes_Deterministic: hashing the same bytes twice produces the
// same hex string. Surprising if it didn't, but it's a load-bearing
// invariant — the whole dedup hinges on this being stable.
func TestHashBytes_Deterministic(t *testing.T) {
	a := hashBytes([]byte(`{"operation":"buy-object","version":"1.0"}`))
	b := hashBytes([]byte(`{"operation":"buy-object","version":"1.0"}`))
	if a != b {
		t.Errorf("hashBytes is not deterministic: a=%s b=%s", a, b)
	}
	if len(a) != 64 {
		t.Errorf("hashBytes returned %d-char hash; want 64 (SHA-256 hex)", len(a))
	}
}

// TestHashBytes_DifferingPayloadsDiffer: a single byte change in the input
// flips the hash. Confirms we're not accidentally collapsing payloads.
func TestHashBytes_DifferingPayloadsDiffer(t *testing.T) {
	a := hashBytes([]byte(`{"operation":"buy-object","version":"1.0"}`))
	b := hashBytes([]byte(`{"operation":"buy-object","version":"1.1"}`))
	if a == b {
		t.Errorf("hashBytes collapses differing payloads to the same hash: %s", a)
	}
}

// TestBuildDeclarationHashMap_LatestWins: when the campfire has multiple
// convention:operation broadcasts for the same op (e.g. from prior sidecar
// boots), the LATEST publication's hash is the one we compare against. The
// helper iterates ReadResult.Messages in timestamp order (oldest first),
// so the last-seen entry overrides earlier ones — matches what a cf-client
// schema cache would see.
func TestBuildDeclarationHashMap_LatestWins(t *testing.T) {
	msgs := []protocol.Message{
		{Payload: []byte(`{"operation":"buy-object","version":"1.0"}`)},
		{Payload: []byte(`{"operation":"buy-object","version":"1.1"}`)}, // latest
		{Payload: []byte(`{"operation":"delete-object","version":"1.0"}`)},
	}
	got := buildDeclarationHashMap(msgs)
	wantBuyHash := hashBytes(msgs[1].Payload) // latest of two for buy-object
	if got["buy-object"] != wantBuyHash {
		t.Errorf("buy-object hash = %s; want %s (latest publication)", got["buy-object"], wantBuyHash)
	}
	if got["delete-object"] != hashBytes(msgs[2].Payload) {
		t.Errorf("delete-object hash unexpectedly differs")
	}
	if len(got) != 2 {
		t.Errorf("map size = %d; want 2 (one entry per distinct op-name)", len(got))
	}
}

// TestBuildDeclarationHashMap_MalformedSkipped: junk payloads (failed JSON
// decode, empty operation name) are dropped without poisoning the map.
// Production has historical malformed entries from earlier experiments;
// dedup must tolerate them.
func TestBuildDeclarationHashMap_MalformedSkipped(t *testing.T) {
	msgs := []protocol.Message{
		{Payload: []byte(`not-json`)},
		{Payload: []byte(`{"operation":""}`)}, // empty op-name
		{Payload: []byte(`{"operation":"valid-op","x":1}`)},
	}
	got := buildDeclarationHashMap(msgs)
	if len(got) != 1 || got["valid-op"] == "" {
		t.Errorf("got %+v; want a single entry for valid-op", got)
	}
}

// TestBuildDeclarationHashMap_Empty: zero input yields an empty map (no
// nil-panic in the dedup decision branch).
func TestBuildDeclarationHashMap_Empty(t *testing.T) {
	got := buildDeclarationHashMap(nil)
	if got == nil {
		t.Error("nil map returned; want non-nil empty map for downstream loop safety")
	}
	if len(got) != 0 {
		t.Errorf("len=%d; want 0", len(got))
	}
}

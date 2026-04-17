/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"encoding/json"
	"testing"
)

// TestExtractPersistID covers the three shapes perception events take:
// numeric id, missing id, and the zero-id case which we treat as "none".
func TestExtractPersistID(t *testing.T) {
	cases := []struct {
		name string
		in   string
		want string
	}{
		{"numeric", `{"persist_id": 2, "name": "baron"}`, "2"},
		{"large numeric", `{"persist_id": 17179869184}`, "17179869184"},
		{"missing", `{"name": "baron"}`, ""},
		{"zero", `{"persist_id": 0}`, ""},
		{"empty", ``, ""},
	}
	for _, tc := range cases {
		t.Run(tc.name, func(t *testing.T) {
			got := extractPersistID(json.RawMessage(tc.in))
			if got != tc.want {
				t.Errorf("got %q want %q", got, tc.want)
			}
		})
	}
}

// TestBridgeHandleUnknownKindDoesNotCrash: a bot that emits a novel "kind"
// should not take the sidecar down — we log and broadcast it through with the
// reported kind. Regression guard for when future items add new kinds.
func TestBridgeHandleUnknownKindDoesNotCrash(t *testing.T) {
	b := &Bridges{cf: nil, bot: nil} // cf nil triggers BroadcastEvent nil-deref
	// Instead we pass a recording cf stub:
	rec := newRecordingCampfire()
	b.cf = rec
	b.handle([]byte(`{"kind":"future-event","t":1,"payload":{"x":1}}`))
	if len(rec.recorder.(*recordingCampfire).broadcasts) != 1 {
		t.Fatalf("expected 1 broadcast, got %d", len(rec.recorder.(*recordingCampfire).broadcasts))
	}
	if rec.recorder.(*recordingCampfire).broadcasts[0].kind != "future-event" {
		t.Errorf("kind: got %q want %q", rec.recorder.(*recordingCampfire).broadcasts[0].kind, "future-event")
	}
}

// TestBridgeHandleMalformedDropped — malformed JSON doesn't crash and doesn't
// broadcast. We don't want the sidecar relaying noise to agents.
func TestBridgeHandleMalformedDropped(t *testing.T) {
	rec := newRecordingCampfire()
	b := &Bridges{cf: rec}
	b.handle([]byte(`{not json`))
	b.handle([]byte(``))
	b.handle([]byte(`{}`)) // no kind
	if len(rec.recorder.(*recordingCampfire).broadcasts) != 0 {
		t.Errorf("expected 0 broadcasts for malformed input, got %d", len(rec.recorder.(*recordingCampfire).broadcasts))
	}
}

// TestBridgeCapturesSimID — the first perception with a persist_id should
// set simID; subsequent events use it as the sim:<id> tag.
func TestBridgeCapturesSimID(t *testing.T) {
	rec := newRecordingCampfire()
	b := &Bridges{cf: rec}
	b.handle([]byte(`{"kind":"system","payload":{"event":"ready"}}`))
	if b.simID != "" {
		t.Errorf("simID should be empty before first perception, got %q", b.simID)
	}
	b.handle([]byte(`{"kind":"perception","t":42,"avatar":{"persist_id":2,"name":"baron"}}`))
	if b.simID != "2" {
		t.Errorf("simID after perception: got %q want %q", b.simID, "2")
	}
	b.handle([]byte(`{"kind":"dialog","payload":{"text":"hi"}}`))
	// Check tags on the dialog broadcast.
	if len(rec.recorder.(*recordingCampfire).broadcasts) < 3 {
		t.Fatalf("expected 3 broadcasts, got %d", len(rec.recorder.(*recordingCampfire).broadcasts))
	}
	last := rec.recorder.(*recordingCampfire).broadcasts[len(rec.recorder.(*recordingCampfire).broadcasts)-1]
	if last.simID != "2" {
		t.Errorf("dialog broadcast simID: got %q want %q", last.simID, "2")
	}
}

// --- recordingCampfire ---

type recordedBroadcast struct {
	kind    string
	payload []byte
	simID   string
}

// recordingCampfire is a minimal in-process Campfire that records broadcasts
// without touching a real campfire store. The bridge tests interact with it
// via the BroadcastEvent method we add in production. We keep the type here
// (not a separate file) because it is test-only.
type recordingCampfire struct {
	broadcasts []recordedBroadcast
}

func (rc *recordingCampfire) record(kind string, payload []byte, simID string) {
	rc.broadcasts = append(rc.broadcasts, recordedBroadcast{
		kind:    kind,
		payload: payload,
		simID:   simID,
	})
}

func newRecordingCampfire() *Campfire {
	return &Campfire{recorder: &recordingCampfire{}}
}

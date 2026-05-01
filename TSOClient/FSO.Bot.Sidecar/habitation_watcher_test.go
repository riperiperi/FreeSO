/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"encoding/json"
	"os"
	"testing"
	"time"
)

// makePerceptionLine constructs a minimal perception NDJSON line with the
// specified motive values, action queue, and lot state.
// motives: map of motive name → int16 value.
// targetObjectID: the object_id of the running interaction target (0 = none).
// objectCategory: the category to return for targetObjectID in nearby_objects ("" = absent).
// ownerIsMe: whether lot.owner_is_me is true.
func makePerceptionLine(motives map[string]int16, targetObjectID int16, objectCategory string, ownerIsMe bool) []byte {
	motiveObjs := map[string]any{}
	for k, v := range motives {
		motiveObjs[k] = map[string]any{
			"value":           v,
			"delta_per_minute": 0.0,
			"trend":           "stable",
		}
	}

	var actionQueue []any
	if targetObjectID != 0 {
		actionQueue = append(actionQueue, map[string]any{
			"interaction_id":   1,
			"name":             "use",
			"target_object_id": targetObjectID,
			"status":           "running",
		})
	}

	var nearbyObjects []any
	if targetObjectID != 0 && objectCategory != "" {
		nearbyObjects = append(nearbyObjects, map[string]any{
			"object_id": targetObjectID,
			"category":  objectCategory,
			"name":      "test object",
		})
	}

	tick := map[string]any{
		"kind": "perception",
		"t":    time.Now().UnixMilli(),
		"avatar": map[string]any{
			"persist_id":   2,
			"name":         "Botrous",
			"action_queue": actionQueue,
		},
		"motives":        motiveObjs,
		"nearby_objects": nearbyObjects,
		"lot": map[string]any{
			"owner_is_me": ownerIsMe,
			"name":        "Botrous Place",
			"lot_id":      2,
		},
	}
	line, _ := json.Marshal(tick)
	return line
}

// withOwnedLots pre-populates owned-lots.json with a single entry for testing.
func withOwnedLots(t *testing.T) {
	t.Helper()
	entry := OwnedLotEntry{
		Name:        "Test Home",
		LocationHex: "0x00F9015C",
		PurchasedAt: time.Now().Add(-1 * time.Hour).UnixMilli(),
		Habitation:  OwnedLotHabitation{},
		IsHabitable: false,
	}
	if err := WriteOwnedLots([]OwnedLotEntry{entry}); err != nil {
		t.Fatalf("WriteOwnedLots setup: %v", err)
	}
}

// --- Unit tests ---

// TestHabitationWatcher_NonPerceptionIgnored asserts that non-perception events
// do not mutate owned-lots.json and return false.
func TestHabitationWatcher_NonPerceptionIgnored(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "hab-test-non-perception")
	withConfigHome(t, tmp)
	withOwnedLots(t)

	w := NewHabitationWatcher()
	dialLine := []byte(`{"kind":"dialog","t":1000,"text":"hello"}`)
	if w.ObservePerception(dialLine) {
		t.Error("want false for non-perception kind")
	}
	sysLine := []byte(`{"kind":"system","payload":{"event":"ready"}}`)
	if w.ObservePerception(sysLine) {
		t.Error("want false for system kind")
	}

	lots, err := ReadOwnedLots()
	if err != nil {
		t.Fatalf("ReadOwnedLots: %v", err)
	}
	if lots[0].Habitation.FirstMealEatenHere != nil {
		t.Error("non-perception events must not mutate habitation")
	}
}

// TestHabitationWatcher_NotOnOwnedLotIgnored asserts that perception ticks with
// owner_is_me=false do not set habitation flags. This is the I0-7 falsifying test:
// walking the property line without eating/sleeping/toileting must not mark habitable.
func TestHabitationWatcher_NotOnOwnedLotIgnored(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "hab-test-not-owner")
	withConfigHome(t, tmp)
	withOwnedLots(t)

	w := NewHabitationWatcher()

	// First tick: hunger is negative.
	line1 := makePerceptionLine(
		map[string]int16{"hunger": -50, "energy": -50, "bladder": -50},
		0, "", false, // owner_is_me = false
	)
	w.prevMotives["hunger"] = -50
	w.ObservePerception(line1)

	// Second tick: hunger goes positive — but we're NOT the owner.
	line2 := makePerceptionLine(
		map[string]int16{"hunger": 20, "energy": -50, "bladder": -50},
		5, "food", false, // owner_is_me = false, interacting with food
	)
	updated := w.ObservePerception(line2)
	if updated {
		t.Error("ObservePerception returned true for non-owned lot (I0-7 falsifying test violation)")
	}

	lots, _ := ReadOwnedLots()
	if lots[0].Habitation.FirstMealEatenHere != nil {
		t.Error("I0-7 falsifying test: first_meal_eaten_here must not be set on non-owned lot")
	}
	if lots[0].IsHabitable {
		t.Error("I0-7 falsifying test: is_habitable must not be true on non-owned lot")
	}
}

// TestHabitationWatcher_MealTriggersFirstMealFlag asserts that when hunger
// crosses neg→pos while on an owned lot with a food-class interaction running,
// first_meal_eaten_here is populated.
func TestHabitationWatcher_MealTriggersFirstMealFlag(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "hab-test-meal")
	withConfigHome(t, tmp)
	withOwnedLots(t)

	w := NewHabitationWatcher()

	// Seed prior motive: hunger was -30.
	w.prevMotives["hunger"] = -30

	// Tick: hunger crosses to +10, interacting with fridge (food category), on owned lot.
	line := makePerceptionLine(
		map[string]int16{"hunger": 10, "energy": -20, "bladder": -10},
		3, "food", true,
	)
	updated := w.ObservePerception(line)
	if !updated {
		t.Fatal("want true (habitation updated) for hunger crossing while eating on owned lot")
	}

	lots, err := ReadOwnedLots()
	if err != nil {
		t.Fatalf("ReadOwnedLots: %v", err)
	}
	if lots[0].Habitation.FirstMealEatenHere == nil {
		t.Fatal("first_meal_eaten_here should be set after meal on owned lot")
	}
	if lots[0].Habitation.FirstSleepHere != nil {
		t.Error("first_sleep_here should not be set yet")
	}
	if lots[0].Habitation.FirstUseToiletHere != nil {
		t.Error("first_use_toilet_here should not be set yet")
	}
	if lots[0].IsHabitable {
		t.Error("is_habitable should remain false until all three are set")
	}
	t.Logf("first_meal_eaten_here = %d ms", *lots[0].Habitation.FirstMealEatenHere)
}

// TestHabitationWatcher_SleepTriggersSleepFlag asserts that energy crossing
// neg→pos with bed-class interaction on owned lot sets first_sleep_here.
func TestHabitationWatcher_SleepTriggersSleepFlag(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "hab-test-sleep")
	withConfigHome(t, tmp)
	withOwnedLots(t)

	w := NewHabitationWatcher()
	w.prevMotives["energy"] = -40

	line := makePerceptionLine(
		map[string]int16{"hunger": 10, "energy": 5, "bladder": -10},
		7, "energy", true,
	)
	updated := w.ObservePerception(line)
	if !updated {
		t.Fatal("want true for energy crossing while sleeping on owned lot")
	}

	lots, _ := ReadOwnedLots()
	if lots[0].Habitation.FirstSleepHere == nil {
		t.Fatal("first_sleep_here should be set after sleeping on owned lot")
	}
	if lots[0].IsHabitable {
		t.Error("is_habitable should remain false until all three are set")
	}
}

// TestHabitationWatcher_ToiletTriggersToiletFlag asserts that bladder crossing
// neg→pos with toilet-class interaction on owned lot sets first_use_toilet_here.
func TestHabitationWatcher_ToiletTriggersToiletFlag(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "hab-test-toilet")
	withConfigHome(t, tmp)
	withOwnedLots(t)

	w := NewHabitationWatcher()
	w.prevMotives["bladder"] = -60

	line := makePerceptionLine(
		map[string]int16{"hunger": 10, "energy": 10, "bladder": 20},
		9, "bladder", true,
	)
	updated := w.ObservePerception(line)
	if !updated {
		t.Fatal("want true for bladder crossing while using toilet on owned lot")
	}

	lots, _ := ReadOwnedLots()
	if lots[0].Habitation.FirstUseToiletHere == nil {
		t.Fatal("first_use_toilet_here should be set after toilet use on owned lot")
	}
}

// TestHabitationWatcher_AllThreeSetsMeansHabitable is the integration test:
// after a scripted sequence of eat → sleep → toilet on an owned lot, is_habitable
// must become true.
func TestHabitationWatcher_AllThreeSetsMeansHabitable(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "hab-test-full")
	withConfigHome(t, tmp)
	withOwnedLots(t)

	w := NewHabitationWatcher()

	// --- Phase 1: Eat ---
	w.prevMotives["hunger"] = -50
	w.prevMotives["energy"] = -50
	w.prevMotives["bladder"] = -50

	eat := makePerceptionLine(
		map[string]int16{"hunger": 10, "energy": -50, "bladder": -50},
		3, "food", true,
	)
	if !w.ObservePerception(eat) {
		t.Fatal("Phase 1 (eat): want true from ObservePerception")
	}
	lots, _ := ReadOwnedLots()
	if lots[0].Habitation.FirstMealEatenHere == nil {
		t.Fatal("Phase 1: first_meal_eaten_here not set")
	}
	if lots[0].IsHabitable {
		t.Error("Phase 1: is_habitable must be false (only meal done)")
	}

	// --- Phase 2: Sleep ---
	// Energy was -50 (seed), now goes positive.
	sleep := makePerceptionLine(
		map[string]int16{"hunger": 20, "energy": 5, "bladder": -50},
		7, "energy", true,
	)
	if !w.ObservePerception(sleep) {
		t.Fatal("Phase 2 (sleep): want true from ObservePerception")
	}
	lots, _ = ReadOwnedLots()
	if lots[0].Habitation.FirstSleepHere == nil {
		t.Fatal("Phase 2: first_sleep_here not set")
	}
	if lots[0].IsHabitable {
		t.Error("Phase 2: is_habitable must be false (meal + sleep but no toilet)")
	}

	// --- Phase 3: Toilet ---
	// Bladder was -50 (seed), now goes positive.
	toilet := makePerceptionLine(
		map[string]int16{"hunger": 20, "energy": 15, "bladder": 30},
		9, "bladder", true,
	)
	if !w.ObservePerception(toilet) {
		t.Fatal("Phase 3 (toilet): want true from ObservePerception")
	}
	lots, _ = ReadOwnedLots()
	if lots[0].Habitation.FirstUseToiletHere == nil {
		t.Fatal("Phase 3: first_use_toilet_here not set")
	}
	if !lots[0].IsHabitable {
		t.Fatal("Phase 3: is_habitable must be TRUE after all three thresholds met")
	}
	t.Logf("PASS: all three habitation thresholds met → is_habitable=true")
}

// TestHabitationWatcher_FlagsAreIdempotent asserts that a second meal crossing
// after first_meal_eaten_here is already set does not overwrite it (first-timestamp wins).
func TestHabitationWatcher_FlagsAreIdempotent(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "hab-test-idempotent")
	withConfigHome(t, tmp)
	withOwnedLots(t)

	w := NewHabitationWatcher()

	// First meal.
	w.prevMotives["hunger"] = -30
	eat1 := makePerceptionLine(
		map[string]int16{"hunger": 5, "energy": -20, "bladder": -10},
		3, "food", true,
	)
	w.ObservePerception(eat1)

	lots1, _ := ReadOwnedLots()
	firstTs := *lots1[0].Habitation.FirstMealEatenHere

	// Second meal crossing (hunger goes negative again then positive).
	// Simulate: prev was already positive, so no crossing. But let's force a
	// neg→pos cross by setting prevMotive explicitly.
	w.prevMotives["hunger"] = -10
	eat2 := makePerceptionLine(
		map[string]int16{"hunger": 15, "energy": -20, "bladder": -10},
		3, "food", true,
	)
	w.ObservePerception(eat2)

	lots2, _ := ReadOwnedLots()
	secondTs := *lots2[0].Habitation.FirstMealEatenHere

	if firstTs != secondTs {
		t.Errorf("first_meal_eaten_here should be idempotent: first=%d second=%d", firstTs, secondTs)
	}
	t.Logf("idempotency verified: ts=%d", firstTs)
}

// TestHabitationWatcher_NoCrossNoFlag asserts that when a motive is already
// positive (no neg→pos cross), no habitation flag is set — even with the right
// object category.
func TestHabitationWatcher_NoCrossNoFlag(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "hab-test-no-cross")
	withConfigHome(t, tmp)
	withOwnedLots(t)

	w := NewHabitationWatcher()

	// Prior hunger was already positive (50).
	w.prevMotives["hunger"] = 50

	line := makePerceptionLine(
		map[string]int16{"hunger": 60, "energy": -20, "bladder": -10},
		3, "food", true,
	)
	updated := w.ObservePerception(line)
	if updated {
		t.Error("want false when motive does not cross neg→pos (was already positive)")
	}

	lots, _ := ReadOwnedLots()
	if lots[0].Habitation.FirstMealEatenHere != nil {
		t.Error("first_meal_eaten_here must not be set when motive doesn't cross neg→pos")
	}
}

// TestHabitationWatcher_WrongCategoryNoFlag asserts that a correct motive crossing
// on the wrong object category does not trigger the flag.
func TestHabitationWatcher_WrongCategoryNoFlag(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "hab-test-wrong-cat")
	withConfigHome(t, tmp)
	withOwnedLots(t)

	w := NewHabitationWatcher()
	w.prevMotives["hunger"] = -30

	// Hunger crosses but the object is a TV (fun category), not food.
	line := makePerceptionLine(
		map[string]int16{"hunger": 10, "energy": -20, "bladder": -10},
		5, "fun", true,
	)
	updated := w.ObservePerception(line)
	if updated {
		t.Error("want false when motive crosses but object category does not match")
	}

	lots, _ := ReadOwnedLots()
	if lots[0].Habitation.FirstMealEatenHere != nil {
		t.Error("first_meal_eaten_here must not be set for wrong object category")
	}
}

// TestHabitationWatcher_NoInteractionNoFlag asserts that a motive crossing with
// no running interaction (no target object) does not trigger a flag. This prevents
// passive motive drift (e.g., hunger recovering by itself over time) from being
// mistaken for habitation-qualifying behavior.
func TestHabitationWatcher_NoInteractionNoFlag(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "hab-test-no-interaction")
	withConfigHome(t, tmp)
	withOwnedLots(t)

	w := NewHabitationWatcher()
	w.prevMotives["hunger"] = -30

	// Hunger crosses but no interaction is running (target_object_id=0).
	line := makePerceptionLine(
		map[string]int16{"hunger": 10, "energy": -20, "bladder": -10},
		0, "", true, // no interaction
	)
	updated := w.ObservePerception(line)
	if updated {
		t.Error("want false when motive crosses but no interaction is running")
	}

	lots, _ := ReadOwnedLots()
	if lots[0].Habitation.FirstMealEatenHere != nil {
		t.Error("first_meal_eaten_here must not be set without an active interaction")
	}
}

// TestHabitationWatcher_EmptyOwnedLotsNoOp asserts that when owned-lots.json
// does not exist or is empty, ObservePerception returns false without crashing.
func TestHabitationWatcher_EmptyOwnedLotsNoOp(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "hab-test-no-lots")
	withConfigHome(t, tmp)
	// Do NOT call withOwnedLots — owned-lots.json absent.

	w := NewHabitationWatcher()
	w.prevMotives["hunger"] = -30

	line := makePerceptionLine(
		map[string]int16{"hunger": 10, "energy": -20, "bladder": -10},
		3, "food", true,
	)
	// Should not panic or return error.
	updated := w.ObservePerception(line)
	if updated {
		t.Error("want false when owned-lots.json is absent (no owned lots)")
	}
}

// TestHabitationWatcher_MalformedLineDropped asserts that a malformed JSON line
// is dropped gracefully (no panic, returns false).
func TestHabitationWatcher_MalformedLineDropped(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "hab-test-malformed")
	withConfigHome(t, tmp)
	withOwnedLots(t)

	w := NewHabitationWatcher()
	if w.ObservePerception([]byte(`{not json`)) {
		t.Error("want false for malformed JSON")
	}
	if w.ObservePerception([]byte(`{}`)) {
		t.Error("want false for empty object (no kind)")
	}
}

// TestHabitationWatcher_FSO_USER_UnsetNoOp asserts that when FSO_USER is not set,
// ObservePerception returns false without crashing (PersonaStateDir returns error).
func TestHabitationWatcher_FSO_USER_UnsetNoOp(t *testing.T) {
	// Ensure FSO_USER is unset for this test.
	prior, hasPrior := os.LookupEnv("FSO_USER")
	os.Unsetenv("FSO_USER")
	t.Cleanup(func() {
		if hasPrior {
			os.Setenv("FSO_USER", prior)
		} else {
			os.Unsetenv("FSO_USER")
		}
	})

	w := NewHabitationWatcher()
	w.prevMotives["hunger"] = -30

	line := makePerceptionLine(
		map[string]int16{"hunger": 10, "energy": -20, "bladder": -10},
		3, "food", true,
	)
	// Must not panic.
	updated := w.ObservePerception(line)
	if updated {
		t.Error("want false when FSO_USER is unset (no persona state dir)")
	}
}

// TestHabitationWatcher_BridgeIntegration tests that the Bridges.handle method
// calls the habitation watcher on perception lines and that the watcher correctly
// populates owned-lots.json when conditions are met.
//
// This is the full integration path: bridge → watcher → persona state.
// Uses the recordingCampfire stub (from bridges_test.go) to avoid needing a real
// campfire store. Persona state is isolated via withFSO_USER + withConfigHome.
func TestHabitationWatcher_BridgeIntegration(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "hab-bridge-integ")
	withConfigHome(t, tmp)
	withOwnedLots(t)

	rec := newRecordingCampfire()
	watcher := NewHabitationWatcher()
	b := &Bridges{cf: rec, habWatcher: watcher}

	// Seed prior motive state.
	watcher.prevMotives["hunger"] = -40

	// Handle a perception event: hunger crosses neg→pos, interacting with food on owned lot.
	percLine := makePerceptionLine(
		map[string]int16{"hunger": 15, "energy": -30, "bladder": -20},
		3, "food", true,
	)
	b.handle(percLine)

	// Verify the broadcast happened.
	broadcasts := rec.recorder.(*recordingCampfire).broadcasts
	if len(broadcasts) != 1 {
		t.Fatalf("expected 1 broadcast, got %d", len(broadcasts))
	}
	if broadcasts[0].kind != "perception" {
		t.Errorf("broadcast kind: want perception, got %q", broadcasts[0].kind)
	}

	// Verify habitation flag was updated.
	lots, err := ReadOwnedLots()
	if err != nil {
		t.Fatalf("ReadOwnedLots: %v", err)
	}
	if lots[0].Habitation.FirstMealEatenHere == nil {
		t.Fatal("bridge integration: first_meal_eaten_here should be set after perception with food crossing")
	}
	t.Logf("PASS: bridge integration → habitation watcher → owned-lots.json written (first_meal_eaten_here=%d)",
		*lots[0].Habitation.FirstMealEatenHere)
}

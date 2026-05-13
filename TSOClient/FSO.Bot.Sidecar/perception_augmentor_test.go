/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

// Integration tests for PerceptionAugmentor (freesoexperiment-ef1, updated ea0).
//
// Tests exercise:
//   1. Non-perception events pass through unmodified.
//   2. home_lot=null when no lots owned.
//   3. home_lot populated from owned-lots.json[0] with is_habitable=false (pre-habitation).
//   4. home_lot.is_habitable=true after habitation watcher updates owned-lots.json.
//   5. lot.has_bulletin_board=true and lot.has_ballot_box=true on community lot (FREESO_COMMUNITY_LOT_ID match).
//   6. lot.has_bulletin_board=false on non-community lot.
//   7. lot.is_home=true when owner_is_me && home_lot present.
//   8. lot.is_home=false when owner_is_me=false.
//   9. LatestMayorStatus() returns zero before first tick; no mayor_status injected on tick without it.
//   10. mayor_status from C# tick is cached by augmentor and returned by LatestMayorStatus().
//   11. Malformed JSON passes through original unchanged.
//
// Each test sets FSO_USER to a per-test temp persona so state dirs are isolated.

import (
	"encoding/json"
	"os"
	"testing"
	"time"
)

// setupAugmentorTestEnv sets FSO_USER to a unique per-test persona derived from
// t.Name() and returns a cleanup func that clears FSO_USER and removes the
// persona state dir.
func setupAugmentorTestEnv(t *testing.T) func() {
	t.Helper()
	persona := "augtest_" + sanitizeTestName(t.Name())
	t.Setenv("FSO_USER", persona)
	// Ensure persona state dir is fresh.
	dir, err := PersonaStateDir()
	if err != nil {
		t.Fatalf("PersonaStateDir: %v", err)
	}
	if err := os.MkdirAll(dir, 0o700); err != nil {
		t.Fatalf("mkdir persona dir: %v", err)
	}
	return func() {
		os.RemoveAll(dir)
	}
}

// sanitizeTestName converts a test name to a safe persona identifier (lower-
// case, only alphanumeric and underscores, truncated to 32 chars).
func sanitizeTestName(name string) string {
	out := make([]byte, 0, 32)
	for _, c := range name {
		if c >= 'a' && c <= 'z' {
			out = append(out, byte(c))
		} else if c >= 'A' && c <= 'Z' {
			out = append(out, byte(c+'a'-'A'))
		} else if c >= '0' && c <= '9' {
			out = append(out, byte(c))
		} else {
			out = append(out, '_')
		}
		if len(out) >= 32 {
			break
		}
	}
	return string(out)
}

// makeAugmentorPerceptionLine creates a minimal perception tick JSON with
// the given lot_id, owner_is_me, and lot_name. Used across augmentor tests.
func makeAugmentorPerceptionLine(lotID int64, ownerIsMe bool, lotName string) []byte {
	tick := map[string]any{
		"kind": "perception",
		"t":    time.Now().UnixMilli(),
		"avatar": map[string]any{
			"persist_id": 2,
			"name":       "TestSim",
		},
		"motives": map[string]any{},
		"lot": map[string]any{
			"name":        lotName,
			"lot_id":      lotID,
			"owner_is_me": ownerIsMe,
		},
	}
	b, _ := json.Marshal(tick)
	return b
}

// decodeAugmented decodes an augmented perception line into a generic map.
func decodeAugmented(t *testing.T, line []byte) map[string]any {
	t.Helper()
	var m map[string]any
	if err := json.Unmarshal(line, &m); err != nil {
		t.Fatalf("decode augmented line: %v — line was: %s", err, string(line))
	}
	return m
}

// getLotField extracts a field from the "lot" sub-object in an augmented map.
func getLotField(t *testing.T, m map[string]any, field string) any {
	t.Helper()
	lotRaw, ok := m["lot"]
	if !ok {
		t.Fatalf("augmented tick missing 'lot' key")
	}
	// Re-encode and decode as a map to handle interface{} nesting.
	lotBytes, _ := json.Marshal(lotRaw)
	var lotMap map[string]any
	if err := json.Unmarshal(lotBytes, &lotMap); err != nil {
		t.Fatalf("decode lot sub-object: %v", err)
	}
	return lotMap[field]
}

// ---- tests ----

func TestAugmentor_NonPerceptionPassThrough(t *testing.T) {
	cleanup := setupAugmentorTestEnv(t)
	defer cleanup()

	a := NewPerceptionAugmentor(nil, false)
	dialog := []byte(`{"kind":"dialog","t":1000,"payload":{"text":"hello"}}`)
	out := a.AugmentPerception(dialog)
	if string(out) != string(dialog) {
		t.Errorf("non-perception line was modified\ngot:  %s\nwant: %s", out, dialog)
	}
}

func TestAugmentor_HomeLotNullWhenNoLotsOwned(t *testing.T) {
	cleanup := setupAugmentorTestEnv(t)
	defer cleanup()

	// No lots in owned-lots.json.
	a := NewPerceptionAugmentor(nil, false)
	line := makeAugmentorPerceptionLine(2, true, "Test Lot")
	out := a.AugmentPerception(line)
	m := decodeAugmented(t, out)

	// home_lot must be present but null.
	homeLot, exists := m["home_lot"]
	if !exists {
		t.Fatal("augmented tick missing 'home_lot' key")
	}
	if homeLot != nil {
		t.Errorf("home_lot = %v, want nil (no owned lots)", homeLot)
	}
}

func TestAugmentor_HomeLotPopulatedWithIsHabitableFalse(t *testing.T) {
	cleanup := setupAugmentorTestEnv(t)
	defer cleanup()

	// Write owned-lots.json with one lot, is_habitable=false.
	entry := OwnedLotEntry{
		Name:        "Botrous Corner",
		LocationHex: "0x00F9015E",
		PurchasedAt: time.Now().UnixMilli(),
		IsHabitable: false,
	}
	if err := WriteOwnedLots([]OwnedLotEntry{entry}); err != nil {
		t.Fatalf("write owned-lots: %v", err)
	}

	a := NewPerceptionAugmentor(nil, false)
	line := makeAugmentorPerceptionLine(3, true, "Botrous Corner")
	out := a.AugmentPerception(line)
	m := decodeAugmented(t, out)

	homeLotRaw, exists := m["home_lot"]
	if !exists {
		t.Fatal("augmented tick missing 'home_lot' key")
	}
	if homeLotRaw == nil {
		t.Fatal("home_lot is null, expected non-null (lots were written)")
	}

	homeLotBytes, _ := json.Marshal(homeLotRaw)
	var proj HomeLotProjection
	if err := json.Unmarshal(homeLotBytes, &proj); err != nil {
		t.Fatalf("decode home_lot projection: %v", err)
	}

	if proj.Name != "Botrous Corner" {
		t.Errorf("home_lot.name = %q, want %q", proj.Name, "Botrous Corner")
	}
	if proj.LocationHex != "0x00F9015E" {
		t.Errorf("home_lot.location_hex = %q, want %q", proj.LocationHex, "0x00F9015E")
	}
	if proj.IsHabitable {
		t.Errorf("home_lot.is_habitable = true, want false (habitation not yet met)")
	}
}

func TestAugmentor_HomeLotIsHabitableTrueAfterHabitation(t *testing.T) {
	cleanup := setupAugmentorTestEnv(t)
	defer cleanup()

	// Write owned-lots.json with is_habitable=true (simulates all three
	// thresholds met by the habitation watcher).
	nowMs := time.Now().UnixMilli()
	entry := OwnedLotEntry{
		Name:        "Sage Retreat",
		LocationHex: "0x00F9015F",
		PurchasedAt: nowMs,
		Habitation: OwnedLotHabitation{
			FirstMealEatenHere: &nowMs,
			FirstSleepHere:     &nowMs,
			FirstUseToiletHere: &nowMs,
		},
		IsHabitable: true,
	}
	if err := WriteOwnedLots([]OwnedLotEntry{entry}); err != nil {
		t.Fatalf("write owned-lots: %v", err)
	}

	a := NewPerceptionAugmentor(nil, false)
	line := makeAugmentorPerceptionLine(4, true, "Sage Retreat")
	out := a.AugmentPerception(line)
	m := decodeAugmented(t, out)

	homeLotRaw := m["home_lot"]
	if homeLotRaw == nil {
		t.Fatal("home_lot is null, expected populated entry")
	}
	homeLotBytes, _ := json.Marshal(homeLotRaw)
	var proj HomeLotProjection
	if err := json.Unmarshal(homeLotBytes, &proj); err != nil {
		t.Fatalf("decode home_lot: %v", err)
	}
	if !proj.IsHabitable {
		t.Errorf("home_lot.is_habitable = false, want true (all thresholds met)")
	}
}

func TestAugmentor_CommunityLotAffordancesTrue(t *testing.T) {
	cleanup := setupAugmentorTestEnv(t)
	defer cleanup()

	// Set community lot to lot_id=17.
	t.Setenv("FREESO_COMMUNITY_LOT_ID", "17")

	a := NewPerceptionAugmentor(nil, false)
	line := makeAugmentorPerceptionLine(17, false, "Town Hall")
	out := a.AugmentPerception(line)
	m := decodeAugmented(t, out)

	hasBB := getLotField(t, m, "has_bulletin_board")
	hasBallot := getLotField(t, m, "has_ballot_box")

	if hasBB != true {
		t.Errorf("lot.has_bulletin_board = %v, want true on community lot (lot_id=17)", hasBB)
	}
	if hasBallot != true {
		t.Errorf("lot.has_ballot_box = %v, want true on community lot (lot_id=17)", hasBallot)
	}
}

func TestAugmentor_NonCommunityLotAffordancesFalse(t *testing.T) {
	cleanup := setupAugmentorTestEnv(t)
	defer cleanup()

	// Set community lot to lot_id=17, but current lot is 2.
	t.Setenv("FREESO_COMMUNITY_LOT_ID", "17")

	a := NewPerceptionAugmentor(nil, false)
	line := makeAugmentorPerceptionLine(2, true, "Baron's Main")
	out := a.AugmentPerception(line)
	m := decodeAugmented(t, out)

	hasBB := getLotField(t, m, "has_bulletin_board")
	hasBallot := getLotField(t, m, "has_ballot_box")

	if hasBB != false {
		t.Errorf("lot.has_bulletin_board = %v, want false on residential lot (lot_id=2)", hasBB)
	}
	if hasBallot != false {
		t.Errorf("lot.has_ballot_box = %v, want false on residential lot (lot_id=2)", hasBallot)
	}
}

func TestAugmentor_AffordancesFalseWhenCommunityLotNotSet(t *testing.T) {
	cleanup := setupAugmentorTestEnv(t)
	defer cleanup()

	// FREESO_COMMUNITY_LOT_ID not set.
	os.Unsetenv("FREESO_COMMUNITY_LOT_ID")

	a := NewPerceptionAugmentor(nil, false)
	line := makeAugmentorPerceptionLine(17, false, "Town Hall")
	out := a.AugmentPerception(line)
	m := decodeAugmented(t, out)

	hasBB := getLotField(t, m, "has_bulletin_board")
	hasBallot := getLotField(t, m, "has_ballot_box")

	if hasBB != false {
		t.Errorf("lot.has_bulletin_board = %v, want false (no FREESO_COMMUNITY_LOT_ID)", hasBB)
	}
	if hasBallot != false {
		t.Errorf("lot.has_ballot_box = %v, want false (no FREESO_COMMUNITY_LOT_ID)", hasBallot)
	}
}

func TestAugmentor_IsHomeTrueWhenOwnerIsMeAndHomeLotPresent(t *testing.T) {
	cleanup := setupAugmentorTestEnv(t)
	defer cleanup()

	// Write owned lots so home_lot is populated.
	entry := OwnedLotEntry{
		Name:        "My Place",
		LocationHex: "0x00F90160",
		PurchasedAt: time.Now().UnixMilli(),
	}
	if err := WriteOwnedLots([]OwnedLotEntry{entry}); err != nil {
		t.Fatalf("write owned-lots: %v", err)
	}

	a := NewPerceptionAugmentor(nil, false)
	// owner_is_me=true simulates being on your own lot.
	line := makeAugmentorPerceptionLine(5, true, "My Place")
	out := a.AugmentPerception(line)
	m := decodeAugmented(t, out)

	isHome := getLotField(t, m, "is_home")
	if isHome != true {
		t.Errorf("lot.is_home = %v, want true (owner_is_me=true with home lot present)", isHome)
	}
}

func TestAugmentor_IsHomeFalseWhenNotOwner(t *testing.T) {
	cleanup := setupAugmentorTestEnv(t)
	defer cleanup()

	// Write owned lots so home_lot is populated.
	entry := OwnedLotEntry{
		Name:        "My Place",
		LocationHex: "0x00F90161",
		PurchasedAt: time.Now().UnixMilli(),
	}
	if err := WriteOwnedLots([]OwnedLotEntry{entry}); err != nil {
		t.Fatalf("write owned-lots: %v", err)
	}

	a := NewPerceptionAugmentor(nil, false)
	// owner_is_me=false — visiting someone else's lot.
	line := makeAugmentorPerceptionLine(6, false, "Ellis's Corner")
	out := a.AugmentPerception(line)
	m := decodeAugmented(t, out)

	isHome := getLotField(t, m, "is_home")
	if isHome != false {
		t.Errorf("lot.is_home = %v, want false (owner_is_me=false)", isHome)
	}
}

// TestAugmentor_MayorStatusZeroBeforeFirstTick verifies that LatestMayorStatus()
// returns the zero value (is_mayor=false, mayor_nhood=0) before any perception
// tick has arrived. Also verifies that a tick WITHOUT mayor_status passes through
// unchanged (augmentor does not inject a zero mayor_status block).
// (freesoexperiment-ea0: mayor_status now comes from the C# VM tick, not a file.)
func TestAugmentor_MayorStatusZeroBeforeFirstTick(t *testing.T) {
	a := NewPerceptionAugmentor(nil, false)

	// Before any tick: cache is zero.
	ms := a.LatestMayorStatus()
	if ms.IsMayor {
		t.Errorf("LatestMayorStatus().IsMayor = true before first tick, want false")
	}
	if ms.MayorNhood != 0 {
		t.Errorf("LatestMayorStatus().MayorNhood = %d before first tick, want 0", ms.MayorNhood)
	}

	// A tick without mayor_status should pass through without injecting the field.
	line := makeAugmentorPerceptionLine(2, true, "Baron's Main")
	out := a.AugmentPerception(line)
	m := decodeAugmented(t, out)
	if _, exists := m["mayor_status"]; exists {
		t.Errorf("augmentor injected 'mayor_status' key when C# tick did not include it — want pass-through only")
	}
}

// TestAugmentor_MayorStatusCachedFromTick verifies that when a perception tick
// includes mayor_status from the C# bot, the augmentor caches it and
// LatestMayorStatus() returns the cached value.
// (freesoexperiment-ea0: mayor_status now comes from the C# VM tick, not a file.)
func TestAugmentor_MayorStatusCachedFromTick(t *testing.T) {
	a := NewPerceptionAugmentor(nil, false)

	// Feed a tick with mayor_status={is_mayor:true, mayor_nhood:1}.
	tick := map[string]any{
		"kind": "perception",
		"t":    time.Now().UnixMilli(),
		"lot": map[string]any{
			"lot_id":      int64(2),
			"owner_is_me": true,
			"name":        "Baron's Main",
		},
		"mayor_status": map[string]any{
			"is_mayor":    true,
			"mayor_nhood": 1,
		},
	}
	lineBytes, _ := json.Marshal(tick)
	a.AugmentPerception(lineBytes)

	// Cache must now reflect is_mayor=true.
	ms := a.LatestMayorStatus()
	if !ms.IsMayor {
		t.Errorf("LatestMayorStatus().IsMayor = false after tick with is_mayor=true, want true")
	}
	if ms.MayorNhood != 1 {
		t.Errorf("LatestMayorStatus().MayorNhood = %d, want 1", ms.MayorNhood)
	}

	// Feed another tick with is_mayor=false — cache must update.
	tick2 := map[string]any{
		"kind": "perception",
		"mayor_status": map[string]any{
			"is_mayor":    false,
			"mayor_nhood": 0,
		},
	}
	line2, _ := json.Marshal(tick2)
	a.AugmentPerception(line2)

	ms2 := a.LatestMayorStatus()
	if ms2.IsMayor {
		t.Errorf("LatestMayorStatus().IsMayor = true after tick with is_mayor=false, want false")
	}
	if ms2.MayorNhood != 0 {
		t.Errorf("LatestMayorStatus().MayorNhood = %d, want 0", ms2.MayorNhood)
	}
}

func TestAugmentor_MalformedJSONPassThrough(t *testing.T) {
	cleanup := setupAugmentorTestEnv(t)
	defer cleanup()

	a := NewPerceptionAugmentor(nil, false)
	bad := []byte(`{"kind":"perception","t":bad_value}`)
	out := a.AugmentPerception(bad)
	if string(out) != string(bad) {
		t.Errorf("malformed JSON was modified, want original returned\ngot:  %s\nwant: %s", out, bad)
	}
}

func TestAugmentor_HabitationWatcherRunsBeforeAugmentor(t *testing.T) {
	// This test verifies that when a perception tick triggers a habitation update
	// (habitation watcher runs first), the augmentor's home_lot.is_habitable
	// reflects the UPDATED state (not the pre-update state).
	//
	// Sequence:
	//   1. owned-lots.json has is_habitable=false, all habitation fields nil.
	//   2. A perception tick arrives where hunger crosses neg→pos while eating food.
	//   3. HabitationWatcher.ObservePerception runs → writes first_meal_eaten_here.
	//   4. PerceptionAugmentor.AugmentPerception runs → reads updated owned-lots.json.
	//   5. home_lot.is_habitable should STILL be false (only one of three done).
	//
	// Regression: if watcher ran AFTER augmentor, is_habitable would always lag one tick.
	// This test doesn't directly verify the full pipeline (that's in bridges_test.go),
	// but verifies the is_habitable=false → true sequence across ticks.

	cleanup := setupAugmentorTestEnv(t)
	defer cleanup()

	// Start with is_habitable=false.
	entry := OwnedLotEntry{
		Name:        "Test Lot",
		LocationHex: "0x00F90162",
		PurchasedAt: time.Now().UnixMilli(),
		IsHabitable: false,
	}
	if err := WriteOwnedLots([]OwnedLotEntry{entry}); err != nil {
		t.Fatalf("write owned-lots: %v", err)
	}

	a := NewPerceptionAugmentor(nil, false)
	line := makeAugmentorPerceptionLine(7, true, "Test Lot")
	out := a.AugmentPerception(line)
	m := decodeAugmented(t, out)

	homeLotRaw := m["home_lot"]
	if homeLotRaw == nil {
		t.Fatal("home_lot is null")
	}
	homeLotBytes, _ := json.Marshal(homeLotRaw)
	var proj HomeLotProjection
	if err := json.Unmarshal(homeLotBytes, &proj); err != nil {
		t.Fatalf("decode home_lot: %v", err)
	}
	if proj.IsHabitable {
		t.Errorf("home_lot.is_habitable = true before any habitation events, want false")
	}

	// Now simulate the watcher marking all three as done.
	nowMs := time.Now().UnixMilli()
	updated := OwnedLotEntry{
		Name:        entry.Name,
		LocationHex: entry.LocationHex,
		PurchasedAt: entry.PurchasedAt,
		Habitation: OwnedLotHabitation{
			FirstMealEatenHere: &nowMs,
			FirstSleepHere:     &nowMs,
			FirstUseToiletHere: &nowMs,
		},
		IsHabitable: true,
	}
	if err := WriteOwnedLots([]OwnedLotEntry{updated}); err != nil {
		t.Fatalf("write updated owned-lots: %v", err)
	}

	// Next augmentation tick reads the updated state.
	line2 := makeAugmentorPerceptionLine(7, true, "Test Lot")
	out2 := a.AugmentPerception(line2)
	m2 := decodeAugmented(t, out2)

	homeLotRaw2 := m2["home_lot"]
	if homeLotRaw2 == nil {
		t.Fatal("home_lot is null on second tick")
	}
	homeLotBytes2, _ := json.Marshal(homeLotRaw2)
	var proj2 HomeLotProjection
	if err := json.Unmarshal(homeLotBytes2, &proj2); err != nil {
		t.Fatalf("decode home_lot on second tick: %v", err)
	}
	if !proj2.IsHabitable {
		t.Errorf("home_lot.is_habitable = false after all habitation events, want true")
	}
}

// ---- chargen_pending tests (freesoexperiment-b094) ----

// makeChargenPerceptionLine creates a minimal perception tick for chargen-mode
// testing. persist_id=0 means "no avatar yet"; a non-zero value means the
// avatar already exists.
func makeChargenPerceptionLine(persistID int64) []byte {
	tick := map[string]any{
		"kind": "perception",
		"t":    time.Now().UnixMilli(),
		"avatar": map[string]any{
			"persist_id": persistID,
			"name":       "NewSim",
		},
		"motives": map[string]any{},
	}
	b, _ := json.Marshal(tick)
	return b
}

// TestAugmentor_ChargenPendingTrueWhenNoAvatar verifies that when the augmentor
// is constructed with chargenMode=true and the first tick has persist_id=0,
// chargen_pending=true is emitted.
// (freesoexperiment-b094 done condition §1: sidecar test)
func TestAugmentor_ChargenPendingTrueWhenNoAvatar(t *testing.T) {
	// No tick environ needed — chargen_pending is independent of persona state files.
	a := NewPerceptionAugmentor(nil, true) // chargenMode=true

	line := makeChargenPerceptionLine(0) // persist_id=0 → no avatar yet
	out := a.AugmentPerception(line)
	m := decodeAugmented(t, out)

	cp, exists := m["chargen_pending"]
	if !exists {
		t.Fatal("chargen_pending key absent from augmented tick in chargen-mode, want present")
	}
	if cp != true {
		t.Errorf("chargen_pending = %v, want true when persist_id=0 in chargen-mode", cp)
	}

	// IsChargenPending() must agree.
	if !a.IsChargenPending() {
		t.Errorf("IsChargenPending() = false, want true when persist_id=0 in chargen-mode")
	}
}

// TestAugmentor_ChargenPendingFalseAfterAvatarSeen verifies that once a tick
// with a non-zero persist_id arrives, chargen_pending flips to false and stays
// false on subsequent ticks.
// (freesoexperiment-b094 done condition §1)
func TestAugmentor_ChargenPendingFalseAfterAvatarSeen(t *testing.T) {
	a := NewPerceptionAugmentor(nil, true) // chargenMode=true

	// First tick: no avatar.
	line1 := makeChargenPerceptionLine(0)
	out1 := a.AugmentPerception(line1)
	m1 := decodeAugmented(t, out1)
	if m1["chargen_pending"] != true {
		t.Errorf("tick1: chargen_pending = %v, want true before avatar seen", m1["chargen_pending"])
	}

	// Second tick: avatar appears (persist_id=42).
	line2 := makeChargenPerceptionLine(42)
	out2 := a.AugmentPerception(line2)
	m2 := decodeAugmented(t, out2)
	if m2["chargen_pending"] != false {
		t.Errorf("tick2: chargen_pending = %v, want false after persist_id=42", m2["chargen_pending"])
	}

	// IsChargenPending() must also flip.
	if a.IsChargenPending() {
		t.Errorf("IsChargenPending() = true after persist_id seen, want false")
	}

	// Third tick: still false — not reset.
	line3 := makeChargenPerceptionLine(42)
	out3 := a.AugmentPerception(line3)
	m3 := decodeAugmented(t, out3)
	if m3["chargen_pending"] != false {
		t.Errorf("tick3: chargen_pending = %v, want false (stays false after avatar seen)", m3["chargen_pending"])
	}
}

// TestAugmentor_ChargenPendingAbsentInNormalMode verifies that when chargenMode=false
// (normal lot-joined mode), the chargen_pending key is NOT emitted at all.
// (freesoexperiment-b094: field must be absent in live sessions)
func TestAugmentor_ChargenPendingAbsentInNormalMode(t *testing.T) {
	cleanup := setupAugmentorTestEnv(t)
	defer cleanup()

	a := NewPerceptionAugmentor(nil, false) // chargenMode=false → normal mode

	line := makeAugmentorPerceptionLine(2, true, "Baron's Main")
	out := a.AugmentPerception(line)
	m := decodeAugmented(t, out)

	if _, exists := m["chargen_pending"]; exists {
		t.Errorf("chargen_pending present in normal-mode tick, want absent (value=%v)", m["chargen_pending"])
	}
}

// TestAugmentor_ChargenPendingTrueInitiallyThenFalseAfterCreate verifies the
// full lifecycle: chargen_pending starts true, then the create-avatar op fires,
// and the next tick (with persist_id from the new avatar) flips it to false.
// This is the integration path exercised during a real chargen run.
// (freesoexperiment-b094 done condition §1: deterministic x3)
func TestAugmentor_ChargenPendingTrueInitiallyThenFalseAfterCreate(t *testing.T) {
	for run := 1; run <= 3; run++ {
		a := NewPerceptionAugmentor(nil, true)

		// Tick 1: no avatar.
		t1 := makeChargenPerceptionLine(0)
		m1 := decodeAugmented(t, a.AugmentPerception(t1))
		if m1["chargen_pending"] != true {
			t.Errorf("run %d tick1: chargen_pending = %v, want true", run, m1["chargen_pending"])
		}

		// Simulate create-avatar succeeding: next tick has persist_id=99.
		t2 := makeChargenPerceptionLine(99)
		m2 := decodeAugmented(t, a.AugmentPerception(t2))
		if m2["chargen_pending"] != false {
			t.Errorf("run %d tick2: chargen_pending = %v, want false after create", run, m2["chargen_pending"])
		}
	}
}

// TestHasBotArg verifies that hasBotArg correctly detects --chargen-mode in
// the parsed args slice. This covers the detection logic in main.go.
// (freesoexperiment-b094)
func TestHasBotArg(t *testing.T) {
	cases := []struct {
		args []string
		flag string
		want bool
	}{
		{[]string{"--emit-perception", "--chargen-mode"}, "--chargen-mode", true},
		{[]string{"--emit-perception"}, "--chargen-mode", false},
		{[]string{}, "--chargen-mode", false},
		{[]string{"--chargen-mode"}, "--chargen-mode", true},
		{[]string{"--chargen-mode-extra"}, "--chargen-mode", false}, // prefix mismatch
	}
	for _, c := range cases {
		got := hasBotArg(c.args, c.flag)
		if got != c.want {
			t.Errorf("hasBotArg(%v, %q) = %v, want %v", c.args, c.flag, got, c.want)
		}
	}
}

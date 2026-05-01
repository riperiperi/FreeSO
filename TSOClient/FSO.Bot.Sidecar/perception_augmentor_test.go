/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

// Integration tests for PerceptionAugmentor (freesoexperiment-ef1).
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
//   9. mayor_status zero value when mayor-status.json absent.
//   10. mayor_status populated from mayor-status.json when present.
//   11. Malformed JSON passes through original unchanged.
//
// Each test sets FSO_USER to a per-test temp persona so state dirs are isolated.

import (
	"encoding/json"
	"os"
	"path/filepath"
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

	a := NewPerceptionAugmentor()
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
	a := NewPerceptionAugmentor()
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

	a := NewPerceptionAugmentor()
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

	a := NewPerceptionAugmentor()
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

	a := NewPerceptionAugmentor()
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

	a := NewPerceptionAugmentor()
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

	a := NewPerceptionAugmentor()
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

	a := NewPerceptionAugmentor()
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

	a := NewPerceptionAugmentor()
	// owner_is_me=false — visiting someone else's lot.
	line := makeAugmentorPerceptionLine(6, false, "Ellis's Corner")
	out := a.AugmentPerception(line)
	m := decodeAugmented(t, out)

	isHome := getLotField(t, m, "is_home")
	if isHome != false {
		t.Errorf("lot.is_home = %v, want false (owner_is_me=false)", isHome)
	}
}

func TestAugmentor_MayorStatusZeroWhenFileAbsent(t *testing.T) {
	cleanup := setupAugmentorTestEnv(t)
	defer cleanup()

	// No mayor-status.json written.
	a := NewPerceptionAugmentor()
	line := makeAugmentorPerceptionLine(2, true, "Baron's Main")
	out := a.AugmentPerception(line)
	m := decodeAugmented(t, out)

	msRaw, exists := m["mayor_status"]
	if !exists {
		t.Fatal("augmented tick missing 'mayor_status' key")
	}
	msBytes, _ := json.Marshal(msRaw)
	var ms MayorStatus
	if err := json.Unmarshal(msBytes, &ms); err != nil {
		t.Fatalf("decode mayor_status: %v", err)
	}
	if ms.IsMayor {
		t.Errorf("mayor_status.is_mayor = true, want false (file absent)")
	}
	if ms.ElectionPhase != "none" {
		t.Errorf("mayor_status.election_phase = %q, want %q", ms.ElectionPhase, "none")
	}
	if ms.CurrentMayorName != nil {
		t.Errorf("mayor_status.current_mayor_name = %v, want nil", ms.CurrentMayorName)
	}
}

func TestAugmentor_MayorStatusReadFromFile(t *testing.T) {
	cleanup := setupAugmentorTestEnv(t)
	defer cleanup()

	// Write mayor-status.json.
	dir, err := PersonaStateDir()
	if err != nil {
		t.Fatalf("PersonaStateDir: %v", err)
	}
	mayorName := "Baron"
	ms := MayorStatus{
		IsMayor:          true,
		CurrentMayorName: &mayorName,
		ElectionPhase:    "nomination",
	}
	data, _ := json.Marshal(ms)
	if err := os.WriteFile(filepath.Join(dir, "mayor-status.json"), data, 0o600); err != nil {
		t.Fatalf("write mayor-status.json: %v", err)
	}

	a := NewPerceptionAugmentor()
	line := makeAugmentorPerceptionLine(2, true, "Baron's Main")
	out := a.AugmentPerception(line)
	m := decodeAugmented(t, out)

	msRaw := m["mayor_status"]
	msBytes, _ := json.Marshal(msRaw)
	var got MayorStatus
	if err := json.Unmarshal(msBytes, &got); err != nil {
		t.Fatalf("decode mayor_status: %v", err)
	}
	if !got.IsMayor {
		t.Errorf("mayor_status.is_mayor = false, want true")
	}
	if got.ElectionPhase != "nomination" {
		t.Errorf("mayor_status.election_phase = %q, want %q", got.ElectionPhase, "nomination")
	}
	if got.CurrentMayorName == nil || *got.CurrentMayorName != "Baron" {
		t.Errorf("mayor_status.current_mayor_name = %v, want \"Baron\"", got.CurrentMayorName)
	}
}

func TestAugmentor_MalformedJSONPassThrough(t *testing.T) {
	cleanup := setupAugmentorTestEnv(t)
	defer cleanup()

	a := NewPerceptionAugmentor()
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

	a := NewPerceptionAugmentor()
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

/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

// HabitationWatcher observes perception events while the soul is on an owned lot
// and populates the I0-7 habitation flags in owned-lots.json.
//
// Design: I0-7 habitation invariant (cross-lot-first-class.md §Q9).
// A parcel is "yours" only after the body has eaten, slept, and used a toilet on it.
// The watcher tracks three thresholds:
//
//   - first_meal_eaten_here:  hunger crosses neg→pos while interacting with food-class object
//   - first_sleep_here:       energy crosses neg→pos while interacting with bed-class object
//   - first_use_toilet_here:  bladder crosses neg→pos while interacting with toilet-class object
//
// When all three are populated, is_habitable is set to true.
//
// Motive value range: FSO motives are short values in [-100, 100]. "Negative" means < 0;
// "Positive" means > 0. The crossing condition is: prev_value < 0 AND curr_value >= 0
// (or first-ever reading >= 0 with no prior history — treated as positive from the start,
// which would not count as a "crossing").
//
// GUID catalog (docs/blueprints/object-class-guids.md, item 5):
//
//	Food class:   fridge (0x675C18AF), stove (0xEB686709)
//	Bed class:    bed    (0x0E82C943)
//	Toilet class: toilet (0x84E0774C)
//	(shower 0x85E02A40 maps to hygiene, not bladder — excluded from habitation tracking)
//
// The watcher correlates the action_queue[0].target_object_id with nearby_objects to
// identify the object's category. This is the authoritative correlation path because
// the perception tick does not carry GUID fields on nearby objects — category is derived
// from object names/hint tables by the C# PerceptionProjector.
//
// NOTE: lot ownership check. The watcher only records habitation events when
// lot.owner_is_me is true in the perception tick. Walking a property line without
// ownership does not count; nor does eating on a non-owned lot.
//
// Thread-safety: HabitationWatcher is not goroutine-safe. It is called sequentially
// by the Bridge goroutine for each perception line. No concurrent access.

import (
	"encoding/json"
	"log"
	"time"
)

// HabitationWatcher tracks per-lot habitation events from perception stream.
type HabitationWatcher struct {
	// prevMotives holds the motive value from the last processed perception tick.
	// Key: motive name ("hunger", "energy", "bladder"). Value: short motive value.
	prevMotives map[string]int16

	// currentLotHex is the hex location of the lot identified as owned in the last
	// perception tick. Empty string when not on an owned lot.
	currentLotHex string
}

// NewHabitationWatcher constructs a HabitationWatcher with empty prior state.
func NewHabitationWatcher() *HabitationWatcher {
	return &HabitationWatcher{
		prevMotives: make(map[string]int16),
	}
}

// perceptionPayload is the loose-schema decode of a perception tick from the bridge.
// Only the fields needed by the watcher are decoded; the rest are ignored.
type perceptionPayload struct {
	Kind    string                     `json:"kind"`
	T       int64                      `json:"t"`
	Avatar  *perceptionAvatar          `json:"avatar"`
	Motives map[string]json.RawMessage `json:"motives"`
	Lot     *perceptionLot             `json:"lot"`
}

type perceptionAvatar struct {
	ActionQueue []actionQueueItem `json:"action_queue"`
}

type actionQueueItem struct {
	InteractionID  int    `json:"interaction_id"`
	Name           string `json:"name"`
	TargetObjectID int16  `json:"target_object_id"`
	Status         string `json:"status"`
}

type perceptionLot struct {
	OwnerIsMe bool   `json:"owner_is_me"`
	Name      string `json:"name"`
	LotID     int64  `json:"lot_id"`
}

// motiveValueRaw decodes just the value field from a raw motive JSON object.
// PerceptionProjector serialises motives as {"value": <short>, "delta_per_minute": ..., "trend": ...}.
type motiveValueRaw struct {
	Value int16 `json:"value"`
}

// perceptionNearbyObjects decodes the nearby_objects array to resolve
// target_object_id → category. Decoded lazily on demand.
type perceptionWithNearby struct {
	NearbyObjects []nearbyObjectBrief `json:"nearby_objects"`
}

type nearbyObjectBrief struct {
	ObjectID int16  `json:"object_id"`
	Category string `json:"category"`
	Name     string `json:"name"`
}

// categoryForObject looks up the category of a VM object_id in the nearby_objects
// decoded from the perception line. Returns "" if not found.
func categoryForObject(raw []byte, targetObjectID int16) string {
	if targetObjectID == 0 {
		return ""
	}
	var nearby perceptionWithNearby
	if err := json.Unmarshal(raw, &nearby); err != nil {
		return ""
	}
	for _, obj := range nearby.NearbyObjects {
		if obj.ObjectID == targetObjectID {
			return obj.Category
		}
	}
	return ""
}

// ObservePerception processes one raw perception line from the bridge.
// It returns true if any habitation flag was updated (owned-lots.json was written).
// Errors are logged and do not crash the sidecar.
func (w *HabitationWatcher) ObservePerception(line []byte) bool {
	var p perceptionPayload
	if err := json.Unmarshal(line, &p); err != nil {
		log.Printf("habitation-watcher: parse perception: %v", err)
		return false
	}
	if p.Kind != "perception" {
		return false
	}
	if p.Lot == nil || !p.Lot.OwnerIsMe {
		// Not on an owned lot. Clear prev motives to avoid stale cross-ticks
		// across lot boundaries.
		if w.currentLotHex != "" {
			log.Printf("habitation-watcher: left owned lot %s", w.currentLotHex)
			w.currentLotHex = ""
			w.prevMotives = make(map[string]int16)
		}
		return false
	}

	// We are on a lot where owner_is_me. Resolve which owned lot this is by reading
	// owned-lots.json. We match by lot_id if available, or fall back to currentLotHex
	// for continuity.
	//
	// NOTE: We re-read owned-lots.json on each tick where we are on an owned lot.
	// This is intentionally lightweight — the file is small (≤10 entries) and the
	// tick rate is FSO_PERCEPTION_HZ (default 0.5 Hz = 1 tick/2s). Cost is negligible.
	lots, err := ReadOwnedLots()
	if err != nil {
		log.Printf("habitation-watcher: read owned-lots.json: %v", err)
		return false
	}
	if len(lots) == 0 {
		return false
	}

	// For now, treat the most-recently-purchased (last) lot as "home" if there's only one.
	// When multi-lot is supported, match by lot_id via the lot block.
	// TODO(multi-lot): match by p.Lot.LotID → stored lot ID once we store lot_id in OwnedLotEntry.
	target := &lots[len(lots)-1]
	w.currentLotHex = target.LocationHex

	// Decode current motives.
	currMotives := make(map[string]int16, 3)
	for _, key := range []string{"hunger", "energy", "bladder"} {
		raw, ok := p.Motives[key]
		if !ok {
			continue
		}
		var mv motiveValueRaw
		if err := json.Unmarshal(raw, &mv); err != nil {
			log.Printf("habitation-watcher: decode motive %q: %v", key, err)
			continue
		}
		currMotives[key] = mv.Value
	}

	// Find running interaction object_id from action_queue.
	var runningTargetObjectID int16
	if p.Avatar != nil {
		for _, qi := range p.Avatar.ActionQueue {
			if qi.Status == "running" {
				runningTargetObjectID = qi.TargetObjectID
				break
			}
		}
	}

	// Resolve category for the running interaction's target object.
	interactionCategory := categoryForObject(line, runningTargetObjectID)

	// Check each habitation threshold.
	updated := false
	nowMs := p.T
	if nowMs == 0 {
		nowMs = time.Now().UnixMilli()
	}

	// --- Food / hunger ---
	if target.Habitation.FirstMealEatenHere == nil {
		if prev, hasPrev := w.prevMotives["hunger"]; hasPrev {
			curr := currMotives["hunger"]
			if prev < 0 && curr >= 0 && isFoodCategory(interactionCategory) {
				log.Printf("habitation-watcher: first_meal_eaten_here on %s (hunger %d→%d, cat=%s)",
					w.currentLotHex, prev, curr, interactionCategory)
				target.Habitation.FirstMealEatenHere = &nowMs
				updated = true
			}
		}
	}

	// --- Sleep / energy ---
	if target.Habitation.FirstSleepHere == nil {
		if prev, hasPrev := w.prevMotives["energy"]; hasPrev {
			curr := currMotives["energy"]
			if prev < 0 && curr >= 0 && isBedCategory(interactionCategory) {
				log.Printf("habitation-watcher: first_sleep_here on %s (energy %d→%d, cat=%s)",
					w.currentLotHex, prev, curr, interactionCategory)
				target.Habitation.FirstSleepHere = &nowMs
				updated = true
			}
		}
	}

	// --- Toilet / bladder ---
	if target.Habitation.FirstUseToiletHere == nil {
		if prev, hasPrev := w.prevMotives["bladder"]; hasPrev {
			curr := currMotives["bladder"]
			if prev < 0 && curr >= 0 && isToiletCategory(interactionCategory) {
				log.Printf("habitation-watcher: first_use_toilet_here on %s (bladder %d→%d, cat=%s)",
					w.currentLotHex, prev, curr, interactionCategory)
				target.Habitation.FirstUseToiletHere = &nowMs
				updated = true
			}
		}
	}

	// Update prev motives for next tick.
	for k, v := range currMotives {
		w.prevMotives[k] = v
	}

	if !updated {
		return false
	}

	// Recompute is_habitable.
	target.IsHabitable = target.Habitation.FirstMealEatenHere != nil &&
		target.Habitation.FirstSleepHere != nil &&
		target.Habitation.FirstUseToiletHere != nil

	if target.IsHabitable {
		log.Printf("habitation-watcher: lot %s is now HABITABLE (all three thresholds met)",
			w.currentLotHex)
	}

	// Write back. lots[len(lots)-1] is a copy; we must reassign.
	lots[len(lots)-1] = *target
	if err := WriteOwnedLots(lots); err != nil {
		log.Printf("habitation-watcher: write owned-lots.json: %v", err)
		return false
	}
	return true
}

// isFoodCategory returns true for object categories that count as food-class
// for the first_meal_eaten_here habitation threshold.
// Food class GUIDs per docs/blueprints/object-class-guids.md:
//   - Fridge (0x675C18AF): PerceptionProjector classifies as "food" via hint table
//   - Stove  (0xEB686709): classifies as "food" via hint table
//
// The sidecar uses PerceptionProjector's category string. Any object with category
// "food" counts — this is deliberately broader than a strict GUID list so that future
// catalog changes don't silently break the threshold.
func isFoodCategory(category string) bool {
	return category == "food"
}

// isBedCategory returns true for object categories that count as bed-class
// for the first_sleep_here habitation threshold.
// Bed class GUIDs per docs/blueprints/object-class-guids.md:
//   - Bed (0x0E82C943): PerceptionProjector classifies via hint table as "energy"
func isBedCategory(category string) bool {
	return category == "energy"
}

// isToiletCategory returns true for object categories that count as toilet-class
// for the first_use_toilet_here habitation threshold.
// Toilet class GUIDs per docs/blueprints/object-class-guids.md:
//   - Toilet (0x84E0774C): PerceptionProjector classifies as "bladder" via hint table
//
// NOTE: shower (0x85E02A40) maps to "hygiene" category, not "bladder" — excluded.
func isToiletCategory(category string) bool {
	return category == "bladder"
}

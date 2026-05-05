/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

// PerceptionAugmentor enriches the raw perception JSON emitted by the C# bot
// with sidecar-side fields that the bot cannot compute itself:
//
//   - body.home_lot     — owned-lots.json[0] projected as {name, location_hex, is_habitable}
//   - body.current_lot.is_home        — true when current lot == home lot
//   - body.current_lot.has_bulletin_board — MVP allowlist: community lot only
//   - body.current_lot.has_ballot_box     — MVP allowlist: community lot only
//
// mayor_status is now emitted directly by the C# bot in each perception tick
// (freesoexperiment-ea0). The augmentor caches the latest mayor_status from
// the incoming tick so that convention handlers can read it synchronously via
// LatestMayorStatus() without a bot-cmd round-trip.
//
// These are added as top-level keys on the perception JSON object before it is
// forwarded to the campfire. The augmentation is a best-effort in-place merge:
// if it fails for any reason, the original line is forwarded unchanged and the
// error is logged. No panic, no data loss.
//
// Design: docs/design/cross-lot-first-class.md §Q6
// Invariants: I0-1a (affordance-bound discovery), I0-7 (habitation)
//
// Lot-meta MVP allowlist:
// The community lot is identified by its packed location (uint32 hex). This
// location is set during the mayor bootstrap (item 14) and stored in the
// sidecar env as FREESO_COMMUNITY_LOT_LOCATION (hex string, e.g.
// "0x00F9015E"). Before item 12 ships an engine-side affordance prop, the
// allowlist approach is the only way to surface has_bulletin_board and
// has_ballot_box without a full engine patch.
//
// FSO_KEY_OBJECT:
// Each persona's chart.toml [env] block sets FSO_KEY_OBJECT to a catalog GUID
// (hex string). This is NOT used by the augmentor at runtime — it is an env
// var read by the dispatch-prompt builder (item 13) to inject the key-piece
// paragraph into the wake prompt. Documented here for traceability.

import (
	"encoding/json"
	"log"
	"os"
	"strconv"
	"strings"
	"sync"
)

// HomeLotProjection is the body.home_lot sub-tree emitted in perception.
type HomeLotProjection struct {
	Name        string `json:"name"`
	LocationHex string `json:"location_hex"`
	IsHabitable bool   `json:"is_habitable"`
}

// MayorStatus is the mayor_status sub-tree emitted in each perception tick by
// the C# bot (freesoexperiment-ea0). The augmentor caches the latest value
// so convention handlers can call LatestMayorStatus() without IPC.
type MayorStatus struct {
	IsMayor   bool `json:"is_mayor"`
	MayorNhood int  `json:"mayor_nhood"`
}

// currentLotAffordances is the has_bulletin_board / has_ballot_box / is_home
// enrichment appended to body.current_lot in perception.
type currentLotAffordances struct {
	IsHome           bool `json:"is_home"`
	HasBulletinBoard bool `json:"has_bulletin_board"`
	HasBallotBox     bool `json:"has_ballot_box"`
}

// PerceptionAugmentor holds configuration read once at sidecar startup and
// live state updated on every perception tick.
//
// AugmentPerception is called from the Bridge goroutine sequentially.
// LatestMayorStatus is called from convention handler goroutines concurrently.
// The mayorMu mutex guards mayorCache for cross-goroutine safety.
type PerceptionAugmentor struct {
	// communityLotLocation is the packed lot location of the community lot,
	// normalised to lowercase hex without "0x" prefix (e.g. "00f9015e").
	// Empty string means "no community lot known yet" — affordance flags are false.
	communityLotLocation string

	// mayorMu guards mayorCache.
	mayorMu    sync.RWMutex
	mayorCache MayorStatus
}

// NewPerceptionAugmentor constructs a PerceptionAugmentor. The community lot
// location is read from the FREESO_COMMUNITY_LOT_LOCATION env var (hex string,
// e.g. "0x00F9015E" or "00F9015E" — both accepted). If the env var is absent
// or malformed, the allowlist is empty and affordance flags will be false until
// the sidecar is restarted with the env var set.
func NewPerceptionAugmentor() *PerceptionAugmentor {
	raw := strings.TrimSpace(os.Getenv("FREESO_COMMUNITY_LOT_LOCATION"))
	normalized := normalizeLotHex(raw)
	if normalized != "" {
		log.Printf("perception-augmentor: community lot = 0x%s (affordances enabled)", normalized)
	} else {
		log.Printf("perception-augmentor: FREESO_COMMUNITY_LOT_LOCATION not set — affordance flags will be false")
	}
	return &PerceptionAugmentor{communityLotLocation: normalized}
}

// normalizeLotHex strips an optional "0x" prefix and lowercases a lot location
// string. Returns "" for empty or unparseable input.
func normalizeLotHex(s string) string {
	s = strings.TrimSpace(s)
	if s == "" {
		return ""
	}
	lower := strings.ToLower(s)
	if strings.HasPrefix(lower, "0x") {
		lower = lower[2:]
	}
	if lower == "" {
		return ""
	}
	// Validate: only hex digits allowed.
	for _, c := range lower {
		if !((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')) {
			return ""
		}
	}
	return lower
}

// isCommunityLot returns true when lotHex (the lot block's hex location, or its
// lot_id converted to hex) matches the allowlisted community lot.
func (a *PerceptionAugmentor) isCommunityLot(lotHex string) bool {
	if a.communityLotLocation == "" || lotHex == "" {
		return false
	}
	return normalizeLotHex(lotHex) == a.communityLotLocation
}

// LatestMayorStatus returns the most recently cached MayorStatus from the
// last perception tick emitted by the C# bot. Returns the zero value
// {IsMayor:false, MayorNhood:0} before the first tick arrives.
// Safe for concurrent callers (convention handler goroutines).
func (a *PerceptionAugmentor) LatestMayorStatus() MayorStatus {
	a.mayorMu.RLock()
	defer a.mayorMu.RUnlock()
	return a.mayorCache
}

// perceptionTopLevel is the minimal top-level decode of a perception tick
// needed to extract lot fields for augmentation. All other fields are
// preserved via RawMessage pass-through in AugmentPerception.
type perceptionTopLevel struct {
	Kind string          `json:"kind"`
	Lot  *perceptionLotX `json:"lot"`
	Body *perceptionBody `json:"body"`
}

// perceptionLotX matches the C# LotBlock shape (snake_case from PerceptionEmitter).
type perceptionLotX struct {
	Name      string `json:"name"`
	LotId     int64  `json:"lot_id"`
	OwnerIsMe bool   `json:"owner_is_me"`
}

// perceptionBody is the body sub-tree if it already exists (future-compat).
type perceptionBody struct{}

// AugmentPerception takes a raw perception NDJSON line, merges in the
// sidecar-computed fields, and returns the augmented line. If parsing fails or
// the kind is not "perception", the original line is returned unchanged.
//
// New fields added at the top level:
//
//	"home_lot":     {name, location_hex, is_habitable} | null
//	"mayor_status": {is_mayor, current_mayor_name, election_phase}
//
// Fields merged into the existing "lot" sub-object (creating it if absent):
//
//	"lot.is_home":           bool
//	"lot.has_bulletin_board": bool
//	"lot.has_ballot_box":     bool
//
// JSON roundtrip via map[string]any is intentional: the perception tick can
// evolve without requiring a full struct definition here. Only the fields we
// READ are typed; the rest pass through opaque.
func (a *PerceptionAugmentor) AugmentPerception(line []byte) []byte {
	if len(line) == 0 {
		return line
	}

	// Quick parse to check kind without a full decode.
	var quick struct {
		Kind string `json:"kind"`
	}
	if err := json.Unmarshal(line, &quick); err != nil {
		return line
	}
	if quick.Kind != "perception" {
		return line
	}

	// Full decode into a generic map so we can inject and re-encode without
	// losing any existing fields.
	var tick map[string]json.RawMessage
	if err := json.Unmarshal(line, &tick); err != nil {
		log.Printf("perception-augmentor: decode tick: %v (forwarding original)", err)
		return line
	}

	// --- mayor_status: cache from C# tick (freesoexperiment-ea0) ---
	// The C# bot now emits mayor_status in each perception tick directly from
	// VMTSOAvatarFlags.Mayor. We cache it for checkMayorAuthorization callers
	// and pass the field through to campfire unchanged.
	if rawMs, ok := tick["mayor_status"]; ok {
		var ms MayorStatus
		if err := json.Unmarshal(rawMs, &ms); err != nil {
			log.Printf("perception-augmentor: parse mayor_status from tick: %v", err)
		} else {
			a.mayorMu.Lock()
			a.mayorCache = ms
			a.mayorMu.Unlock()
		}
	}

	// --- home_lot ---
	homeLot := buildHomeLotProjection()
	homeLotJSON, err := json.Marshal(homeLot)
	if err != nil {
		log.Printf("perception-augmentor: marshal home_lot: %v (skipping field)", err)
	} else {
		tick["home_lot"] = json.RawMessage(homeLotJSON)
	}

	// --- lot.is_home, lot.has_bulletin_board, lot.has_ballot_box ---
	// Decode the existing lot sub-object, inject the new flags, and re-encode.
	// If lot is absent (shouldn't happen with a real bot), create a minimal object.
	augmentedLot := augmentLotBlock(tick["lot"], homeLot, a)
	if augmentedLot != nil {
		tick["lot"] = augmentedLot
	}

	// Re-encode the full tick.
	out, err := json.Marshal(tick)
	if err != nil {
		log.Printf("perception-augmentor: re-encode tick: %v (forwarding original)", err)
		return line
	}
	return out
}

// buildHomeLotProjection reads owned-lots.json and returns the home_lot
// projection (first owned lot). Returns nil when no lots are owned.
func buildHomeLotProjection() *HomeLotProjection {
	lots, err := ReadOwnedLots()
	if err != nil {
		log.Printf("perception-augmentor: read owned-lots.json: %v (home_lot=null)", err)
		return nil
	}
	if len(lots) == 0 {
		return nil // persona owns no lot yet
	}
	first := lots[0]
	return &HomeLotProjection{
		Name:        first.Name,
		LocationHex: first.LocationHex,
		IsHabitable: first.IsHabitable,
	}
}

// augmentLotBlock decodes the existing lot JSON (if any), injects the sidecar-
// side affordance flags, and returns the augmented raw JSON. Returns nil on
// errors (caller should leave lot unchanged).
func augmentLotBlock(lotRaw json.RawMessage, home *HomeLotProjection, a *PerceptionAugmentor) json.RawMessage {
	// Decode into a mutable map to preserve all existing fields.
	var lotMap map[string]json.RawMessage
	if len(lotRaw) > 0 {
		if err := json.Unmarshal(lotRaw, &lotMap); err != nil {
			log.Printf("perception-augmentor: decode lot block: %v (leaving lot unchanged)", err)
			return nil
		}
	} else {
		lotMap = make(map[string]json.RawMessage)
	}

	// Extract the lot_id to check the allowlist. The C# emits it as a number.
	var lotID int64
	if raw, ok := lotMap["lot_id"]; ok {
		if err := json.Unmarshal(raw, &lotID); err != nil {
			log.Printf("perception-augmentor: decode lot_id: %v", err)
		}
	}

	// is_home: true when the current lot's location matches the home lot.
	isHome := false
	if home != nil && home.LocationHex != "" {
		// We compare by lot_id because LotBlock.LotId (int64) is the DB lot_id,
		// not the packed location. For MVP, match lot_id to the lot_id stored in
		// OwnedLotEntry once that field is available. Until then, is_home is
		// derived by comparing location hex when we have it.
		//
		// NOTE: OwnedLotEntry.LocationHex is the packed lot location (gotcha 11).
		// The LotBlock does NOT currently carry a location_hex — it carries lot_id
		// (DB row id). These are different fields. We cannot compute is_home from
		// lot_id vs location_hex without a mapping.
		//
		// Resolution: the C# LotBlock carries owner_is_me (bool) which is true when
		// the current avatar is in the Roommates list. For a soul that OWNS their
		// lot, owner_is_me == true AND home != nil is a sufficient is_home proxy at
		// MVP. This is not exact (a soul who is a roommate on another lot would also
		// get is_home=true), but it is the best we can do without a location_hex
		// field on LotBlock. Tracked for cleanup when LotBlock gains location_hex.
		var ownerIsMe bool
		if raw, ok := lotMap["owner_is_me"]; ok {
			if err := json.Unmarshal(raw, &ownerIsMe); err != nil {
				log.Printf("perception-augmentor: decode owner_is_me: %v", err)
			}
		}
		isHome = ownerIsMe
	}

	// has_bulletin_board / has_ballot_box: MVP allowlist on community lot.
	// We use lot_id == 0 as "unknown". The community lot will have a lot_id
	// assigned at bootstrap time. For the MVP, we check against the community
	// lot location allowlist via the lot_id→location mapping stored in the
	// FREESO_COMMUNITY_LOT_ID env var (decimal lot_id, distinct from location).
	//
	// Fallback: FREESO_COMMUNITY_LOT_LOCATION stores the packed location (hex).
	// Since LotBlock only has lot_id, we also support FREESO_COMMUNITY_LOT_ID
	// (decimal lot_id) for the affordance check.
	isCommunity := a.isCommunityLotByID(lotID)
	hasBulletinBoard := isCommunity
	hasBallotBox := isCommunity

	// Inject new fields.
	isHomeJSON, _ := json.Marshal(isHome)
	hasBBJSON, _ := json.Marshal(hasBulletinBoard)
	hasBallotJSON, _ := json.Marshal(hasBallotBox)
	lotMap["is_home"] = json.RawMessage(isHomeJSON)
	lotMap["has_bulletin_board"] = json.RawMessage(hasBBJSON)
	lotMap["has_ballot_box"] = json.RawMessage(hasBallotJSON)

	out, err := json.Marshal(lotMap)
	if err != nil {
		log.Printf("perception-augmentor: re-encode lot block: %v (leaving lot unchanged)", err)
		return nil
	}
	return json.RawMessage(out)
}

// isCommunityLotByID returns true when lotID matches the community lot's DB
// lot_id. The lot_id is read from the FREESO_COMMUNITY_LOT_ID env var (decimal
// integer). Returns false when the env var is absent or 0.
func (a *PerceptionAugmentor) isCommunityLotByID(lotID int64) bool {
	if lotID == 0 {
		return false
	}
	raw := strings.TrimSpace(os.Getenv("FREESO_COMMUNITY_LOT_ID"))
	if raw == "" {
		return false
	}
	communityID, err := strconv.ParseInt(raw, 10, 64)
	if err != nil {
		return false
	}
	return lotID == communityID && communityID != 0
}

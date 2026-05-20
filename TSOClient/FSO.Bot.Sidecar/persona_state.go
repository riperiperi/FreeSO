/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"encoding/json"
	"errors"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"
)

// PersonaStateDir returns the per-persona config directory:
//
//	~/.config/freeso-souls/<persona>/
//
// <persona> is derived from FSO_USER (lowercased, whitespace-trimmed). If
// FSO_USER is not set or empty, the function returns an error — the caller
// decides whether this is fatal or a no-op.
//
// Files written under this directory (all optional, tolerated-missing):
//
//	body-cf.id      — hex campfire ID, persisted across sidecar restarts (this item)
//	body-cf-beacon  — portable beacon string ("beacon:<base64>"), written at first
//	                  sidecar boot / chargen completion (automataisland-f3c).
//	                  Legion jail provisioning reads this for auto_join (item 781).
//	next-lot        — packed lot location (hex uint32) for next bot launch
//	owned-lots.json — JSON array of lot locations this persona owns
func PersonaStateDir() (string, error) {
	persona := strings.TrimSpace(os.Getenv("FSO_USER"))
	if persona == "" {
		return "", errors.New("FSO_USER not set — cannot derive persona state dir")
	}
	// Sanitise: keep only the lowercased value; reject path traversal characters.
	if strings.ContainsAny(persona, "/\\.") {
		return "", fmt.Errorf("FSO_USER %q contains path separators — refusing to build state dir", persona)
	}
	configHome, err := os.UserConfigDir()
	if err != nil {
		return "", fmt.Errorf("os.UserConfigDir: %w", err)
	}
	return filepath.Join(configHome, "freeso-souls", strings.ToLower(persona)), nil
}

// ReadBodyCfID reads the persisted campfire ID for this persona.
// Returns ("", nil) when the file does not exist (first boot).
// Returns ("", error) on I/O or format errors.
func ReadBodyCfID() (string, error) {
	dir, err := PersonaStateDir()
	if err != nil {
		return "", err
	}
	path := filepath.Join(dir, "body-cf.id")
	data, err := os.ReadFile(path)
	if err != nil {
		if os.IsNotExist(err) {
			return "", nil // first boot — no prior ID
		}
		return "", fmt.Errorf("read body-cf.id: %w", err)
	}
	id := strings.TrimSpace(string(data))
	if id == "" {
		return "", fmt.Errorf("body-cf.id is present but empty: %s", path)
	}
	return id, nil
}

// WriteBodyCfID atomically writes campfireID to body-cf.id, creating the
// persona state directory if necessary.
func WriteBodyCfID(campfireID string) error {
	if campfireID == "" {
		return errors.New("WriteBodyCfID: campfireID must not be empty")
	}
	dir, err := PersonaStateDir()
	if err != nil {
		return err
	}
	if err := os.MkdirAll(dir, 0o700); err != nil {
		return fmt.Errorf("mkdir persona state dir: %w", err)
	}
	path := filepath.Join(dir, "body-cf.id")
	// Write to temp then rename for atomic replace.
	tmp := path + ".tmp"
	if err := os.WriteFile(tmp, []byte(campfireID+"\n"), 0o600); err != nil {
		return fmt.Errorf("write body-cf.id tmp: %w", err)
	}
	if err := os.Rename(tmp, path); err != nil {
		return fmt.Errorf("rename body-cf.id: %w", err)
	}
	return nil
}

// ReadBodyCFBeacon reads the persisted body campfire beacon string for this persona.
// The beacon string has the canonical form "beacon:<base64>" and is suitable for
// passing to `cf join` or writing to an auto_join config block.
//
// Returns ("", nil) when the file does not exist (first boot — beacon not yet written).
// Returns ("", error) on I/O or format errors.
func ReadBodyCFBeacon() (string, error) {
	dir, err := PersonaStateDir()
	if err != nil {
		return "", err
	}
	path := filepath.Join(dir, "body-cf-beacon")
	data, err := os.ReadFile(path)
	if err != nil {
		if os.IsNotExist(err) {
			return "", nil // first boot — beacon not yet written
		}
		return "", fmt.Errorf("read body-cf-beacon: %w", err)
	}
	beaconStr := strings.TrimSpace(string(data))
	if beaconStr == "" {
		return "", fmt.Errorf("body-cf-beacon is present but empty: %s", path)
	}
	if !strings.HasPrefix(beaconStr, "beacon:") {
		return "", fmt.Errorf("body-cf-beacon has unexpected format (want 'beacon:...'): %s", path)
	}
	return beaconStr, nil
}

// WriteBodyCFBeacon atomically writes the canonical beacon string to body-cf-beacon,
// creating the persona state directory if necessary.
//
// beaconStr must be a non-empty string starting with "beacon:" — the canonical form
// produced by encoding a campfire beacon struct as CBOR + base64. Legion jail
// provisioning reads this file for auto_join (automataisland-781).
//
// Idempotency: the caller is responsible for the idempotency guard. WriteBodyCFBeacon
// always overwrites any existing file. Callers that want first-write-wins semantics
// should call ReadBodyCFBeacon first and skip the write if a non-empty value is returned.
func WriteBodyCFBeacon(beaconStr string) error {
	if beaconStr == "" {
		return errors.New("WriteBodyCFBeacon: beacon must not be empty")
	}
	if !strings.HasPrefix(beaconStr, "beacon:") {
		truncated := beaconStr
		if len(truncated) > 16 {
			truncated = truncated[:16]
		}
		return fmt.Errorf("WriteBodyCFBeacon: beacon must start with 'beacon:', got %q", truncated)
	}
	dir, err := PersonaStateDir()
	if err != nil {
		return err
	}
	if err := os.MkdirAll(dir, 0o700); err != nil {
		return fmt.Errorf("mkdir persona state dir: %w", err)
	}
	path := filepath.Join(dir, "body-cf-beacon")
	tmp := path + ".tmp"
	if err := os.WriteFile(tmp, []byte(beaconStr+"\n"), 0o600); err != nil {
		return fmt.Errorf("write body-cf-beacon tmp: %w", err)
	}
	if err := os.Rename(tmp, path); err != nil {
		os.Remove(tmp) //nolint:errcheck
		return fmt.Errorf("rename body-cf-beacon: %w", err)
	}
	return nil
}

// ReadNextLot reads the pending lot-location override for the next bot launch.
// The file contains a decimal or hex uint32 string (the packed lot location).
// Returns ("", nil) when the file does not exist (normal case — no pending
// cross-lot transition). Returns ("", error) on I/O errors.
func ReadNextLot() (string, error) {
	dir, err := PersonaStateDir()
	if err != nil {
		return "", err
	}
	path := filepath.Join(dir, "next-lot")
	data, err := os.ReadFile(path)
	if err != nil {
		if os.IsNotExist(err) {
			return "", nil
		}
		return "", fmt.Errorf("read next-lot: %w", err)
	}
	loc := strings.TrimSpace(string(data))
	return loc, nil
}

// WriteNextLot atomically writes lotLocation to the next-lot file, creating
// the persona state directory if necessary. The supervisor loop reads this
// before relaunching the bot after a bot exit.
func WriteNextLot(lotLocation string) error {
	if lotLocation == "" {
		return errors.New("WriteNextLot: lotLocation must not be empty")
	}
	dir, err := PersonaStateDir()
	if err != nil {
		return err
	}
	if err := os.MkdirAll(dir, 0o700); err != nil {
		return fmt.Errorf("mkdir persona state dir: %w", err)
	}
	path := filepath.Join(dir, "next-lot")
	tmp := path + ".tmp"
	if err := os.WriteFile(tmp, []byte(lotLocation+"\n"), 0o600); err != nil {
		return fmt.Errorf("write next-lot tmp: %w", err)
	}
	if err := os.Rename(tmp, path); err != nil {
		return fmt.Errorf("rename next-lot: %w", err)
	}
	return nil
}

// ClearNextLot removes the next-lot file after it has been consumed by the
// supervisor loop. No-op if the file does not exist.
func ClearNextLot() error {
	dir, err := PersonaStateDir()
	if err != nil {
		return err
	}
	path := filepath.Join(dir, "next-lot")
	if err := os.Remove(path); err != nil && !os.IsNotExist(err) {
		return fmt.Errorf("clear next-lot: %w", err)
	}
	return nil
}

// OwnedLotHabitation tracks I0-7: a parcel is "yours" only after the body has
// eaten, slept, and used a toilet on it. Populated by the habitation watcher
// (item 8 / freesoexperiment-xxx) from perception events.
type OwnedLotHabitation struct {
	FirstMealEatenHere *int64 `json:"first_meal_eaten_here"` // unix ms, nil = not yet
	FirstSleepHere     *int64 `json:"first_sleep_here"`
	FirstUseToiletHere *int64 `json:"first_use_toilet_here"`
}

// OwnedLotEntry is one record in owned-lots.json. Location is a decimal uint32
// canonical string (same representation as next-lot).
type OwnedLotEntry struct {
	Name        string             `json:"name"`
	LocationHex string             `json:"location_hex"`
	PurchasedAt int64              `json:"purchased_at"` // unix ms
	Habitation  OwnedLotHabitation `json:"habitation"`
	IsHabitable bool               `json:"is_habitable"`
}

// ReadOwnedLots reads the owned-lots.json file for this persona. Returns an
// empty slice (not nil) when the file does not exist (first purchase). Returns
// an error on I/O or parse failures.
func ReadOwnedLots() ([]OwnedLotEntry, error) {
	dir, err := PersonaStateDir()
	if err != nil {
		return nil, err
	}
	path := filepath.Join(dir, "owned-lots.json")
	data, err := os.ReadFile(path)
	if err != nil {
		if os.IsNotExist(err) {
			return []OwnedLotEntry{}, nil // first purchase — no prior file
		}
		return nil, fmt.Errorf("read owned-lots.json: %w", err)
	}
	var entries []OwnedLotEntry
	if err := json.Unmarshal(data, &entries); err != nil {
		return nil, fmt.Errorf("parse owned-lots.json: %w", err)
	}
	return entries, nil
}

// WriteOwnedLots atomically writes entries to owned-lots.json. Creates the
// persona state directory if necessary.
func WriteOwnedLots(entries []OwnedLotEntry) error {
	dir, err := PersonaStateDir()
	if err != nil {
		return err
	}
	if err := os.MkdirAll(dir, 0o700); err != nil {
		return fmt.Errorf("mkdir persona state dir: %w", err)
	}
	path := filepath.Join(dir, "owned-lots.json")
	data, err := json.MarshalIndent(entries, "", "  ")
	if err != nil {
		return fmt.Errorf("marshal owned-lots: %w", err)
	}
	tmp := path + ".tmp"
	if err := os.WriteFile(tmp, data, 0o600); err != nil {
		return fmt.Errorf("write owned-lots.json tmp: %w", err)
	}
	if err := os.Rename(tmp, path); err != nil {
		return fmt.Errorf("rename owned-lots.json: %w", err)
	}
	return nil
}

// ReadHomeLotFromOwnedLots reads the first entry in owned-lots.json and returns
// its LocationHex as a string suitable for FSO_HOME_LOT_LOCATION. Returns ("",
// nil) when the file does not exist or is empty — persona owns no lot. Returns
// an error on I/O or parse failures.
//
// "First entry" is the lowest-index entry in the JSON array, which is the
// chronologically first purchase (AppendOwnedLot appends new purchases). The
// C# bot parses both "0x..." hex and decimal forms, so passing LocationHex
// directly is correct.
func ReadHomeLotFromOwnedLots() (string, error) {
	lots, err := ReadOwnedLots()
	if err != nil {
		return "", err
	}
	if len(lots) == 0 {
		return "", nil // no owned lot — ok:false path in go-home
	}
	loc := strings.TrimSpace(lots[0].LocationHex)
	if loc == "" {
		return "", fmt.Errorf("owned-lots.json[0].location_hex is empty — corrupt entry")
	}
	return loc, nil
}

// SidecarFailedMarkerPath returns the path to the sidecar-failed marker file
// for this persona. The file is written by the supervisor loop when all bot
// relaunch attempts are exhausted so operators and orchestrators can detect
// a zombie sidecar post-mortem.
//
// Returns ("", error) if the persona state dir cannot be derived.
func SidecarFailedMarkerPath() (string, error) {
	dir, err := PersonaStateDir()
	if err != nil {
		return "", err
	}
	return filepath.Join(dir, "sidecar-failed"), nil
}

// WriteSidecarFailedMarker atomically writes a failure marker to the persona
// state directory. Content is a human-readable timestamp + message line so
// operators can see when the failure occurred and why.
//
// Called by the supervisor loop immediately before os.Exit(1) so the marker
// is always present when the process exits non-zero.
func WriteSidecarFailedMarker(reason string) error {
	path, err := SidecarFailedMarkerPath()
	if err != nil {
		return fmt.Errorf("WriteSidecarFailedMarker: derive path: %w", err)
	}
	if err := os.MkdirAll(filepath.Dir(path), 0o700); err != nil {
		return fmt.Errorf("WriteSidecarFailedMarker: mkdir: %w", err)
	}
	content := fmt.Sprintf("%s %s\n", time.Now().UTC().Format(time.RFC3339), reason)
	tmp := path + ".tmp"
	if err := os.WriteFile(tmp, []byte(content), 0o600); err != nil {
		return fmt.Errorf("WriteSidecarFailedMarker: write tmp: %w", err)
	}
	if err := os.Rename(tmp, path); err != nil {
		return fmt.Errorf("WriteSidecarFailedMarker: rename: %w", err)
	}
	return nil
}

// AppendOwnedLot appends a newly purchased lot to owned-lots.json. The new
// entry's habitation block is zeroed (nil timestamps, is_habitable=false) per
// I0-7: the body must eat/sleep/use-toilet to become habitable.
//
// If an entry with the same LocationHex already exists it is replaced (handles
// the allow_move=true re-purchase case). This is an atomic read-modify-write:
// the file is read, mutated, and written back via WriteOwnedLots.
func AppendOwnedLot(entry OwnedLotEntry) error {
	existing, err := ReadOwnedLots()
	if err != nil {
		return err
	}
	// Replace any existing entry for the same location.
	replaced := false
	for i, e := range existing {
		if e.LocationHex == entry.LocationHex {
			existing[i] = entry
			replaced = true
			break
		}
	}
	if !replaced {
		existing = append(existing, entry)
	}
	return WriteOwnedLots(existing)
}

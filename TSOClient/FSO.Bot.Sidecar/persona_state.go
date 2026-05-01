/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"errors"
	"fmt"
	"os"
	"path/filepath"
	"strings"
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

/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"
)

// JournalWriter writes journal entry files for an embodied persona, fsyncing
// each entry before returning so a kill -9 does not lose the last write.
//
// Paths:
//   - When RUN_ID is set:
//       ~/.local/share/freeso-experiment/runs/<run-id>/journal/<persona>/
//   - When RUN_ID is unset (smoke-test fallback):
//       /tmp/embody-<persona>/journal/
//
// The sync-on-write requirement exists because the soak service may be
// interrupted by SIGKILL (systemd OOM kill, kill -9 during debugging) and
// there is no shutdown hook that can flush unfsynced data in that case.
// Calling fsync per entry is the only safe guarantee.
//
// Persona is derived from FSO_USER (lowercased, trimmed), same as PersonaStateDir.
// If FSO_USER is unset, JournalDir returns an error and callers may skip
// journal writes (non-fatal for smoke tests).
type JournalWriter struct {
	dir string // resolved on construction; empty means disabled
}

// NewJournalWriter constructs a JournalWriter, resolving the target directory
// from the environment. Returns a writer with an empty dir (no-op writes) if
// the required env is absent — callers check IsEnabled() if they care.
func NewJournalWriter() *JournalWriter {
	dir, err := JournalDir()
	if err != nil {
		// Non-fatal: FSO_USER unset (e.g. --no-bot testing), or XDG dirs unavailable.
		return &JournalWriter{}
	}
	return &JournalWriter{dir: dir}
}

// IsEnabled reports whether the writer has a valid target directory.
// When false, Write is a no-op.
func (jw *JournalWriter) IsEnabled() bool {
	return jw.dir != ""
}

// Dir returns the resolved journal directory, or "" if disabled.
func (jw *JournalWriter) Dir() string {
	return jw.dir
}

// Write writes content to a journal file named by slug and the current UTC
// timestamp. The filename format is <ISO-UTC>-<slug>.md, matching the
// convention used by embodied agents. The file is fsynced before Write
// returns — kill -9 after Write returns guarantees the entry is on disk.
//
// slug should be a short identifier (e.g. "arrival", "perception-summary").
// Unsafe characters in slug are replaced with "-".
//
// Returns an error if the write or fsync fails. A disabled writer returns nil
// without writing anything.
func (jw *JournalWriter) Write(slug, content string) error {
	if !jw.IsEnabled() {
		return nil
	}
	if err := os.MkdirAll(jw.dir, 0o755); err != nil {
		return fmt.Errorf("journal mkdir %s: %w", jw.dir, err)
	}

	// Sanitise slug: replace path separators and spaces with "-".
	safeslug := strings.NewReplacer(
		"/", "-",
		"\\", "-",
		" ", "-",
		"\n", "-",
		"\r", "",
	).Replace(slug)
	if safeslug == "" {
		safeslug = "entry"
	}

	ts := time.Now().UTC().Format("20060102T150405")
	filename := ts + "-" + safeslug + ".md"
	fpath := filepath.Join(jw.dir, filename)

	f, err := os.OpenFile(fpath, os.O_WRONLY|os.O_CREATE|os.O_TRUNC, 0o644)
	if err != nil {
		return fmt.Errorf("journal open %s: %w", fpath, err)
	}

	if _, werr := fmt.Fprint(f, content); werr != nil {
		f.Close() //nolint:errcheck
		return fmt.Errorf("journal write %s: %w", fpath, werr)
	}

	// Fsync before close — guarantees entry is on disk even on kill -9.
	if serr := f.Sync(); serr != nil {
		f.Close() //nolint:errcheck
		return fmt.Errorf("journal fsync %s: %w", fpath, serr)
	}

	return f.Close()
}

// JournalDir returns the canonical journal directory for the current persona
// and RUN_ID environment.
//
// Priority:
//  1. RUN_ID set → ~/.local/share/freeso-experiment/runs/<run-id>/journal/<persona>/
//  2. RUN_ID unset → /tmp/embody-<persona>/journal/
//
// Returns an error if FSO_USER is unset or invalid.
func JournalDir() (string, error) {
	persona := strings.TrimSpace(os.Getenv("FSO_USER"))
	if persona == "" {
		return "", fmt.Errorf("FSO_USER not set — cannot derive journal dir")
	}
	if strings.ContainsAny(persona, "/\\.") {
		return "", fmt.Errorf("FSO_USER %q contains path separators", persona)
	}
	persona = strings.ToLower(persona)

	runID := strings.TrimSpace(os.Getenv("RUN_ID"))
	if runID == "" {
		// Smoke-test / no-RUN_ID fallback: /tmp/embody-<persona>/journal/
		return filepath.Join("/tmp", "embody-"+persona, "journal"), nil
	}
	// Sanitise RUN_ID: reject path separators.
	if strings.ContainsAny(runID, "/\\.") {
		return "", fmt.Errorf("RUN_ID %q contains path separators", runID)
	}

	// XDG_DATA_HOME or ~/.local/share
	dataHome := os.Getenv("XDG_DATA_HOME")
	if dataHome == "" {
		home, err := os.UserHomeDir()
		if err != nil {
			return "", fmt.Errorf("os.UserHomeDir: %w", err)
		}
		dataHome = filepath.Join(home, ".local", "share")
	}

	return filepath.Join(dataHome, "freeso-experiment", "runs", runID, "journal", persona), nil
}

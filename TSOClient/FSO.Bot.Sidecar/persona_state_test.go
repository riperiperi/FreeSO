/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

// withFSO_USER sets FSO_USER for the duration of the test and restores the
// prior value on cleanup.
func withFSO_USER(t *testing.T, val string) {
	t.Helper()
	prior, hasPrior := os.LookupEnv("FSO_USER")
	if val == "" {
		os.Unsetenv("FSO_USER")
	} else {
		os.Setenv("FSO_USER", val)
	}
	t.Cleanup(func() {
		if hasPrior {
			os.Setenv("FSO_USER", prior)
		} else {
			os.Unsetenv("FSO_USER")
		}
	})
}

// withConfigHome overrides XDG_CONFIG_HOME / HOME so os.UserConfigDir()
// returns a temp directory for test isolation.
func withConfigHome(t *testing.T, dir string) {
	t.Helper()
	prior, hasPrior := os.LookupEnv("XDG_CONFIG_HOME")
	os.Setenv("XDG_CONFIG_HOME", dir)
	t.Cleanup(func() {
		if hasPrior {
			os.Setenv("XDG_CONFIG_HOME", prior)
		} else {
			os.Unsetenv("XDG_CONFIG_HOME")
		}
	})
}

// TestPersonaStateDirDerivesFromFSO_USER asserts PersonaStateDir builds
// the correct path from FSO_USER.
func TestPersonaStateDirDerivesFromFSO_USER(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "Botrous")

	dir, err := PersonaStateDir()
	if err != nil {
		t.Fatalf("PersonaStateDir: %v", err)
	}
	want := filepath.Join(tmp, "freeso-souls", "botrous")
	if dir != want {
		t.Fatalf("expected %q, got %q", want, dir)
	}
}

// TestPersonaStateDirErrorsWhenFSO_USER_Unset asserts that PersonaStateDir
// returns an error when FSO_USER is not set or empty.
func TestPersonaStateDirErrorsWhenFSO_USER_Unset(t *testing.T) {
	for _, tc := range []struct {
		name string
		val  string
	}{
		{"unset", ""},
		{"empty string", "  "},
	} {
		t.Run(tc.name, func(t *testing.T) {
			withFSO_USER(t, tc.val)
			_, err := PersonaStateDir()
			if err == nil {
				t.Fatal("expected error for missing FSO_USER, got nil")
			}
		})
	}
}

// TestPersonaStateDirRejectsPathTraversal asserts that FSO_USER values
// containing path separators are refused to prevent directory escape.
func TestPersonaStateDirRejectsPathTraversal(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	for _, bad := range []string{"../etc", "foo/bar", "foo.bar", `foo\bar`} {
		t.Run(bad, func(t *testing.T) {
			withFSO_USER(t, bad)
			_, err := PersonaStateDir()
			if err == nil {
				t.Fatalf("expected error for %q, got nil", bad)
			}
		})
	}
}

// TestReadBodyCfIDMissing asserts ReadBodyCfID returns ("", nil) when the
// file does not exist — this is the expected first-boot state.
func TestReadBodyCfIDMissing(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "testpersona")

	id, err := ReadBodyCfID()
	if err != nil {
		t.Fatalf("ReadBodyCfID on missing file: want nil error, got %v", err)
	}
	if id != "" {
		t.Fatalf("ReadBodyCfID on missing file: want empty string, got %q", id)
	}
}

// TestWriteReadBodyCfIDRoundTrip asserts that WriteBodyCfID followed by
// ReadBodyCfID returns the same campfire ID — the fundamental round-trip.
func TestWriteReadBodyCfIDRoundTrip(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "botrous")

	const wantID = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"

	if err := WriteBodyCfID(wantID); err != nil {
		t.Fatalf("WriteBodyCfID: %v", err)
	}

	got, err := ReadBodyCfID()
	if err != nil {
		t.Fatalf("ReadBodyCfID: %v", err)
	}
	if got != wantID {
		t.Fatalf("round-trip mismatch: want %q, got %q", wantID, got)
	}
}

// TestWriteBodyCfIDCreatesDir asserts WriteBodyCfID creates the persona
// state directory if it does not exist (mkdir-p semantics).
func TestWriteBodyCfIDCreatesDir(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "sage")

	const id = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeef"

	// Pre-condition: directory does not exist.
	dir := filepath.Join(tmp, "freeso-souls", "sage")
	if _, err := os.Stat(dir); !os.IsNotExist(err) {
		t.Fatalf("expected dir to not exist before write, got: %v", err)
	}

	if err := WriteBodyCfID(id); err != nil {
		t.Fatalf("WriteBodyCfID: %v", err)
	}

	// Post-condition: directory and file exist.
	if _, err := os.Stat(dir); err != nil {
		t.Fatalf("directory not created: %v", err)
	}
	fpath := filepath.Join(dir, "body-cf.id")
	if _, err := os.Stat(fpath); err != nil {
		t.Fatalf("body-cf.id not created: %v", err)
	}
}

// TestWriteBodyCfIDOverwrites asserts that a second write replaces the
// previous ID, so restarts with a new campfire ID update the persisted value.
func TestWriteBodyCfIDOverwrites(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "marlo")

	const first = "1111111111111111111111111111111111111111111111111111111111111111"
	const second = "2222222222222222222222222222222222222222222222222222222222222222"

	if err := WriteBodyCfID(first); err != nil {
		t.Fatalf("first write: %v", err)
	}
	if err := WriteBodyCfID(second); err != nil {
		t.Fatalf("second write: %v", err)
	}

	got, err := ReadBodyCfID()
	if err != nil {
		t.Fatalf("read: %v", err)
	}
	if got != second {
		t.Fatalf("overwrite: want %q, got %q", second, got)
	}
}

// TestWriteBodyCfIDRejectsEmpty asserts WriteBodyCfID returns an error for
// an empty campfire ID rather than writing a useless file.
func TestWriteBodyCfIDRejectsEmpty(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "jin")

	if err := WriteBodyCfID(""); err == nil {
		t.Fatal("expected error for empty campfire ID, got nil")
	}
}

// TestReadBodyCfIDStripsWhitespace asserts ReadBodyCfID tolerates a trailing
// newline (common when the file was written by other tools or the atomic-write
// path appends \n).
func TestReadBodyCfIDStripsWhitespace(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "ellis")

	dir := filepath.Join(tmp, "freeso-souls", "ellis")
	if err := os.MkdirAll(dir, 0o700); err != nil {
		t.Fatalf("mkdir: %v", err)
	}
	const id = "cafecafecafecafecafecafecafecafecafecafecafecafecafecafecafecafe"
	// Write with trailing newline + spaces to simulate external tooling.
	if err := os.WriteFile(filepath.Join(dir, "body-cf.id"), []byte("  "+id+"  \n"), 0o600); err != nil {
		t.Fatalf("write: %v", err)
	}

	got, err := ReadBodyCfID()
	if err != nil {
		t.Fatalf("ReadBodyCfID: %v", err)
	}
	if got != id {
		t.Fatalf("whitespace not stripped: want %q, got %q", id, got)
	}
}

// TestReadBodyCfIDEmptyFile asserts ReadBodyCfID returns an error when the
// file exists but is empty or only whitespace — a sign of a corrupt state.
func TestReadBodyCfIDEmptyFile(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "jin")

	dir := filepath.Join(tmp, "freeso-souls", "jin")
	if err := os.MkdirAll(dir, 0o700); err != nil {
		t.Fatalf("mkdir: %v", err)
	}
	if err := os.WriteFile(filepath.Join(dir, "body-cf.id"), []byte("  \n"), 0o600); err != nil {
		t.Fatalf("write: %v", err)
	}

	_, err := ReadBodyCfID()
	if err == nil {
		t.Fatal("expected error for empty body-cf.id file, got nil")
	}
	if !strings.Contains(err.Error(), "empty") {
		t.Fatalf("expected 'empty' in error, got: %v", err)
	}
}

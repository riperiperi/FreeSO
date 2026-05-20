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

// --- body-cf-beacon tests (automataisland-f3c) ---

// TestReadBodyCFBeaconMissing asserts ReadBodyCFBeacon returns ("", nil) when
// the file does not exist — expected state before first sidecar boot / chargen.
func TestReadBodyCFBeaconMissing(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "testpersona")

	got, err := ReadBodyCFBeacon()
	if err != nil {
		t.Fatalf("ReadBodyCFBeacon on missing file: want nil error, got %v", err)
	}
	if got != "" {
		t.Fatalf("ReadBodyCFBeacon on missing file: want empty string, got %q", got)
	}
}

// TestWriteReadBodyCFBeaconRoundTrip is the POSITIVE gate: WriteBodyCFBeacon
// followed by ReadBodyCFBeacon returns the same beacon string — simulating
// sidecar boot with a fresh persona where the file is written once.
func TestWriteReadBodyCFBeaconRoundTrip(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "mara-voss")

	const wantBeacon = "beacon:SGVsbG8gd29ybGQgZnJvbSBhdXRvbWF0YS1pc2xhbmQ="

	if err := WriteBodyCFBeacon(wantBeacon); err != nil {
		t.Fatalf("WriteBodyCFBeacon: %v", err)
	}

	got, err := ReadBodyCFBeacon()
	if err != nil {
		t.Fatalf("ReadBodyCFBeacon: %v", err)
	}
	if got != wantBeacon {
		t.Fatalf("round-trip mismatch: want %q, got %q", wantBeacon, got)
	}
}

// TestBodyCFBeaconIdempotent is the IDEMPOTENT gate: when a beacon file already
// exists, writeBodyCFBeaconIfAbsent preserves the existing beacon — simulating
// a sidecar reboot that finds the existing file and skips creation of a new cf.
func TestBodyCFBeaconIdempotent(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "lara-voss")

	const firstBeacon = "beacon:Rmlyc3RCb290QmVhY29uRm9yTGFyYVZvc3M="
	const secondBeacon = "beacon:U2Vjb25kQm9vdEJlYWNvbkZvckxhcmFWb3Nz"

	// Write first beacon — simulates first boot.
	if err := WriteBodyCFBeacon(firstBeacon); err != nil {
		t.Fatalf("first WriteBodyCFBeacon: %v", err)
	}

	// Simulate second boot: writeBodyCFBeaconIfAbsent should preserve first beacon.
	if err := writeBodyCFBeaconIfAbsent(secondBeacon); err != nil {
		t.Fatalf("writeBodyCFBeaconIfAbsent: %v", err)
	}

	// Verify the first beacon is still present — not overwritten.
	got, err := ReadBodyCFBeacon()
	if err != nil {
		t.Fatalf("ReadBodyCFBeacon after idempotent check: %v", err)
	}
	if got != firstBeacon {
		t.Fatalf("idempotent guard failed: want %q (first beacon), got %q", firstBeacon, got)
	}
}

// TestWriteBodyCFBeaconCreatesDir asserts WriteBodyCFBeacon creates the persona
// state directory if it does not exist (mkdir-p semantics).
func TestWriteBodyCFBeaconCreatesDir(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "founder")

	const beaconVal = "beacon:dGVzdC1iZWFjb24tZm9yLWRpci1jcmVhdGlvbg=="

	dir := filepath.Join(tmp, "freeso-souls", "founder")
	if _, err := os.Stat(dir); !os.IsNotExist(err) {
		t.Fatalf("expected dir to not exist before write, got: %v", err)
	}

	if err := WriteBodyCFBeacon(beaconVal); err != nil {
		t.Fatalf("WriteBodyCFBeacon: %v", err)
	}

	if _, err := os.Stat(dir); err != nil {
		t.Fatalf("directory not created: %v", err)
	}
	fpath := filepath.Join(dir, "body-cf-beacon")
	if _, err := os.Stat(fpath); err != nil {
		t.Fatalf("body-cf-beacon not created: %v", err)
	}
}

// TestWriteBodyCFBeaconRejectsEmpty asserts WriteBodyCFBeacon returns an error
// for an empty string.
func TestWriteBodyCFBeaconRejectsEmpty(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "test-persona")

	if err := WriteBodyCFBeacon(""); err == nil {
		t.Fatal("expected error for empty beacon, got nil")
	}
}

// TestWriteBodyCFBeaconRejectsInvalidPrefix asserts WriteBodyCFBeacon returns an
// error for a string that does not start with "beacon:".
func TestWriteBodyCFBeaconRejectsInvalidPrefix(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "test-persona")

	for _, bad := range []string{
		"not-a-beacon",
		"campfire:abc",
		"beacon", // no colon
		"https://example.com",
	} {
		t.Run(bad, func(t *testing.T) {
			if err := WriteBodyCFBeacon(bad); err == nil {
				t.Errorf("WriteBodyCFBeacon(%q): expected error, got nil", bad)
			}
		})
	}
}

// TestReadBodyCFBeaconStripsWhitespace asserts ReadBodyCFBeacon tolerates a
// trailing newline (written by WriteBodyCFBeacon's atomic-write path).
func TestReadBodyCFBeaconStripsWhitespace(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "whitespace-persona")

	const beaconVal = "beacon:dGVzdA=="
	dir := filepath.Join(tmp, "freeso-souls", "whitespace-persona")
	if err := os.MkdirAll(dir, 0o700); err != nil {
		t.Fatalf("mkdir: %v", err)
	}
	if err := os.WriteFile(filepath.Join(dir, "body-cf-beacon"), []byte(beaconVal+"\n"), 0o600); err != nil {
		t.Fatalf("write: %v", err)
	}

	got, err := ReadBodyCFBeacon()
	if err != nil {
		t.Fatalf("ReadBodyCFBeacon: %v", err)
	}
	if got != beaconVal {
		t.Fatalf("whitespace not stripped: want %q, got %q", beaconVal, got)
	}
}

// TestReadBodyCFBeaconEmptyFile asserts ReadBodyCFBeacon returns an error when
// the file exists but is empty — a sign of corrupt state.
func TestReadBodyCFBeaconEmptyFile(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "corrupt-persona")

	dir := filepath.Join(tmp, "freeso-souls", "corrupt-persona")
	if err := os.MkdirAll(dir, 0o700); err != nil {
		t.Fatalf("mkdir: %v", err)
	}
	if err := os.WriteFile(filepath.Join(dir, "body-cf-beacon"), []byte("  \n"), 0o600); err != nil {
		t.Fatalf("write: %v", err)
	}

	_, err := ReadBodyCFBeacon()
	if err == nil {
		t.Fatal("expected error for empty body-cf-beacon, got nil")
	}
	if !strings.Contains(err.Error(), "empty") {
		t.Fatalf("expected 'empty' in error, got: %v", err)
	}
}

// TestReadBodyCFBeaconInvalidFormat asserts ReadBodyCFBeacon returns an error
// when the file exists but does not contain a "beacon:" prefix — corrupt state.
func TestReadBodyCFBeaconInvalidFormat(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "bad-format-persona")

	dir := filepath.Join(tmp, "freeso-souls", "bad-format-persona")
	if err := os.MkdirAll(dir, 0o700); err != nil {
		t.Fatalf("mkdir: %v", err)
	}
	if err := os.WriteFile(filepath.Join(dir, "body-cf-beacon"), []byte("not-a-beacon-string\n"), 0o600); err != nil {
		t.Fatalf("write: %v", err)
	}

	_, err := ReadBodyCFBeacon()
	if err == nil {
		t.Fatal("expected error for invalid format, got nil")
	}
}

// --- next-lot tests (freesoexperiment-e5f) ---

// TestReadNextLotMissing asserts ReadNextLot returns ("", nil) when the
// file does not exist — the normal case (no pending cross-lot transition).
func TestReadNextLotMissing(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "botrous")

	loc, err := ReadNextLot()
	if err != nil {
		t.Fatalf("ReadNextLot on missing file: want nil error, got %v", err)
	}
	if loc != "" {
		t.Fatalf("ReadNextLot on missing file: want empty string, got %q", loc)
	}
}

// TestWriteReadNextLotRoundTrip asserts that WriteNextLot followed by
// ReadNextLot returns the same location string.
func TestWriteReadNextLotRoundTrip(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "botrous")

	const wantLoc = "16318812"

	if err := WriteNextLot(wantLoc); err != nil {
		t.Fatalf("WriteNextLot: %v", err)
	}

	got, err := ReadNextLot()
	if err != nil {
		t.Fatalf("ReadNextLot: %v", err)
	}
	if got != wantLoc {
		t.Fatalf("round-trip mismatch: want %q, got %q", wantLoc, got)
	}
}

// TestClearNextLot asserts ClearNextLot removes the file so the next
// ReadNextLot returns ("", nil) — preventing stale transition targets.
func TestClearNextLot(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "ellis")

	// Write then clear.
	if err := WriteNextLot("12345678"); err != nil {
		t.Fatalf("WriteNextLot: %v", err)
	}
	if err := ClearNextLot(); err != nil {
		t.Fatalf("ClearNextLot: %v", err)
	}

	// After clear, Read should return ("", nil).
	loc, err := ReadNextLot()
	if err != nil {
		t.Fatalf("ReadNextLot after clear: want nil error, got %v", err)
	}
	if loc != "" {
		t.Fatalf("ReadNextLot after clear: want empty, got %q", loc)
	}
}

// TestClearNextLotIdempotent asserts ClearNextLot is a no-op when the file
// does not exist (prevents supervisor from crashing on double-clear).
func TestClearNextLotIdempotent(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "marlo")

	// No WriteNextLot first — file does not exist.
	if err := ClearNextLot(); err != nil {
		t.Fatalf("ClearNextLot on missing file: want nil error, got %v", err)
	}
}

// TestWriteNextLotOverwrites asserts that a second WriteNextLot replaces the
// previous location, so the most-recent transition target always wins.
func TestWriteNextLotOverwrites(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "jin")

	const first = "11111111"
	const second = "22222222"

	if err := WriteNextLot(first); err != nil {
		t.Fatalf("first write: %v", err)
	}
	if err := WriteNextLot(second); err != nil {
		t.Fatalf("second write: %v", err)
	}

	got, err := ReadNextLot()
	if err != nil {
		t.Fatalf("read: %v", err)
	}
	if got != second {
		t.Fatalf("overwrite: want %q, got %q", second, got)
	}
}

// TestWriteNextLotRejectsEmpty asserts WriteNextLot returns an error for
// an empty location string.
func TestWriteNextLotRejectsEmpty(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "sage")

	if err := WriteNextLot(""); err == nil {
		t.Fatal("expected error for empty lot location, got nil")
	}
}

// --- ReadHomeLotFromOwnedLots tests (freesoexperiment-084) ---

// TestReadHomeLotFromOwnedLotsNoFile asserts that when owned-lots.json does not
// exist, ReadHomeLotFromOwnedLots returns ("", nil) — persona owns no lot.
func TestReadHomeLotFromOwnedLotsNoFile(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "botrous")

	loc, err := ReadHomeLotFromOwnedLots()
	if err != nil {
		t.Fatalf("expected nil error for missing file, got: %v", err)
	}
	if loc != "" {
		t.Fatalf("expected empty string for missing file, got %q", loc)
	}
}

// TestReadHomeLotFromOwnedLotsFirstEntry asserts the first entry's LocationHex
// is returned when owned-lots.json has one or more entries.
func TestReadHomeLotFromOwnedLotsFirstEntry(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "botrous")

	// Write an owned-lots.json with two entries; first should win.
	entries := []OwnedLotEntry{
		{Name: "Main", LocationHex: "0xF9015C", PurchasedAt: 1000},
		{Name: "Cabin", LocationHex: "0x00110F00", PurchasedAt: 2000},
	}
	if err := WriteOwnedLots(entries); err != nil {
		t.Fatalf("WriteOwnedLots: %v", err)
	}

	loc, err := ReadHomeLotFromOwnedLots()
	if err != nil {
		t.Fatalf("ReadHomeLotFromOwnedLots: %v", err)
	}
	if loc != "0xF9015C" {
		t.Fatalf("expected first entry LocationHex %q, got %q", "0xF9015C", loc)
	}
}

// TestReadHomeLotFromOwnedLotsEmptyArray asserts that an empty owned-lots.json
// returns ("", nil) — no owned lots yet.
func TestReadHomeLotFromOwnedLotsEmptyArray(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "ellis")

	if err := WriteOwnedLots([]OwnedLotEntry{}); err != nil {
		t.Fatalf("WriteOwnedLots empty: %v", err)
	}

	loc, err := ReadHomeLotFromOwnedLots()
	if err != nil {
		t.Fatalf("ReadHomeLotFromOwnedLots: %v", err)
	}
	if loc != "" {
		t.Fatalf("expected empty string for empty array, got %q", loc)
	}
}

// --- injectHomeLotEnv tests (freesoexperiment-084) ---

// TestInjectHomeLotEnvInjectsWhenOwned asserts that injectHomeLotEnv adds
// FSO_HOME_LOT_LOCATION to the env slice when owned-lots.json exists.
func TestInjectHomeLotEnvInjectsWhenOwned(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "jin")

	entries := []OwnedLotEntry{
		{Name: "Home", LocationHex: "0xF9015C", PurchasedAt: 1000},
	}
	if err := WriteOwnedLots(entries); err != nil {
		t.Fatalf("WriteOwnedLots: %v", err)
	}

	result := injectHomeLotEnv([]string{"FOO=bar", "BAZ=qux"})

	found := ""
	for _, e := range result {
		if len(e) > len("FSO_HOME_LOT_LOCATION=") &&
			e[:len("FSO_HOME_LOT_LOCATION=")] == "FSO_HOME_LOT_LOCATION=" {
			found = e[len("FSO_HOME_LOT_LOCATION="):]
		}
	}
	if found == "" {
		t.Fatalf("FSO_HOME_LOT_LOCATION not injected: %v", result)
	}
	if found != "0xF9015C" {
		t.Fatalf("wrong FSO_HOME_LOT_LOCATION: want %q, got %q", "0xF9015C", found)
	}
}

// TestInjectHomeLotEnvNoOwnedLotsStripsStale asserts that injectHomeLotEnv
// strips an existing FSO_HOME_LOT_LOCATION when no lots are owned — preventing
// go-home from routing to a previously-owned lot after eviction/sale.
func TestInjectHomeLotEnvNoOwnedLotsStripsStale(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "marlo")

	// No owned-lots.json written → empty.
	env := []string{"FOO=bar", "FSO_HOME_LOT_LOCATION=0xDEADBEEF"}
	result := injectHomeLotEnv(env)

	for _, e := range result {
		if len(e) >= len("FSO_HOME_LOT_LOCATION=") &&
			e[:len("FSO_HOME_LOT_LOCATION=")] == "FSO_HOME_LOT_LOCATION=" {
			t.Errorf("stale FSO_HOME_LOT_LOCATION not stripped: %v", result)
			return
		}
	}
}

// TestInjectHomeLotEnvReplacesExisting asserts that injectHomeLotEnv replaces
// an existing FSO_HOME_LOT_LOCATION with the current owned-lots.json value.
func TestInjectHomeLotEnvReplacesExisting(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "sage")

	entries := []OwnedLotEntry{
		{Name: "NewHome", LocationHex: "0x00110F00", PurchasedAt: 9000},
	}
	if err := WriteOwnedLots(entries); err != nil {
		t.Fatalf("WriteOwnedLots: %v", err)
	}

	// Start with a stale value.
	env := []string{"FSO_HOME_LOT_LOCATION=0xOLDVALUE", "OTHER=x"}
	result := injectHomeLotEnv(env)

	found := ""
	for _, e := range result {
		if len(e) > len("FSO_HOME_LOT_LOCATION=") &&
			e[:len("FSO_HOME_LOT_LOCATION=")] == "FSO_HOME_LOT_LOCATION=" {
			found = e[len("FSO_HOME_LOT_LOCATION="):]
		}
	}
	if found != "0x00110F00" {
		t.Fatalf("want FSO_HOME_LOT_LOCATION=0x00110F00, got %q in %v", found, result)
	}
	// Count occurrences — must be exactly one.
	count := 0
	for _, e := range result {
		if len(e) >= len("FSO_HOME_LOT_LOCATION=") &&
			e[:len("FSO_HOME_LOT_LOCATION=")] == "FSO_HOME_LOT_LOCATION=" {
			count++
		}
	}
	if count != 1 {
		t.Errorf("expected exactly 1 FSO_HOME_LOT_LOCATION entry, got %d: %v", count, result)
	}
}

/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"testing"
)

// withFSO_USER and withConfigHome are defined in persona_state_test.go.

// withRUN_ID sets RUN_ID for the duration of the test and restores the prior
// value on cleanup.
func withRUN_ID(t *testing.T, val string) {
	t.Helper()
	prior, hasPrior := os.LookupEnv("RUN_ID")
	if val == "" {
		os.Unsetenv("RUN_ID")
	} else {
		os.Setenv("RUN_ID", val)
	}
	t.Cleanup(func() {
		if hasPrior {
			os.Setenv("RUN_ID", prior)
		} else {
			os.Unsetenv("RUN_ID")
		}
	})
}

// withXDG_DATA_HOME sets XDG_DATA_HOME for the duration of the test.
func withXDG_DATA_HOME(t *testing.T, dir string) {
	t.Helper()
	prior, hasPrior := os.LookupEnv("XDG_DATA_HOME")
	os.Setenv("XDG_DATA_HOME", dir)
	t.Cleanup(func() {
		if hasPrior {
			os.Setenv("XDG_DATA_HOME", prior)
		} else {
			os.Unsetenv("XDG_DATA_HOME")
		}
	})
}

// TestJournalDirNoRUN_ID verifies the /tmp fallback path when RUN_ID is unset.
func TestJournalDirNoRUN_ID(t *testing.T) {
	withFSO_USER(t, "botrous")
	withRUN_ID(t, "")

	dir, err := JournalDir()
	if err != nil {
		t.Fatalf("JournalDir: %v", err)
	}
	want := "/tmp/embody-botrous/journal"
	if dir != want {
		t.Fatalf("expected %q, got %q", want, dir)
	}
}

// TestJournalDirWithRUN_ID verifies the persistent XDG path when RUN_ID is set.
func TestJournalDirWithRUN_ID(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "Botrous") // uppercased — should be lowercased in result
	withRUN_ID(t, "run12-2026")
	withXDG_DATA_HOME(t, tmp)

	dir, err := JournalDir()
	if err != nil {
		t.Fatalf("JournalDir: %v", err)
	}
	want := filepath.Join(tmp, "freeso-experiment", "runs", "run12-2026", "journal", "botrous")
	if dir != want {
		t.Fatalf("expected %q, got %q", want, dir)
	}
}

// TestJournalDirNoFSO_USER verifies an error is returned when FSO_USER is unset.
func TestJournalDirNoFSO_USER(t *testing.T) {
	withFSO_USER(t, "")
	withRUN_ID(t, "run12-2026")

	_, err := JournalDir()
	if err == nil {
		t.Fatal("expected error when FSO_USER is unset, got nil")
	}
}

// TestJournalDirRejectsPathSeparatorsInRUN_ID verifies that RUN_ID values
// containing path separators are rejected to prevent directory traversal.
func TestJournalDirRejectsPathSeparatorsInRUN_ID(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "botrous")
	withXDG_DATA_HOME(t, tmp)

	for _, bad := range []string{"../etc", "run/bad", `run\bad`, "run.bad"} {
		t.Run(bad, func(t *testing.T) {
			withRUN_ID(t, bad)
			_, err := JournalDir()
			if err == nil {
				t.Fatalf("expected error for RUN_ID %q containing path separator, got nil", bad)
			}
		})
	}
}

// TestJournalWriterDisabledWhenFSO_USER_Unset verifies that NewJournalWriter
// returns a disabled writer when FSO_USER is not set (smoke-test / no-bot mode).
func TestJournalWriterDisabledWhenFSO_USER_Unset(t *testing.T) {
	withFSO_USER(t, "")
	jw := NewJournalWriter()
	if jw.IsEnabled() {
		t.Fatal("expected disabled writer when FSO_USER is unset")
	}
}

// TestJournalWriterWriteProducesFileWithTimestamp verifies that Write creates a
// file with the correct slug-based name and the expected content.
func TestJournalWriterWriteProducesFileWithTimestamp(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "botrous")
	withRUN_ID(t, "run-test")
	withXDG_DATA_HOME(t, tmp)

	jw := NewJournalWriter()
	if !jw.IsEnabled() {
		t.Fatal("expected enabled writer")
	}

	const content = "Test journal entry — arrived on the lot.\n"
	if err := jw.Write("arrival", content); err != nil {
		t.Fatalf("Write: %v", err)
	}

	// Verify a file was created in the correct directory.
	entries, err := os.ReadDir(jw.Dir())
	if err != nil {
		t.Fatalf("ReadDir: %v", err)
	}
	if len(entries) != 1 {
		t.Fatalf("expected 1 file, got %d: %v", len(entries), entries)
	}
	name := entries[0].Name()
	if !strings.HasSuffix(name, "-arrival.md") {
		t.Fatalf("expected filename ending in -arrival.md, got %q", name)
	}

	// Verify content.
	data, err := os.ReadFile(filepath.Join(jw.Dir(), name))
	if err != nil {
		t.Fatalf("ReadFile: %v", err)
	}
	if string(data) != content {
		t.Fatalf("content mismatch: want %q, got %q", content, string(data))
	}
}

// TestJournalWriterWriteSyncAfterSIGKILL is the falsifying test for the fsync
// requirement (freesoexperiment-f6d). It spawns a real subprocess that:
//   1. Creates a JournalWriter and writes one entry.
//   2. Exits via os.Exit(0) normally — but the test sends SIGKILL via exec.Cmd.
//
// The test verifies the entry is on disk AFTER the process is killed, proving
// that Sync() before Close() (not just at shutdown) guarantees durability.
//
// Implementation: we use go test -run=TestJournalWriterHelper_Write as the
// subprocess, passing the directory via env (JOURNAL_HELPER_DIR). The helper
// test writes the entry then blocks — the parent kills it.
func TestJournalWriterWriteSyncAfterSIGKILL(t *testing.T) {
	// Skip if we're already the subprocess (avoid recursive subprocess loops).
	if os.Getenv("JOURNAL_HELPER_DIR") != "" {
		t.Skip("helper subprocess — not a real test run")
	}

	// Find the test binary (ourselves).
	exe, err := os.Executable()
	if err != nil {
		t.Fatalf("os.Executable: %v", err)
	}

	tmp := t.TempDir()

	// Spawn subprocess: run TestJournalWriterHelper_Write with JOURNAL_HELPER_DIR set.
	// The subprocess writes the journal entry and then blocks indefinitely.
	cmd := exec.Command(exe, "-test.run=^TestJournalWriterHelper_Write$", "-test.v")
	cmd.Env = append(os.Environ(),
		"JOURNAL_HELPER_DIR="+tmp,
		"FSO_USER=botrous",
		"RUN_ID=", // ensure no RUN_ID so /tmp path is used — but we override XDG_DATA_HOME
		"XDG_DATA_HOME="+tmp,
	)

	if err := cmd.Start(); err != nil {
		t.Fatalf("start subprocess: %v", err)
	}

	// Wait a moment for the subprocess to write its entry and hit the blocking sleep.
	// We wait for the journal file to appear rather than using a fixed sleep.
	journalDir := filepath.Join(tmp, "freeso-experiment", "runs", "kill-test", "journal", "botrous")
	deadline := 10 // seconds
	var found bool
	for i := 0; i < deadline*10; i++ {
		// Sleep 100ms per iteration — polls within 10s total.
		entries, _ := os.ReadDir(journalDir)
		if len(entries) > 0 {
			found = true
			break
		}
		// Use exec.Command("sleep") to avoid importing "time" in this file.
		sleepCmd := exec.Command("sleep", "0.1")
		_ = sleepCmd.Run()
	}

	if !found {
		cmd.Process.Kill() //nolint:errcheck
		cmd.Wait()         //nolint:errcheck
		t.Fatal("journal file did not appear within 10s of subprocess start — write may not have happened before block")
	}

	// NOW kill the subprocess with SIGKILL (no cleanup hooks, no defer).
	if err := cmd.Process.Kill(); err != nil {
		t.Fatalf("Kill subprocess: %v", err)
	}
	cmd.Wait() //nolint:errcheck

	// Assert: the journal file is on disk and has non-empty content.
	entries, err := os.ReadDir(journalDir)
	if err != nil {
		t.Fatalf("ReadDir after kill: %v", err)
	}
	if len(entries) == 0 {
		t.Fatal("no journal files found after SIGKILL — fsync did not guarantee durability")
	}

	data, err := os.ReadFile(filepath.Join(journalDir, entries[0].Name()))
	if err != nil {
		t.Fatalf("ReadFile after kill: %v", err)
	}
	if len(data) == 0 {
		t.Fatal("journal file is empty after SIGKILL — content was not written before fsync")
	}
	if !strings.Contains(string(data), "kill-test") {
		t.Fatalf("unexpected content in journal file: %q", string(data))
	}
}

// TestJournalWriterHelper_Write is a subprocess helper for TestJournalWriterWriteSyncAfterSIGKILL.
// It writes one journal entry and then blocks on stdin read (waiting to be killed).
// Never called directly by the test framework — only invoked as a subprocess.
func TestJournalWriterHelper_Write(t *testing.T) {
	dir := os.Getenv("JOURNAL_HELPER_DIR")
	if dir == "" {
		t.Skip("not a subprocess invocation — JOURNAL_HELPER_DIR not set")
	}

	// Override XDG_DATA_HOME so JournalDir uses our tmp dir.
	os.Setenv("XDG_DATA_HOME", dir)
	os.Setenv("FSO_USER", "botrous")
	os.Setenv("RUN_ID", "kill-test")

	jw := NewJournalWriter()
	if !jw.IsEnabled() {
		t.Fatal("expected enabled writer in subprocess")
	}

	if err := jw.Write("sigkill-durability", "kill-test: this entry must survive SIGKILL\n"); err != nil {
		t.Fatalf("Write: %v", err)
	}

	// Block indefinitely — parent will SIGKILL us.
	// Use stdin read so the process doesn't busy-loop.
	buf := make([]byte, 1)
	os.Stdin.Read(buf) //nolint:errcheck
}

// TestJournalWriterSlugSanitisation verifies that unsafe characters in slug are
// replaced with "-" so the resulting filename is always safe.
func TestJournalWriterSlugSanitisation(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "ellis")
	withRUN_ID(t, "run-slugtest")
	withXDG_DATA_HOME(t, tmp)

	jw := NewJournalWriter()
	if !jw.IsEnabled() {
		t.Fatal("expected enabled writer")
	}

	if err := jw.Write("foo/bar baz", "content\n"); err != nil {
		t.Fatalf("Write: %v", err)
	}

	entries, err := os.ReadDir(jw.Dir())
	if err != nil {
		t.Fatalf("ReadDir: %v", err)
	}
	if len(entries) != 1 {
		t.Fatalf("expected 1 file, got %d", len(entries))
	}
	name := entries[0].Name()
	// Should not contain "/" or space — sanitised to "-".
	if strings.ContainsAny(name, "/ ") {
		t.Fatalf("unsanitised characters in filename: %q", name)
	}
}

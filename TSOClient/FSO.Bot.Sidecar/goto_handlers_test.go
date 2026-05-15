/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"bytes"
	"context"
	"log"
	"strings"
	"sync"
	"testing"
	"time"

	"github.com/campfire-net/campfire/pkg/convention"
)

// TestGoToDeprecationOnceEmission asserts that the objectNameDeprecationOnce guard
// emits the deprecation warning exactly once, even when the handler is called
// multiple times with object_name.
//
// Strategy: capture log output with bytes.Buffer/log.SetOutput, reset the sync.Once
// via reassignment in test scope, call goToHandler twice with object_name, and verify
// the DEPRECATED message appears exactly once in the captured output.
func TestGoToDeprecationOnceEmission(t *testing.T) {
	// Save original stdout and log output.
	originalOut := log.Default().Writer()
	defer log.SetOutput(originalOut)

	// Capture log output to a buffer.
	var logBuf bytes.Buffer
	log.SetOutput(&logBuf)

	// CRITICAL: Reset the package-level Once for this test.
	// This must run BEFORE we call the handler, since Once.Do fires only once
	// per process lifetime. By reassigning in test scope, the handler will see
	// a fresh Once that has never fired.
	objectNameDeprecationOnce = sync.Once{}

	// Set up a fake bot so goToHandler can forward to IPC.
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	// Drain stdin in the background so IPC.Send() doesn't block.
	go func() {
		for range fake.stdinLines {
		}
	}()

	// Create an empty MemoryStore (no name resolution needed for this test).
	store := NewMemoryStore()

	// Create the handler.
	handler := goToHandler(ipc, store)

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	// First call with object_name: should emit the deprecation message.
	resp1, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"object_name": "my_object",
		},
	})
	if err != nil {
		t.Fatalf("first handler call: %v", err)
	}
	if resp1 == nil {
		t.Fatal("first call: nil response")
	}

	// Small delay to allow any goroutine logging to flush.
	time.Sleep(10 * time.Millisecond)

	// Second call with object_name: should NOT emit the message again.
	resp2, err := handler(ctx, &convention.Request{
		Args: map[string]any{
			"object_name": "another_object",
		},
	})
	if err != nil {
		t.Fatalf("second handler call: %v", err)
	}
	if resp2 == nil {
		t.Fatal("second call: nil response")
	}

	// Check the captured output.
	captured := logBuf.String()
	count := strings.Count(captured, "DEPRECATED:")
	if count != 1 {
		t.Errorf("want exactly 1 DEPRECATED message, got %d\nCaptured:\n%s", count, captured)
	}

	// Verify the message contains the expected text.
	if !strings.Contains(captured, "object_name is deprecated") {
		t.Errorf("deprecation message doesn't contain expected text. Captured:\n%s", captured)
	}
}

// TestGoToDeclarationPresent asserts the go-to declaration loads and carries
// a galtrader-style description.
func TestGoToDeclarationPresent(t *testing.T) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}
	var d *convention.Declaration
	for _, x := range decls {
		if x.Operation == "go-to" {
			d = x
			break
		}
	}
	if d == nil {
		t.Fatal("declaration for go-to missing")
	}
	if d.Convention != "freeso-embodiment" {
		t.Errorf("convention=%q, want freeso-embodiment", d.Convention)
	}
	if d.Description == "" {
		t.Fatal("empty description")
	}
}

/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

// body_cf_beacon_format_test.go — automataisland-7ef
//
// Closes the Wave 3 closeout veracity gap on f3c (body-cf-beacon write).
// persona_state_test.go already tests WriteBodyCFBeacon → ReadBodyCFBeacon
// round-trip with HARDCODED "beacon:..." strings. If `cf.Share()` (a.k.a.
// `encodeBeaconString`'s underlying CBOR+base64 format) ever changes, the
// validator's prefix check would still pass for any string starting with
// "beacon:" — so a hardcoded test cannot catch a format drift.
//
// This file generates a REAL beacon by creating a campfire via the canonical
// protocol.Client.Create path (the same path the sidecar uses at campfire
// creation), encodes it with the same encodeBeaconString that ships beacons
// into the body-cf-beacon file, and round-trips through Write/Read.
//
// If the upstream beacon encoding changes shape (e.g., adds a version byte,
// switches base encoding, or breaks the "beacon:" prefix contract), this
// test fails and forces the prefix validator to be reconsidered.

package main

import (
	"encoding/base64"
	"os"
	"path/filepath"
	"strings"
	"testing"

	"github.com/campfire-net/campfire/cf-protocol/protocol"
)

// TestBodyCFBeaconFormatStability_RealBeacon is the format-stability gate.
// Generates a beacon from a real campfire and round-trips it through the
// body-cf-beacon file. Fails if `encodeBeaconString` output drifts away from
// what `WriteBodyCFBeacon`/`ReadBodyCFBeacon` expect.
func TestBodyCFBeaconFormatStability_RealBeacon(t *testing.T) {
	tmp := t.TempDir()
	withConfigHome(t, tmp)
	withFSO_USER(t, "beacon-format-test")

	// Create a real campfire via protocol.Init + Client.Create — the same path
	// the sidecar uses in campfire.go::ensureBodyCampfire.
	client, _, initErr := protocol.Init(tmp)
	if initErr != nil {
		t.Fatalf("protocol.Init: %v", initErr)
	}
	defer client.Close()

	transportDir := filepath.Join(tmp, "campfires")
	if err := os.MkdirAll(transportDir, 0o700); err != nil {
		t.Fatalf("mkdir transport dir: %v", err)
	}
	res, err := client.Create(protocol.CreateRequest{
		Description:  "body-cf-beacon-format-stability",
		JoinProtocol: "invite-only",
		Transport:    protocol.FilesystemTransport{Dir: transportDir},
	})
	if err != nil {
		t.Fatalf("client.Create: %v", err)
	}
	if res.Beacon == nil {
		t.Fatal("createResult.Beacon is nil — protocol regressed, beacon no longer attached to create result")
	}

	// Encode via the same function the sidecar uses.
	encoded, err := encodeBeaconString(res.Beacon)
	if err != nil {
		t.Fatalf("encodeBeaconString: %v", err)
	}

	// Prefix contract — both WriteBodyCFBeacon and ReadBodyCFBeacon enforce
	// strings.HasPrefix(s, "beacon:").
	if !strings.HasPrefix(encoded, "beacon:") {
		t.Errorf("encodeBeaconString output missing 'beacon:' prefix: %q", encoded)
	}

	// Body must be valid base64.StdEncoding. If the upstream encoder switches
	// to URL-safe base64 or adds padding requirements, this fails.
	body := strings.TrimPrefix(encoded, "beacon:")
	if body == "" {
		t.Fatalf("encoded beacon has empty body after prefix strip: %q", encoded)
	}
	if _, err := base64.StdEncoding.DecodeString(body); err != nil {
		t.Errorf("beacon body is not valid base64.StdEncoding: %v (body: %q)", err, body)
	}

	// Round-trip through the body-cf-beacon file.
	if err := WriteBodyCFBeacon(encoded); err != nil {
		t.Fatalf("WriteBodyCFBeacon: %v", err)
	}
	got, err := ReadBodyCFBeacon()
	if err != nil {
		t.Fatalf("ReadBodyCFBeacon: %v", err)
	}
	if got != encoded {
		t.Errorf("round-trip mismatch:\n  wrote: %q\n  read:  %q", encoded, got)
	}

	// Sanity: the encoded string is non-trivially long (CBOR-encoded beacon
	// structs are typically >50 bytes raw, >70 chars base64). This guards
	// against a regression where the beacon is emptied or truncated.
	if len(encoded) < len("beacon:")+20 {
		t.Errorf("encoded beacon suspiciously short (%d chars): %q", len(encoded), encoded)
	}
}

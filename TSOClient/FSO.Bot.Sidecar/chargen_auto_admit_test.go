/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

// chargen_auto_admit_test.go — Unit / integration tests for CampfireConfig.AdditionalAdmitKeys
// (automataisland-67ed).
//
// Test depth: feature (integration). StartCampfire uses a real protocol.Client with filesystem
// transport in a tmpdir — no mocked campfire interfaces.
//
// Test cases:
//   POSITIVE_AdditionalKey_Admitted:      AdditionalAdmitKeys contains a valid pubkey hex →
//                                         key appears in the campfire member list.
//   POSITIVE_EmptyAdditionalKeys_NoOp:    AdditionalAdmitKeys is nil → no panic, campfire
//                                         created normally.
//   POSITIVE_MultipleKeys_AllAdmitted:    Two keys in AdditionalAdmitKeys → both appear in
//                                         member list.
//   POSITIVE_Idempotent_DuplicateKey:     Same key appears twice in AdditionalAdmitKeys →
//                                         only one member record (Admit is idempotent).
//   POSITIVE_BadKey_NonFatal:             A malformed / garbage key in AdditionalAdmitKeys →
//                                         StartCampfire succeeds (error is logged, not fatal).
//   EXISTING_CF_NoAdmit:                  When CampfireID is set (resume path), AdditionalAdmitKeys
//                                         is NOT applied (loop is creation-only).

import (
	"context"
	"strings"
	"testing"

	"github.com/campfire-net/campfire/cf-protocol/protocol"
)

// ============================================================================
// Helper: derive a second independent pubkey from a second protocol.Init call.
// ============================================================================

// newAdditionalClient initialises a fresh cf identity in a subdirectory of
// t.TempDir() and returns the client + its public key hex. Used as the
// "additional admit key" target in tests — a real ed25519 pubkey.
func newAdditionalClient(t *testing.T, cfHome string) (*protocol.Client, string) {
	t.Helper()
	client, _, err := protocol.Init(cfHome)
	if err != nil {
		t.Fatalf("protocol.Init for additional client in %s: %v", cfHome, err)
	}
	pk := client.PublicKeyHex()
	return client, pk
}

// ============================================================================
// POSITIVE: single additional key is admitted
// ============================================================================

// TestStartCampfire_AdditionalKey_Admitted is the primary POSITIVE gate for
// automataisland-67ed.
//
// Verifies: when CampfireConfig.AdditionalAdmitKeys contains a valid pubkey hex,
// StartCampfire creates the campfire AND admits the key as a member.
//
// Uses real protocol.Client (filesystem transport, tmpdir). No mocks.
func TestStartCampfire_AdditionalKey_Admitted(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "admit-test-single")
	withConfigHome(t, tmp)

	// Create a second identity — its pubkey is what we want admitted.
	additionalCFHome := tmp + "/additional"
	_, additionalPK := newAdditionalClient(t, additionalCFHome)

	sidecarCFHome := tmp + "/sidecar-cf"
	ctx := context.Background()

	cf, err := StartCampfire(ctx, CampfireConfig{
		Home:                sidecarCFHome,
		Description:         "admit-test",
		Declarations:        conventionFiles,
		AdditionalAdmitKeys: []string{additionalPK},
	})
	if err != nil {
		t.Fatalf("StartCampfire: %v", err)
	}
	defer cf.Close()

	// Verify additionalPK is in the member list.
	members, err := cf.Client.Members(cf.ID)
	if err != nil {
		t.Fatalf("Members: %v", err)
	}

	found := false
	for _, m := range members {
		if strings.EqualFold(m.MemberPubkey, additionalPK) {
			found = true
			break
		}
	}
	if !found {
		pks := make([]string, 0, len(members))
		for _, m := range members {
			if len(m.MemberPubkey) >= 12 {
				pks = append(pks, m.MemberPubkey[:12]+"…")
			} else {
				pks = append(pks, m.MemberPubkey)
			}
		}
		t.Errorf("additional key %s not found in member list; members: %v", additionalPK[:12]+"…", pks)
	} else {
		t.Logf("POSITIVE: additional key %s admitted to campfire %s", additionalPK[:12]+"…", cf.ID[:12]+"…")
	}
}

// ============================================================================
// POSITIVE: nil AdditionalAdmitKeys — no-op, campfire created normally
// ============================================================================

// TestStartCampfire_EmptyAdditionalKeys_NoOp verifies that leaving AdditionalAdmitKeys
// nil or empty does not break campfire creation (backwards-compatible with existing flows).
func TestStartCampfire_EmptyAdditionalKeys_NoOp(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "admit-test-empty")
	withConfigHome(t, tmp)

	ctx := context.Background()
	cf, err := StartCampfire(ctx, CampfireConfig{
		Home:         tmp + "/sidecar-cf",
		Description:  "no-op-test",
		Declarations: conventionFiles,
		// AdditionalAdmitKeys deliberately omitted — must be safe.
	})
	if err != nil {
		t.Fatalf("StartCampfire with nil AdditionalAdmitKeys: %v", err)
	}
	defer cf.Close()

	if cf.ID == "" {
		t.Error("campfire ID should be non-empty")
	}
	t.Logf("POSITIVE (no-op): campfire %s created without additional admits", cf.ID[:12]+"…")
}

// ============================================================================
// POSITIVE: multiple keys — all admitted
// ============================================================================

// TestStartCampfire_MultipleKeys_AllAdmitted verifies that when AdditionalAdmitKeys
// contains two valid pubkey hexes, both are admitted as members.
func TestStartCampfire_MultipleKeys_AllAdmitted(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "admit-test-multi")
	withConfigHome(t, tmp)

	_, pk1 := newAdditionalClient(t, tmp+"/client1")
	_, pk2 := newAdditionalClient(t, tmp+"/client2")

	ctx := context.Background()
	cf, err := StartCampfire(ctx, CampfireConfig{
		Home:                tmp + "/sidecar-cf",
		Description:         "multi-admit-test",
		Declarations:        conventionFiles,
		AdditionalAdmitKeys: []string{pk1, pk2},
	})
	if err != nil {
		t.Fatalf("StartCampfire: %v", err)
	}
	defer cf.Close()

	members, err := cf.Client.Members(cf.ID)
	if err != nil {
		t.Fatalf("Members: %v", err)
	}

	memberSet := make(map[string]bool, len(members))
	for _, m := range members {
		memberSet[strings.ToLower(m.MemberPubkey)] = true
	}

	for _, pk := range []string{pk1, pk2} {
		if !memberSet[strings.ToLower(pk)] {
			t.Errorf("key %s not found in members after StartCampfire", pk[:12]+"…")
		} else {
			t.Logf("POSITIVE (multi): key %s admitted", pk[:12]+"…")
		}
	}
}

// ============================================================================
// POSITIVE: duplicate key — idempotent (single member record)
// ============================================================================

// TestStartCampfire_DuplicateKey_Idempotent verifies that listing the same key
// twice in AdditionalAdmitKeys does not cause an error or duplicate member records.
// The campfire SDK's Admit() is already idempotent; this test verifies our loop
// doesn't break on duplicate entries.
func TestStartCampfire_DuplicateKey_Idempotent(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "admit-test-dedup")
	withConfigHome(t, tmp)

	_, pk := newAdditionalClient(t, tmp+"/client")

	ctx := context.Background()
	cf, err := StartCampfire(ctx, CampfireConfig{
		Home:                tmp + "/sidecar-cf",
		Description:         "dedup-test",
		Declarations:        conventionFiles,
		AdditionalAdmitKeys: []string{pk, pk}, // same key twice
	})
	if err != nil {
		t.Fatalf("StartCampfire with duplicate key: %v", err)
	}
	defer cf.Close()

	members, err := cf.Client.Members(cf.ID)
	if err != nil {
		t.Fatalf("Members: %v", err)
	}

	count := 0
	for _, m := range members {
		if strings.EqualFold(m.MemberPubkey, pk) {
			count++
		}
	}
	if count == 0 {
		t.Errorf("key %s should appear in member list, got 0 occurrences", pk[:12]+"…")
	} else if count > 1 {
		t.Errorf("key %s should appear exactly once, got %d (duplicate admit not idempotent?)", pk[:12]+"…", count)
	} else {
		t.Logf("POSITIVE (dedup): key %s admitted exactly once (idempotent)", pk[:12]+"…")
	}
}

// ============================================================================
// POSITIVE: malformed / garbage key — non-fatal (StartCampfire still succeeds)
// ============================================================================

// TestStartCampfire_BadKey_NonFatal verifies that a malformed key in AdditionalAdmitKeys
// causes only a logged warning — StartCampfire still returns a valid campfire handle.
// The error from client.Admit() is non-fatal by design (automataisland-67ed spec).
func TestStartCampfire_BadKey_NonFatal(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "admit-test-bad-key")
	withConfigHome(t, tmp)

	ctx := context.Background()
	cf, err := StartCampfire(ctx, CampfireConfig{
		Home:                tmp + "/sidecar-cf",
		Description:         "bad-key-test",
		Declarations:        conventionFiles,
		AdditionalAdmitKeys: []string{"not-a-valid-hex-pubkey"},
	})
	if err != nil {
		t.Fatalf("StartCampfire should succeed even with bad admit key; got: %v", err)
	}
	defer cf.Close()

	if cf.ID == "" {
		t.Error("campfire ID should be non-empty even after bad-key admit attempt")
	}
	t.Logf("POSITIVE (bad-key non-fatal): campfire %s created despite bad admit key", cf.ID[:12]+"…")
}

// ============================================================================
// EXISTING CF resume path: AdditionalAdmitKeys NOT applied (creation-only)
// ============================================================================

// TestStartCampfire_ExistingCF_NoAdditionalAdmit verifies the item spec invariant:
// the admit loop runs ONLY when a NEW campfire is created, not on resume.
//
// Why: Mara and Lara already have body cfs. Their sidecars will resume those cfs
// on every restart (CampfireID set from body-cf.id). We must not re-admit on each
// resume — that would be harmless but the test documents the intended boundary.
// The test verifies that with CampfireID set (resume path), AdditionalAdmitKeys in the
// CampfireConfig has no effect (the code only loops inside `if id == ""`).
func TestStartCampfire_ExistingCF_NoAdditionalAdmit(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "admit-test-resume")
	withConfigHome(t, tmp)

	// Step 1: create a campfire (no additional keys).
	ctx := context.Background()
	cf1, err := StartCampfire(ctx, CampfireConfig{
		Home:         tmp + "/sidecar-cf",
		Description:  "resume-test",
		Declarations: conventionFiles,
	})
	if err != nil {
		t.Fatalf("StartCampfire (first, create): %v", err)
	}
	campfireID := cf1.ID
	cf1.Close()

	// Step 2: build a second identity pubkey (this is what we'd expect to be admitted
	// if the loop ran on resume — but it should NOT be).
	_, pk2 := newAdditionalClient(t, tmp+"/client2")

	// Step 3: resume the campfire via CampfireID, passing the new key.
	cf2, err := StartCampfire(ctx, CampfireConfig{
		Home:                tmp + "/sidecar-cf",
		CampfireID:          campfireID, // explicit resume — skips create block
		Description:         "resume-test",
		Declarations:        conventionFiles,
		AdditionalAdmitKeys: []string{pk2}, // should be ignored on resume
	})
	if err != nil {
		t.Fatalf("StartCampfire (second, resume): %v", err)
	}
	defer cf2.Close()

	if cf2.ID != campfireID {
		t.Errorf("expected resumed campfire ID %s, got %s", campfireID[:12]+"…", cf2.ID[:12]+"…")
	}

	members, err := cf2.Client.Members(cf2.ID)
	if err != nil {
		t.Fatalf("Members: %v", err)
	}

	for _, m := range members {
		if strings.EqualFold(m.MemberPubkey, pk2) {
			t.Errorf("pk2 %s should NOT be admitted on resume path, but found in member list", pk2[:12]+"…")
			return
		}
	}
	t.Logf("POSITIVE (resume no-admit): pk2 correctly NOT admitted on CampfireID resume path")
}

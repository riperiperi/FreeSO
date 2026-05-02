/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"encoding/json"
	"os"
	"path/filepath"
	"testing"
	"time"

	"github.com/campfire-net/campfire/pkg/convention"
)

// TestGrantCommunityAccessMissingLotID asserts that a missing lot_id returns ok:false.
func TestGrantCommunityAccessMissingLotID(t *testing.T) {
	handler := grantCommunityAccessHandler(nil)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"persona_name": "botrous",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for missing lot_id, got %v", payload)
	}
	if payload["reason"] != "INVALID_LOT_ID" {
		t.Errorf("want reason=INVALID_LOT_ID, got %v", payload["reason"])
	}
}

// TestGrantCommunityAccessMissingPersona asserts that an empty persona_name returns ok:false.
func TestGrantCommunityAccessMissingPersona(t *testing.T) {
	handler := grantCommunityAccessHandler(nil)
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"lot_id": float64(17),
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for missing persona_name, got %v", payload)
	}
	if payload["reason"] != "INVALID_PERSONA" {
		t.Errorf("want reason=INVALID_PERSONA, got %v", payload["reason"])
	}
}

// TestGrantCommunityAccessNoAuth asserts that a non-mayor gets NO_AUTH.
// Uses --no-bot mode with FSO_MAYOR_NHOOD unset (or "0").
func TestGrantCommunityAccessNoAuth(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "grant-access-noauth-test")
	withConfigHome(t, tmp)
	withSharedDataHome(t, tmp)

	// Ensure FSO_MAYOR_NHOOD is not set.
	prior, hasPrior := os.LookupEnv("FSO_MAYOR_NHOOD")
	os.Unsetenv("FSO_MAYOR_NHOOD")
	t.Cleanup(func() {
		if hasPrior {
			os.Setenv("FSO_MAYOR_NHOOD", prior)
		}
	})

	handler := grantCommunityAccessHandler(nil) // nil = --no-bot, falls back to env
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"lot_id":       float64(17),
		"persona_name": "botrous",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for non-mayor, got %v", payload)
	}
	if payload["reason"] != "NO_AUTH" {
		t.Errorf("want reason=NO_AUTH, got %v", payload["reason"])
	}
}

// TestGrantCommunityAccessMayorBotCheck asserts that a non-mayor bot response
// (check-mayor returns is_mayor=false) causes NO_AUTH refusal.
func TestGrantCommunityAccessMayorBotCheck(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "grant-access-mayorbotcheck-test")
	withConfigHome(t, tmp)
	withSharedDataHome(t, tmp)

	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	go func() {
		line := <-fake.stdinLines
		var req BotCmdRequest
		if err := json.Unmarshal(line, &req); err != nil {
			t.Errorf("unmarshal check-mayor req: %v", err)
			return
		}
		if req.Cmd != "check-mayor" {
			t.Errorf("want cmd=check-mayor, got %q", req.Cmd)
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             true,
			"data": map[string]any{
				"is_mayor":    false,
				"mayor_nhood": 0,
			},
		}))
	}()

	handler := grantCommunityAccessHandler(pump)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"lot_id":       float64(17),
		"persona_name": "botrous",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for is_mayor=false, got %v", payload)
	}
	if payload["reason"] != "NO_AUTH" {
		t.Errorf("want reason=NO_AUTH, got %v", payload["reason"])
	}
}

// TestGrantCommunityAccessSuccessPath is the integration test for a mayor granting
// access to a specific persona.
//
// Verifies:
//   - community-access.json is written under SharedCommunityAccessDir (NOT PersonaStateDir).
//   - HasCommunityAccess returns true for the granted persona + lot.
//   - HasCommunityAccess returns false for a different persona.
//   - Response carries ok=true, lot_id, persona_name, grant_count.
func TestGrantCommunityAccessSuccessPath(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "grant-access-success-test")
	withConfigHome(t, tmp)
	withSharedDataHome(t, tmp)

	// Mayor via FSO_MAYOR_NHOOD env (no bot needed for mayor check).
	prior, hasPrior := os.LookupEnv("FSO_MAYOR_NHOOD")
	os.Setenv("FSO_MAYOR_NHOOD", "1")
	t.Cleanup(func() {
		if hasPrior {
			os.Setenv("FSO_MAYOR_NHOOD", prior)
		} else {
			os.Unsetenv("FSO_MAYOR_NHOOD")
		}
	})

	handler := grantCommunityAccessHandler(nil) // nil = no bot, uses FSO_MAYOR_NHOOD
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"lot_id":       float64(17),
		"persona_name": "botrous",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != true {
		t.Fatalf("want ok=true: %v", payload)
	}

	// Verify response fields. lot_id is returned as uint32 from the handler.
	switch v := payload["lot_id"].(type) {
	case uint32:
		if v != 17 {
			t.Errorf("want lot_id=17, got %v", v)
		}
	case float64:
		if v != 17 {
			t.Errorf("want lot_id=17, got %v", v)
		}
	default:
		t.Errorf("unexpected lot_id type %T value %v", payload["lot_id"], payload["lot_id"])
	}
	if payload["persona_name"] != "botrous" {
		t.Errorf("want persona_name=botrous, got %v", payload["persona_name"])
	}
	if payload["idempotent"] != false {
		t.Errorf("want idempotent=false (first grant), got %v", payload["idempotent"])
	}

	// Verify community-access.json was written to the SHARED dir, not the persona dir.
	// Shared dir = XDG_DATA_HOME/freeso-souls-shared/ (in tests, XDG_DATA_HOME=tmp).
	sharedDir := filepath.Join(tmp, "freeso-souls-shared")
	accessPath := filepath.Join(sharedDir, "community-access.json")
	if _, err := os.Stat(accessPath); err != nil {
		t.Fatalf("community-access.json not written to shared dir %s: %v", sharedDir, err)
	}
	// Verify NOT written to the old per-persona dir.
	personaDir := filepath.Join(tmp, "freeso-souls", "grant-access-success-test")
	personaAccessPath := filepath.Join(personaDir, "community-access.json")
	if _, err := os.Stat(personaAccessPath); err == nil {
		t.Errorf("community-access.json must NOT be written to per-persona dir %s (cross-persona bug)", personaAccessPath)
	}

	// Verify HasCommunityAccess gate — FELT EFFECT: persona now can visit the lot.
	if !HasCommunityAccess(17, "botrous") {
		t.Error("HasCommunityAccess: want true for granted persona 'botrous' on lot 17")
	}
	if !HasCommunityAccess(17, "Botrous") {
		t.Error("HasCommunityAccess: want true for case-insensitive match 'Botrous'")
	}
	if HasCommunityAccess(17, "ellis") {
		t.Error("HasCommunityAccess: want false for non-granted persona 'ellis' on lot 17")
	}
	if HasCommunityAccess(18, "botrous") {
		t.Error("HasCommunityAccess: want false for different lot_id 18")
	}
	t.Logf("grant-community-access success: %v", payload)
}

// TestGrantCommunityAccessWildcard asserts that persona_name="*" grants all personas.
func TestGrantCommunityAccessWildcard(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "grant-access-wildcard-test")
	withConfigHome(t, tmp)
	withSharedDataHome(t, tmp)

	os.Setenv("FSO_MAYOR_NHOOD", "1")
	t.Cleanup(func() { os.Unsetenv("FSO_MAYOR_NHOOD") })

	handler := grantCommunityAccessHandler(nil)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{Args: map[string]any{
		"lot_id":       float64(17),
		"persona_name": "*",
	}})
	if err != nil {
		t.Fatalf("handler error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != true {
		t.Fatalf("want ok=true for wildcard grant: %v", payload)
	}

	// Any persona should now have access.
	if !HasCommunityAccess(17, "botrous") {
		t.Error("HasCommunityAccess: want true for any persona after wildcard grant (botrous)")
	}
	if !HasCommunityAccess(17, "randomPersona") {
		t.Error("HasCommunityAccess: want true for any persona after wildcard grant (randomPersona)")
	}
	if HasCommunityAccess(18, "botrous") {
		t.Error("HasCommunityAccess: want false for lot 18 (different lot, no grant)")
	}
}

// TestGrantCommunityAccessIdempotent asserts that granting the same (lot_id, persona) twice
// is idempotent — the grant count stays at 1 and idempotent=true is returned.
func TestGrantCommunityAccessIdempotent(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "grant-access-idempotent-test")
	withConfigHome(t, tmp)
	withSharedDataHome(t, tmp)

	os.Setenv("FSO_MAYOR_NHOOD", "1")
	t.Cleanup(func() { os.Unsetenv("FSO_MAYOR_NHOOD") })

	handler := grantCommunityAccessHandler(nil)
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	// First grant.
	resp1, err := handler(ctx, &convention.Request{Args: map[string]any{
		"lot_id":       float64(17),
		"persona_name": "jin",
	}})
	if err != nil {
		t.Fatalf("first grant error: %v", err)
	}
	p1, _ := resp1.Payload.(map[string]any)
	if p1["ok"] != true {
		t.Fatalf("first grant: want ok=true: %v", p1)
	}
	if p1["idempotent"] != false {
		t.Errorf("first grant: want idempotent=false, got %v", p1["idempotent"])
	}

	// Second grant — same lot/persona.
	resp2, err := handler(ctx, &convention.Request{Args: map[string]any{
		"lot_id":       float64(17),
		"persona_name": "jin",
	}})
	if err != nil {
		t.Fatalf("second grant error: %v", err)
	}
	p2, _ := resp2.Payload.(map[string]any)
	if p2["ok"] != true {
		t.Fatalf("second grant: want ok=true: %v", p2)
	}
	if p2["idempotent"] != true {
		t.Errorf("second grant: want idempotent=true, got %v", p2["idempotent"])
	}
	// grant_count should still be 1.
	if gc, _ := p2["grant_count"].(float64); gc != 1 {
		if gcI, _ := p2["grant_count"].(int); gcI != 1 {
			t.Errorf("second grant: want grant_count=1, got %v (type %T)", p2["grant_count"], p2["grant_count"])
		}
	}
}

// TestCommunityAccessRoundTrip tests the readCommunityAccess / writeCommunityAccess
// round-trip directly.
func TestCommunityAccessRoundTrip(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "community-access-rt")
	withConfigHome(t, tmp)
	withSharedDataHome(t, tmp)

	// Empty state.
	grants, err := readCommunityAccess()
	if err != nil {
		t.Fatalf("readCommunityAccess empty: %v", err)
	}
	if len(grants) != 0 {
		t.Fatalf("want 0 grants, got %d", len(grants))
	}

	// Write two grants.
	toWrite := []CommunityAccessGrant{
		{LotID: 17, PersonaName: "botrous", GrantedAt: 1000},
		{LotID: 17, PersonaName: "*", GrantedAt: 2000},
	}
	if err := writeCommunityAccess(toWrite); err != nil {
		t.Fatalf("writeCommunityAccess: %v", err)
	}

	grants, err = readCommunityAccess()
	if err != nil {
		t.Fatalf("readCommunityAccess: %v", err)
	}
	if len(grants) != 2 {
		t.Fatalf("want 2 grants, got %d", len(grants))
	}
	if grants[0].LotID != 17 || grants[0].PersonaName != "botrous" {
		t.Errorf("grant[0]: want {17, botrous}, got %+v", grants[0])
	}
}

// TestCrossPersonaGrant is the regression test for the CRITICAL bug identified
// by the veracity adversary: grants written by one persona's sidecar must be
// readable by a different persona's sidecar.
//
// Bug scenario (pre-fix):
//   - baron's sidecar (FSO_USER=baron) calls grant-community-access for ellis on lot 17.
//   - ellis's sidecar (FSO_USER=ellis) calls visit-lot on lot 17.
//   - IsCommunityGated(17) reads from ~/.config/freeso-souls/ellis/community-access.json
//     which is EMPTY — the gate is a no-op for every visitor.
//
// Fix: community-access.json lives in SharedCommunityAccessDir() (keyed by
// XDG_DATA_HOME, NOT by FSO_USER). All sidecars on the same machine share it.
//
// This test will FAIL with the old per-PersonaStateDir storage and PASS with
// SharedCommunityAccessDir storage — that's the mutation evidence.
func TestCrossPersonaGrant(t *testing.T) {
	tmp := t.TempDir()
	// XDG_DATA_HOME → shared dir isolation for community-access.json.
	withSharedDataHome(t, tmp)

	// Mayor env var for grant authorization (no bot needed).
	os.Setenv("FSO_MAYOR_NHOOD", "1")
	t.Cleanup(func() { os.Unsetenv("FSO_MAYOR_NHOOD") })

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	// --- Step 1: baron's sidecar grants ellis access to lot 17. ---
	// baron is the mayor; FSO_USER=baron during grant.
	withFSO_USER(t, "baron")
	withConfigHome(t, tmp) // baron's persona state goes to tmp/freeso-souls/baron/

	grantHandler := grantCommunityAccessHandler(nil)
	grantResp, err := grantHandler(ctx, &convention.Request{Args: map[string]any{
		"lot_id":       float64(17),
		"persona_name": "ellis",
	}})
	if err != nil {
		t.Fatalf("grant-community-access: %v", err)
	}
	grantPayload, _ := grantResp.Payload.(map[string]any)
	if grantPayload["ok"] != true {
		t.Fatalf("grant-community-access failed (prerequisite): %v", grantPayload)
	}

	// Verify grant was written to shared dir, NOT to baron's persona dir.
	sharedAccessPath := filepath.Join(tmp, "freeso-souls-shared", "community-access.json")
	if _, err := os.Stat(sharedAccessPath); err != nil {
		t.Fatalf("grant must be in SharedCommunityAccessDir (%s), not found: %v", sharedAccessPath, err)
	}
	baronPersonaAccessPath := filepath.Join(tmp, "freeso-souls", "baron", "community-access.json")
	if _, err := os.Stat(baronPersonaAccessPath); err == nil {
		t.Errorf("grant must NOT be in baron's PersonaStateDir (%s) — cross-persona bug", baronPersonaAccessPath)
	}
	t.Logf("STEP 1 PASS: grant written to shared dir by baron's sidecar")

	// --- Step 2: ellis's sidecar reads the grant (FSO_USER=ellis). ---
	// Switch FSO_USER to ellis — simulates ellis's sidecar reading.
	withFSO_USER(t, "ellis")
	withConfigHome(t, tmp) // ellis's persona state goes to tmp/freeso-souls/ellis/

	// IsCommunityGated must return true — lot 17 has a grant in the shared file.
	if !IsCommunityGated(17) {
		t.Fatal("CROSS-PERSONA BUG: IsCommunityGated(17) returned false when FSO_USER=ellis — " +
			"grant written by baron's sidecar is not visible to ellis's sidecar. " +
			"Storage must use SharedCommunityAccessDir, not PersonaStateDir.")
	}

	// HasCommunityAccess(17, "ellis") must return true.
	if !HasCommunityAccess(17, "ellis") {
		t.Fatal("CROSS-PERSONA BUG: HasCommunityAccess(17, ellis) returned false when FSO_USER=ellis — " +
			"grant written by baron's sidecar is not visible to ellis's sidecar. " +
			"Storage must use SharedCommunityAccessDir, not PersonaStateDir.")
	}
	t.Logf("STEP 2 PASS: ellis's sidecar sees baron's grant — cross-persona storage working")

	// --- Step 3: marlo's sidecar (no grant) is denied. ---
	withFSO_USER(t, "marlo")
	withConfigHome(t, tmp) // marlo's persona state goes to tmp/freeso-souls/marlo/

	// IsCommunityGated still returns true (lot 17 is still gated).
	if !IsCommunityGated(17) {
		t.Fatal("IsCommunityGated(17) returned false for marlo — shared state should still show gating")
	}

	// HasCommunityAccess must return false for marlo (no grant).
	if HasCommunityAccess(17, "marlo") {
		t.Fatal("HasCommunityAccess(17, marlo) returned true — marlo has no grant, must be denied")
	}
	t.Logf("STEP 3 PASS: marlo is denied (no grant) — gate discriminates correctly")

	t.Logf("FULL CROSS-PERSONA TEST PASS: baron grants → ellis visits OK, marlo denied")
}

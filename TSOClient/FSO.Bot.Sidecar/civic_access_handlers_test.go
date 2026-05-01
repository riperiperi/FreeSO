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
//   - community-access.json is written under PersonaStateDir.
//   - HasCommunityAccess returns true for the granted persona + lot.
//   - HasCommunityAccess returns false for a different persona.
//   - Response carries ok=true, lot_id, persona_name, grant_count.
func TestGrantCommunityAccessSuccessPath(t *testing.T) {
	tmp := t.TempDir()
	withFSO_USER(t, "grant-access-success-test")
	withConfigHome(t, tmp)

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

	// Verify community-access.json was written.
	dir := filepath.Join(tmp, "freeso-souls", "grant-access-success-test")
	accessPath := filepath.Join(dir, "community-access.json")
	if _, err := os.Stat(accessPath); err != nil {
		t.Fatalf("community-access.json not written: %v", err)
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

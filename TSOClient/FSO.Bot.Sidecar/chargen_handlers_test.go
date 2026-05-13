/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"encoding/json"
	"strings"
	"testing"
	"time"

	"github.com/campfire-net/campfire/pkg/convention"
)

// ----------------------------------------------------------------------------
// Layer 1: argument validation unit tests (freesoexperiment-92a done condition §1)
//
// These tests exercise the sidecar-tier validation in createAvatarHandler.
// No real FSO server, no real bot process. A fake BotCmdPump is wired so the
// handler can call botCmds.Send; tests that expect early rejection drain the
// stub goroutine so it doesn't block.
//
// Mocking rationale: these tests check that missing / malformed args are
// rejected BEFORE the bot-cmd is dispatched. The bot-cmd level (BotCmdPump)
// and FSO server are mocked because:
//   - They are not relevant for arg-validation coverage.
//   - Live server round-trips belong to Layer 2 (C# integration test) and
//     Layer 3 (verb-create-avatar.sh), per the freesoexperiment-92a spec.
// ----------------------------------------------------------------------------

// validArgs returns a minimal set of valid create-avatar args for use in tests
// that want to exercise non-arg paths (e.g. bot-cmd success/failure shapes).
// GUIDs (949, 601) are baron's real head/body high-32-bit IDs from the DB.
func validArgs() map[string]any {
	return map[string]any{
		"first_name": "Vivi",
		"last_name":  "Romero",
		"gender":     "F",
		"head_guid":  float64(399), // Lady's head_guid (399 = 0x18F)
		"body_guid":  float64(228), // Lady's body_guid (228 = 0xE4)
	}
}

// -- Missing required args --

func TestCreateAvatarHandler_MissingFirstName_RejectsOkFalse(t *testing.T) {
	pump := stubBotCmdPump(t, nil) // no reply expected — handler returns early
	handler := createAvatarHandler(pump)
	args := validArgs()
	delete(args, "first_name")

	resp, err := callHandler(t, handler, args)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	assertOkFalse(t, resp, "first_name")
}

func TestCreateAvatarHandler_EmptyFirstName_RejectsOkFalse(t *testing.T) {
	pump := stubBotCmdPump(t, nil)
	handler := createAvatarHandler(pump)
	args := validArgs()
	args["first_name"] = "   " // whitespace-only

	resp, err := callHandler(t, handler, args)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	assertOkFalse(t, resp, "first_name")
}

func TestCreateAvatarHandler_MissingLastName_RejectsOkFalse(t *testing.T) {
	pump := stubBotCmdPump(t, nil)
	handler := createAvatarHandler(pump)
	args := validArgs()
	delete(args, "last_name")

	resp, err := callHandler(t, handler, args)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	assertOkFalse(t, resp, "last_name")
}

func TestCreateAvatarHandler_MissingGender_RejectsOkFalse(t *testing.T) {
	pump := stubBotCmdPump(t, nil)
	handler := createAvatarHandler(pump)
	args := validArgs()
	delete(args, "gender")

	resp, err := callHandler(t, handler, args)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	assertOkFalse(t, resp, "gender")
}

func TestCreateAvatarHandler_InvalidGender_RejectsOkFalse(t *testing.T) {
	pump := stubBotCmdPump(t, nil)
	handler := createAvatarHandler(pump)
	args := validArgs()
	args["gender"] = "X" // invalid

	resp, err := callHandler(t, handler, args)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	assertOkFalse(t, resp, "gender")
}

func TestCreateAvatarHandler_GenderCaseInsensitive_AcceptedLower(t *testing.T) {
	// "f" and "m" (lowercase) must be accepted — sidecar normalises to uppercase.
	pump := stubBotCmdPump(t, map[string]any{
		"status":    "SUCCESS",
		"avatar_id": float64(99),
	})
	handler := createAvatarHandler(pump)
	args := validArgs()
	args["gender"] = "f"

	resp, err := callHandler(t, handler, args)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != true {
		t.Errorf("lowercase gender 'f' should be accepted, got ok=%v error=%v", payload["ok"], payload["error"])
	}
}

func TestCreateAvatarHandler_MissingHeadGUID_RejectsOkFalse(t *testing.T) {
	pump := stubBotCmdPump(t, nil)
	handler := createAvatarHandler(pump)
	args := validArgs()
	delete(args, "head_guid")

	resp, err := callHandler(t, handler, args)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	assertOkFalse(t, resp, "head_guid")
}

func TestCreateAvatarHandler_ZeroHeadGUID_RejectsOkFalse(t *testing.T) {
	pump := stubBotCmdPump(t, nil)
	handler := createAvatarHandler(pump)
	args := validArgs()
	args["head_guid"] = float64(0)

	resp, err := callHandler(t, handler, args)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	assertOkFalse(t, resp, "head_guid")
}

func TestCreateAvatarHandler_MissingBodyGUID_RejectsOkFalse(t *testing.T) {
	pump := stubBotCmdPump(t, nil)
	handler := createAvatarHandler(pump)
	args := validArgs()
	delete(args, "body_guid")

	resp, err := callHandler(t, handler, args)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	assertOkFalse(t, resp, "body_guid")
}

func TestCreateAvatarHandler_ZeroBodyGUID_RejectsOkFalse(t *testing.T) {
	pump := stubBotCmdPump(t, nil)
	handler := createAvatarHandler(pump)
	args := validArgs()
	args["body_guid"] = float64(0)

	resp, err := callHandler(t, handler, args)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	assertOkFalse(t, resp, "body_guid")
}

// -- Invalid optional args --

func TestCreateAvatarHandler_InvalidSkinTone_RejectsOkFalse(t *testing.T) {
	pump := stubBotCmdPump(t, nil)
	handler := createAvatarHandler(pump)
	args := validArgs()
	args["skin_tone"] = "green" // invalid

	resp, err := callHandler(t, handler, args)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	assertOkFalse(t, resp, "skin_tone")
}

func TestCreateAvatarHandler_ValidSkinTones_Accepted(t *testing.T) {
	for _, tone := range []string{"light", "medium", "dark"} {
		pump := stubBotCmdPump(t, map[string]any{
			"status":    "SUCCESS",
			"avatar_id": float64(42),
		})
		handler := createAvatarHandler(pump)
		args := validArgs()
		args["skin_tone"] = tone

		resp, err := callHandler(t, handler, args)
		if err != nil {
			t.Fatalf("skin_tone=%q: unexpected error: %v", tone, err)
		}
		payload, _ := resp.Payload.(map[string]any)
		if payload["ok"] != true {
			t.Errorf("skin_tone=%q should be accepted, got ok=%v error=%v", tone, payload["ok"], payload["error"])
		}
	}
}

// -- NilBotCmds guard --

func TestCreateAvatarHandler_NilBotCmds_ReturnsOkFalse(t *testing.T) {
	handler := createAvatarHandler(nil) // nil botCmds

	resp, err := callHandler(t, handler, validArgs())
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	assertOkFalse(t, resp, "bot-cmd channel not available")
}

// -- Bot-side failure categories surface correctly --

func TestCreateAvatarHandler_EngineNameTaken_SurfacesReason(t *testing.T) {
	pump := stubBotCmdPump(t, map[string]any{
		"status": "FAILED",
		"reason": "category:name-taken",
	})
	handler := createAvatarHandler(pump)

	resp, err := callHandler(t, handler, validArgs())
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for FAILED status, got %v", payload)
	}
	reason, _ := payload["reason"].(string)
	if reason == "" {
		t.Errorf("reason should be populated for FAILED status, got %v", payload)
	}
	t.Logf("name-taken reason: %v", reason)
}

func TestCreateAvatarHandler_BotCmdError_SurfacesError(t *testing.T) {
	// Simulate bot-cmd-reply ok=false (e.g. bot not in chargen mode).
	// The bot-cmd transport failure path uses the "error" key (not "reason").
	// "reason" is reserved for engine-tier semantic categories (e.g. category:name-taken).
	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	// Drain stdin and deliver an error reply.
	go func() {
		line := <-fake.stdinLines
		var req BotCmdRequest
		if err := json.Unmarshal(line, &req); err != nil {
			t.Errorf("unmarshal: %v", err)
			return
		}
		pump.Deliver(mustMarshal(map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             false,
			"error":          "category:session-already-bound",
		}))
	}()

	handler := createAvatarHandler(pump)
	resp, err := callHandler(t, handler, validArgs())
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != false {
		t.Errorf("want ok=false for bot error reply, got %v", payload)
	}
	// Bot-cmd transport failures surface in the "error" key, not "reason".
	errorMsg, _ := payload["error"].(string)
	if errorMsg == "" {
		t.Errorf("want non-empty error key for bot-cmd transport failure, got %v", payload)
	}
	t.Logf("error from bot (transport failure): %v", errorMsg)
}

// extractUint32 extracts a uint32 from a payload value using a type-switch
// that handles both in-process uint32 (direct assignment) and float64 (JSON
// round-trip deserialization via json.Unmarshal). The type-switch prevents
// panics if the response crosses a JSON boundary in the future.
func extractUint32(t *testing.T, payload map[string]any, key string) uint32 {
	t.Helper()
	v, ok := payload[key]
	if !ok {
		t.Fatalf("key %q not found in payload %v", key, payload)
	}
	switch x := v.(type) {
	case uint32:
		return x
	case float64:
		return uint32(x)
	default:
		t.Fatalf("unexpected type %T for key %q (value=%v); expected uint32 or float64", v, key, v)
		return 0
	}
}

func TestCreateAvatarHandler_Success_ReturnsAvatarID(t *testing.T) {
	pump := stubBotCmdPump(t, map[string]any{
		"status":    "SUCCESS",
		"avatar_id": float64(99),
	})
	handler := createAvatarHandler(pump)

	resp, err := callHandler(t, handler, validArgs())
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != true {
		t.Errorf("want ok=true for SUCCESS, got %v", payload)
	}
	// Use extractUint32 which handles both uint32 (in-process) and float64
	// (JSON-deserialized). This prevents a type-assertion panic if the
	// response ever crosses a JSON boundary (freesoexperiment-4e3).
	avatarID := extractUint32(t, payload, "avatar_id")
	if avatarID != 99 {
		t.Errorf("want avatar_id=99, got %v", avatarID)
	}
	if payload["status"] != "SUCCESS" {
		t.Errorf("want status=SUCCESS, got %v", payload["status"])
	}
}

// TestCreateAvatarHandler_Success_AvatarID_JSONRoundTrip verifies that the
// avatar_id field survives a JSON encode/decode round-trip. json.Unmarshal
// converts JSON numbers to float64 by default, so a test using uint32 type
// assertion would panic after the round-trip. This test exercises the
// float64 branch of extractUint32.
func TestCreateAvatarHandler_Success_AvatarID_JSONRoundTrip(t *testing.T) {
	pump := stubBotCmdPump(t, map[string]any{
		"status":    "SUCCESS",
		"avatar_id": float64(42),
	})
	handler := createAvatarHandler(pump)

	resp, err := callHandler(t, handler, validArgs())
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload["ok"] != true {
		t.Errorf("want ok=true for SUCCESS, got %v", payload)
	}

	// JSON round-trip: marshal the payload then unmarshal — number becomes float64.
	encoded, encErr := json.Marshal(payload)
	if encErr != nil {
		t.Fatalf("marshal: %v", encErr)
	}
	var decoded map[string]any
	if decErr := json.Unmarshal(encoded, &decoded); decErr != nil {
		t.Fatalf("unmarshal: %v", decErr)
	}

	// After JSON round-trip the type is float64 — extractUint32 must handle it.
	avatarID := extractUint32(t, decoded, "avatar_id")
	if avatarID != 42 {
		t.Errorf("want avatar_id=42 after JSON round-trip, got %v", avatarID)
	}
}

// -- coerceUint64 coverage --
// Note: coerceUint64(v any) (uint64, error) is declared in tax_handlers.go.

func TestCoerceUint64_Float64(t *testing.T) {
	v, err := coerceUint64(float64(949))
	if err != nil || v != 949 {
		t.Errorf("want (949, nil), got (%d, %v)", v, err)
	}
}

func TestCoerceUint64_NegativeFloat64_ReturnsError(t *testing.T) {
	_, err := coerceUint64(float64(-1))
	if err == nil {
		t.Errorf("want error for negative float64, got nil")
	}
}

func TestCoerceUint64_String(t *testing.T) {
	v, err := coerceUint64("601")
	if err != nil || v != 601 {
		t.Errorf("want (601, nil), got (%d, %v)", v, err)
	}
}

func TestCoerceUint64_StringInvalid_ReturnsError(t *testing.T) {
	_, err := coerceUint64("not-a-number")
	if err == nil {
		t.Errorf("want error for non-numeric string")
	}
}

func TestCoerceUint64_Nil_ReturnsError(t *testing.T) {
	_, err := coerceUint64(nil)
	if err == nil {
		t.Errorf("want error for nil")
	}
}

// -- Helpers for this test file --

// callHandler invokes the handler with the given args and a 2s timeout.
func callHandler(t *testing.T, h convention.HandlerFunc, args map[string]any) (*convention.Response, error) {
	t.Helper()
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()
	return h(ctx, &convention.Request{Args: args})
}

// assertOkFalse checks that the response has ok=false and that the error
// string contains the given keyword. The keyword check is strict — a test
// that passes a keyword but the error doesn't mention it is a test failure.
// This ensures the contract the parameter implies is actually enforced and
// tests cannot pass vacuously on a wrong error message.
func assertOkFalse(t *testing.T, resp *convention.Response, keyword string) {
	t.Helper()
	if resp == nil {
		t.Fatal("nil response from handler")
	}
	payload, _ := resp.Payload.(map[string]any)
	if payload == nil {
		t.Fatalf("nil payload in response")
	}
	if payload["ok"] != false {
		t.Errorf("want ok=false, got ok=%v (full payload: %v)", payload["ok"], payload)
	}
	// Check that error message mentions the keyword for diagnosability.
	errMsg, _ := payload["error"].(string)
	if errMsg == "" {
		t.Errorf("want non-empty error mentioning %q, got empty error", keyword)
		return
	}
	// Strict keyword check: the error message must contain the keyword.
	// This prevents tests from passing vacuously with a wrong error message.
	if !strings.Contains(errMsg, keyword) {
		t.Errorf("error message did not contain expected keyword %q: got %q", keyword, errMsg)
	}
	t.Logf("reject ok=false error=%q (keyword=%q present=true)", errMsg, keyword)
}

// stubBotCmdPump returns a BotCmdPump that immediately delivers a fixed reply
// for any bot-cmd. If replyData is nil, no reply is consumed (for tests that
// expect early rejection before any bot-cmd is sent).
func stubBotCmdPump(t *testing.T, replyData map[string]any) *BotCmdPump {
	t.Helper()
	fake := newFakeBotProcess()
	pump := NewBotCmdPump(fake.bot)

	if replyData == nil {
		// Drain stdin silently so fake.stdinLines doesn't block on GC.
		go func() {
			for range fake.stdinLines {
			}
		}()
		return pump
	}

	// Respond to the first (and only) bot-cmd frame with the given data.
	go func() {
		line, ok := <-fake.stdinLines
		if !ok {
			return
		}
		var req BotCmdRequest
		if err := json.Unmarshal(line, &req); err != nil {
			t.Errorf("stub: unmarshal bot-cmd req: %v", err)
			return
		}
		reply := map[string]any{
			"kind":           "bot-cmd-reply",
			"correlation_id": req.CorrelationID,
			"ok":             true,
			"data":           replyData,
		}
		pump.Deliver(mustMarshal(reply))
		// Drain any subsequent frames (shouldn't be any, but keep it clean).
		go func() {
			for range fake.stdinLines {
			}
		}()
	}()

	return pump
}

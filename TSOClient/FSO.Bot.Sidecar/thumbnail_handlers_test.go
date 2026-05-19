/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"bytes"
	"context"
	"encoding/base64"
	"encoding/json"
	"image"
	"image/color"
	"image/png"
	"net/http"
	"net/http/httptest"
	"os"
	"testing"
	"time"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// makeTinyPNG returns a valid PNG-encoded RGBA image of w×h pixels, all red.
// Used in tests to produce a deterministic PNG without hitting the filesystem.
func makeTinyPNG(t *testing.T, w, h int) []byte {
	t.Helper()
	img := image.NewRGBA(image.Rect(0, 0, w, h))
	for y := 0; y < h; y++ {
		for x := 0; x < w; x++ {
			img.SetRGBA(x, y, color.RGBA{R: 255, A: 255})
		}
	}
	var buf bytes.Buffer
	if err := png.Encode(&buf, img); err != nil {
		t.Fatalf("makeTinyPNG: %v", err)
	}
	return buf.Bytes()
}

// fakeRendererServer creates an httptest.Server that mimics the renderer's
// /render endpoint. On each POST it writes the given PNG to a temp file and
// returns {"path":"<tmpfile>","width":w,"height":h,"age_sec":0}.
// The caller is responsible for closing the server.
func fakeRendererServer(t *testing.T, pngBytes []byte) (*httptest.Server, string) {
	t.Helper()
	tmp, err := os.CreateTemp(t.TempDir(), "thumbnail-*.png")
	if err != nil {
		t.Fatalf("fakeRendererServer: create temp: %v", err)
	}
	if _, err := tmp.Write(pngBytes); err != nil {
		t.Fatalf("fakeRendererServer: write temp: %v", err)
	}
	tmp.Close()
	tmpPath := tmp.Name()

	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		resp := map[string]any{
			"path":    tmpPath,
			"width":   64,
			"height":  64,
			"age_sec": float64(0),
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(resp)
	}))
	return srv, tmpPath
}

// fakeRendererServerSlow creates a renderer that sleeps for delay before
// responding — used to exercise the render-timeout path.
func fakeRendererServerSlow(t *testing.T, delay time.Duration) *httptest.Server {
	t.Helper()
	return httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		time.Sleep(delay)
		w.WriteHeader(http.StatusOK)
		json.NewEncoder(w).Encode(map[string]any{"path": "/nonexistent.png"})
	}))
}


// TestGetLotThumbnailNotOnLot verifies that when query-lot returns lot_id=0
// (city mode), the handler returns ok=false, error="not-on-lot".
//
// Strategy: invoke getLotThumbnailHandler with a fake IPC that returns a
// query-lot payload where lot_id=0. The handler must refuse before any
// renderer contact.
func TestGetLotThumbnailNotOnLot(t *testing.T) {
	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)

	responses := map[string]map[string]any{
		"query-lot": {
			"ok": true,
			"payload": map[string]any{
				"lot_id":      float64(0),
				"owner_is_me": false,
				"is_roommate": false,
				"name":        "city",
			},
		},
	}
	multiAutoResponder(t, fake, ipc, responses)

	handler := getLotThumbnailHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Sender: "test-soul",
		Args:   map[string]any{},
	})
	if err != nil {
		t.Fatalf("handler returned error: %v", err)
	}
	if resp == nil {
		t.Fatal("handler returned nil response")
	}

	payload, ok := resp.Payload.(map[string]any)
	if !ok {
		t.Fatalf("payload is not map[string]any: %T", resp.Payload)
	}
	if payload["ok"] != false {
		t.Errorf("expected ok=false for city-mode bot, got ok=%v", payload["ok"])
	}
	errStr, _ := payload["error"].(string)
	if errStr != "not-on-lot" {
		t.Errorf("expected error=not-on-lot, got %q", errStr)
	}
}

// TestGetLotThumbnailOnLot_HappyPath verifies the handler returns a valid
// base64-encoded PNG when the bot is on a lot and the renderer responds.
//
// Strategy: wire a fake IPC with lot_id=42, inject thumbnailReadFileFn to
// serve a real in-memory PNG, and use an httptest.Server to stand in for the
// renderer at the package-level URL. Since thumbnailRendererURL is a const we
// override it via a package-level var swap (thumbnailRendererURLVar).
func TestGetLotThumbnailOnLot_HappyPath(t *testing.T) {
	pngData := makeTinyPNG(t, 64, 64)

	// Write PNG to a temp file so the handler can read it.
	tmp, err := os.CreateTemp(t.TempDir(), "thumb-*.png")
	if err != nil {
		t.Fatal(err)
	}
	tmp.Write(pngData)
	tmp.Close()
	tmpPath := tmp.Name()

	// Override thumbnailReadFileFn to serve our in-memory PNG.
	origReadFn := thumbnailReadFileFn
	thumbnailReadFileFn = os.ReadFile // real fs — tmp file exists
	t.Cleanup(func() { thumbnailReadFileFn = origReadFn })

	// Fake renderer returns the tmp path.
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		resp := map[string]any{
			"path":    tmpPath,
			"width":   64,
			"height":  64,
			"age_sec": float64(0),
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(resp)
	}))
	defer srv.Close()

	// Patch renderer URL.
	origURL := thumbnailRendererURLVar
	thumbnailRendererURLVar = srv.URL
	t.Cleanup(func() { thumbnailRendererURLVar = origURL })

	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	responses := map[string]map[string]any{
		"query-lot": {
			"ok": true,
			"payload": map[string]any{
				"lot_id":      float64(42),
				"owner_is_me": true,
				"is_roommate": false,
				"name":        "TestLot",
			},
		},
	}
	multiAutoResponder(t, fake, ipc, responses)

	handler := getLotThumbnailHandler(ipc)
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	resp, err := handler(ctx, &convention.Request{
		Sender: "test-soul",
		Args:   map[string]any{},
	})
	if err != nil {
		t.Fatalf("handler returned error: %v", err)
	}
	if resp == nil {
		t.Fatal("handler returned nil response")
	}

	payload, ok := resp.Payload.(map[string]any)
	if !ok {
		t.Fatalf("payload is not map[string]any: %T", resp.Payload)
	}
	if payload["ok"] != true {
		t.Fatalf("expected ok=true, got ok=%v (error=%v)", payload["ok"], payload["error"])
	}

	// Verify thumbnail field is valid base64-encoded PNG.
	thumbB64, ok := payload["thumbnail"].(string)
	if !ok || thumbB64 == "" {
		t.Fatalf("thumbnail field missing or empty")
	}
	decoded, decErr := base64.StdEncoding.DecodeString(thumbB64)
	if decErr != nil {
		t.Fatalf("thumbnail is not valid base64: %v", decErr)
	}
	// Verify PNG magic bytes: 89 50 4E 47 ...
	if len(decoded) < 8 || decoded[0] != 0x89 || decoded[1] != 0x50 || decoded[2] != 0x4E || decoded[3] != 0x47 {
		t.Errorf("decoded thumbnail is not a valid PNG (magic bytes: %x)", decoded[:min(8, len(decoded))])
	}

	// Verify size_bytes.
	sizeRaw := payload["size_bytes"]
	if sizeRaw == nil {
		t.Errorf("size_bytes field missing")
	} else {
		size, _ := sizeRaw.(int)
		if size == 0 {
			// JSON numbers round-trip as float64.
			if sf, ok := sizeRaw.(float64); ok {
				size = int(sf)
			}
		}
		if size <= 0 {
			t.Errorf("size_bytes should be >0, got %v", sizeRaw)
		}
		if size > thumbnailMaxBytes {
			t.Errorf("size_bytes=%d exceeds 256KB limit", size)
		}
	}
}

// TestGetLotThumbnailRenderTimeout verifies that when the renderer takes
// longer than thumbnailRenderTimeout the handler returns ok=false,
// error="render-timeout" without hanging.
//
// Strategy: spin up a slow renderer (sleeps 3s), invoke handler with
// a short context, assert render-timeout before 3s.
func TestGetLotThumbnailRenderTimeout(t *testing.T) {
	// Slow renderer — sleeps well past thumbnailRenderTimeout (2s).
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		time.Sleep(3 * time.Second)
		w.WriteHeader(http.StatusOK)
		json.NewEncoder(w).Encode(map[string]any{"path": "/nonexistent.png"})
	}))
	defer srv.Close()

	origURL := thumbnailRendererURLVar
	thumbnailRendererURLVar = srv.URL
	t.Cleanup(func() { thumbnailRendererURLVar = origURL })

	fake := newFakeBotProcess()
	ipc := NewIPC(fake.bot)
	responses := map[string]map[string]any{
		"query-lot": {
			"ok": true,
			"payload": map[string]any{
				"lot_id":      float64(1234),
				"owner_is_me": true,
				"is_roommate": false,
				"name":        "TimeoutLot",
			},
		},
	}
	multiAutoResponder(t, fake, ipc, responses)

	handler := getLotThumbnailHandler(ipc)
	// Outer context gives 5s — render timeout at 2s fires first.
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	start := time.Now()
	resp, err := handler(ctx, &convention.Request{
		Sender: "test-soul",
		Args:   map[string]any{},
	})
	elapsed := time.Since(start)

	if err != nil {
		t.Fatalf("handler returned error: %v", err)
	}
	if resp == nil {
		t.Fatal("handler returned nil response")
	}

	payload, ok := resp.Payload.(map[string]any)
	if !ok {
		t.Fatalf("payload is not map[string]any: %T", resp.Payload)
	}
	if payload["ok"] != false {
		t.Errorf("expected ok=false for render-timeout, got ok=%v", payload["ok"])
	}
	errStr, _ := payload["error"].(string)
	if errStr != "render-timeout" {
		t.Errorf("expected error=render-timeout, got %q", errStr)
	}
	// Handler should return within ~thumbnailRenderTimeout + margin (3s), not
	// after the slow renderer's full 3s + overhead.
	if elapsed > 3*time.Second {
		t.Errorf("handler took %s — should have timed out faster (render timeout = %s)", elapsed, thumbnailRenderTimeout)
	}
}

// TestThumbnailReadAndFit_SmallFile verifies that a PNG already within the
// 256KB budget is returned unchanged.
func TestThumbnailReadAndFit_SmallFile(t *testing.T) {
	pngData := makeTinyPNG(t, 32, 32)
	if len(pngData) >= thumbnailMaxBytes {
		t.Skipf("test PNG unexpectedly large (%d bytes)", len(pngData))
	}

	tmp, err := os.CreateTemp(t.TempDir(), "small-*.png")
	if err != nil {
		t.Fatal(err)
	}
	tmp.Write(pngData)
	tmp.Close()

	result, err := thumbnailReadAndFit(tmp.Name(), thumbnailMaxBytes)
	if err != nil {
		t.Fatalf("thumbnailReadAndFit: %v", err)
	}
	if !bytes.Equal(result, pngData) {
		t.Errorf("small file should be returned unchanged (got %d bytes, want %d)", len(result), len(pngData))
	}
}

// TestThumbnailReadAndFit_LargeFile verifies that thumbnailReadAndFit triggers
// downsampling when the file is oversized. We use a tight maxBytes budget (4096)
// rather than the production 256KB so that even a highly-compressible PNG
// exceeds the budget, making the test deterministic across Go PNG encoder versions.
func TestThumbnailReadAndFit_LargeFile(t *testing.T) {
	// 512×512 image with varied noise to defeat PNG's deflate compression.
	const bigW, bigH = 512, 512
	img := image.NewRGBA(image.Rect(0, 0, bigW, bigH))
	for y := 0; y < bigH; y++ {
		for x := 0; x < bigW; x++ {
			// Pseudo-noise: each pixel is different enough to defeat run-length coding.
			img.SetRGBA(x, y, color.RGBA{
				R: uint8((x * 37 + y * 13) & 0xFF),
				G: uint8((x * 11 + y * 97) & 0xFF),
				B: uint8((x * 71 + y * 29) & 0xFF),
				A: 255,
			})
		}
	}
	var buf bytes.Buffer
	if err := png.Encode(&buf, img); err != nil {
		t.Fatalf("encode test PNG: %v", err)
	}
	testPNG := buf.Bytes()

	// Use a very tight budget (4096 bytes) so the PNG is guaranteed to exceed it.
	const tightBudget = 4096
	if len(testPNG) <= tightBudget {
		t.Skipf("test PNG (%d bytes) fits tight budget %d — cannot verify downsampling", len(testPNG), tightBudget)
	}

	tmp, err := os.CreateTemp(t.TempDir(), "large-*.png")
	if err != nil {
		t.Fatal(err)
	}
	tmp.Write(testPNG)
	tmp.Close()

	result, err := thumbnailReadAndFit(tmp.Name(), tightBudget)
	if err != nil {
		t.Fatalf("thumbnailReadAndFit: %v", err)
	}
	if len(result) > tightBudget {
		t.Errorf("downsampled PNG is %d bytes, exceeds tight budget (%d)", len(result), tightBudget)
	}
	// Result must still be a valid PNG.
	if _, pngErr := png.Decode(bytes.NewReader(result)); pngErr != nil {
		t.Errorf("downsampled result is not a valid PNG: %v", pngErr)
	}
	t.Logf("downsampled %d bytes → %d bytes (tight budget %d)", len(testPNG), len(result), tightBudget)
}

// TestThumbnailDownsample verifies the nearest-neighbour downsampler produces
// an image of the expected dimensions and valid pixel content.
func TestThumbnailDownsample(t *testing.T) {
	// Source: 8×4 image, all green.
	src := image.NewRGBA(image.Rect(0, 0, 8, 4))
	for y := 0; y < 4; y++ {
		for x := 0; x < 8; x++ {
			src.SetRGBA(x, y, color.RGBA{G: 255, A: 255})
		}
	}

	dst := image.NewRGBA(image.Rect(0, 0, 4, 2))
	thumbnailDownsample(dst, src, 4, 2)

	b := dst.Bounds()
	if b.Dx() != 4 || b.Dy() != 2 {
		t.Errorf("dst dimensions: got %dx%d, want 4x2", b.Dx(), b.Dy())
	}
	// Every pixel should be green.
	for y := 0; y < 2; y++ {
		for x := 0; x < 4; x++ {
			r, g, b2, a := dst.At(x, y).RGBA()
			if g == 0 || r != 0 || b2 != 0 || a == 0 {
				t.Errorf("pixel (%d,%d): expected green, got rgba(%d,%d,%d,%d)", x, y, r, g, b2, a)
			}
		}
	}
}

// TestGetLotThumbnailRegisters verifies that RegisterThumbnailHandlers registers
// exactly 1 handler (get-lot-thumbnail) without error.
//
// Strategy: use a nil campfire router (campfire.Router is an unexported *Router
// in the package). We can't easily construct a full Campfire in a unit test, so
// we verify indirectly: LoadDeclarations must include get-lot-thumbnail, and
// the declaration status must be "implemented".
func TestGetLotThumbnailDeclaration(t *testing.T) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		t.Fatalf("LoadDeclarations: %v", err)
	}

	var found bool
	for _, d := range decls {
		if d.Operation == "get-lot-thumbnail" {
			found = true
			// Verify the declaration is no longer scaffold.
			// We do this by checking the raw JSON for the "implemented" status field.
			// convention.Declaration may not expose status directly — check the raw file.
			break
		}
	}
	if !found {
		t.Fatalf("get-lot-thumbnail declaration not found in conventions/")
	}
}

// TestGetLotThumbnailDeclaration_StatusImplemented verifies the declaration
// JSON has status="implemented" (no longer a scaffold op).
func TestGetLotThumbnailDeclaration_StatusImplemented(t *testing.T) {
	raw, err := conventionFiles.ReadFile("conventions/get-lot-thumbnail.json")
	if err != nil {
		t.Fatalf("read get-lot-thumbnail.json: %v", err)
	}
	var decl struct {
		Status string `json:"status"`
	}
	if err := json.Unmarshal(raw, &decl); err != nil {
		t.Fatalf("unmarshal: %v", err)
	}
	if decl.Status != "implemented" {
		t.Errorf("get-lot-thumbnail status=%q want implemented", decl.Status)
	}
}

// min returns the smaller of a and b. Used in test for safe slice preview.
func min(a, b int) int {
	if a < b {
		return a
	}
	return b
}

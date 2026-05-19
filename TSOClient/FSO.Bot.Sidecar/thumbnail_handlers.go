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
	"fmt"
	"image"
	"image/png"
	"io"
	"math"
	"net/http"
	"os"
	"time"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

const (
	// thumbnailMaxBytes is the maximum allowed PNG size (256 KB).
	// If the renderer returns a larger PNG the handler downsamples to fit.
	thumbnailMaxBytes = 256 * 1024

	// thumbnailRenderTimeout is the deadline we give the renderer.
	// The item spec says: "if render takes >2s, return error:render-timeout".
	// We use 2s for the renderer POST itself; the outer handler has a 5s
	// budget overall (convention call timeout at the cf layer). Keeping the
	// renderer timeout tight ensures the handler never hangs the sidecar's
	// single-dispatch goroutine.
	thumbnailRenderTimeout = 2 * time.Second
)

// thumbnailRendererURLVar is the headless renderer endpoint. Declared as a var
// (not a const) so integration tests can point it at an httptest.Server without
// requiring a running renderer on localhost:9101. Matches the URL used by
// takeScreenshotHandler (screenshot_handlers.go).
var thumbnailRendererURLVar = "http://localhost:9101/render"

// thumbnailReadFileFn is the file-read function used by thumbnailReadAndFit.
// Extracted to a variable so tests can inject a stub without touching the
// filesystem. Default: os.ReadFile.
var thumbnailReadFileFn = os.ReadFile

// RegisterThumbnailHandlers (automataisland-ca6) wires the get-lot-thumbnail op.
//
// The handler returns a base64-encoded PNG of the current lot view using the
// same headless renderer pipeline as take-screenshot, but with:
//   - no permission gate (any caller on the campfire may request it)
//   - a hard 2s render timeout (returns error:render-timeout instead of hanging)
//   - a 256KB size cap (downsamples the PNG if the renderer returns more)
//   - an explicit not-on-lot guard (returns error:not-on-lot when lot_id == 0)
//
// Unlike take-screenshot, the PNG bytes are returned inline as base64 in the
// convention response rather than written to a file path. This keeps the op
// self-contained for automated capture cadences (operating-mode.md).
//
// Handler must not block the perception bridge. The render HTTP POST uses a
// tight 2s deadline, so the longest this handler can run is ~2s + overhead.
func RegisterThumbnailHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"get-lot-thumbnail": getLotThumbnailHandler(ipc),
	}

	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		return 0, fmt.Errorf("load declarations: %w", err)
	}
	byOp := make(map[string]*convention.Declaration, len(decls))
	for _, d := range decls {
		byOp[d.Operation] = d
	}

	started := 0
	for op, handler := range ops {
		decl, ok := byOp[op]
		if !ok {
			return started, fmt.Errorf("declaration for op %q missing (expected in conventions/%s.json)", op, op)
		}
		cf.Router.Register(decl, handler)
		started++
	}
	return started, nil
}

// getLotThumbnailHandler implements get-lot-thumbnail.
//
// Steps:
//  1. Call query-lot to confirm bot is on a lot (lot_id != 0).
//  2. Build renderer payload from lot_id / lot_location.
//  3. POST to renderer with thumbnailRenderTimeout deadline.
//  4. Read rendered PNG; downsample if > thumbnailMaxBytes.
//  5. Base64-encode and return.
func getLotThumbnailHandler(ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		// Step 1: Check bot is on a lot.
		lotResp, err := forwardIPC(ctx, ipc, "query-lot", map[string]any{})
		if err != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("query-lot failed: %v", err),
				},
			}, nil
		}

		outerPayload, ok := lotResp.Payload.(map[string]any)
		if !ok {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": "query-lot returned unexpected payload type",
				},
			}, nil
		}
		lotPayload, ok := outerPayload["payload"].(map[string]any)
		if !ok {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("query-lot inner payload missing: %v", outerPayload),
				},
			}, nil
		}

		// lot_id == 0 means the bot is in city mode (not joined to a lot).
		// The C# QueryLot handler returns 0u when lotState is null (QueryHandlers.cs:253).
		lotIDFloat, _ := lotPayload["lot_id"].(float64)
		if lotIDFloat == 0 {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": "not-on-lot",
				},
			}, nil
		}

		// Step 2: Resolve lot_location (uint32 packed = x<<16 | y).
		// query-lot returns the engine's LotID which IS the packed location for
		// residential lots (same logic as takeScreenshotHandler, screenshot_handlers.go).
		var lotLocationUint uint32
		if lotIDFloat > 0 && lotIDFloat <= math.MaxUint32 {
			lotLocationUint = uint32(lotIDFloat)
		}
		if lotLocationUint == 0 {
			// Fallback: owned-lots.json (home lot only).
			if locHex, locErr := ReadHomeLotFromOwnedLots(); locErr == nil && locHex != "" {
				if parsed, perr := parseLotLocation(locHex); perr == nil {
					lotLocationUint = parsed
				}
			}
		}

		// Step 3: POST to renderer with render timeout.
		// thumbnailRenderTimeout (2s) is the hard deadline. A context timeout
		// produces error:render-timeout rather than hanging the dispatch goroutine.
		rendererPayload := map[string]any{
			"shard":  "Alphaville",
			"lot_id": lotPayload["lot_id"],
			"level":  float64(1),
			"angle":  "iso-ne",
			"zoom":   "far", // renderer-native enum value (= "small" in user vocab)
		}
		if lotLocationUint != 0 {
			rendererPayload["lot_location"] = lotLocationUint
		}

		payloadBody, marshalErr := json.Marshal(rendererPayload)
		if marshalErr != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("marshal renderer payload: %v", marshalErr),
				},
			}, nil
		}

		renderCtx, renderCancel := context.WithTimeout(ctx, thumbnailRenderTimeout)
		defer renderCancel()

		httpReq, httpErr := http.NewRequestWithContext(renderCtx, http.MethodPost, thumbnailRendererURLVar, bytes.NewReader(payloadBody))
		if httpErr != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("build renderer request: %v", httpErr),
				},
			}, nil
		}
		httpReq.Header.Set("Content-Type", "application/json")

		client := &http.Client{}
		resp, postErr := client.Do(httpReq)
		if postErr != nil {
			// Context deadline exceeded == render-timeout.
			if renderCtx.Err() != nil {
				return &convention.Response{
					Payload: map[string]any{
						"ok":    false,
						"error": "render-timeout",
					},
				}, nil
			}
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("renderer POST failed: %v", postErr),
				},
			}, nil
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusOK {
			errBody, _ := io.ReadAll(io.LimitReader(resp.Body, 512))
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("renderer returned status %d: %s", resp.StatusCode, string(errBody)),
				},
			}, nil
		}

		// Parse renderer JSON response: {"path":"...","width":W,"height":H,"age_sec":N}
		bodyBytes, readErr := io.ReadAll(resp.Body)
		if readErr != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("read renderer response: %v", readErr),
				},
			}, nil
		}
		var renderResp struct {
			Path   string  `json:"path"`
			Width  int     `json:"width"`
			Height int     `json:"height"`
			AgeSec float64 `json:"age_sec"`
			Error  string  `json:"error"`
		}
		if err := json.Unmarshal(bodyBytes, &renderResp); err != nil {
			preview := len(bodyBytes)
			if preview > 256 {
				preview = 256
			}
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("parse renderer response: %v (body: %s)", err, string(bodyBytes[:preview])),
				},
			}, nil
		}
		if renderResp.Error != "" {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("renderer error: %s", renderResp.Error),
				},
			}, nil
		}
		if renderResp.Path == "" {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": "renderer returned empty path",
				},
			}, nil
		}

		// Step 4: Read rendered PNG from disk and downsample if > 256KB.
		pngBytes, pngErr := thumbnailReadAndFit(renderResp.Path, thumbnailMaxBytes)
		if pngErr != nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":    false,
					"error": fmt.Sprintf("read/fit PNG: %v", pngErr),
				},
			}, nil
		}

		// Step 5: Base64-encode and return.
		encoded := base64.StdEncoding.EncodeToString(pngBytes)
		return &convention.Response{
			Payload: map[string]any{
				"ok":         true,
				"thumbnail":  encoded,
				"size_bytes": len(pngBytes),
			},
		}, nil
	}
}

// thumbnailReadAndFit reads the PNG at path and returns raw PNG bytes that fit
// within maxBytes. If the file is already within budget it is returned as-is.
// If it exceeds the budget the image is decoded, scaled down proportionally
// until the re-encoded PNG fits, and the scaled bytes are returned.
//
// The scaling loop halves both dimensions on each iteration and re-encodes;
// it terminates when the result fits or when the image has been reduced below
// 16×16 (at which point we return what we have — the 256KB budget is always
// met at 16×16 for any real lot thumbnail).
func thumbnailReadAndFit(path string, maxBytes int) ([]byte, error) {
	raw, err := thumbnailReadFileFn(path)
	if err != nil {
		return nil, fmt.Errorf("read PNG from %s: %w", path, err)
	}
	if len(raw) <= maxBytes {
		return raw, nil
	}

	// Decode, scale, re-encode.
	img, decErr := png.Decode(bytes.NewReader(raw))
	if decErr != nil {
		return nil, fmt.Errorf("decode PNG from %s: %w", path, decErr)
	}

	for {
		b := img.Bounds()
		w, h := b.Dx(), b.Dy()
		if w <= 16 || h <= 16 {
			// Already very small — encode and return even if still over budget.
			break
		}
		// Halve dimensions.
		newW, newH := w/2, h/2
		dst := image.NewRGBA(image.Rect(0, 0, newW, newH))
		thumbnailDownsample(dst, img, newW, newH)
		img = dst

		var buf bytes.Buffer
		if encErr := png.Encode(&buf, img); encErr != nil {
			return nil, fmt.Errorf("re-encode PNG: %w", encErr)
		}
		if buf.Len() <= maxBytes {
			return buf.Bytes(), nil
		}
	}

	// Encode final (possibly still over budget at very tiny size — caller is lenient).
	var buf bytes.Buffer
	if encErr := png.Encode(&buf, img); encErr != nil {
		return nil, fmt.Errorf("encode final PNG: %w", encErr)
	}
	return buf.Bytes(), nil
}

// thumbnailDownsample performs a simple nearest-neighbour scale of src into
// dst (newW × newH). dst must already be allocated as *image.RGBA.
func thumbnailDownsample(dst *image.RGBA, src image.Image, newW, newH int) {
	srcBounds := src.Bounds()
	srcW := srcBounds.Dx()
	srcH := srcBounds.Dy()
	for y := 0; y < newH; y++ {
		for x := 0; x < newW; x++ {
			sx := srcBounds.Min.X + x*srcW/newW
			sy := srcBounds.Min.Y + y*srcH/newH
			dst.Set(x, y, src.At(sx, sy))
		}
	}
}

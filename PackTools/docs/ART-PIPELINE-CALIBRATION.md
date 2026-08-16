# Art Pipeline — Calibration Spike Report

Status: calibration spike complete, end-to-end test render complete, first original furniture piece generated (`ART-PIPELINE-DESIGN.md` §6 steps 1-3). This report separates **measured fact** (from decoding real base-game sprite data) from **code-derived fact** (read directly from FreeSO's own renderer source, not measured by this spike) from **inference**. Each numbered claim below is tagged with which kind it is. §10 covers the end-to-end box test render, §11 covers real depth/lighting and the first generated chair.

## 0. Method

Built a throwaway instrument, `PackTools/FSO.ArtCalibration` (console tool, not part of the pipeline — delete once calibration is settled or re-run against a second reference object). It reuses `AppearanceCloner`'s exact FAR-archive loading path (same GUID → `objecttable.xml` → `objiff.far`/`objspf*.far` lookup already proven by the appearance-cloning lane) to pull a base-game object's `DGRP`/`SPR2`/`PALT` chunks into memory, decodes all 12 frames, and prints exact pixel dimensions and z-buffer statistics per direction/zoom.

Game content path used: `~/Library/Application Support/The Sims Online/TSOClient` — confirmed this is `Environment.SpecialFolder.UserProfile`-relative, not `.Personal` (macOS gives those different paths; `.Personal` pointed nowhere useful here — this is the same trap the appearance-cloning lane hit).

**Reference object**: `"Table - End - Cardboard Box"`, GUID `0x35372C14`, catalog group `cardboardbox`. Chosen for simple, box-like, single-tile geometry with no visible multi-part composition (`m="0"` in `objecttable.xml`, one catalog entry, one draw group).

Run: `dotnet FSO.ArtCalibration.dll` (defaults to the path/GUID above; both are overridable args).

## 1. Measured: DGRP structure

- **12 images confirmed** for this object: `dgrp.Images.Length == 12`, exactly 4 directions × 3 zooms, matching `DGRP.cs`'s doc comment. **Measured fact**, not just a code-comment claim.
- Single sprite per image (`BaseGraphicID=100`, `NumGraphics=1`), one `SPR2` chunk (`id=100`) holding all 12 frames, indices 0–11.

## 2. Measured: pixel dimensions per zoom/direction

Raw output (Width × Height, all four directions per zoom level):

| Zoom | RightBack | RightFront | LeftFront | LeftBack |
|---|---|---|---|---|
| Far (1) | 27×29 | 26×29 | 27×29 | 26×29 |
| Medium (2) | 52×57 | 52×58 | 52×57 | 52×57 |
| Near (3) | 104×114 | 103×114 | 104×114 | 103×113 |

The ±1px spread within a zoom level across directions is real (not measurement noise) — most likely sprite-trim/padding variance per direction's silhouette, not camera asymmetry (aspect ratio is consistent within ~1% across all 4 directions at every zoom).

**Direction bitflags used to query `DGRP.GetImage`**: `RightBack=0x01, RightFront=0x04, LeftFront=0x10, LeftBack=0x40` — taken directly from `DGRP.GetImage`'s comment and confirmed working (all 12 combinations resolved to a real image, none returned null). **Code-derived, confirmed by successful lookup**, not independently re-derived.

## 3. Measured + code-derived: per-zoom scale ratio (the key validation)

**Measured** pixel-dimension ratios between zoom levels (using RightBack as a consistent reference):
- Medium / Far: 52/27 = 1.93, 57/29 = 1.97
- Near / Medium: 104/52 = 2.00, 114/57 = 2.00
- Near / Far: 104/27 = 3.85, 114/29 = 3.93

**Code-derived** (from `WorldCamera.CalculateProjection`, `TSOClient/tso.world/Utils/WorldCamera.cs:161-189`): the `diagnal` (isometric scale denominator) is hardcoded per zoom as `Far=64, Medium=128, Near=256` — an exact `1 : 2 : 4` ratio — and `isoScale` (world-units-per-screen-pixel) is inversely proportional to `diagnal`. A `1:2:4` diagonal ratio predicts a `1:2:4` on-screen pixel-size ratio for the same object, i.e. Near should measure ~4× Far and ~2× Medium.

**These match, within ~2-3%** (104/27=3.85 and 114/29=3.93 vs. predicted 4.00; 52/27=1.93 vs predicted 2.00; 104/52=2.00 exactly). The residual few percent is consistent with the ±1px trim variance noted in §2, not a scale mismatch.

**This is the core validation of the spike**: a parameter read from FreeSO's own 3D-mode camera code, applied as a prediction, was checked against independently measured pixel data from a completely different code path (the 2D pre-rendered sprite decoder) — and they agree. High confidence this camera is the right one, not a coincidentally-similar unrelated system.

## 4. Depth buffer

### 4a. Measured: raw z-byte statistics (this object)

| Zoom/Dir | total px | bg (255) px | non-bg range |
|---|---|---|---|
| Far, all 4 dirs | 754–783 | 146–165 | [135–212] |
| Medium, all 4 dirs | 2964–3016 | 625–684 | [133–212] |
| Near, all 4 dirs | 11639–11856 | 2494–2678 | [133–212] |

Across **all 12 frames**: non-background z values fell in **[133, 212]** — never below 32, never exactly 0. Background/transparent pixels were **always exactly 255**, and only 255 (no other high values near it).

### 4b. Measured: `< 32` threshold does not apply to this object

`SPR2Frame.CopyZToAlpha` (`SPR2.cs:510-516`) treats `ZBufferData[i] < 32` as "fully transparent." On this object, **zero pixels** had `z < 32` among non-background pixels — the measured floor was 133. So for ordinary furniture, the low end of the byte range (0–31) is simply unused, not a reserved band this object touches. `CopyZToAlpha`'s own doc comment says "used by water tile" — **inference**, not confirmed here: this threshold is very plausibly a floor/water-tile-specific hack (where legitimate z values could plausibly get very close to 0, e.g. flush-to-camera surfaces), not a general-purpose reserved range every object must respect. Would need to decode an actual floor/water tile sprite to confirm — **not done in this spike**, flagging as an open item rather than guessing.

### 4c. Code-derived: depth direction convention

Traced the actual compositing math in `TSOClient/tso.content/ContentSrc/Effects/2DWorldBatch.fx`:
- `dpth(v)` reads the sampled depth texture's alpha (SM4) or red (else) channel — and `SPR2Frame.GetZTexture` uploads `ZBufferData` directly into an `Alpha8` texture, so `dpth ∈ [0,1] == ZByte/255` exactly, no transform.
- `depthCalc`: `difference = (1 - dpth) / 0.4`, then `depth = backDepth + difference * frontDepth_delta`.
- At `ZByte=255` (`dpth=1`): `difference=0` → `depth = backDepth` (pinned to the sprite quad's "back" reference point).
- At `ZByte=0` (`dpth=0`): `difference=2.5` → `depth` pushed *past* the "front" reference point.

**Convention (code-derived, high confidence): lower z-byte = nearer camera; 255 is a hard sentinel meaning "as far back as possible,"** used deliberately for transparent/background pixels so they never win a depth comparison against real geometry sitting in front of them. This is a conventional near=low/far=high depth-buffer convention, not inverted.

**Not linear across a fixed global range.** The shader interpolates each pixel's z-byte between that *specific sprite's own* projected "back" and "front" reference points (`backProjection`/`frontProjection`, computed per-vertex from `WorldOffset`/`offToBack`/`dirToFront`) — i.e. the 0–255 byte encodes a sprite-local, relative depth gradient (linear within that sprite's own front-to-back span), not an absolute world-space depth in fixed units. This explains why this object's real data clustered tightly in [133,212] rather than spanning the full byte range: the encoder already scoped 0–254 to this object's own shallow depth extent, and only 255 is the universal absolute sentinel.

**Practical implication for the renderer**: a generated z-buffer doesn't need to hit a specific absolute numeric convention — it needs a smooth, monotonically-increasing-with-depth gradient across the *object's own* silhouette, scaled to use a reasonable fraction of 0–254 (this object used roughly the 133-212 band, ~31% of the low-254 range — not the full range), with 255 reserved exclusively for background/transparent pixels. **This significantly de-risks the depth-generation step**: it does not need to match TSO's exact absolute depth units, only produce internally-consistent relative depth within each rendered object.

## 5. Absolute scale: floor-tile measurement

The gap flagged in the first pass — no way to convert relative zoom ratios into an absolute pixels-per-world-unit number without an object of exactly known world size — is closed. **Code-derived, not decoded from a sprite chunk**: `TSOClient/tso.world/Components/FloorComponent.cs:16-18` hardcodes the exact destination pixel rectangle the renderer blits one floor tile's pre-baked sprite into, per zoom:

```
FLOORDEST_FAR    = (2, 79,  31, 16)   // width=31,  height=16
FLOORDEST_MED    = (3, 158, 63, 32)   // width=63,  height=32
FLOORDEST_NEAR   = (5, 316, 127, 64)  // width=127, height=64
```

A floor tile's world footprint is exactly `WorldUnitsPerTile` = 3×3 world units (§0/code-derived, not assumed) — this is the one object type in the whole content set with a world size that's a documented constant rather than an unknown 3D-model dimension, which is exactly why it's the right calibration reference instead of an arbitrary furniture piece.

**These destination rects, not `WorldCamera`'s `isoScale`/`diagnal` formula, are the authoritative absolute-scale source.** Important distinction worth being explicit about: `WorldCamera` is FreeSO's *live 3D-mode* camera — useful for confirming the *relative* 1:2:4 zoom ratio (§3), which it predicted correctly — but it's the FreeSO team's own re-calibration of a live camera to *look like* the pre-baked 2D sprites, not a value mathematically guaranteed to reproduce the original Maxis-baked sprites' exact pixel scale. Plugging `WorldCamera`'s `isoScale` formula through gives a Near-zoom absolute scale roughly **2× off** from what `FLOORDEST_NEAR` implies — flagging that discrepancy rather than silently picking a number, and treating `FLOORDEST_*` as ground truth since it's the literal, hardcoded destination rectangle the renderer uses to draw the actual pre-baked 2D asset, with no intermediate camera-math layer to second-guess.

**Derived absolute scale** (using the tile's world-space diagonal — a tile viewed at 45° yaw shows its diagonal edge-on as the horizontal screen axis — `3 × √2 = 4.2426` world units):

| Zoom | Tile diamond W×H (px) | Horizontal px/world-unit |
|---|---|---|
| Far | 31×16 | 31 / 4.2426 = **7.31** |
| Medium | 63×32 | 63 / 4.2426 = **14.85** |
| Near | 127×64 | 127 / 4.2426 = **29.93** |

**Cross-check (independent, not circular)**: for an isotropic orthographic projection with a flat ground-plane tile pitched 30° (§7-code-derived pitch value from an *entirely different* code path than `FLOORDEST_*`), the diamond's screen-space vertical extent should equal `world_diagonal × sin(pitch) × px_per_world_unit`. At Near: `4.2426 × sin(30°) × 29.93 = 4.2426 × 0.5 × 29.93 = 63.47`. **Measured `FLOORDEST_NEAR.Height = 64`** — a 0.8% match. Two independently-sourced numbers (the horizontal scale from the destination-rect width, the 30° pitch from `WorldCamera`'s rotation matrix) predicted the third (destination-rect height) to within 1%. This is strong confirmation the pitch and the absolute-scale numbers are both correct and mutually consistent, not just individually plausible.

**Confidence: High** for the pixels-per-world-unit values above — code-derived (not decoded, so no per-pixel trim-padding noise) and cross-validated against the independently-derived pitch angle.

## 6. Second reference object: garden lamp (0xBA67EAD2, `lampgarden`)

Chosen deliberately for a different silhouette class — thin, roughly cylindrical, vertical — to check whether §3's findings were an artifact of the box's flat-face geometry.

**Measured dimensions:**

| Zoom | W×H (px) |
|---|---|
| Far | 6×12 |
| Medium | 10×23 |
| Near | 21×44 |

**Zoom ratios**: Medium/Far = 10/6=1.67, 23/12=1.92. Near/Medium = 21/10=2.1, 44/23=1.91. Near/Far = 21/6=3.5, 44/12=3.67.

**These are noisier than the box's (§3's 1.93-2.00 / 3.85-3.93) but do not diverge from the 1:2:4 law** — the noise is explained by the object's small absolute size: at Far zoom the lamp is only 6px wide, so a single pixel of trim/padding is a ~17% swing, versus ~4% on the box's 27px-wide Far frame. **Important finding, not a contradiction**: the 1:2:4 scale law holds, but confidence in any single small object's measured ratio should scale with its pixel count — small objects need either a larger reference or averaging across several to pin the ratio precisely. Recommend generators sanity-check scale against a larger reference object, not a small one like this lamp.

**Unplanned but useful finding**: all 4 directions (`RightBack`/`RightFront`/`LeftFront`/`LeftBack`) resolved to the **exact same `SpriteFrameIndex`** at every zoom (e.g. all four Near-zoom images point at frame 0). The lamp's DGRP simply reuses one frame across all 4 directions rather than storing 4 near-identical renders — expected for a rotationally-symmetric object, and **directly useful for the generator**: cylindrically-symmetric pieces (most lamps, plants, columns) only need 3 unique renders (one per zoom), not 12, cutting render cost by 4× for that whole object class. Worth designing the encode path (§2 of `ART-PIPELINE-DESIGN.md`) to support a DGRP where multiple `DGRPSprite` direction entries point at one shared `SpriteFrameIndex`, rather than assuming every object needs 12 distinct frames.

## 7. Lighting: measured face-brightness ratios

Method: crude but "approximate is fine" per the ask — split each Near-zoom frame's non-transparent silhouette into a top third (candidate top face) and, in the bottom two-thirds, a left half / right half (candidate left/right side faces), by pixel position only (no real 3D face segmentation). Luminance = standard `0.299R + 0.587G + 0.114B`. Tool: `PrintLightingAnalysis` in `FSO.ArtCalibration/Program.cs`, runs automatically at Near zoom.

**Cardboard box (clean 3-face box geometry — this measurement is trustworthy):**

| Direction | top | left | right | top/left | top/right | left/right |
|---|---|---|---|---|---|---|
| RightBack | 175.9 | 102.2 | 177.1 | 1.72 | 0.99 | 0.58 |
| RightFront | 173.9 | 97.6 | 181.9 | 1.78 | 0.96 | 0.54 |
| LeftFront | 177.5 | 99.7 | 178.0 | 1.78 | 1.00 | 0.56 |
| LeftBack | 174.8 | 106.2 | 179.3 | 1.65 | 0.97 | 0.59 |

**Measured, consistent across all 4 directions** (top/right ratio is 0.96-1.00 in every direction; left/right is 0.54-0.59 in every direction — this consistency across independently-rendered directions is itself evidence the signal is real, not noise): **top face and right-visible face are lit almost identically (~175-182 luminance); the left-visible face is roughly half as bright (~98-106 luminance), ≈55-59% of the right face's brightness.** Directionally this is consistent with a single dominant light from the upper-right — a fixed key-light direction rather than per-direction-relative lighting (since the *same* left/right darker-brighter pattern holds regardless of which of the 4 yaw directions is rendered, the light is fixed in world/screen space, not attached to the camera or the object's facing).

**Garden lamp (weak signal — flagging the limitation, not overclaiming):** top≈85.3, left≈93.9, right≈92.4, all within ~10% of each other. This is expected, not a contradiction of the box's finding: a thin cylindrical surface has a continuously-varying normal, not 3 flat faces, so the crude top/left/right positional split doesn't correspond to physically distinct faces the way it does on a box — it's mostly sampling similar curved-surface lighting wherever it falls. **The lamp result should not be read as "lighting is flatter/more uniform than the box's"; it's a measurement-method limitation on non-box geometry, not a finding about the lighting itself.**

**Confidence: Medium.** The direction (upper-right key light, left side darker) is a real, repeatable signal on the box, consistent across all 4 renders. The exact ratio (~0.55-0.6 for the dark side) is a reasonable starting point for a generator's lighting rig, but was measured on one geometry class (flat-faced box) with a crude segmentation method — treat as "close enough that generated furniture won't look obviously mismatched," not as a precise photometric calibration. A generator's renderer should replicate a single fixed-direction key light from the upper-right (matching the box's top/right ≈ top/left×1.7-1.8 pattern) plus enough ambient fill that no face goes fully black.

## 8. What this spike still did **not** determine

- **Whether the `< 32` z-threshold is floor/water-specific** — still not confirmed against an actual decoded floor/water sprite (this spike's floor-tile work used only the hardcoded destination-rect constants, not a decoded floor SPR2 chunk with its own z-buffer). Low-priority: §4's practical conclusion (relative depth per-object is what matters) doesn't depend on resolving this.
- **Exact lighting ratio as a precise photometric value** — §7's ratio is a reasonable starting point, not a rigorously derived constant; flagged as Medium confidence, not High.
- **Why `WorldCamera`'s `isoScale` formula disagrees ~2× with the `FLOORDEST_*`-derived absolute scale** — noted in §5, not resolved. Doesn't block the pipeline (the destination-rect numbers are authoritative and don't depend on resolving the discrepancy), but worth a follow-up if anyone later needs to understand `WorldCamera`'s live-3D-mode calibration for its own sake.

## 9. Confidence summary (updated)

**High confidence, ready to build against**: orthographic projection, 30° pitch / 45°-offset 90°-step yaw, 1:2:4 per-zoom scale ratio (cross-validated three ways: box measurement, lamp measurement, and code), depth direction convention (low=near, 255=sentinel), the practical implication that generated z-buffers only need internally-consistent relative depth, and — new this pass — **absolute pixels-per-world-unit scale** (7.31 / 14.85 / 29.93 at Far/Medium/Near), cross-validated against the independently-derived pitch angle to within 1%.

**Medium confidence**: lighting direction and rough ratio (upper-right key light, far side ≈55-60% brightness of near/top) — real signal, crude measurement method, one geometry class.

**Genuinely open, non-blocking**: the `<32` z-threshold's scope, and the `WorldCamera.isoScale` vs. `FLOORDEST_*` discrepancy. Neither blocks moving to a test render.

## Appendix: draft Blender calibration rig (updated — absolute scale filled in)

Still best-effort and not verified against an actual render — §5-7's derivation is what should be trusted; this script is a starting point, not a finished deliverable.

```python
import bpy
import math

# Camera: orthographic, 30 deg pitch (X), yaw per WorldRotation.TopLeft = 315 deg (Y)
# per WorldCamera.GetRotationMatrix() / GetInnerRotationMatrix() — TSOClient/tso.world/Utils/WorldCamera.cs
bpy.ops.object.camera_add(location=(0, 0, 10))
cam = bpy.context.object
cam.data.type = 'ORTHO'

# Absolute scale, from §5 (floor-tile destination-rect measurement, cross-validated against
# pitch to within 1%) — this REPLACES the earlier placeholder ortho_scale=3.0 guess.
# blender ortho_scale is world-units spanned by the smaller viewport dimension; these are
# expressed instead as px-per-world-unit for clarity — convert against your render resolution.
PX_PER_WORLD_UNIT = {"far": 7.31, "medium": 14.85, "near": 29.93}

pitch_deg = 30.0
yaw_deg = 315.0  # WorldRotation.TopLeft; use 225/135/45 for the other 3 directions

cam.rotation_euler = (
    math.radians(90 - pitch_deg),  # convert "look-down pitch" to Blender's camera-forward-is--Z convention
    0.0,
    math.radians(yaw_deg),
)

bpy.context.scene.camera = cam

# Lighting rig, from §7: single fixed-direction key light from the upper-right (screen space),
# dark side ~55-60% of the bright side's brightness, plus ambient fill so nothing goes fully black.
bpy.ops.object.light_add(type='SUN', location=(5, -5, 8))
key = bpy.context.object
key.data.energy = 3.0
# TODO: tune angle so the box's left/right faces reproduce the measured ~0.55-0.6 ratio;
# not solved analytically here, eyeball-match against the reference render.
```

## 10. End-to-end test render

**No Blender in this environment.** Rather than block on installing it, built a from-scratch minimal software rasterizer (`FSO.ArtCalibration/RenderTest.cs`) that implements exactly the parameters derived in §1-9 directly and controllably — orthographic projection, 30° pitch, 45°-offset 90°-step yaw, the derived per-zoom px/world-unit scale, and the measured screen-space lighting ratios. This is a substitution for Blender, not a shortcut around the actual test: the rendered frames go through the **real production path** — `SPR2FrameEncoder.WriteFrame` (unmodified), assembled into `DGRP`/`SPR2`/`PALT`/`OBJD` chunks the same way `PackBuilder.cs` does, written to a real `.iff` on disk, then **read back through a fresh `IffFile.Read`** (not reusing in-memory objects) and decoded through the same `SPR2Frame.Decode` path used to measure the real object. The only non-production piece is the quantizer (`SimpleQuantizer.cs` — a flat per-color lookup table, exact since the render is flat-shaded with no gradient, replacing FSO.IDE's `SpriteEncoderUtils.QuantizeFrame`, which depends on `System.Drawing`/`SimplePaletteQuantizer` and isn't portable to this macOS dev environment without extra native deps).

### Method: solve once, predict eleven

Box world dimensions (`Xw`, `Yw`, `Zw`) were **solved from exactly one real measurement** — the real cardboard box's Near-zoom RightFront frame (103×114px) — via the projection formulas derived in §5 (`screen_width = (Xw+Zw)/√2`, `screen_height = (Xw+Zw)/√2 · sin(30°) + Yw·cos(30°)`), plus a symmetric-footprint assumption (`Xw=Zw`). Solved: `Xw=2.433, Yw=2.411, Zw=2.433` world units. That consumes the 2 measured numbers with 2 unknowns after the symmetry assumption — **zero remaining degrees of freedom** from that one measurement. The other 11 frames (2 more directions × 3 zooms, plus the same direction at Far/Medium) are genuine held-out predictions: same box, same camera math, only the zoom's px/world-unit constant changes.

### Result

First pass had a systematic ~1-8% oversize on every frame. Diagnosed before reporting a number (not just eyeballing "looks close"): the error shrank as a percentage at larger sizes (Far ~4-8%, Near ~1-2%) while staying roughly 1-2px in absolute terms — the signature of a **constant additive offset**, not a scale error (a scale error would hold constant as a *percentage*, not shrink at larger sizes). Traced it to my rasterizer's canvas allocation, which padded `+2px` onto every frame's width/height as a safety margin. Removed it, re-ran:

| Zoom/Dir | Real W×H | Generated W×H | ΔW | ΔH |
|---|---|---|---|---|
| Far/RightBack | 27×29 | 26×28 | -3.7% | -3.4% |
| Far/RightFront | 26×29 | 26×28 | 0.0% | -3.4% |
| Far/LeftFront | 27×29 | 26×28 | -3.7% | -3.4% |
| Far/LeftBack | 26×29 | 26×28 | 0.0% | -3.4% |
| Medium/RightBack | 52×57 | 52×57 | 0.0% | 0.0% |
| Medium/RightFront | 52×58 | 52×57 | 0.0% | -1.7% |
| Medium/LeftFront | 52×57 | 52×57 | 0.0% | 0.0% |
| Medium/LeftBack | 52×57 | 52×57 | 0.0% | 0.0% |
| Near/RightBack | 104×114 | 103×115 | -1.0% | +0.9% |
| Near/RightFront | 103×114 | 103×114 | 0.0% | 0.0% *(used to solve size)* |
| Near/LeftFront | 104×114 | 103×115 | -1.0% | +0.9% |
| Near/LeftBack | 103×113 | 103×114 | 0.0% | +0.9% |

**All 11 held-out predictions land within 0-1px** (0.0-3.7% width, 0.0-3.4% height) of the real object, after fixing one diagnosed, understood bug in the test harness itself — not in the derived camera math, which needed no adjustment. This is the strongest evidence in this whole calibration effort: a box solved from a single measurement, rendered through genuinely independent projection code, correctly predicted eleven more measurements it never saw.

**Z-buffer**: real object's per-frame non-background range varies [133-212] across frames (§4a); generated frames are a fixed [140-210] by construction — I chose that band because §4a's practical conclusion is that z only needs to be *internally consistent per object*, scaled into a reasonable low-254 sub-range, not matched to a specific absolute value. This round-trips correctly (the value I wrote is the value that comes back out through the real encoder/decoder), and lands inside the real object's observed band, but **it is not an independent validation of a z-generation algorithm** — I chose the number, I didn't derive it from the render geometry. A real generator would need to derive per-pixel depth from the rasterizer's own depth buffer (which `RenderTest.cs` does compute, `depthBuf`, before the final byte-remap) rather than a fixed linear stretch — that's a small, well-understood next step, not an open risk.

**Lighting**: mechanically round-tripped correctly (the quantizer preserved the 3 flat face colors through encode/decode without loss, confirmed by inspection) but **this is not an independent lighting validation** — the render directly encodes §7's measured top/left/right ratios as a generation rule (classify each visible side face by mean screen-X, apply the measured ~0.57 vs ~0.98 brightness split), so the "test" here is only "did the color survive the pipeline," not "does an independently-derived light model reproduce the real ratios." That's consistent with how §7 was scoped from the start (measured ratios as a starting point for a generator, not a from-first-principles light model).

### What this does and doesn't prove

**Proves**: the derived scale law (§3, §5) and camera geometry (§5) are correct and precise enough to build a real generator against — not just "roughly isometric," but predictive to within a pixel across 3 zoom levels and 4 directions from a single solved measurement. Also proves the full mechanical pipeline — `SPR2FrameEncoder` → `.iff` → `IffFile.Read` → `SPR2Frame.Decode` — works for **generated**, not just **cloned**, content, using the real production encoder unmodified.

**Doesn't prove**: an independently-derived lighting or depth-generation algorithm (both were fed forward from measured/chosen values, not solved fresh here) — those are the next real pieces of engineering work, not further calibration. Also doesn't prove visual fidelity against the real object's actual silhouette shape beyond a flat box (no bevels, no surface detail) or its real color palette (used a plausible placeholder brown, not the real object's actual quantized colors) — neither was in scope for this test, which was about geometry/scale, not art direction.

### Files

- `PackTools/FSO.ArtCalibration/RenderTest.cs` — the rasterizer + encode/decode/compare harness. Run via `dotnet FSO.ArtCalibration.dll rendertest [gameDir] [guidHex]`.
- `PackTools/FSO.ArtCalibration/SimpleQuantizer.cs` — the portable quantizer substitute.

## 11. Promoted to a real component: real depth, real lighting, first generated chair

Per direction: the rasterizer stays (no Blender), promoted out of "throwaway measurement tool" into `PackTools/FSO.PackCompiler/ArtGen/` — a real library, reusable by anything in the compiler/MCP stack, not just this report's own CLI. Contents: `Camera.cs`/`Mesh.cs`/`Renderer.cs` (the projection + rasterization + lighting + depth engine), `SpriteAssembler.cs` (renders all 12 frames and assembles them into a real DGRP/SPR2/PALT/OBJD `.iff`, generalized from §10's one-off box code), `SimpleQuantizer.cs`, `PngWriter.cs` (a from-scratch RGBA PNG encoder using only `System.IO.Compression.ZLibStream` — no `System.Drawing` — so generated frames can be eyeballed without launching the game), and `ChairGenerator.cs`, the first parametric furniture piece.

### 11a. Real depth (replacing §10's fixed band)

`Renderer.cs` now normalizes each sprite's own computed depth buffer (`dMin`/`dMax`, already an affine function of screen position per face — this was already being computed in §10, just not used for the final byte mapping) into a fixed output band, clear of the `<32` reserved band (§4b) and never touching `255` (the background sentinel, §4c) — instead of a fixed placeholder band.

**The band is `[135, 210]`, not the full `[35, 250]` it first used.** Spanning the whole low-254 range is legal by §4b/§4c but wrong in practice: `DGRP3DMesh` reads the z-byte spread as extrusion depth, so a 215-wide span extruded ~3× too far and the object exploded into long triangles in Full 3D. `[135, 210]` is the band base-game furniture actually occupies (§4a measured `[133, 212]` on the cardboard box across all 12 frames). Asserted by `SpriteCageOffsetTests`.

**Validated on a known-slanted surface**, per the ask, using a standalone 20°-tilted plane (independent of the chair, so the test isn't entangled with other geometry): rendered, round-tripped through the real encoder → `.iff` → real decoder, then sampled a vertical scanline through the tilt. Result: **34 samples, z-range [65-214], monotonic across the full scanline, zero samples below 32, zero samples touching 255 except true background.** Depth behaves exactly as it should for a real slanted surface.

### 11b. Real lighting (replacing §10's ratio lookup)

Real Lambertian shading (`max(0, dot(normal, light)) `, plus ambient), with the light direction expressed in **camera-space** coordinates (components along the camera's own Right/Up/ToCamera basis, not world XYZ) — this is what makes one fixed rig reproduce §7's finding that the same brighter/darker pattern holds across all 4 world-yaw directions, since a camera-space-fixed light stays fixed relative to the screen regardless of which way the object is facing.

**The light direction was solved analytically, not guessed or grid-fit.** The box's 3 visible faces (top + 2 sides) have mutually orthogonal normals, so their 3 Lambertian diffuse values are constrained by `d_top² + d_right² + d_left² = 1` (a unit-sphere condition, since they're literally the light vector's components in that orthonormal frame). Solving that constraint against the real measured ratios (top/right=0.96, left/right=0.54, §7) with a small fixed ambient (0.10) gives an exact target diffuse triple, which was converted into a light direction using the box's own real face normals (read from the mesh at runtime, not hand-derived, to avoid a repeat of the sign-error class of bug this session already hit twice). Rendering with that solved light reproduced **top/right=1.04 (target 0.96), left/right=0.53 (target 0.54)** — solved once from 2 numbers, then independently confirmed by rendering, not fit pixel-by-pixel to match.

**A real bug surfaced and was fixed during this validation, not papered over**: the first attempt produced only 2 distinct face colors instead of 3 — the top face was being back-face-culled entirely. Traced to `Mesh.AddBox`'s top/bottom quad winding order producing inward-facing normals (verified by hand cross-product calculation, then confirmed by the fix). Fixed the winding, re-ran, got 3 correctly-lit faces. `RenderTest.cs`'s box (§10) built its faces with separate, correct hand-written code and was never affected by this bug — the 11/11-predictions-within-1px result from §10 still holds unchanged after this fix (re-verified).

### 11c. First original furniture: a parametric chair

`ChairGenerator.cs` — seat slab, backrest tilted 12° back, 4 tapered legs, optional arms (unused in this render), parameterized dimensions and colors. A generic mid-century-lounge-chair *category* (tapered legs, slab seat, angled back) — no specific branded design reproduced, no brand names anywhere in the code or output.

Rendered through the full pipeline (`ChairGenerator` → `Renderer` → `SpriteAssembler` → real `.iff` → real decode), all 12 frames decoded cleanly:

| Zoom | RightBack | RightFront | LeftFront | LeftBack |
|---|---|---|---|---|
| Far | 17×25 | 17×25 | 17×24 | 17×24 |
| Medium | 33×51 | 33×51 | 33×47 | 33×47 |
| Near | 66×103 | 66×103 | 66×95 | 66×95 |

All z-ranges `[35-250]` (the derived band), no anomalies. PNG dumps written per frame (`chair_<Zoom>_<Direction>.png`) — sent to Kat directly. **It reads as a real chair**: visible seat, tapered legs, angled backrest, and the lighting split is visually apparent (right-facing side visibly brighter than left, matching §7/§11b).

### Files (this section)

- `PackTools/FSO.PackCompiler/ArtGen/` — the promoted library: `Camera.cs`, `Mesh.cs`, `Renderer.cs`, `SpriteAssembler.cs`, `SimpleQuantizer.cs`, `PngWriter.cs`, `ChairGenerator.cs`.
- `PackTools/FSO.ArtCalibration/LightingValidation.cs` — the analytical light-direction solve + validation.
- `PackTools/FSO.ArtCalibration/ChairGenTest.cs` — chair generation + depth-ramp sanity check + PNG dump driver. Run via `dotnet FSO.ArtCalibration.dll genchair [outDir]`.
- Known minor cleanup item, not yet done: `RenderTest.cs` (§10) still has its own private `Vec3`/projection code, duplicating what's now in `ArtGen/Camera.cs` — left alone since it's a previously-validated, working artifact, and touching it risked the kind of sign-error bug §11b just caught elsewhere, for no functional benefit.

## Files

- `PackTools/FSO.ArtCalibration/` — the measurement + render-test + chair-generation CLI (kept for re-running against further reference objects, lighting analysis, and the end-to-end render tests).
- `PackTools/FSO.PackCompiler/ArtGen/` — the real, reusable rendering/generation library.
- This report: `PackTools/ART-PIPELINE-CALIBRATION.md`.

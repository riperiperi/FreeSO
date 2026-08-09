# Original Art Pipeline — Design Draft

Status: design only, no code. Goal: produce **original** object art (owned outright, no EA assets) that drops into the pack compiler alongside authored behavior. Complements `SCHEMA.md`/`MCP-DESIGN.md` (behavior) and `AppearanceCloner` (borrowed appearance from base game).

Approach: **parametric 3D furniture, rendered to TSO's sprite format.** Not AI-generated 2D sprites — see §1 for why.

## 1. Why 3D→render, not direct sprite generation

A TSO object isn't one image. Each DGRP chunk holds exactly **12 images** — 4 directions × 3 zoom levels (`DGRP.cs`, "A DGRP chunk always consists of 12 images") — and each sprite frame carries a **per-pixel z-buffer** (`SPR2Frame.ZBufferData`, one byte per pixel, enabled by frame flag `0x02`) that the engine uses to depth-sort objects against sims and walls.

Asking an image model for that directly means demanding 12 mutually-consistent views of the same object plus per-pixel depth. That's the failure mode, not the feature. Rendering from a 3D source gives all 12 views and the depth pass for free, deterministically, from one asset.

Secondary reason: **coherence**. A furniture line reads as designed when everything shares one lighting rig, palette, and proportion system. Independent generations don't.

## 2. Why recreate rather than source models

At TSO's render scale an object occupies roughly the footprint of a lot tile on screen — small enough that surface detail, bevels, and topology quality are invisible. What survives at that size is **silhouette, proportion, and color**.

Downloaded/purchased models are typically far over-detailed for this, and carry license questions that conflict with the content-ownership goal. Most furniture is boxes, cylinders, and tapers — cheaply expressed as parametric geometry:

```
chair(seat_h, back_angle, leg_style, arms, wood, upholstery)
table(top_shape, top_thickness, base_style, height)
lamp(base, stem_h, shade_shape)
```

One generator yields a family. Vary parameters → visually distinct pieces that still belong to the same world.

**On references**: real retail furniture (West Elm, RH, etc.) is fine as *reference and vibe*. Generic forms — a mid-century tapered-leg lounge chair, a slab-top dining table — are categories, not property. Do not reproduce a signature silhouette 1:1, and never use brand names or branding in-game or in asset metadata.

## 3. What already exists (do not rebuild)

Verified in this repo:

- **`SPR2FrameEncoder.WriteFrame(frame, output)`** (`tso.files/Formats/IFF/Chunks/SPR2FrameEncoder.cs`) — writes an `SPR2Frame` (pixels + z-buffer) into the chunk format. The encode path is done.
- **Palette quantization** — `SPR2FrameEncoder.QuantizeFrame` is a pluggable delegate (`QuantizerFunction`); a working implementation exists as `SpriteEncoderUtils.QuantizeFrame`, wired up in `FSO.IDE/Program.cs:71`. SPR2 is palette-indexed via `PALT` (256 colors), so quantization is mandatory, not optional.
- **Chunk assembly into an .iff** — `AppearanceCloner` (just landed) already demonstrates building DGRP/SPR2/PALT into a target `IffFile`, including the constraint that **DGRP must resolve its SPR2 through its own `ChunkParent`** — sprites must live in the same .iff, not be cross-referenced.
- **`DGRPSprite.SpriteOffset` (Vector2) / `ObjectOffset` (Vector3)** — per-sprite placement within a draw group. These control how the rendered bitmap sits on the tile.

So the pipeline's engine-facing half is largely solved. The new work is upstream: geometry → correctly-projected pixels + depth.

## 4. The unsolved part: matching TSO's projection

The sprites must look *native*, not merely isometric. Getting camera angle, scale, and lighting subtly wrong is the difference between "a new object" and "an object from a different game."

**Do not guess these values — derive them empirically.** Method:

1. Pick a known base-game object with simple, readable geometry (a plain table or box-like object).
2. Extract its 12 SPR2 frames and record exact pixel dimensions per zoom level and per direction.
3. Model a matching primitive at known world dimensions (one lot tile = a known unit in `LotTilePos` terms).
4. Solve for the camera: projection type (near-certainly orthographic), rotation, and per-zoom scale that reproduce the reference frames' dimensions and edge angles.
5. Match lighting by comparing rendered face brightness ratios against the reference's top/left/right faces.

Direction encoding is documented in `DGRP.GetImage`: `RightBack = 0x01, RightFront = 0x04, LeftFront = 0x10, LeftBack = 0x40` — 4 rotations, combined with 3 zoom levels, for the 12 required images.

Deliverable of this step is a **calibrated render rig** (Blender scene + Python script): input geometry, output 12 RGBA frames + 12 depth passes at correct dimensions.

## 5. Depth buffer

`ZBufferData` is one byte per pixel. The renderer's depth pass must be normalized into the same 0–255 space and orientation the engine expects. Worth verifying against a decoded base-game sprite's z-buffer before trusting a generated one — decode a reference frame, inspect its depth ramp across a known flat surface, and match that convention.

Note `SPR2Frame` line ~514 treats `ZBufferData[i] < 32` as transparent when composing alpha, which implies meaningful reserved range at the low end — confirm empirically rather than assuming a naive 0–255 linear map.

## 6. Proposed build order

1. **Calibration spike** — §4's derive-the-camera exercise, on one reference object. Output: render rig + a single generated sprite that visually matches a base-game object placed beside it in-game. This is the risk-retiring step; everything else is downstream of it.
2. **Encode path** — rendered frames → `SPR2Frame` (via existing encoder) → DGRP → .iff, reusing `AppearanceCloner`'s chunk-assembly approach. Verify in-client that a generated object renders at all.
3. **Parametric generators** — chairs first (highest-value, most variations), then tables, beds, lamps, storage.
4. **Compiler integration** — a schema path for `appearance.generated` (parameters, not a GUID) alongside today's `clone_from_guid`, so an authored object can specify its own look.
5. **Agent-facing** — only after 1–4 work: let the authoring agent choose or parameterize appearance conversationally.

Steps 1–2 are the real risk. Step 3 is volume work once the rig is calibrated.

## 7. Open questions

- Multi-tile objects need one DGRP per tile — out of scope for a v1 furniture set (single-tile pieces only), but the generator shouldn't architecturally preclude it.
- Does a generated object need `OBJD` graphics fields beyond `BaseGraphicID`/`NumGraphics` (which `AppearanceCloner` already sets) to animate or accept color variants? Check how base-game objects express color/material variants — likely additional draw groups rather than separate objects.
- Palette strategy: per-object palettes (simpler, more colors per piece) vs. one shared world palette (more coherent, smaller). Probably per-object for v1; revisit if coherence suffers.

# Pack Schema v0.1

A **pack** is the unit of creation and sharing: one JSON file (plus optional assets) describing objects, their interactions, and their behavior. Packs are authored by agents on behalf of players, compiled to `.iff` by the pack compiler, and loaded by the FreeSO engine like any base-game object.

Design rules:
- **Names over numbers.** Primitives, scopes, operators, and branch targets are written as names/labels. The compiler owns the byte layouts (from `tso.files` + the vocabulary reference).
- **Named booleans over flag bytes.** Every flag bit is a named field defaulting to false.
- **Dialect-tagged.** `"engine": "tso"` (v0.1 supports tso only; ts1 later).
- **Fail loud.** The compiler validates everything it can (tree size ≤ 253 nodes, locals ≤ 255, label resolution, enum values, GUID collisions) and refuses to emit on any error — because the VM itself fails silently.

## Top level

```json
{
  "schema": "fso-pack/0.1",
  "engine": "tso",
  "pack": {
    "id": "gossip-gnome",
    "name": "Gossip Gnome",
    "author": "kat",
    "version": "1.0.0",
    "description": "A garden gnome Sims can talk to. Listens. Judges silently. Restores a little social."
  },
  "objects": [ ... ]
}
```

## Object

```json
{
  "id": "gossip_gnome",
  "guid": "0x6B4F0001",
  "name": "Gossip Gnome",
  "price": 120,
  "category": "decorative",
  "appearance": { "clone_from_guid": "0x..." },
  "attributes": ["times_gossiped"],
  "strings": {
    "dialog": { "1": "The gnome listens intently.", "2": "Nice." }
  },
  "interactions": [
    {
      "name": "Gossip",
      "action": "gossip_action",
      "test": "gossip_test",
      "allow": { "visitors": true, "owner": true },
      "autonomy": { "advertised_motives": { "social": 20 } }
    }
  ],
  "trees": { ... },
  "entry_points": { "main": "main_loop", "init": "init" }
}
```

- `guid`: unique object id. Compiler reserves a community GUID range; collides = error.
- `category`: Buy Mode catalog category. Objects in `{ContentDir}/Objects/*.iff` are registered but **invisible in Buy Mode** unless `{ContentDir}/Objects/catalog_downloads.xml` also has a `<P g="GUID" s="index" p="price" t="tags" n="Name" />` entry (`tso.content/WorldObjectCatalog.cs` `Init()`); the compiler emits these entries for you. Category names map to the `s` index per Buy Mode's `CategoryMap` (`tso.client/UI/Panels/UIBuyMode.cs` `InitCategoryMap()`):

  | name | s |
  |---|---|
  | `seating` | 12 |
  | `surfaces` | 13 |
  | `appliances` | 14 |
  | `electronics` | 15 |
  | `skill` | 16 |
  | `decorative` | 17 |
  | `misc` | 18 |
  | `lighting` | 19 |
  | `pets` | 20 |

  Omitted category defaults to `misc` for the catalog entry.
- `tags`: optional array of search-tag strings, joined into the catalog entry's `t` attribute (e.g. `"tags": ["gnome", "gossip"]` → `t="gnome, gossip"`).
- `appearance.clone_from_guid`: v0.1 borrows sprites from an existing base-game object. The compiler copies the source's `DGRP` draw groups, `SPR2` sprites and `PALT` palettes **inline into the emitted `.iff`**, and sets `BaseGraphicID`/`NumGraphics` to point at them. Inlining is required, not an optimization: a compiled pack loads as a `Standalone` object, which `WorldObjectProvider` gives no sibling `.spf`, and `DGRP.GetTexture` resolves its sprites through its own `ChunkParent` — so a draw group that references sprites in another file silently renders nothing.
  - **Cloning needs the base game content on disk at compile time.** Supply it via `PackCompilerApi.Build(pack, outDir, gameDir)`, the `FSO_VM_GAME_LOCATION` environment variable, or a default install path. Compiling *without* it still succeeds, but emits no graphics and adds a build-report note saying the object will be **invisible in the client** — it stays fully functional in the VM (interactions run, trees execute), so a headless test passes while the client shows nothing. That asymmetry is why the note exists.
- `appearance.generated`: renders **original art** from a parametric generator instead of borrowing a base-game GUID — no base game content directory needed at compile time, since nothing is copied. Mutually exclusive with `clone_from_guid` (specifying both is a compile error).

  ```json
  "appearance": {
    "generated": {
      "generator": "chair",
      "params": {
        "seat_width": 1.6,
        "seat_depth": 1.5,
        "seat_height": 1.1,
        "seat_thickness": 0.18,
        "back_height": 1.7,
        "back_thickness": 0.15,
        "back_angle_deg": 12.0,
        "leg_top_width": 0.22,
        "leg_bottom_width": 0.12,
        "arms": false,
        "arm_height": 0.6,
        "arm_thickness": 0.14,
        "wood_color": [120, 82, 48],
        "upholstery_color": [168, 140, 92]
      }
    }
  }
  ```

  - `generator`: name of the parametric generator to run — `chair`, `table`, `bed`, `lamp`, `storage`, or `primitives`; unknown names are a compile error. `params` is optional — omitted fields fall back to the generator's defaults (shown above for `chair`, below for the rest).
  - Dimensions are in world units (one lot tile = one unit); `*_color` fields are `[r, g, b]` integers 0-255. Unknown `params` fields and non-positive dimensions are compile errors — a zero or negative size produces a degenerate mesh with no error from the renderer itself, so the compiler catches it here.
  - The compiler renders all 12 views (4 directions × 3 zoom levels) for most generators, quantizes them to a palette, and assembles `DGRP`/`SPR2`/`PALT` chunks inline into the emitted `.iff`, same file-locality requirement as `clone_from_guid` above. `lamp` and a round-`table`/pedestal-base `table` are rotationally symmetric — no side of the object looks different from another — so the compiler renders only 3 unique frames (one per zoom level) and points all 4 direction entries in the `DGRP` at the same sprite, instead of rendering (and storing) the same silhouette 4 times.

- `appearance.imported`: renders **original art** from a CC0 mesh file (OBJ + MTL). Mutually exclusive with `clone_from_guid` and `generated`. Mesh paths are relative to the pack JSON file's directory.

  ```json
  "appearance": {
    "imported": {
      "mesh": "../assets/cc0/quaternius/Bathroom_Toilet.obj",
      "height": 1.4,
      "symmetric": false,
      "provenance": {
        "source": "Quaternius Ultimate House Interior Pack",
        "url": "https://opengameart.org/content/lowpoly-house-interior-pack",
        "license": "CC0",
        "retrieved": "2026-08-11",
        "model": "Bathroom_Toilet"
      }
    }
  }
  ```

  - `mesh`: path to an OBJ file. Companion MTL in the same directory supplies per-face `Kd` colours (`Kd * 255`, per `CATALOG-SOURCING.md`).
  - `height`: target height in world units after normalization (bottom at Y=0, centered on X/Z).
  - `symmetric`: when `true`, renders one view per zoom and mirrors it across all four directions (same as rotationally-symmetric generators).
  - `provenance`: license record for browser distribution. Required fields: `source`, `license`, `model`; `url` and `retrieved` recommended.
  - Batch import: `FSO.PackCompiler import-batch <manifest.csv> -o <pack.json>` — CSV columns `obj_path,name,category,height,symmetric,provenance_model[,guid]`.

  **`table`** — rectangular slab on four tapered legs, or a round top on a single pedestal:
  ```json
  "generated": {
    "generator": "table",
    "params": {
      "top_shape": "rectangular",
      "base_style": "four_leg",
      "top_width": 2.4,
      "top_depth": 1.2,
      "top_diameter": 1.6,
      "top_thickness": 0.12,
      "height": 1.15,
      "leg_top_width": 0.16,
      "leg_bottom_width": 0.10,
      "pedestal_top_radius": 0.10,
      "pedestal_base_radius": 0.32,
      "wood_color": [110, 74, 44],
      "top_color": [168, 140, 92]
    }
  }
  ```
  `top_shape` is `rectangular` or `round`; `base_style` is `four_leg` or `pedestal`. `top_width`/`top_depth` only apply when `top_shape` is `rectangular`; `top_diameter` only when `round`. `leg_top_width`/`leg_bottom_width` only apply when `base_style` is `four_leg`; `pedestal_top_radius`/`pedestal_base_radius` only when `pedestal`. `round` + `pedestal` together is the rotationally-symmetric case (see above).

  **`bed`** — frame on tapered legs, mattress, headboard, optional footboard:
  ```json
  "generated": {
    "generator": "bed",
    "params": {
      "mattress_width": 1.9,
      "mattress_depth": 2.4,
      "mattress_thickness": 0.28,
      "frame_thickness": 0.14,
      "leg_height": 0.22,
      "leg_width": 0.14,
      "headboard_height": 0.9,
      "headboard_thickness": 0.12,
      "footboard": false,
      "footboard_height": 0.35,
      "frame_color": [96, 68, 42],
      "mattress_color": [232, 228, 216],
      "headboard_color": [140, 108, 70]
    }
  }
  ```
  `footboard_height` is only checked (must be `> 0`) when `footboard` is `true`.

  **`lamp`** — tapered foot, stem, tapered shade; always rotationally symmetric:
  ```json
  "generated": {
    "generator": "lamp",
    "params": {
      "base_radius": 0.28,
      "base_height": 0.09,
      "stem_radius": 0.045,
      "stem_height": 0.95,
      "shade_bottom_radius": 0.32,
      "shade_top_radius": 0.22,
      "shade_height": 0.38,
      "base_color": [70, 62, 54],
      "shade_color": [222, 208, 178]
    }
  }
  ```

  **`storage`** — bookshelf (open shelf cavities) or dresser (solid carcass with proud drawer-front bands):
  ```json
  "generated": {
    "generator": "storage",
    "params": {
      "kind": "bookshelf",
      "width": 0.9,
      "depth": 0.35,
      "height": 1.8,
      "sections": 4,
      "panel_thickness": 0.055,
      "leg_height": 0.06,
      "carcass_color": [108, 76, 46],
      "accent_color": [58, 50, 40]
    }
  }
  ```
  `kind` is `bookshelf` or `dresser` — proportions differ (a dresser is typically wide and low, e.g. `width: 1.2, depth: 0.5, height: 0.85`), but the same params apply to both; `sections` means shelf cavities for a bookshelf, drawer bands for a dresser. `leg_height` may be `0` (no feet); every other dimension must be `> 0`. `panel_thickness` has a real floor in practice, not just `> 0` — a value too small to cover a few pixels at Near zoom (roughly 30px/world-unit) renders as an invisible seam rather than a visible shelf board or drawer gap; the default is tuned to stay visible.

  **`primitives`** — a general small-object generator for whimsical one-offs that don't fit a furniture category (a gnome, a pet rock, a wishing well): assembles a mesh from an author-supplied list of `box`/`cylinder`/`cone`/`sphere` parts instead of a fixed named parameter set. See `GENERIC-GENERATOR-DESIGN.md` for the design rationale.
  ```json
  "generated": {
    "generator": "primitives",
    "params": {
      "symmetric": false,
      "parts": [
        { "type": "cone", "pos": [0, 0.95, 0], "size": [0.32, 0.4, 0], "color": [180, 40, 40] },
        { "type": "sphere", "pos": [0, 0.60, 0], "size": [0.3, 0.3, 0.3], "color": [230, 195, 150] },
        { "type": "cylinder", "pos": [0, 0.28, 0], "size": [0.28, 0.55, 0.22], "color": [40, 80, 160] }
      ]
    }
  }
  ```
  - Every part's `pos` is its geometric **center**, regardless of `type` — including `cylinder`/`cone`, which are otherwise base-centered at the primitive level. One uniform placement rule across all four types.
  - `size` semantics differ per type: `box` is `[width, height, depth]`; `cylinder` is `[radius_bottom, height, radius_top]`; `cone` is `[radius_bottom, height, _unused]` (radius_top is forced to 0 — the third value is ignored, but the array must still have 3 entries); `sphere` is `[radius_x, radius_y, radius_z]` (an ellipsoid — equal values give a true sphere).
  - `symmetric: true` is only correct when every part is centered on the vertical (Y) axis — the compiler does not verify this geometrically, it trusts the flag. Setting it on an object that isn't actually rotationally symmetric (e.g. anything with an off-axis part) silently renders the same single-angle view from all 4 directions, which is wrong for that object, not just a missed optimization.
  - At least one part is required; an unknown `type`, a missing `pos`/`size` array of the wrong length, or a non-positive size component (in the dimensions that type actually uses) is a compile error. There is no enforced cap on part count — more than a handful rarely helps, since detail beyond silhouette/proportion/color doesn't survive TSO's render scale anyway (see `ART-PIPELINE-DESIGN.md`), but nothing stops you from trying.
- `attributes`: named per-instance storage; compiler assigns indices. If the object declares no `entry_points.init` and no tree named `init`, the compiler **generates one** that zeroes every declared attribute — don't hand-write it.
- **Boilerplate you no longer author.** Three things every pack used to write by hand are now supplied by the compiler. All three only ever *add*: name your own and yours wins.
  - **`main_loop`** — omit `entry_points.main` and a standard `idle_for_input` loop is generated (`allow_push: false`). **Never set `allow_push: true` on furniture** — that path calls `VMThread.AttemptPush`, which casts the caller to `VMAvatar` and crashes with `Unable to cast VMGameObject to VMAvatar`. `allow_push` is for Sims only.
  - **`init`** — omit it and attribute-zeroing is generated (above).
  - **An always-true `test` tree** — omit an interaction's `test` entirely and it is always allowed. Writing a tree whose body is `1 == 1` is pure waste; it was never required.
- `strings`: string tables by chunk role; ids are 1-based (0 = none) per engine convention.
- `interactions` → TTAB/TTAs entries; `action`/`test` reference tree names.

## Trees (behavior scripts)

A tree is a named graph of nodes. Each node is one primitive (or a call to another tree); `then`/`else` name the next node or a terminal: `"return true"`, `"return false"`, `"error"`.

```json
"trees": {
  "gossip_action": {
    "args": [],
    "locals": ["dialog_roll"],
    "nodes": [
      {
        "id": "walk_over",
        "prim": "goto_relative",
        "location": "in_front_of", "direction": "facing",
        "then": "chat_anim", "else": "return false"
      },
      {
        "id": "chat_anim",
        "prim": "animate",
        "animation": { "source": "person_stock", "ref": "talk" },
        "then": "reward", "else": "reward"
      },
      {
        "id": "reward",
        "prim": "expression",
        "lhs": { "scope": "my_motives", "name": "social" },
        "op": "+=",
        "rhs": { "scope": "literal", "value": 15 },
        "then": "count_it", "else": "error"
      },
      {
        "id": "count_it",
        "prim": "expression",
        "lhs": { "scope": "my_attributes", "name": "times_gossiped" },
        "op": "+=",
        "rhs": { "scope": "literal", "value": 1 },
        "then": "return true", "else": "error"
      }
    ]
  },
  "gossip_test": {
    "args": [], "locals": [],
    "nodes": [
      { "id": "always", "prim": "expression",
        "lhs": { "scope": "literal", "value": 1 }, "op": "==",
        "rhs": { "scope": "literal", "value": 1 },
        "then": "return true", "else": "return false" }
    ]
  }
}
```

Node fields per primitive mirror the operand fields in the vocabulary reference (`simantics-vocabulary.md`), using names:
- **Scopes** (`VMVariableScope`) by snake_case name: `literal`, `temps`, `parameters`, `local`, `my_attributes`, `stack_object_attributes`, `my_motives`, `stack_object`, `tuning`, ... Named references (`"name": "social"`) resolve to indices via engine enums (motives) or pack declarations (attributes, locals, args).
  - **`"value"` means two different things depending on scope — do not confuse them.** For `"scope": "literal"`, `"value"` is the constant itself (e.g. `{ "scope": "literal", "value": 15 }` means the number 15). For every non-literal scope (`temps`, `my_motives`, `global`, ...), `"value"` instead holds the register/slot index (e.g. `{ "scope": "temps", "value": 0 }` means temp slot 0, not the number 0) — use `"name"` there if a named reference resolves to that index instead of hand-picking one. Same key, different meaning per scope; mixing them up produces a misleading `unknown_field` error rather than a value-type error, because the compiler only knows the key exists, not which scope's convention you meant.
  - **`my_*` scopes in an interaction tree mean the CALLER avatar, not the object.** Interaction action/test trees run on the interacting sim's thread, so `my_attributes`/`my_motives` read and write the *sim* — `my_attributes.times_gossiped` in a Gossip action silently increments an avatar attribute while the object's stays 0 (verified empirically in the headless VM). To touch the object's own attributes from an interaction, use `stack_object_attributes` (the interaction's stack object is the object being interacted with). `my_*` means the object only in trees the object itself runs (`entry_points` `main`/`init`).
- **Operators**: `>`, `<`, `==`, `!=`, `>=`, `<=`, `=`, `+=`, `-=`, `*=`, `/=`, `%=`, `set_flag`, `clear_flag`, `is_flag_set`, `inc_and_less`, `dec_and_greater`, `push`, `pop`. For `push`/`pop`, the **lhs is the list** (`my_list`/`stack_object_list` scope) and rhs is the value; lhs `value`/`data` selects position: 0 = front, 1 = back (TSO dialect; VMExpression.cs:203-249).
- **Tree calls**: `{ "call": "other_tree", "args": [1, 2], "then": ..., "else": ... }`. Compiler assigns private tree ids (4096+).
- Unset operand fields default to 0/false. Unknown fields are a compile **error** (catches LLM hallucination).

## Patches (modify base-game content)

Later schema version: `"patches": [...]` expressing piff-style diffs against base objects ("make all showers also clean the cat"). Out of scope for v0.1.

## Compiler contract

Input: pack JSON. Output: one `.iff` per object (or one combined pack iff), placed in the engine's Objects/ directory; plus a build report (tree ids assigned, GUIDs, warnings) and a Buy Mode catalog fragment (`catalog-entries.xml`, the `<P ...>` lines for all objects). The `install` subcommand copies the iffs into `{gameDir}/Objects/` and idempotently upserts the entries into `{gameDir}/Objects/catalog_downloads.xml` (matched by `g`; existing unrelated entries preserved).
Round-trip goal: the compiler ships with a decompiler (`.iff` → pack JSON where possible) so agents can read and remix existing objects.

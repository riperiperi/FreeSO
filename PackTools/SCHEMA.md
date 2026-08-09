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

  - `generator`: name of the parametric generator to run. v0.1 supports `chair` only; unknown names are a compile error. `params` is optional — omitted fields fall back to the generator's defaults (shown above for `chair`).
  - Dimensions are in world units (one lot tile = one unit); `*_color` fields are `[r, g, b]` integers 0-255. Unknown `params` fields and non-positive dimensions are compile errors — a zero or negative size produces a degenerate mesh with no error from the renderer itself, so the compiler catches it here.
  - The compiler renders all 12 views (4 directions × 3 zoom levels), quantizes them to a palette, and assembles `DGRP`/`SPR2`/`PALT` chunks inline into the emitted `.iff`, same file-locality requirement as `clone_from_guid` above.
- `attributes`: named per-instance storage; compiler assigns indices.
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

# Session lanes (live)

Same worktree: `packtools-on-archive`. One folder. Do not cross lanes.

| Lane | Chat | Touch | Do not touch |
|---|---|---|---|
| **Phase F — browser** | parked 2026-08-12 | — | `FSO.HouseGen`, `FSO.VMHarness`, `examples/layouts` |
| **Phase A — house** | this window (handoff 2026-08-12 23:21) | `FSO.HouseGen`, `FSO.VMHarness`, `examples/layouts`, house examples, `STATE.md`, `task_plan.md` | `FSO.Browser*`, LotView, `:5259`, KNI rebuild paths |

## Live status (2026-08-13)

### Goal
Floor plan / photo → AI builds house on a lot → friends hang out there. Desktop path is fine for the video. Browser polish after that looks like Sims.

### Phase F
Parked. Join-city scaffolding, KNIF, Mario-optional, LotView net8 dual-target, `?lot=real` all landed. Diamond / debug Blazor is a tech spike, not the product. Leave `:5259` alone if still listening.

### Phase A (this window)
- Committed: `4caba4c23` — windows on generated houses (local **ahead 1**, not pushed).
- Uncommitted WIP: `PackTools/FSO.VMHarness/Program.cs` — `--walk`. Routing frame vanishes at tick 1. **Leave it.** Occupancy proof, not the north-star.
- **`--from-image` proven on a real photo (2026-08-13):** `grove-2br-97sqm.jpg` → 13 rooms, harness green. Fixed: opus-5 default thinking ate the whole 4096 `max_tokens` (→ "empty text"); now 16000.
- **In-game session 2026-08-13 (late):** kat-flat-from-image seen rendering correctly after the `-2d` fix (v0.6.0 silently forces 3D — STATE.md). Imported objects were ⅓ size + no pie menu — fixed in `7b12e6e87`, packs reinstalled. Grove blueprint is installed as `housedata/blueprints/grove-flat.xml`, screenshot still owed.
- **Browser session 2026-08-13 (very late):** `?lot=real` no longer falls back to diamonds. Fixed: Reach→HiDef profile, `MipTextureFromFile` FileStream→TitleContainer (WASM has no fs), null-guard in `_3DFloorGeometry.DrawFloor` (no TSO content manager in browser). **XNB shaders are byte-patched in the local build output only** (`#if GL_ARB_shader_texture_lod`→`#if 0` — GLSL ES rejects the desktop-GL directive; repatch after every build, or rebuild the 11 FX with a WebGL profile in `kni-effects-blazor.yml`, which is the real fix).
- **Browser open blocker:** lot draws but terrain rasterizes nothing — canvas verified working (clear-color probe visible, triangle + UI draw), camera/VB/matrices verified sane (`lotdbg` line in console), `DrawImmediate=true` so `TerrainComponent.Draw` runs. Suspect the GrassShader `DrawBase` pass (`Passes[WorldConfig.PassOffset]`, PassOffset=0) outputs nothing under WebGL — next step is a minimal GrassShader test draw or comparing pass semantics vs desktop.
- **Overnight 2026-08-13 (late→dawn): browser milestone run.** Real GrassShader terrain renders in Chrome (`48e0e1d76` — software-depth discard + missing FloorGeom FullReset were the last blockers). **The AI-generated grove house stands on the browser lot** (`65eea822f` — `?house=grove`: BlueprintArchLoader loads XML arch into LotView with no VM/no TSO assets; RC flat-color walls forced in 2D). **Two tabs reach LotJoined simultaneously** (`5d37f91f3` — join + house together; fake city/lot servers on 33101/34101, gateway 8087). Run recipe: serve FSO.BrowserClient on :5259, start `tools/fake-city-server.py 33101` + `fake-lot-server.py 34101`, open `?join=1&gateway=ws://127.0.0.1:8087&house=grove`.
- **Dawn additions:** the house is **furnished and inhabited** (`7195f05d6`) — ContactSheet `--export-dir` emits per-object PNGs + manifest from the compiled packs; `FurnitureLayer` billboards them per `houses/grove-furnish.json`, plus capsule placeholder sims. Full demo: two tabs at `?join=1&gateway=ws://127.0.0.1:8087&house=grove`, both LotJoined, furnished house on screen.
- **Still open, in order:** (1) VM tick streaming so joined clients share state/avatars (real Phase B), (2) swap fake servers for the real archive server, (3) WebGL-profile FX rebuild to retire the XNB byte-patch (repatch after every build until then!), (4) real object pipeline in browser (billboards → ObjectComponent; depth vs walls, interactions), (5) Vitaboy avatars in browser, (6) MTL-color import bug (green toilet), (7) desktop grove screenshot, (8) SLOT/sit.

## Shared collision surface
`STATE.md`, `task_plan.md` — re-read HEAD before editing; throwaway-index + **exact paths only**. Never `git add -A`.

## Build note
Phase A harness: always `-p:FSO_GRAPHICS=MonoGame`. Bare `dotnet build` can pick up KNI node state from the browser lane and fail on `TouchPanel.EnableMouseTouchPoint`. HouseGen itself does not use XNA.

Remove this file when both lanes are idle.

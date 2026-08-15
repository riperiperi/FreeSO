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
- **Still open, in order:** (1) ~~VM tick streaming~~ **DONE 2026-08-13 (remote session): real Phase B shipped** — `?vm=1` runs the full SimAntics VM in the browser in lockstep with `FSO.LotHostLite` (sandbox protocol over the gateway `/sandbox` route); two tabs + native smoke clients share one world, identical entity hashes, zero desyncs; TTAB pie menu on click (DOM overlay) sends real interactions; chat overlay crosses runtimes. Content = `tools/make_browser_content.py` 253MB trimmed bundle (197MB tar.gz) → MEMFS → stock SERVER `Content.Init` (~15s in-tab). Run recipe: LotHostLite `--tso-dir <bundle>/tso --bare-objects` + WsGateway + serve publish + `?vm=1&name=…`. (2) swap fake servers for the real archive server (sandbox host stands in), (3) ~~XNB byte-patch~~ **RETIRED: patched bytes committed** (`patch_glsl_es.py`), (4) real object pipeline in browser — KNIF anomaly still ledgered below; **billboards now VM-fed** (live entity positions), which is the demo look, (5) Vitaboy avatars in browser (capsules stand in), (6) MTL-color import bug (green toilet), (7) desktop grove screenshot, (8) SLOT/sit.

### Real-furniture renderer ledger (2026-08-13, remote session)

The full pipeline short of pixels works headless in-container: 58 pack `.iff`s fetched over HTTP → `IffFile.Read` (WASM needed compile-time chunk factories — Mono fails `Activator.CreateInstance` per never-instantiated type) → `GameObject`s → `ObjectComponent`s in the Blueprint → `WorldEntities.Draw` issues the GL draws with verified-correct vertex data, live textures (GetData round-trips), backbuffer bound, blend/scissor/viewport sane — and no fragments. **The compiled KNIF `2DWorldBatchiOS.xnb` is at fault**; a synthetic sprite draws only when the pixel matrix is ALSO written into `worldViewProjection` (the compiler collapsed the three `: ViewProjection`-semantic matrices, and position reads the collapsed slot), and `vsSimple` never draws (KNI misbinds when the vertex declaration outnumbers shader inputs, proven by an exact-3-attr struct drawing). Even with matrix workarounds + depth discard disabled the DGRP sprites don't rasterize — some further per-draw state interaction in this XNB.

Eliminated with probes (do not re-chase): textures (CPU decode, GPU round-trip, live-instance readback all good), Alpha8 depth format, depthTexture binding, `TimedReferenceController` disposal, VB freshness/SetData timing, technique table order, technique-switch flushing, hardware depth state (engine now forces None — real fix kept), render targets, culling rects, `Mode` flags, trimming (XmlSerializer→XDocument was real and kept).

**Fix attempted and insufficient (2026-08-13, late):** `2DWorldBatchiOS.fx` rebuilt via CI with distinct matrix semantics + 5-attr `vsSimple` (both changes verified in the shipped XNB) — behavior unchanged. Per-draw matrix re-writes inside `DrawImmediate` (the exact sequence of the working probe) — unchanged. The stable anomaly: one specific hand-built `_2DStandaloneSprite` (created ~50s in, inside `DrawSpriteTest`) renders through the batch every time; every DGRP-built or layer-built sprite through the byte-identical call path renders nothing — same technique, same uniforms, same textures (GPU round-trip verified on the live instance), same vertex data (logged at draw), same depth/blend/RT state. Suspect: per-VertexBuffer/Texture GL object state at creation time under KNI BlazorGL. Next tools if resumed: SpectorJS/WebGL call capture in a headed browser, or a KNI-source-level trace of buffer/attribute binding. `?furnish=real`, `?fx=fixed`, `?spritetest=`, `?v2diag=` all remain wired for whoever picks this up. **Billboards remain the visible furniture** (Kat-approved look); furniture *behavior* is fully served by the VM path (see LotHostLite — pie menus + interactions proven in lockstep).

### Running the browser demo (2026-08-15)

`PackTools/tools/run_browser_demo.sh` is the supported way to start it —
preflight, builds, three services, readiness checks that actually verify each one
is listening and serving. `--doctor` (in a *second* terminal tab; the script's own
tab is busy) prints the state of everything. **`PackTools/FSO.BrowserClient/README.md`
is the play/troubleshoot doc**; it replaced the 2026-08-11 spike notes.

An evening was lost to the runner rather than the game: a surviving publish dir
serving a two-day-old app, a squatting dev server, a gateway binary older than the
`--sandbox` flag, an empty packs dir from `dotnet` missing on PATH, and a 404 on
the content bundle. Each now fails loudly and early; don't reintroduce
"build only if the binary is missing" or symlink staging.

## Shared collision surface
`STATE.md`, `task_plan.md` — re-read HEAD before editing; throwaway-index + **exact paths only**. Never `git add -A`.

## Build note
Phase A harness: always `-p:FSO_GRAPHICS=MonoGame`. Bare `dotnet build` can pick up KNI node state from the browser lane and fail on `TouchPanel.EnableMouseTouchPoint`. HouseGen itself does not use XNA.

Remove this file when both lanes are idle.

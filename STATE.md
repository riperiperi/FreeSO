# Project state

Last verified: 2026-08-11. Branch: `packtools-on-archive`.

> **What this project does:** A player uploads a floor plan or a room photo. The AI builds their actual home in a real-geography city. Their friends visit and hang out inside it. All multiplayer, all live.

Everything below is separated into what **FreeSO already provides**, what **we added**, and what is **still ahead** — because confusing those three caused four duplicated efforts in a single day. Claims marked ✅ were verified by running or reading them on this branch. Claims marked ⚠ come from another document and have not been independently checked.

---

## Layer 1 — What FreeSO gives us (not ours)

[FreeSO](https://github.com/riperiperi/FreeSO) is a full reimplementation of The Sims Online in C#/MonoGame, MPL 2.0, maintained by riperiperi for roughly a decade. **This is the overwhelming majority of the code and the reason the project is possible at all.**

### The simulation, already working
- **SimAntics VM** — the game's original scripting engine: BHAV trees, ~50 primitives, ~50-entry variable scope space, TSO and TS1 dialects
- **Needs and motives** — hunger, hygiene, bladder, energy, comfort, social, fun, room
- **Multiplayer** — lots, visiting, chat, roommates, an economy, jobs, relationships, skills
- **Build mode** — walls, floors, multiple storeys, terrain, furniture placement
- **Buy mode** — the full catalog UI, categories, inventory
- **Houses as data** ✅ — a lot is described by a blueprint XML (`<floors>`, `<walls>`, `<object>` per tile and level) and `VMWorldActivator.LoadFromXML()` builds the world from it. `VMArchitecture.SetWall()` / `SetFloor()` do the same programmatically. **This is the target format for house generation — no house-building engine needs writing.**
- **Lot persistence** — `VMMarshal`, blueprints, lot save/load, facades; upstream also has a `lot-serialize` branch
- **Volcanic / FSO.IDE** — a complete object editor with a BHAV script editor, live against a running VM
- **3D mode** — meshes reconstructed at runtime from sprite z-buffers

### Recently added upstream on `archive` (373 commits ahead of `master`) ✅
The branch that is actually maintained. `master` has not moved since Aug 2025.
- **Archive Mode** — self-hosted play from a local SQLite clone. No external server needed.
- **City Painter UI** — in-game panels for elevation, roads, forests, terrain type, with live previews
- **Blank city canvases** — `city_0900` "Flat Grass", `city_0901` "Empty Ocean"
- **Built-in TSO installer** — appears when game files aren't found. Materially reduces bring-your-own-copy friction.
- **Weekly signed releases** — v0.1.0-beta → v0.5.3-beta, June–August 2026, with an auto-update system
- **Native macOS** — working CI, code signing, publish pipeline
- **.NET 9** across the solution (PR #283); MonoGame 3.8.5
- Free-will toggle, keyboard navigation, moderation levels, join history, encryption, improved terrain rendering

### What upstream does *not* have
- Any AI or agent-assisted content creation — absent from the codebase, the forum, and every PR
- Any browser/WebAssembly target, or any WebSocket transport (the client speaks raw TCP via Mina.NET)
- Any real-world-geography city generation

---

## Layer 2 — What we added

### House replication — the player-facing product
| Piece | State |
|---|---|
| **Address → city** ✅ | `PackTools/citygen/generate_city.py` turns a place name into FreeSO's full city raster set. Verified on San Francisco: 39.4 km square, elevation −5..781 m, 42,159 OSM road ways. Written to disk; **not yet loaded into the running game.** |
| **Blueprint → live house** ✅ | **Proven.** A hand-authored blueprint XML loads through `VMBlueprintRestoreCmd` into a running VM and the engine derives a sealed interior. `PackTools/examples/house-one-room.xml` + `FSO.VMHarness --house`. Verified both directions — remove the walls and the same probe reports outdoors. |
| **Layout → blueprint → rendered house** ✅ | **Seen on screen, 2026-08-10.** `FSO.HouseGen` turns a room-layout JSON into blueprint XML; `kat-flat.json` (living/bedroom/bathroom, three doors) loads through Sandbox Mode and **stands on the lot in the real client** — walls up, floors down, all three doorways cut. Kat's screenshot, not a harness assertion. |
| **Floor plan → layout JSON** | **Not built.** Now the *only* missing link, and the narrowest it has ever been: an image becomes the small typed layout the converter already consumes. Everything downstream of that JSON is verified end to end, pixels included. |
| **Photo → furnishing** | **Not built.** Depends on the object pipeline below, which works. |
| **Friends inside it** | FreeSO's multiplayer, unchanged. Untested with an AI-generated house. |

### AI modding infrastructure — internal, powers the above
| Component | What it does | Evidence |
|---|---|---|
| **`FSO.PackCompiler`** | JSON pack → real `.iff`, and back. Reuses `tso.files` serialization, so output is indistinguishable from base-game content. | 62/62 tests, incl. byte-identical compile→decompile→recompile + 6 import tests |
| **`FSO.ModServer`** | MCP server, 13 tools over stdio JSON-RPC: `create_pack`, `add_object`, `add_interaction`, `add_tree`, `edit_tree_node`, `remove_tree_node`, `validate`, `compile`, `test_in_vm`, `decompile_object`, `set_dialog_string`, `find_base_object`, `list_vocabulary` | 48/48; verified over live JSON-RPC to a subprocess |
| **`FSO.VMHarness`** | Headless scripted VM runs with step-through traces, so an agent sees *why* something misbehaved | exercised by both suites |
| **`FSO.LiveInject`** | Registers a compiled object into an already-running game — no restart | proof harness boots a VM, ticks, injects, interacts |
| **`FSO.AgentBridge`** | Plain language in, compiled object out. Anthropic/OpenAI providers, turn caps, prompt caching. | pet rock $0.084 / gnome $0.788 / fortune cat $1.718, independently verified |
| **Art generators** | Original parametric art: chair, sofa, table, bed, lamp, storage, generic primitives | real DGRP/SPR2 chunks; palette-corruption guards |
| **CC0 mesh import** | `ObjImporter` + `appearance.imported` — Kenney/Quaternius OBJ+MTL through same sprite pipeline | 51 objects (6 Quaternius + 45 Kenney tier-1). Plumbing installed to FreeSO.app; Kenney pack generated, not installed. Provenance in `assets/cc0/PROVENANCE.json` |
| **`import-batch` CLI** | CSV manifest → pack JSON with imported appearances | `FSO.PackCompiler import-batch` |
| **"Make Something" panel** | Buy Mode button → chat → object appears live. A debug surface for the object pipeline, not the player experience. | builds clean on net9.0; **never yet clicked by a human** |
| **`ContactSheet` / `ArtCalibration`** | Render-and-review surfaces for generated art | used to fix real sprite bugs |
| **`FSO.WsGateway`** | WebSocket↔TCP byte gateway + JS Aries protocol debugger (`wwwroot/`) | 5/5 tests. Live handshake proven; browser now also sends type-21 session response and reaches canned `HostOnlinePDU` on the fake city. Join-lot still open. |
| **`FSO.BrowserContent`** | `IContentStore` — File / Http / **Composite** (`Content/` overlay) | Tests green. Wired into `Content.GetResource` + `FileProvider` (BasePath + Content/). `FAR3Archive(Stream)`. |
| **KNI migration plan** | Library-first retarget to `nkast.Xna.Framework.*`, then BlazorGL head | `docs/KNI-MIGRATION.md` — S0–S1 done; S2 texture-via-HTTP in BrowserClient |
| **`FSO.BrowserClient`** | KNI BlazorGL — HTTP texture + **Aries join** via `FSO.BrowserAries` | Auto-joins gateway after load; stage bars → LotJoined |
| **`FSO.BrowserAries`** | WASM-safe Aries codec + `ArchiveJoinDemo` (city→lot) | 5 unit tests + gateway integration → LotJoined |
| **KNI S1 graphics switch** | `FSO_GRAPHICS=MonoGame\|Kni` via `Directory.Build.props` + `msbuild/FSO.Xna.packages.targets` | Lib chain through `FSO.Client` builds on KNI; `FSO.Mac` on MonoGame |
| **Aries join path** | City through FindLot + **lot** `/lot` type 22→21→HostOnline→ClientOnline→empty VM tick | Fake city 33101 + fake lot 34101; gateway demo auto-opens `/lot`. Real VM state still open. |

### Engine changes we made (small, in `TSOClient/`)
- `WorldObjectCatalog.AddLive()` — register a catalog item after startup
- `__placement_init` tree in `PackBuilder` — sets `AllowedHeightFlags`, without which every compiled object is unplaceable and renders floating. Byte-level regression test.
- Make Something panel wiring in `CoreGameScreen` / `UIBuyMode`

### Reference material we produced
`simantics-vocabulary.md` (the VM's opcodes, scopes, operand layouts and silent-failure modes, reverse-engineered), `SCHEMA.md` (pack authoring format), `MODEL-EVALUATION.md` (which models can actually drive the bridge, measured).

### Design only — written, no code ⚠
`PLAYER-LAYER-DESIGN.md`, `SHARING-DESIGN.md`, `RECIPE-DESIGN.md`, `CATALOG-PARITY-PLAN.md`, `BROWSER-VIABILITY.md`, `FIRST-RUN-DESIGN.md`, `CAPTURE-DESIGN.md`

Two that were on this list and shouldn't be: `GENERIC-GENERATOR-DESIGN.md` is **built** (`ArtGen/PartsGenerator.cs`), and `NARRATION-CONTRACT.md` documents a system prompt that ships in `FSO.AgentBridge/MakeSomethingAgent.cs`.

---

## Layer 3 — Roadmap

| Phase | Goal | State |
|---|---|---|
| **A — Your house, from a photo** | Upload a floor plan → AI emits blueprint XML → the house stands on a lot. The first real integration, and the north-star video. | next |
| **B — Friends in a house together (BYO)** | Two players, one server, one generated San Francisco. One uploads a floor plan and gets a house at their real address; the other walks in. | pending |
| **C — Make it look like mine** | Photo-based furnishing and conversational refinement: "move the sofa left", "the window should be bigger". Extends A using the working object pipeline. | pending |
| **D — Persistence and sharing** | Houses survive restarts; publish, discover, fork, remix. Lot serialization already exists in-engine — check before building. | pending |
| **E — Original content** | ~200 original objects so a shared house looks right without EA assets. CC0 import pipeline live; 51 objects imported (6 Quaternius plumbing + 45 Kenney tier-1). | in_progress |
| **F — Browser client** | KNI/BlazorGL port, content over HTTP, threading. WebSocket gateway **proven against the live game** (`PackTools/FSO.WsGateway`): a browser decoded the Archive handshake from Kat's running server, zero server changes. Networking unknown closed; rendering port is the remaining bulk. Pulled forward by Kat 2026-08-11. | in_progress |
| **G — Neighbourhood scaling** | SF generation is done and verified. Remaining: host it as a playable city and let a player claim a lot by address. | partly done |

---

## Known gaps and risks

**Floor-plan-to-lot translation is untested — this decides Phase A.** We can compile and inject objects. We have never tested whether a vision model can turn a floor-plan image into a valid room layout, or whether the resulting blueprint XML loads cleanly. The format and loader exist; the translation does not.

**The generated SF has never been loaded into the game.** It produced correct-looking rasters on disk. Playable is a claim nobody has checked.

**Untested, not unbuilt.** The object loop is complete in code and has never run in front of a person — latency, ambiguity, error handling and multiplayer behaviour all unknown.

**Cost blocks scale for agent-authored objects, not CC0 imports.** Art import removes per-object LLM cost for catalog building; behavior trees are still hand/recipe-authored. Recipes designed, unbuilt.

**The agent is blind to live world state.** Not a problem for generating a house from scratch. It becomes one during conversational refinement — "move the sofa two tiles left" requires knowing where the sofa is. Deferrable to Phase C.

**Global rules aren't reachable yet.** The engine supports patching base behaviour (piff diffs, global BHAVs, tuning); our schema deferred it as out of scope for v0.1. A scope decision, not an engine limitation.

**No moderation or rollback.** A stranger's compiled behaviour running on your lot, with no undo.

**Content provenance.** The engine is ours by fork (MPL 2.0) and the authoring layer is new IP, but art, sounds and animations are still EA's, self-downloaded per player. Generated originals replace them progressively; that replacement is what makes browser distribution clean.

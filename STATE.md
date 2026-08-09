# Project state

Last verified: 2026-08-08. Branch: `packtools-on-archive`.

Separates three things that are easy to conflate: what the **FreeSO project already gives us**, what **we built on top**, and what is **still ahead**. Written because four separate duplications happened in one day from not knowing which was which.

Everything below marked ✅ was verified by running or reading it on this branch, not from a doc's status line. Claims taken from docs without independent checking are marked ⚠.

---

## Layer 1 — What FreeSO gives us (not ours)

[FreeSO](https://github.com/riperiperi/FreeSO) is a full reimplementation of The Sims Online in C#/MonoGame, MPL 2.0, maintained by riperiperi for roughly a decade. **This is the overwhelming majority of the code and the reason the project is possible at all.**

### The simulation, already working
- **SimAntics VM** — the game's original scripting engine: BHAV trees, ~50 primitives, ~50-entry variable scope space, TSO and TS1 dialects
- **Needs and motives** — hunger, hygiene, bladder, energy, comfort, social, fun, room. Sims live or don't.
- **Multiplayer** — lots, visiting, chat, roommates, an economy, jobs, relationships, skills
- **Build mode** — walls, floors, multiple storeys, terrain, furniture placement
- **Buy mode** — the full catalog UI, categories, inventory
- **Lot persistence** — `VMMarshal`, blueprints, lot save/load, facades; upstream also has a `lot-serialize` branch
- **Volcanic / FSO.IDE** — a complete object editor with a BHAV script editor, live against a running VM. The pre-existing way to author objects, by hand, on Windows.
- **3D mode** — meshes reconstructed at runtime from sprite z-buffers (`DGRP3DGeometry`, `FSOF`)

### Recently added upstream on `archive` (373 commits ahead of `master`) ✅
The branch that is actually maintained. `master` has not moved since Aug 2025.
- **Archive Mode** — self-hosted play from a local SQLite clone of a live server. No external server needed.
- **City Painter UI** — real in-game panels for elevation, roads, forests, terrain type, with live previews. Not the old CTRL-F1 debug tool.
- **Blank city canvases** — `city_0900` "Flat Grass", `city_0901` "Empty Ocean", shipped for building custom cities
- **Built-in TSO installer** — appears when the game files aren't found. Materially reduces bring-your-own-copy friction.
- **Weekly signed releases** — v0.1.0-beta → v0.5.3-beta between June and August 2026, with an auto-update system
- **Native macOS** — working CI, code signing, publish pipeline. A `.dmg` of v0.5.3-beta is already on this machine.
- **.NET 9** across the solution (PR #283); MonoGame 3.8.5
- Free-will toggle, keyboard navigation, moderation levels, join history, encryption, improved terrain rendering

### What upstream does *not* have
- Any AI or agent-assisted content creation — confirmed absent from the codebase, the forum, and every PR
- Any browser/WebAssembly target — no official MonoGame web support; a community attempt was abandoned
- Any WebSocket transport — the client speaks raw TCP via Mina.NET
- Any real-world-geography city generation

---

## Layer 2 — What we added

All under `PackTools/` unless noted. This is the new IP: an authoring layer that lets a non-developer create game content by describing it.

### Working and verified ✅
| Component | What it does | Evidence |
|---|---|---|
| **`FSO.PackCompiler`** | Compiles a JSON pack → real `.iff`, and decompiles back. Reuses `tso.files` serialization, so output is indistinguishable from base-game content. | 56/56 tests, including byte-identical compile→decompile→recompile |
| **`FSO.ModServer`** | MCP server exposing 13 fine-grained tools over stdio JSON-RPC: `create_pack`, `add_object`, `add_interaction`, `add_tree`, `edit_tree_node`, `remove_tree_node`, `validate`, `compile`, `test_in_vm`, `decompile_object`, `set_dialog_string`, `find_base_object`, `list_vocabulary` | 48/48 tests; verified over live JSON-RPC to a subprocess, not just in-process |
| **`FSO.VMHarness`** | Runs a compiled object through a scripted scenario in a headless VM and returns a step-through trace, so an agent can see *why* something misbehaved | exercised by both suites |
| **`FSO.LiveInject`** | Registers a freshly compiled object into an already-running game's content system — no restart | `FSO.LiveInject.Proof` boots a VM, ticks it, then injects and interacts |
| **`FSO.AgentBridge`** | Plain language in, compiled object out. Provider abstraction (Anthropic/OpenAI), turn caps, prompt caching. | pet rock $0.084 / gnome $0.788 / fortune cat $1.718, all independently verified |
| **Art generators** | Parametric original art: chair, sofa, table, bed, lamp, storage, plus a generic primitive-composition generator | emit real DGRP/SPR2 chunks; palette-corruption guards in tests |
| **"Make Something" panel** | In-game Buy Mode button → chat dialog → agent → compiled object → live catalog registration | client builds clean on net9.0; **never yet clicked by a human** |
| **`citygen/generate_city.py`** | Place name → the full FreeSO city raster set (elevation, terrain type, roads, forest, vertex colour), from Nominatim + AWS terrarium elevation + OSM | ran it: San Francisco, 39.4 km square, elevation −5..781 m, 42,159 OSM ways |
| **`FSO.ContactSheet` / `FSO.ArtCalibration`** | Render-and-review surfaces for generated art | used to fix real sprite bugs |

### Engine changes we made (small, in `TSOClient/`)
- `WorldObjectCatalog.AddLive()` — register a catalog item after startup, since the catalog is otherwise populated once at init
- `__placement_init` tree in `PackBuilder` — sets `AllowedHeightFlags`, without which **every** compiled object is unplaceable and renders floating. Byte-level regression test.
- Make Something panel wiring in `CoreGameScreen` / `UIBuyMode`

### Design only — written, no code ⚠
`PLAYER-LAYER-DESIGN.md` (in-client chat/agent runtime), `SHARING-DESIGN.md` (publish/discover/fork/moderate), `RECIPE-DESIGN.md` (cost reduction), `CATALOG-PARITY-PLAN.md` (~200 original objects), `BROWSER-VIABILITY.md`, `GENERIC-GENERATOR-DESIGN.md`, `FIRST-RUN-DESIGN.md`, `CAPTURE-DESIGN.md`, `NARRATION-CONTRACT.md`

### Reference material we produced
`simantics-vocabulary.md` — the VM's opcodes, scopes, operand layouts and silent-failure modes, reverse-engineered. `SCHEMA.md` — the pack authoring format. `MODEL-EVALUATION.md` — which models can actually drive the bridge, measured.

---

## Layer 3 — Roadmap

Full sequencing and rationale in `task_plan.md`. Condensed:

| Phase | Goal | State |
|---|---|---|
| **A** | **Click "Make Something" in a running game and watch it work.** Built, on the right base, never exercised by a human. Also: catalog thumbnails render blank. | next |
| **B** | **Friends in a lot together.** Then the WebSocket gateway spike — browsers cannot open raw TCP and no prior art exists anywhere. The only open-ended unknown. | pending |
| **C** | **Build *your* house, fast.** Starts by measuring how long it takes by hand today — unmeasured, and tooling built before that is a guess. | pending |
| **D** | **Sharing and remixing** rooms, objects, stories. Lot serialization already exists in-engine — check before building. | pending |
| **E** | **Browser client** — KNI/BlazorGL port (2-4 wks), content over HTTP, threading. Gated on B. | pending |
| **F** | **Original content** — ~200 objects to make browser distribution clean. Blocked on per-object cost. | pending |
| **G** | **Your neighbourhood** — SF generation works; needs installing as a city and playing in. | partly done |

---

## Known gaps and risks

**Untested, not unbuilt** — the loop is complete in code and has never run in front of a person. Latency, ambiguity, error handling and multiplayer behaviour are all unknown.

**Cost blocks scale.** $1.72 for a complex object. Two hundred objects would cost more than the game. Recipes are designed and unbuilt; output tokens are the entire bill.

**The agent is blind to the live world.** Tools are authoring-time only. "Make this couch only usable by friends" needs runtime queries against relationships and lot state — no such layer exists.

**Global rules aren't reachable yet.** The engine supports patching base behaviour (piff diffs, global BHAVs, tuning); our schema deferred it as out of scope for v0.1. A scope decision, not an engine limitation.

**Networking is the one true unknown.** Everything else has named prior art and people who've shipped it.

**No moderation or rollback.** A stranger's compiled behaviour running on your lot, with no undo.

**Content provenance.** The engine is ours by fork (MPL 2.0) and the authoring layer is new IP, but the art, sounds and animations are still EA's, self-downloaded by each player. Original generated content replaces them progressively; that replacement is what makes browser distribution clean.

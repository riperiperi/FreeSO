# Task Plan

## What this is
> A player uploads a floor plan or a room photo. The AI builds their actual home in a real-geography city. Their friends visit and hang out inside it. All multiplayer, all live.

Kat, 2026-08-08: *"what i want is people to make a mini replica of their house themself and hang out with friends virtually."* That sentence is the product. Everything below serves it.

### The three pillars
1. **Browser-based multiplayer life simulation** — no install, click a link, your friends are already in there.
2. **Easy in-world creation and customization** — your house and your furniture, made by describing or showing.
3. **Sharing and remixing** — rooms, objects, and stories.

### What this replaces
Supersedes `PRODUCT-DIRECTION.md` (which centred "content worth having + player modding" and left browser-vs-desktop open). Browser is decided, but moved late — it lowers install friction, it doesn't prove the idea. Sharing/remixing is now a pillar, not deferred. The house-replica goal reorders everything.

**Mechanic decided 2026-08-08:** the AI builds the house **from a photo or floor plan**, then the player refines it conversationally. Not manual building with AI assistance.

## Current Phase
Phase A — A1 done, A2 next

## Why this is smaller than it looks
**Houses are already data.** A lot is a blueprint XML — `<floors>`, `<walls>`, `<object>`, each with tile coordinates and a level. `XmlHouse.cs` parses it, `VMWorldActivator.LoadFromXML()` builds the world from it, and **`VMBlueprintRestoreCmd` is a live network command that takes that XML as raw bytes and rebuilds the lot mid-game** — the server already uses it to reset lots.

So nothing needs a house-building engine written. The AI's job is "read a floor plan, emit coordinates." The delivery path exists and is exercised by the server today.

One constraint that shapes the work: `VMBlueprintRestoreCmd.Verify()` returns `!FromNet`, so a client cannot send it. The generator runs server-side.

## Phases

### Phase 0: Correct base — COMPLETE
- [x] PackTools on `upstream/archive` (`packtools-on-archive`) — 56/56 + 48/48 green on net9.0
- [x] Make Something panel + wiring ported (`8ddc26826`); client builds clean. Our hunks only — archive had added the user list, city painter layer, surround puppets and city edit button that a wholesale copy would have deleted
- [x] Duplication audit; stale docs corrected; standing rules in `CLAUDE.md`
- **Status:** complete

### Phase A: Your house, from a photo
Split deliberately. A1 is hours and carries no AI risk; if it fails, no amount of vision work matters.

**A1 — prove the delivery path — DONE ✅** (`d962fed12`)
- [x] Hand-authored `PackTools/examples/house-one-room.xml`: one 4x4 room, 16 floor tiles, 15 wall tiles
- [x] Loads through `VMBlueprintRestoreCmd` into a live headless VM; the engine derives a **sealed interior** from it. Run: `~/.dotnet/dotnet PackTools/FSO.VMHarness/bin/Debug/net9.0/FSO.VMHarness.dll --house <xml>`
- [x] Test is non-vacuous, proven both directions: the full house reports the probe tile indoors; the same house with 12 of 15 walls removed reports it outdoors and fails. (First version of the check was wrong — counting indoor rooms lot-wide reports 1 even for the empty lot, which has no walls at all.)
- [ ] Walk a Sim inside — not yet done; architecture is proven, occupancy is not
- [x] Decide the scale mapping — **answered, and it was the wrong worry.** `FloorClip`/`Offset`/`TargetSize` are job-lot machinery: `LotContainer.BlueprintReset` passes `Rectangle.Empty`, offset `(0,0)`, `targetSize = 0` for residential lots. Usable grid is ~75×75 = 5,625 tiles. At **1 tile = 1 metre**, a 1,400 sq ft home is ~130 tiles — roughly 12×11 of interior. Nothing gets dropped for capacity reasons; you could fit a mansion. The real limit is **legibility**: anything under 1 m (closets, narrow halls, island gaps) cannot be represented, and openings quantize to whole tile edges. Enforced as `MinRoomDimension = 2` in `BlueprintWriter`, which rejects rather than approximates.

**A2 — vision → that same XML**

Sequenced so the vision model is the *last* variable introduced, not the first.

- [x] **Scale mapping decided** — see A1 above. 1 tile = 1 m.
- [x] **Intermediate room-layout model** — `PackTools/FSO.HouseGen/RoomLayout.cs`. Rooms as tile rectangles. Exists so the vision step and the XML step fail separately: when a house comes out wrong, the layout says whether the model misread the plan or the converter mis-encoded it.
- [x] **Deterministic layout → XML converter** — `PackTools/FSO.HouseGen/BlueprintWriter.cs`. No AI in this path. **Reproduces `examples/house-one-room.xml` element-for-element** from `examples/layouts/one-room.json`, which is the strongest check available: the known-good file is the test oracle.
- [x] **Harness-verified, multi-room** — `examples/layouts/two-room-flat.json` → 46 floor tiles, 31 wall tiles / 34 segments, **3 indoor rooms** (baseline 1 + both rooms sealed independently). Shared walls dedupe: the party wall at x=36 appears once, bits OR-ed.
- [x] Validation is non-vacuous — sub-2-tile rooms, overlapping rooms and out-of-grid rooms are each rejected with a message naming the room.
- [x] **Doors.** `examples/layouts/two-room-flat-doors.json` → 4 objects placed, 0 out of world, **2 door cuts**, rooms still sealed. North-edge doors too (`one-room-north-door.json`). A door with no wall to cut is rejected by name.
  - A door is an **object**, not a wall attribute: `VMEntityFlags2.ArchitectualDoor` makes it call `SetWallStyle`, clearing `TopLeftSolid`/`TopRightSolid` so `VMRoomMap` stops adding a pathing obstacle.
  - Two things are both required, and each fails silently on its own — proven with a control run. **(1)** Every wall must be stored twice, low edge plus the mirrored high-edge bit on the neighbour: a door is a 2-tile group whose halves demand `TopLeft` and `BottomRight` of the *same* wall, and `GetWall` never merges neighbours. **(2)** The group anchors one tile *before* the wall tile; anchor on the wall itself and it targets the next boundary over.
  - `VMWorldActivator.CreateObject` discards `SetPosition`'s error, so both failures present as "the door just isn't there". `--house` now reports objects placed/out-of-world, door cuts, and retries a failed placement to surface the real `VMPlacementError`.
- [x] **SEEN IN THE GAME, 2026-08-10.** `kat-flat.xml` via Sandbox Mode: three rooms standing on the lot, walls up, floors down, three door frames in the wall line (confirmed again in walls-down view). Kat's screenshots. First time this pipeline has produced anything visible.
  - **A house is not a lot.** `VMWorldActivator.LoadFromXML` sets `VM.TSOState.Size` and the placement offset *only* if the blueprint contains the lot phone `0x313D2F9A`. Without it, `VMLotTerrainRestoreTools` and `VMContext` have no lot, and the client draws an empty grey screen while every architecture check passes. Generate with `--base Content/Blueprints/empty_lot_fso.xml`.
  - All of that is behind `if (VM.UseWorld)`, which the harness sets false — so the harness is structurally incapable of catching it. It now reports `lot phone: present/MISSING` as the one rendering prerequisite it can check.
  - The Buy Mode NRE (`UICatalogItem.MouseEvt` → `CreateObjectInstance`) was the no-world symptom, not a content bug. It disappeared when the lot appeared.
- [ ] **Windows.** Same object-on-a-wall mechanism, not yet tried.
- [ ] Walk a Sim through a door — closes A1's last open item.
- [ ] Floor patterns are placeholder (`3`). A home wants wood/carpet per room; cosmetic, cheap.
- [ ] **Then** the vision model: floor-plan image → layout JSON. It only ever emits the model above; it never writes XML.
- [ ] Cheap by construction: one XML per house, not 200 agent runs

**A3 — the object loop, in passing**
- [ ] Click "Make Something" in a running game and watch an object appear. Built, never seen by a human. It's the debug surface for the object pipeline, not the player experience.
- [x] ~~Catalog thumbnails render blank~~ — **not a bug on this branch, don't re-chase.** Checked, as this line said to: `UICatalog.GetObjIcon` sets `null` on a missing BMP chunk (and we emit none) only on `master`. Upstream `4c89dab20` added a `CatThumbGenerator.GenerateThumb` fallback on `archive` (`UICatalog.cs:449`), so ours render. A `mac-port`-era bug that survived the base change.
- **Status:** in_progress

### Phase B: Friends in a house together (BYO)
- [ ] Two players, one server, one generated San Francisco. Player 1's floor plan becomes a house at their real address; player 2 walks in.
- [ ] Establish what a second person joining actually takes — Archive Mode is self-hosted off a local SQLite clone.
- [ ] **Claiming a lot by real address** is what fuses the city and the house. It belongs here.
- **Why early:** "hang out with friends" is the point. A house nobody can visit is a screensaver. This needs no browser.
- **Status:** pending

### Phase C: Make it look like mine
- [ ] Photo of a room → wall colours, floors, furniture positions adjusted to match
- [ ] Photo of a couch → generated look-alike, placed. Uses the working object pipeline and existing generators (chair, sofa, table, bed, lamp, storage, primitives).
- [ ] Conversational fixes: "move the sofa left", "the window should be bigger"
- [ ] **This is where agent world-blindness starts to matter** — "move the sofa two tiles left" needs to know where the sofa is. Either a world-query tool or regenerate-the-room wholesale.
- **Status:** pending

### Phase D: Persistence and sharing
- [ ] Houses survive restarts; publish, discover, fork, remix
- [ ] **Rooms, not just objects.** Lot serialization already exists in-engine (`VMMarshal`, blueprints, upstream `lot-serialize`) — check how far it gets before building.
- [ ] `SHARING-DESIGN.md` covers publish/discover/fork/re-attribute and safety when a stranger's compiled behaviour runs on your lot — design only, no code.
- [ ] Precedent: EA-Land's Custom Content Creator program — in-game upload, brand/artist metadata, age rating, moderation for duplicates, no creator payment.
- **Status:** pending

### Phase E: Original content
- [ ] ~200 original objects, not 3,132 (`CATALOG-PARITY-PLAN.md`); Tier 1 (~70) is the motive loop
- [x] **Art import pipeline** — `ObjImporter`, `appearance.imported`, `import-batch` CLI, provenance tracking
- [x] **Plumbing/appliance pilot** — 6 Quaternius CC0 imports + original motive BHAVs (`examples/plumbing-pilot.json`)
- [x] **Kenney tier-1 batch** — 45 CC0 imports (`examples/kenney-tier1.json`, manifest at `assets/cc0/kenney-tier1.csv`)
- [ ] Remaining Tier 1+2+3 to ~200 — Kenney full kit + Quaternius gaps + generators for parametric variants
- [ ] **Cost partially lifted for art** — imports replace $0.79/object agent runs; behavior still authored as trees/recipes
- [ ] Gates clean browser distribution — a web server serving EA's assets is the blocker (`STRATEGY.private.md`)
- **Status:** in_progress

### Phase F: Browser client
**Pulled forward by Kat, 2026-08-11** (*"i want to make a browser based multiplayer version. dont worry about ea"*) — original-content gating no longer blocks starting this; the CC0 catalog replaces EA assets in parallel, not as a prerequisite.

- [x] **WebSocket gateway spike — the open unknown, now derisked at the byte level.** `PackTools/FSO.WsGateway`: a WS↔TCP byte pipe (Kestrel, no NuGet deps, ~120 lines) in front of the existing Archive ports. Works because Aries is a length-prefixed byte stream (`CustomCumulativeProtocolDecoder` reassembles regardless of chunking), so **zero FreeSO server changes**. Proof: a real `RequestClientSessionArchive` (type 2000, the packet `CityServer.ArchiveHandshake` sends on connect), serialized by `FSO.Server.Protocol` itself, framed per `AriesProtocolEncoder`, survives the bridge and deserializes — 3/3 tests. Routes are fixed (`/city`→33101, `/lot`→34101), not an open proxy.
- [x] **Gateway vs live server — PROVEN, 2026-08-11, by Kat herself.** FreeSO hosting Archive Mode (Quick Start, ports 33101/34101), browser at the gateway demo page: decoded `RequestClientSessionArchive` from the live game — "Kat's Server", 1 player online, v0.6.0-beta manifest + RSA key, shard "San Francisco (5)", map 0902. A real browser, the real server, zero server changes. **The networking unknown is closed.**
- [x] **Browser speaks Aries — seen in a real browser, 2026-08-11.** `FSO.WsGateway/wwwroot/index.html`: a JS Aries framer + decoder (12-byte LE header, PascalVLC varint strings) served by the gateway itself. Verified in Chrome against a fake city server (`tools/fake-city-server.py`) emitting the byte-exact handshake: page connects over WS, decodes `RequestClientSessionArchive`, displays server name/players/shard/map. Screenshot taken by browser automation.
- [x] **Browser session response path** — after type-2000 handshake, JS client sends `RequestClientSessionResponse` (type 21); fake city replies with Voltron `HostOnlinePDU`. Stage UI through HostOnline. 5/5 gateway tests. Live join still needs valid PKCS#1 token + ClientOnline → avatar → lot on 34101.
- [x] **KNI Blazor speaks Aries** — `FSO.BrowserAries` + BrowserClient auto-join through gateway to LotJoined (integration test).
- [x] **Aries city + lot join (handshake)** — FindLot FOUND → `/lot` type 22 → ticket type 21 → HostOnline → ClientOnline → empty `FSOVMTickBroadcast`.
- [ ] **Lot VM stream / real LotView** — real tick contents; wire `FSO.LotView` (needs Mario stub + S3 iOS XNBs).
- [x] **S5 lot placeholder** — after `LotJoined` (or `?lot=1`), BrowserClient draws isometric grass diamonds (FreeSO GRASS colors); WASD pan. Not real LotView.
- [x] **KNI BlazorGL S0 + S2 texture** — `FSO.BrowserClient` loads `HttpContentStore` → `Texture2D` (`sample-content/textures/squares.png`).
- [x] **KNI S1** — `FSO_GRAPHICS` switch; lib chain through `FSO.Client` on KNI; Mac on MonoGame.
- [x] **Content store wired** — Composite (BasePath + Content/) + GetResource/FileProvider; remaining providers/TS1 still disk.
- [ ] S3–S4, S6, S8 of KNI-MIGRATION (effects, audio, threads, full UI); real S5 LotView
- [ ] Threading cleanup; `VMServerDriver` is the risky one — 1-2 weeks
- **Status:** in_progress
### Phase G: Neighbourhood scaling
- [x] `PackTools/citygen/generate_city.py` reviewed and run ✅ — San Francisco: 39.4 km square, elevation −5..781 m, 42,159 OSM road ways, full raster set written to disk
- [ ] **Never loaded into the game.** Host it as a playable city; correct-looking PNGs are not a playable world.
- [ ] Population, density, landmarks — an empty accurate map is a map, not a neighbourhood
- **Status:** partly done

## Key Questions
1. **Does a hand-authored blueprint XML load cleanly into a live lot?** A1 answers it, and everything in Phase A depends on it.
2. ~~**What is the scale mapping from a real home to a 77-tile lot?**~~ **Answered: 1 tile = 1 m, and capacity was never the constraint** — the 77/`FloorClip` framing was job-lot machinery. Legibility below 1 m is the real limit. See A1.
3. Can a vision model produce a valid room layout from a floor plan at all? The one genuinely untested integration.
4. Does the WebSocket gateway come back buildable? Only affects Phase F now, not the whole plan.
5. Can per-object cost get under ~$0.15 before the catalog gets built?

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| AI builds the house from a photo/floor plan | Kat, 2026-08-08, chosen over manual-build-with-AI-assist and describe-it-in-chat |
| House replica + friends is the product | Kat's own framing. Multiplayer moves early; object authoring becomes supporting cast |
| Browser moved to the tail | It lowers install friction, it doesn't prove the idea. Its two costs — a gateway with no prior art, and ~200 original objects — shouldn't gate the demo |
| Sharing/remixing is a pillar | Stated directly as one of three |
| Build on `upstream/archive`, not `master` | `master` is a year stale; `archive` is where upstream develops, ships weekly, has native macOS CI |
| MVP stays on self-downloaded TSO assets | Legal and proven; upstream now ships a built-in installer. Original content replaces them progressively |

## Notes
- **Standing rules in `CLAUDE.md`** — check all four places before building anything (this tree, other branches, upstream, the wider world). Four duplications in one day is why.
- Build mode, lot save/load, blueprint restore, multiplayer and visiting all already exist in the engine. **Assume a feature exists until proven otherwise.**
- Compiling clean is not rendering. Verify by running.

# Task Plan

## What this is
**A browser-based multiplayer life simulation with easy in-world creation, where rooms, objects and stories are shared and remixed.**

Kat, 2026-08-08, the version that matters most: *"what i want is people to make a mini replica of their house themself and hang out with friends virtually."*

That sentence is the product. Every pillar below serves it.

### The three pillars
1. **Browser-based multiplayer life simulation** — no install, click a link, your friends are already in there.
2. **Easy in-world creation and customization** — make your house, your furniture, in the game, by describing it.
3. **Sharing and remixing** — rooms, objects, and stories, not just objects.

### What this replaces
Supersedes the framing in `PRODUCT-DIRECTION.md` (which centred "content worth having + player modding" and left browser-vs-desktop open). Browser is decided. Sharing/remixing moves from "deferred past MVP" to a pillar. The house-replica goal is new and reorders everything below.

## Current Phase
Phase A

## What "replica of your house" changes
FreeSO **already has build mode** — walls, floors, multiple storeys, furniture placement, and lot save/load (`VMMarshal`, blueprints, plus an upstream `lot-serialize` branch). So the job is not "let people build houses." It is **making it fast to build *your* house**, and getting friends standing in it.

That reorders the work: the multiplayer/hosting story stops being a browser prerequisite and becomes the product itself. Object authoring becomes supporting cast — you need *your* couch, not whimsical one-offs. Cities-from-geography stops being a separate pillar and becomes the setting: your house, in your actual neighbourhood.

## Phases

### Phase 0: Correct base — COMPLETE
- [x] PackTools on `upstream/archive` (`packtools-on-archive`) — 56/56 + 48/48 green on net9.0
- [x] Make Something panel + wiring ported (`8ddc26826`); client builds clean, 0 errors. Ported our hunks only — archive had since added the user list, city painter layer, surround puppets and city edit button that a wholesale copy would have deleted
- [x] Duplication audit; stale docs corrected; standing rules in `CLAUDE.md`
- **Status:** complete

### Phase A: Watch the loop work — nobody has
- [ ] Launch the client, open Buy Mode, **click "Make Something"** and watch an object appear. Everything is built and now builds on the right base; no human has seen it run.
- [ ] Catalog thumbnails render blank. Lead: `UICatalog.GetObjIcon` reads a BMP chunk we never emit. **Check first** — archive added `IconCache`/`GetOrAddGeneratedIcon` that the old base lacked.
- **Status:** in_progress

### Phase B: Friends in a lot together — this is the product, not plumbing
- [ ] Get two people into one lot, hosted somewhere real. Archive Mode runs self-hosted off a local SQLite clone — establish what it takes for a second person to join.
- [ ] **WebSocket gateway spike.** FreeSO speaks raw TCP (`AriesClient` → Mina); browsers cannot open raw TCP, no client-side workaround. No prior art here, upstream, or in the MonoGame/FNA community — the only open-ended piece in the plan.
- **Why this is early:** "hang out with friends" is the point. A house nobody can visit is a screensaver.
- **Status:** pending

### Phase C: Build *your* house, fast
- [ ] Establish the real baseline first: how long does building a small real house take in FreeSO's existing build mode today, by hand? Nobody has measured it. **Do not build tooling before knowing what's actually slow.**
- [ ] Then target whatever that shows — plausible candidates: describe-a-room in the chat panel, floor-plan tracing, room templates. Pick after measuring.
- [ ] Furniture matching real rooms — existing generators: chair, sofa, table, bed, lamp, storage, primitives.
- **Status:** pending

### Phase D: Sharing and remixing — pillar, not an afterthought
- [ ] `SHARING-DESIGN.md` exists (publish, discover, fork, re-attribute, safety when a stranger's compiled behaviour runs on your lot) — design only, no code.
- [ ] **Rooms, not just objects.** Lot serialization already exists in-engine (`VMMarshal`, blueprints, upstream `lot-serialize`) — check how far it gets before building anything.
- [ ] Precedent worth copying: EA-Land's Custom Content Creator program — in-game upload, brand/collection/artist metadata, age rating, moderation for duplicates, no creator payment.
- **Status:** pending

### Phase E: Browser client port
- [ ] `MonoGame.Framework.DesktopGL` → KNI `nkast.Xna.Framework.*` / BlazorGL, across client **and** audio (`tso.sound` depends on it too) — 2-4 weeks, well-trodden
- [ ] Content loading disk → HTTP fetch (~86 files use `FileStream`) — 1-2 weeks
- [ ] Threading cleanup, 5 shipping files; `VMServerDriver` is the risky one — 1-2 weeks
- **Gated on B.** Rendering first would produce a browser tab that renders a lot nobody can join.
- **Status:** pending

### Phase F: Original content — gates legal browser distribution
- [ ] A web server serving EA's assets is the blocker (`STRATEGY.private.md`). ~200 original objects, not 3,132 (`CATALOG-PARITY-PLAN.md`); Tier 1 (~70) is the motive loop.
- [ ] Generators still needed: toilet, shower, sink, stove, fridge.
- [ ] **Cost blocks scale**: $0.08 trivial / $0.79 interactive / $1.72 complex per object. At $1.72, 200 objects costs more than the game. Recipes designed (`RECIPE-DESIGN.md`), unbuilt.
- **Status:** pending

### Phase G: Your neighbourhood
- [ ] **Read `citygen/generate_city.py` first** — exists, unreviewed, may be most of the job or a stub.
- [ ] A city is stacked image layers (`Documentation/Crafting a City.md`): elevation, terrain type, roads on tile edges, forest type + density, vertex colour. Real elevation → elevation; coastline → water; OSM roads → roads.
- [ ] Reference existing OSM→terrain work (Cities:Skylines OSM import, CityGen3D, Osmundi) rather than writing the transform from zero.
- [ ] Server cannot distribute custom cities — bundle with client, point the shard's `map` at it.
- **Status:** pending

## Key Questions
1. Does the WebSocket gateway come back buildable? If not, browser is off and the whole plan changes shape.
2. What is actually slow about building a house by hand today? Unmeasured — and Phase C's design depends entirely on the answer.
3. Can per-object cost get under ~$0.15 before the catalog gets built?
4. Audience: adults who played the original, or new players? Shapes art direction and onboarding.

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| Browser target | Kat, 2026-08-08: "we want in browser if possible." Costs a gateway + ~200 original objects; buys no-install distribution and no EA exposure |
| House replica + friends is the product | Kat's own framing. Reorders everything: multiplayer moves early, object authoring becomes supporting cast |
| Sharing/remixing is a pillar, not deferred | Stated directly as one of three pillars |
| Build on `upstream/archive`, not `master` | `master` is a year stale; `archive` is where upstream develops, ships weekly, has native macOS CI |
| Networking spike before rendering port | Rendering has named prior art; networking has none anywhere |
| MVP stays on self-downloaded TSO assets | Legal and proven today; original content replaces it progressively |

## Notes
- **Standing rules in `CLAUDE.md`** — check all four places before building anything (this tree, other branches, upstream, the wider world). Four duplications in one day is why.
- Build mode, lot save/load, multiplayer, and visiting already exist in the engine. Assume a feature exists until proven otherwise.
- Compiling clean is not rendering. Verify by running.

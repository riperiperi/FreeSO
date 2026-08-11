# Start here

Rewritten 2026-08-08 (evening). Read this before touching anything.

## What Kat is building

> A player uploads a floor plan or a room photo. The AI builds their actual home in a
> real-geography city. Their friends visit and hang out inside it. All multiplayer, all live.

Her words: *"what i want is people to make a mini replica of their house themself and
hang out with friends virtually."*

Three pillars: **browser-based multiplayer life simulation**, **easy in-world creation**,
**sharing and remixing rooms, objects and stories**.

Judge every task against that. A day was lost to work that was individually reasonable
and moved none of it.

**Mechanic decided:** the AI builds the house from a photo or floor plan; the player
refines it conversationally. Not manual building with AI assistance.

**Browser is decided but deliberately last.** It lowers install friction; it does not
prove the idea. Its two costs — a WebSocket gateway with no prior art anywhere, and
~200 original objects — should not gate the demo.

## State

- **Repo**: `~/Desktop/Github-Wiki/GitHub/FreeSO` — one folder, one worktree. Keep it that way.
- **Branch**: `packtools-on-archive`, based on `upstream/archive`. **Never pushed.**
- `mac-port` is superseded — it forked from `master`, which has been stale since Aug 2025.
  Do not build on it.
- Tests: `FSO.PackCompiler.Tests` 56/56, `FSO.ModServer.Tests` 48/48, on net9.0.
- `dotnet` is not on PATH here; use `~/.dotnet/dotnet`.

## Read these before you build anything

1. **`../CLAUDE.md`** — standing rules. Check all four places before writing code. Four
   duplications happened in one day for want of this.
2. **`../STATE.md`** — what FreeSO already provides vs what we added vs what's ahead.
3. **`../task_plan.md`** — the phases and why they're in that order.
4. **`Documentation/` on `upstream/archive`** — official docs from FreeSO's author.
   `git show upstream/archive:"Documentation/Crafting a City.md"`. They were missed for a
   whole day while the same ground was reverse-engineered.

## The next thing to do

**Floor-plan image → layout JSON.** That is the whole remaining gap in Phase A, and it is
one step wide. Everything downstream of that JSON is verified end to end, pixels included.

What already exists, so do not rebuild it:

- **`PackTools/FSO.HouseGen`** — `HouseLayout` (rooms as tile rectangles, doors on wall
  edges) and `BlueprintWriter`, layout JSON → blueprint XML, deterministic, no AI in the
  path. The vision model's only job is to emit that JSON.
- **The delivery path** — `XmlHouse.cs` parses the XML, `VMWorldActivator.LoadFromXML()`
  builds the world, and `VMBlueprintRestoreCmd` rebuilds a lot mid-game from raw bytes.
- **Two oracles.** `examples/house-one-room.xml` is hand-authored and the writer reproduces
  it element-for-element on the low edges. `examples/layouts/kat-flat.json` is a
  three-room flat with doors that has been **seen standing in the client**.

```sh
# layout -> blueprint (ALWAYS pass --base, see below)
~/.dotnet/dotnet PackTools/FSO.HouseGen/bin/Debug/net9.0/FSO.HouseGen.dll \
  PackTools/examples/layouts/kat-flat.json out.xml \
  --base TSOClient/FSO.Content.TSO/Content/Blueprints/empty_lot_fso.xml

# blueprint -> live VM, reports rooms/doors/objects and the lot-phone check
~/.dotnet/dotnet PackTools/FSO.VMHarness/bin/Debug/net9.0/FSO.VMHarness.dll --house out.xml
```

To see one in the game: copy the XML into
`~/Library/Application Support/The Sims Online/TSOClient/housedata/blueprints/`, launch
FreeSO, click **Sandbox Mode** (top-left of the login screen), pick the file. **Keep `0`
out of the filename** — `BlueprintReset` infers a job level from the path via
`path.Substring(path.IndexOf('0'), 2)` and will clip and offset the house if that parses.

Three things that pass every headless check and still fail:

1. **No lot phone (`0x313D2F9A`) → grey screen.** `VMWorldActivator` sets
   `VM.TSOState.Size` only when the blueprint contains it. Always generate with `--base`.
2. **Objects use a different level convention than floors and walls.** Floors/walls get
   `+1` applied; objects do not, and `level="0"` skips positioning entirely.
3. **Walls must be written twice** — low edge plus the mirrored high-edge bit on the
   neighbour. Enclosure works without it; doors do not.

Scale mapping is settled: **1 tile = 1 metre**, minimum room dimension 2 tiles. The
`FloorClip`/`TargetSize` machinery is job-lot only; a residential lot gives ~75x75 usable,
so capacity is never the constraint — legibility below 1 m is.

Still open from A1: nobody has walked a Sim through a door.

Note `VMBlueprintRestoreCmd.Verify()` returns `!FromNet` — a client cannot send it. The
generator runs server-side.

## How to work here

- **Do not build what already exists.** Not ours, not upstream's, not the community's.
  Check this tree, other branches (`git log --all -S "<symbol>"`), upstream, then the
  wider world. See `../CLAUDE.md`.
- **Docs go stale within hours.** A status line saying "not yet implemented" is a lead to
  verify, never a fact. That exact line nearly caused a duplicate fix.
- **Verify before Kat looks.** She was sent into the game four times to test broken builds.
  Get the screenshot yourself.
- **Assert pixels, not chunk presence.** Three bugs shipped because a test checked a chunk
  existed. Existence is not rendering.
- **Commit with a throwaway index** — sessions share this working tree:
  ```sh
  TMPIDX=$(mktemp); export GIT_INDEX_FILE="$TMPIDX"
  git read-tree HEAD; git add <exact paths>; git commit -F -
  unset GIT_INDEX_FILE; rm -f "$TMPIDX"; git reset
  ```

## Working / not working

**Working**: pack compiler + decompiler, MCP server (13 tools), headless VM harness, live
object injection, agent bridge (pet rock ~$0.08, gnome ~$0.79, fortune cat ~$1.72), seven
art generators, contact-sheet review, prompt caching, `PackTools/citygen` — San Francisco
generated and verified (39.4 km square, 42,159 OSM ways) — and **blueprint XML → live
enclosed house** (A1, `d962fed12`).

**Seen on screen, 2026-08-10**: a generated three-room house with doors, standing on a lot
in the real client via Sandbox Mode (`FSO.HouseGen` + `examples/layouts/kat-flat.json`).
The first visible output of this pipeline. Note the trap it cost a day to find: a blueprint
with no lot phone (`0x313D2F9A`) passes every architecture check and renders a grey screen —
generate with `--base Content/Blueprints/empty_lot_fso.xml`.

**Not working / untested**: the Make Something panel has never been clicked by a human;
no Sim has walked *through a door* yet; the generated SF has never been loaded into
the game; **floor-plan image → layout JSON does not exist** (the last missing link);
per-object cost is too high for a 200-object catalog.

**Fixed, don't re-chase**: catalog thumbnails. They rendered blank on `master`/`mac-port`
because `UICatalog.GetObjIcon` set `null` when an object had no BMP chunk, and we emit
none. Upstream `4c89dab20` added a `CatThumbGenerator.GenerateThumb` fallback on
`archive`, so on this branch they render (`UICatalog.cs:449`).

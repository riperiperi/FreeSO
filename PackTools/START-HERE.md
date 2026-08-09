# Start here

Handoff written 2026-08-08, end of the first real build day. Read this before
touching anything.

## What Kat is building

Her pitch, in her own words to a friend that afternoon:

> I'm making an open source version of sims… Making it "ai native" so instead of
> being a dev to make a mod, you can make your own mod in the game with ai.
>
> Also you can make a new city that is a real version of a city. Like it takes
> the geo of SF and makes an sf world.

**Two pillars: in-game AI modding, and real cities from real geography.** Judge
every task against those. A day was lost to work that was individually
reasonable and moved neither.

Mods mean **more than objects** — behaviours, rules, events. Objects are a
special case. `PRODUCT-DIRECTION.md` has the longer version, including two
framings that were explored and rejected.

## Read these before you build anything

1. **`Documentation/` on `upstream/archive`** — 13 official docs from FreeSO's
   author. `git show upstream/archive:"Documentation/Crafting a City.md"`.
   They were missed for an entire day while the same ground was reverse-
   engineered by watching objects render black. Don't repeat that.
2. `PRODUCT-DIRECTION.md` — what this is and isn't.
3. `RENDER-VERIFICATION-STATUS.md` — how to actually get into the game.
4. `BROWSER-VIABILITY.md` — rendering is near-solved (KNI); the blocker is raw
   TCP needing a WebSocket gateway.

## The three things to do next, in order

**1. Click the Make Something button.** The full loop is built, wired and
committed (`ef5d5c2b7`, `be6baeed9`) — panel → agent process → compiled object →
registered live into the running game. Both sides compile; the protocol is
verified end to end from the command line. **Nobody has clicked it in a running
game.** Do that first. Everything else is speculation until it's watched working.

**2. Catalog thumbnails render blank in-client.** Our objects reach Buy Mode but
their tiles are empty. Best lead, not yet confirmed: `UICatalog.GetObjIcon` reads
`obj.Resource.Get<BMP>(obj.OBJ.CatalogStringsID)` — a dedicated BMP chunk we have
never emitted. A prototype exists at
`/Applications/FreeSO.app/Contents/MacOS/Content/Objects/prototype_chair.iff`
(GUID `0x6B4FEE01`): a real base-game chair with our sprites swapped in, keeping
its original BMP. If it shows a thumbnail and ours don't, that confirms it.

**3. Cities from real geography.** Per `Crafting a City.md`, a city is a stack of
image layers — elevation, terrain type (0 grass, 1 water, 2 rock, 3 snow, 4 sand),
roads on tile edges, forest type and density. So real elevation → elevation
layer, coastline → water, OSM roads → road layer. A data transformation, not
research, and it is half the pitch with nothing built.

## How to work here

- **Verify before Kat looks.** She was sent into the game four times to test
  broken builds and found two real bugs doing it. Get the screenshot yourself.
- **Assert pixels, not chunk presence.** Three separate bugs shipped because a
  test checked chunks existed. Existence is not rendering.
- **A tool that fails opaquely teaches a capable model to do the wrong thing** —
  five instances so far, see the memory note.
- **Commit with a throwaway index.** Sessions share one working tree, so
  `git add` can sweep in someone else's staged files:
  ```sh
  TMPIDX=$(mktemp); export GIT_INDEX_FILE="$TMPIDX"
  git read-tree HEAD; git add <exact paths>; git commit -F -
  unset GIT_INDEX_FILE; rm -f "$TMPIDX"; git reset
  ```

## State

Repo is `/Users/katlaszlo/Desktop/FreeSO`, branch `mac-port`, **never pushed**.
Working tree clean. PackCompiler and ModServer suites green.

Working: AI authoring end to end (pet rock ~$0.08, gnome ~$0.79, fortune cat
~$1.72), six art generators, one mid-century collection, contact-sheet review,
live object registration, prompt caching.

Not working: catalog thumbnails; cost is too high for players (recipe design in
`RECIPE-DESIGN.md`, unbuilt); the upstream migration is scoped and unfinished on
branch `macos-archive-migration`.

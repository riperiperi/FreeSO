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

**A2: floor-plan image → blueprint XML.** The only missing link in Phase A.

**A1 is done** (`d962fed12`). Houses are already data: a lot is a blueprint XML —
`<floors>`, `<walls>`, `<object>`, each with tile coordinates and a level.
`XmlHouse.cs` parses it, `VMWorldActivator.LoadFromXML()` builds the world, and
**`VMBlueprintRestoreCmd` is a live network command that takes that XML as raw bytes
and rebuilds a lot mid-game** — the server already uses it to reset lots. A hand-written
`PackTools/examples/house-one-room.xml` goes through that path into a live headless VM
and the engine derives a sealed interior from it:

```sh
~/.dotnet/dotnet PackTools/FSO.VMHarness/bin/Debug/net9.0/FSO.VMHarness.dll \
  --house PackTools/examples/house-one-room.xml
# -> 16 floor tiles, 15 wall tiles, probe (33,33) inside an enclosed room
```

So A2 has a known-good file to compare its output against. Still open from A1: nobody
has walked a Sim inside — architecture is proven, occupancy is not.

Note `VMBlueprintRestoreCmd.Verify()` returns `!FromNet` — a client cannot send it. The
generator runs server-side.

**Unanswered and it will bite:** lots are 77 tiles with `FloorClip`/`Offset`/`TargetSize`.
A real home needs a tiles-per-foot rule and a rule for what gets dropped.

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

**Not working / untested**: the Make Something panel has never been clicked by a human;
no Sim has walked inside a generated house; the generated SF has never been loaded into
the game; floor-plan → XML does not exist; per-object cost is too high for a 200-object
catalog.

**Fixed, don't re-chase**: catalog thumbnails. They rendered blank on `master`/`mac-port`
because `UICatalog.GetObjIcon` set `null` when an object had no BMP chunk, and we emit
none. Upstream `4c89dab20` added a `CatThumbGenerator.GenerateThumb` fallback on
`archive`, so on this branch they render (`UICatalog.cs:449`).

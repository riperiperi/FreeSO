# Render verification status — MILESTONE HIT: confirmed rendering, one real bug found

Goal: confirm an AI-authorable object actually renders in the running game (not just
compiles/installs cleanly) — see `ART-PIPELINE-DESIGN.md`/`SCHEMA.md` for why this
specific gap matters (objects have compiled clean and rendered nothing before, silently).

## Result: it renders. This is the first time anyone has watched that happen.

Kat got into a live, hosted lot and opened Buy Mode → Seating. `Verify Stool`
(GUID `0x6B4F0A01`, `appearance.generated.generator: "chair"`) appears in the catalog
with a correct chair thumbnail and `Retail Price: §75`. `Gossip Gnome` (`0x6B4F0001`,
§120) was also visible in the same catalog. Registration, catalog entry, content-dir
install path, and sprite loading are all proven end to end, in the real client, not a
headless harness.

## Root cause of the multi-hour GUI blocker: Quick Start joins, it doesn't host

**The actual fix that unblocked everything**: the title screen's **Host Server**
button, not **Quick Start**. Quick Start silently tries to *join* a local server —
`archiveConfig.json` had `lastJoinedHost: "127.0.0.1"` pointing at ports (city 33101,
lot 34101) nothing was listening on. With no server, there's no world, so
`VMContext.CreateObjectInstance` NREs (`VMMultitileGroup.ChangePosition`) the instant
*anything* is placed or a category is opened with items in it — this produced two
scary-looking crashes (one in Build Mode, one in Buy Mode) that were 100% infrastructure,
not content. Confirmed via `lsof` (nothing on 33101/34101/37564) and the config file.
**Next person: click Host Server, not Quick Start, to get a lot with no external server
needed.**

Two false leads worth recording so nobody re-chases them:
- Two "coloured blob" objects seen near the Sim on the lot were misread as an art
  defect in our renderer. They're pre-existing seasonal decor already installed in the
  shipped app's catalog (`Fly Agaric Fungi` `0xFE699E4`, `Scarlet Spotted Stool`
  `0x6A17DB40` — mushrooms). Not ours, no sprite-offset bug demonstrated.
- The "floating" appearance was also initially misattributed to sprite offsets. It's
  not — see the real bug below, same root cause as the placement error.

## The one real, confirmed bug: `AllowedHeightFlags` never set on generated objects

**Symptom**: our objects refuse placement on grass ("Must place on floor tile") and
render floating above the tile when they can be placed at all.

**Root cause** (found by reading, not guessing): `VMContext.GetObjPlace`
(`TSOClient/tso.simantics/VMContext.cs:1240`) reads
`VMStackObjectVariable.AllowedHeightFlags` and returns `HeightNotAllowed` when bit 0 is
clear. `UIObjectHolder.cs:143` uses the same flag/bit to decide the object's vertical
draw offset — bit 0 clear draws it `4/5` of a level too high. Base-game objects set
this flag in their own `init` BHAV; our generator's `init` tree (in `PackBuilder.cs`)
only zeroes declared attributes and never touches it, so it stays 0 on every object we
emit.

**Fix** (not yet implemented — `PackBuilder.cs` is owned by other sessions, coordinate
before editing): generated `init` trees need to set `my_object` scope index 4
(`AllowedHeightFlags`) to the value a working base-game chair uses (confirm by
decompiling one's init BHAV rather than assuming — there may be a wider mask for
floor+terrain both). Add a regression test on the emitted OBJD/init, since this is
exactly the class of bug a headless VM harness can't see (placement/draw-offset only
matters with `UseWorld` true).

## Process notes, for next time

- **Desktop contention is real and expensive.** ~90 minutes lost to coordinate-click
  automation racing 10+ other Claude sessions' terminal windows for foreground on a
  shared machine. Escalating to ask the user to clear her own desktop (rather than an
  agent unilaterally hiding/quitting her other apps) was the right call — she chose to
  do it herself. Two stray clicks briefly exposed personal content in debug screenshots
  during the contention; both were deleted immediately and not acted on. If a task
  needs sustained foreground GUI control, ask before assuming a busy desktop is
  available for automation, and prefer a human driving the mouse once the target
  screen is reached — it's faster and doesn't fight the user for the cursor.
- Kat driving directly (once the desktop was clear and the host-vs-join issue was
  found) was faster and more reliable than any amount of coordinate automation.

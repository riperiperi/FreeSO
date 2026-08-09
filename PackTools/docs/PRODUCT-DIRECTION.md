> **Superseded 2026-08-08 (evening) — kept for the reasoning, not the conclusions.**
>
> The product is now stated in one line: *a player uploads a floor plan or room photo, the
> AI builds their actual home in a real-geography city, and their friends visit and hang
> out inside it.* See `../task_plan.md` and `../STATE.md`.
>
> What changed since this was written, same day:
> - **The open browser-vs-bring-your-own decision is closed.** Browser, but late — it
>   lowers install friction rather than proving the idea, so it sits at the tail.
> - **"Mods mean more than objects" is now concrete**: the first mod anyone makes is their
>   own house. Object authoring is supporting cast — you need *your* couch.
> - **Cities stopped being a separate concern** and became the setting: your house, in your
>   real neighbourhood.
> - **Sharing and remixing was promoted** from post-MVP to a pillar.
>
> Still true and worth reading below: the simulation is the point and stays; FreeSO already
> gives us multiplayer and lots; recreating EA's catalog is worth paying for only to unlock
> distribution, not for its own sake; and both rejected framings are still rejected.

# Product direction

The single page everything else gets judged against.

## What it is

**A multiplayer Sims.** Kat's words, 2026-08-08: *"honestly a multiplayer sims is
what I want."*

That already exists — it is FreeSO, and it runs today: needs simulation, careers,
lots, avatars, chat, an economy, multiplayer. **We are not building a new game.**
We are building two things on top of one that works:

1. **Content worth having** — furniture and objects that look good, in styles the
   game never had.
2. **Player modding** — people changing the game themselves, not just spawning
   objects.

Everything else in this repo is in service of those two.

## What that keeps

The simulation stays. Needs, motives, hunger, sleep, hygiene are the point, not
texture — a lot has to keep a Sim alive, so beds, toilets, showers, sinks,
stoves and fridges are load-bearing content, not decoration.

Social is already there. FreeSO has the multiplayer, the lots, the chat. We do
not need to invent a social layer; we need to give people reasons to be in it.

## Mods mean more than objects

Kat, same session: *"the idea was that they could make their own mods in general.
not just objects."* New interactions, rules, behaviours, events — objects are a
special case. This suits the toolchain: SimAntics BHAV trees **are** the game's
logic layer, so the authoring pipeline points at behaviour as naturally as
appearance. Behaviour mods are also cheaper (little or no art generation) and
mostly reuse existing art.

## The one decision that shapes months of work

**Do players bring their own copy of the game, or does it run in a browser?**

| | Bring your own copy | Browser |
|---|---|---|
| Base catalog | **Use EA's objects.** Free, better than ours, works today. | **Recreate ~200 objects** as original art — we must own everything we serve. |
| Our generators | Make *new* furniture on top (mid-century, Scandinavian, contemporary). Purely additive. | Do parity first, then the new lines. |
| Blocker | None — this works now. | Raw TCP → WebSocket gateway (`BROWSER-VIABILITY.md`). Rendering is near-solved via KNI. |

This is genuinely undecided and worth deciding deliberately rather than drifting
into. `CATALOG-PARITY-PLAN.md` holds the analysis for the browser branch
(3,132 objects, 341 families, what a lot actually needs, ~200 target).

Note that recreating EA's catalog produces a *worse* version of art that already
exists — at current fidelity, sound proportions and flat colour. That cost is
worth paying only to unlock browser distribution, not for its own sake.

## Open questions

- Browser or bring-your-own — see above. Everything downstream waits on it.
- Audience: adults who played the original, or new players?
- Whether fidelity gets revisited. FreeSO already contains a 3D geometry path
  (`tso.files/RC/DGRP3DGeometry.cs`, `FSOF.cs`), so it is a decision, not a
  rewrite. Deferred, not ruled out.

## Rejected framings, and why

- **Habbo / Club Penguin social world.** Explored 2026-08-08 and dropped the same
  evening. It threw away the simulation, which is the thing Kat actually wants.
- **A new game inspired by the Sims.** Same problem: we would rebuild what
  FreeSO already gives us for free.

## What survives all of this

The pipeline, the six generators, the contact-sheet review surface, the cost work
(caching, `find_base_object`, batched tree authoring), and the mid-century
collection. None of it depends on which branch we take. See
[[freeso-state-2026-08-08]] for current technical state and the open in-client
rendering bug.

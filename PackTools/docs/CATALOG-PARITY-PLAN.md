> **Re-scoped 2026-08-08.** The browser decision is made — we ship in a browser — but
> browser work moved to the *tail* of the roadmap (`../task_plan.md` Phase F), so this
> plan is no longer conditional, just late. It gates clean browser distribution, not the
> demo. The "still open" line below is stale; the rest stands.

> **Status: conditional.** This plan only applies if we ship in a **browser**,
> where we must own everything we serve. If players bring their own copy of the
> game, use EA's objects and point our generators at *new* furniture instead.
> See `PRODUCT-DIRECTION.md` — that decision is still open.

# Base catalog parity plan

Goal: recreate enough of the base game's catalog **as original art** that a lot
feels complete without shipping any of EA's assets. That is what makes browser
distribution possible; it is not an art exercise for its own sake.

Then, on top of parity, add contemporary collections (mid-century is done;
Scandinavian/flat-pack and modern next). Parity makes the game playable; the
modern lines make it feel like ours.

## Scope: recreate ~200, not 3,132

`packingslips/objecttable.xml` holds **3,132** base objects across **341**
families. Most of that count is not what a house needs — the largest families are
Car (215), Window (144), Trees (138), Door (129), Stair (129). Those are
architecture, scenery and vehicles, not furnishings.

**Target: ~200 objects.** The ordering principle is *how badly a functioning lot
needs it*, not how many variants the base game shipped.

## Tier 1 — a lot is unusable without these (~70 objects)

The motive loop: sleep, hygiene, bladder, hunger, comfort, energy. Miss one and
Sims cannot live there.

| Family | Base count | Recreate | Generator | Notes |
|---|---:|---:|---|---|
| Chair (dining) | 45 | 8 | `chair` | done for mid-century |
| Sofa | 132 | 8 | `sofa` | done for mid-century |
| Recliner / armchair | 20 | 5 | `chair` | wider seat, arms |
| Dining Table | 74 | 6 | `table` | |
| Coffee / side table | 11 | 6 | `table` | done for mid-century |
| Bed | 78 | 8 | `bed` | |
| Toilet | 15 | 4 | **needed** | plumbing |
| Shower | 4 | 3 | **needed** | plumbing |
| Tub | 17 | 3 | **needed** | plumbing |
| Sink | 8 | 4 | **needed** | plumbing |
| Stove | 6 | 4 | **needed** | appliance |
| Fridge | 4 | 3 | **needed** | appliance |
| Counter | 25 | 6 | `storage` | surface + cabinet |
| Lamp (table/floor) | 59 | 8 | `lamp` | |
| Trash | 8 | 2 | `primitives` | |

**Six generators do not exist yet** — toilet, shower, tub, sink, stove, fridge.
All are box-and-cylinder compositions; the `primitives` composer may cover the
first pass, with named generators only where construction knowledge repays it
(the storage generator's "shelf boards need real thickness or they vanish"
lesson is the precedent).

## Tier 2 — a lot feels empty without these (~70 objects)

Fun, social, skill, and the things that make a room read as furnished.

Dresser (35→6), Bookshelf (10→5), Desk (19→5), Television (11→5),
Stereo (11→4), Computer (7→3), Painting (74→10), Rug (112→10),
Plant (16→6), Mirror (6→3), Clock (4→2), Bar (33→5).

Paintings and rugs are the cheapest wins in the catalog: flat, forgiving at
20-40px, and they do most of the work of making a room look decorated.

## Tier 3 — venue and social objects (~60 objects)

What makes lots worth visiting rather than just livable: Pool Table (7→3),
Piano (19→3), Fireplace (19→4), Barbecue (5→2), Pool (21→4), Fence (20→6),
Column (18→4), Sculptures (57→8), Awning (16→4), Sign (16→4).

## Deliberately excluded

Car (215), Window (144), Door (129), Stair (129), Trees/Tree/Shrub (182),
NPC (36), Job Object (28), Food (31) and the venue-specific families
(Restaurant, Night Club, Front Desk, Fishing Pier, Igloo, Tent…). These are
architecture, scenery, or gameplay props tied to systems we are not recreating
yet. Revisit after Tier 3.

## Naming rules (these decide whether it ships)

- Our objects get **our own names**. Functional descriptions are fine
  ("Walnut Dining Chair"); EA's specific product names are not.
- Modern collections take the **design language** of contemporary retailers —
  proportions, palettes, materials — and **never their brand or product names**.
- Nothing derives from EA's art. Generated only.

## Honest constraint

At current fidelity a recreated catalog looks like the mid-century sheet: sound
proportions, flat colour, ~20-40px. Adequate for parity, not exciting. The
argument for revisiting fidelity gets stronger once a full catalog exists to
judge — and FreeSO already contains a 3D geometry path
(`tso.files/RC/DGRP3DGeometry.cs`, `FSOF.cs`), so the engine does not block it.
Deferred deliberately, not ruled out.

## Order of work

1. Six missing Tier 1 generators (plumbing + appliances) — the true blocker.
2. Tier 1 parity in one coherent style, contact-sheeted as a set.
3. Tier 2, prioritising paintings and rugs for the effort-to-effect ratio.
4. Second and third collections (Scandinavian, contemporary) across Tier 1+2.
5. Tier 3 venue objects.

Regenerate these counts rather than trusting them:
`packingslips/objecttable.xml`, group by the display name before the first
` - `.

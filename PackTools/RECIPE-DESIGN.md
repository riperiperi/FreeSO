# Recipe Authoring — Design (Tier 2)

**Status: design only, nothing built.** Written while the AI authoring lane was parked so it can be resumed from a document rather than from memory. Supersedes nothing; Tier 1 (generated `main_loop`/`init`, implicit always-test) shipped in `57ab7d4dc`.

## The problem this solves

Cost per object is the blocker. Measured, both completing:

| object | turns | output tokens | cost |
|---|---:|---:|---:|
| pet rock (trivial) | 9 | 1,357 | **$0.084** |
| gossip gnome (interactive) | 21 | 14,872 | **$0.788** |
| fortune cat (complex) | 33 | 24,996 | **$1.718** |

Kat's verdict on that curve: *"that's like too much cost tho in general for every single object."* She's right — five objects would cost more than a game.

**Output tokens are the entire bill now** (input is ~free post-caching). So the lever is making the model *emit less*, not making tokens cheaper. A cheaper model divides the price; a higher abstraction divides the volume — and they multiply.

The gnome's 14,872 output tokens were spent overwhelmingly on emitting tree nodes one at a time. Tier 1 removed the boilerplate trees; Tier 2 removes node-level authoring for the common cases.

## The idea

The agent emits a **recipe** — a declaration of what the object *does* — and the compiler expands it deterministically into trees, nodes, branches, and string tables.

```json
"recipes": [
  {
    "on_use": {
      "name": "Gossip",
      "walk_to": true,
      "say": ["Did you hear about Bob?", "Don't tell anyone, but..."],
      "motive": { "fun": 15, "social": 10 },
      "count": "times_gossiped"
    }
  }
]
```

That one block replaces four trees and ~10 nodes. One recipe ≈ one interaction ≈ one tool call, versus roughly a dozen `edit_tree_node` calls today.

## Vocabulary (v1 proposal)

Deliberately small. Every field maps to a primitive sequence the compiler already emits.

| Field | Type | Expands to |
|---|---|---|
| `name` | string | TTAB/TTAs entry (the pie-menu label) |
| `walk_to` | bool | `goto_relative` in front of the object, facing it |
| `animate` | `{source, id}` | `animate` node with the given animation |
| `say` | string \| string[] | STR# dialog entries + `dialog_private`; an array picks one at random (`random_number` + branch) |
| `balloon` | string | `set_balloon_headline` |
| `motive` | `{motive: delta}` | one `expression` per motive against `my_motives` |
| `count` | attribute name | `+= 1` against `stack_object_attributes` (**the object**, not the caller — see below) |
| `set` | `{attribute: value}` | assignment against `stack_object_attributes` |
| `chance` | 0.0-1.0 | wraps the remaining effects in a `random_number` gate |
| `require` | expression-ish | becomes the interaction's `test` tree instead of always-allowed |

Two rules the expansion must enforce, because both have already bitten us:

1. **`count`/`set` target the object, not the caller.** `my_attributes` inside an interaction resolves to the *avatar* (documented in SCHEMA.md; it cost hours). The recipe layer must always emit `stack_object_attributes`, so the trap becomes unreachable rather than merely documented.
2. **Motive deltas need a visible baseline.** Motives start at cap, so `+=` is often invisible. Expansion should be capped/clamped and the docs should say what a player will actually observe.

## Coverage against the five examples

Checked field-by-field against each pack in `examples/`.

| pack | expressible? | what's missing |
|---|---|---|
| `pet-rock` | ✅ fully | nothing — it's decorative with no interactions |
| `gossip-gnome` | ✅ fully | `walk_to` + `say[]` + `motive` + `count` covers it exactly |
| `mood-lamp` | ⚠️ mostly | needs a **state toggle** (on/off) — recipes have `set` but no notion of a value that alternates |
| `wishing-well` | ⚠️ mostly | `chance` covers the random payout; **modifying a global/simoleons** is not in the vocabulary |
| `fortune-cat` | ⚠️ partly | three interactions is fine, but one **spawns another object** (`create_object_instance`) which recipes cannot express |

**So v1 covers 2 of 5 fully and 3 partially.** That is the honest number and it is the argument for the escape hatch below, not an argument against recipes — the two it covers fully are the two simplest, which is also where the volume will be.

Candidate v2 additions, in the order the examples demand them: `toggle` (mood-lamp), `spawn` (fortune-cat), `money` (wishing-well).

## Raw tree authoring stays

**Recipes are the common path, not the only path.** `add_tree`/`edit_tree_node` remain, and a pack may mix: recipes for the ordinary parts, hand-authored trees for anything else.

The failure mode to design against is specific: **a recipe vocabulary that quietly cannot express what a player asked for.** If the agent can only reach for recipes, it will approximate — and the player gets an object that isn't what they described, with nothing in the narration admitting it. So:

- The recipe compiler must **fail loudly** on an unknown field rather than ignoring it (same rule as the rest of the schema).
- The tool description must state plainly that recipes cover common behaviours and that unusual ones need trees, so falling back reads as normal rather than as failure.
- When the agent falls back, that's a **signal worth logging** — a recurring fallback is a missing recipe field, and that's how the vocabulary should grow. Let usage decide v2, not guesswork.

## Expected saving (prediction, untested)

The gnome's cost is dominated by node-level output. Recipes should collapse ~10 `edit_tree_node` calls into 1 recipe call, cutting **both** turns and output tokens.

- Prediction from the other session: **Tier 1 + Tier 2 → gnome under $0.15**, and **~$0.03 on Haiku**.
- My own read: plausible on output volume, but *turns* may not fall as far — the gnome spent 6 turns on vocabulary lookups and VM testing that recipes don't touch.

**Both numbers are predictions with nothing measured behind them.** Mark them as such until a run exists.

## Sequence when this resumes

1. Re-measure the gnome on Tier 1 alone — that number is not yet taken, and it sets the baseline Tier 2 is judged against.
2. Build recipes for the fully-covered cases (`on_use` with the v1 fields).
3. Re-measure. Then, and only then, compare models — measuring model prices against a workload that is mostly boilerplate optimises the wrong thing.

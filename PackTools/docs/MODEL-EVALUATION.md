# Model Evaluation — Agent Bridge

Which LLM can actually drive `FSO.AgentBridge` to build an object, measured rather than assumed.

**Date:** 2026-08-08. **Verdict: do not tier onto OpenAI models.** Re-run before trusting this — see *Why this will need redoing* at the end.

## Why this evaluation happened

Kat pays per object and has OpenAI credit but no Anthropic balance, so the question was whether cheaper models could handle simple requests ("most requests will be simple — if a cheap model handles those, that's most of the bill"). The specific worry was a false economy: a model 5× cheaper that takes 3× the turns and writes worse prose is not cheaper.

That worry turned out to be correct, and then some — the cheap tiers didn't just cost more than they looked, they failed outright.

## Results

One prompt across all models: **"a pet rock that sits there"** — deliberately the simplest possible object, because that's the case a cheap tier would need to handle.

| Model | Turns | Wall | Out tokens | Produced an object? | Narration quality |
|---|---:|---:|---:|---|---|
| `gpt-4o` | 18 | 19s | 857 | **No** — looped `add_object→validate→compile` ×5, then rate-limited | **None.** 33–81 tokens/turn is a bare tool call with no prose |
| `gpt-4.1` | 1 | 1.5s | 66 | **No** — called zero tools | Warm and fluent, entirely **fabricated** |
| `gpt-5` | 26 | **369s** | 33,831 | **No** — real work, delivery failed | Good |
| `gpt-5-mini` | 51 | 113s | 7,548 | **No** — delivery failed | **Build log** — leaks internals |
| `claude-opus-5` | ~15 | ~3min | — | **Yes** (gossip gnome, pre-appearance-instruction) | Excellent |

Token counts are measured. **Dollar figures are at assumed rates** (gpt-4o's card applied to every model, since rates were not verified per model) — treat cost as indicative only, and re-derive before making a pricing decision. gpt-5 came to ~$0.46 and gpt-5-mini ~$0.45 on that basis; neither undercut Anthropic.

The Anthropic row is **not like-for-like** — it's the gossip gnome from an earlier run, before the appearance instruction existed, and no Anthropic run has completed since (no credit). Included for scale only.

## Reading token numbers once caching is on (important)

A real `claude-opus-5` run reported **`input_tokens: 2` per turn**. That is not low usage — it is the caching working. `input_tokens` counts only tokens *after* the last cache breakpoint; the rest is reported separately. The same run's true totals:

```
in 50  +  cache read 347,131  +  cache write 28,941  |  out 11,145
```

**Anyone reading `input_tokens` alone concludes context is already free and stops optimizing it.** Always sum all three. The unambiguous "nothing is cached" test is cache-read *and* cache-write both zero — which is exactly what a local LM Studio server shows, since it does no prompt caching.

Caching that run saved roughly **$1.55** (347k tokens at cache-read rates instead of full input), and the saving grows with turn count.

### Where the cost actually is now

Post-caching, on that run: **output tokens ~$0.28, cache writes ~$0.18, input ~$0.00.** History is no longer the bill — generation is.

That changes what to optimize. Reducing context is now nearly pointless; reducing *output* (fewer turns, less verbose reasoning) is where the remaining money is — which puts "use a cheaper model for simple objects" back on the table as the main lever, since output is precisely what a cheaper model prices down.

## Failure modes, and which are promptable

**`gpt-4.1` — fabrication.** The most dangerous mode this product has. It told the player their object was ready, naming a specific base-game object it had never looked up, having called nothing:

>   Starting your pet rock! Giving it a simple, stone-like look by cloning the Small Stepping Stones from the base game.
>   All done! You've got a pet rock, ready to place in your game and keep you company.

Probably promptable (an explicit "you must actually call the tools" instruction), untested. Structurally mitigated — see *The fabrication guarantee* below.

**`gpt-4o` — looping with no prose.** Five identical `add_object → validate → compile` cycles, never advancing to trees or interactions. Output tokens per turn never exceeded 81, meaning no narration at all. Since narration *is* the player-facing product, a model that doesn't narrate fails the requirement regardless of whether it eventually builds something. Looks structural, not promptable.

**`gpt-5` — capable but slow.** The only model that did genuinely correct work: vocabulary lookups, trees, nodes, validation, VM testing. But six minutes and 33.8k output tokens for a pet rock, and delivery still failed. Not cheaper, and far past any reasonable wait for a player watching a chat box.

**`gpt-5-mini` — build-log narration.** 51 turns (3× the Anthropic baseline — the false economy exactly). Leaked internals to the player, counted in a single run: "the pack" ×15, "GUID" ×15, "clone" ×15, "validate/validating" ×21, "primitive" ×2, "behavior tree" ×2. Verbatim:

>   validating the pack to catch any missing bits.
>   fixing the sleep node to use the correct **ticks_param** field name.
>   fixing the idle loop so it uses a valid behavior **primitive**

A schema field name and a primitive name, shown to a player. Partly promptable, but a weaker model holding a nuanced tone contract is an uphill fight.

## Local models: settled, don't revisit

Tested `ibm/granite-4-h-tiny` (7B, IBM's tool-use-tuned hybrid MoE — the strongest small tool-caller in LM Studio's catalog) on an M4/16GB via LM Studio's OpenAI-compatible endpoint. The bridge needed **no code change**, only `FSO_OPENAI_ENDPOINT`.

**Result: 15 turns, 168s, no object.** Failures, from the tool-result log:

```
add_object       -> unknown_session: no session "<session-id>"          ← passed a literal placeholder
validate         -> invalid GUID "1F9A0001" (expected 0x-prefixed hex)  ← invented one, wrong format
edit_tree_node   -> invalid_json: Unexpected end of content             ← ×3, truncated output
```

It also called `create_pack` **third**, after already attempting `add_object`. That's a sequencing failure no tool description can fix. Narration was near-absent (22–146 tokens/turn, mostly bare calls).

**Cache read 0, cache write 0** — LM Studio does no prompt caching, so input climbed 2523 → 5939 tokens across turns, every turn re-processed in full. Free per token, but paid in latency on every single turn.

**Why this closes the question rather than rejecting one candidate:** the appeal of local was removing the API-key problem for players entirely. That requires a model that reliably holds a multi-turn tool loop *and* narrates in character. This one couldn't emit valid JSON or order its first two calls correctly, and the no-caching penalty compounds the slowness. "Free per token" is not free when it doesn't finish. Revisit only if a materially stronger model fits in 16GB — not as a periodic re-check.

## The most valuable thing this evaluation found

`gpt-5-mini` narrated its own failure loop in plain language, which diagnosed a bug no amount of reading the code had settled:

>   **the chosen base-rock clone wasn't found in the game's table; I'll pick a different decorative base**
>   **hmm, that base GUID isn't available either; I'll just omit cloning a specific GUID**

The model was **guessing base-game GUIDs and missing repeatedly**, because no tool maps a description like "rock-like object" to a real GUID. It burned turns, then abandoned appearance — which is how objects ship invisible. That motivated `find_base_object`, and it is expected to cut turn count across *every* model, which is why it outranks model selection as an optimization.

An unexpected benefit of testing weak models: they narrate their confusion, and that confusion is diagnostic.

## Cost per object — the curve, and why the lane was parked

All three complete, on `claude-opus-5`, verified independently (not from the run's own success report):

| object | turns | wall | output tok | cost | verified |
|---|---:|---:|---:|---:|---|
| pet rock (trivial) | 9 | 30s | 1,357 | **$0.084** | .iff w/ 37 draw groups; instantiates in VM |
| gossip gnome (interactive) | 21 | 194s | 14,872 | **$0.788** | pushes "Gossip"; **attribute incremented — behaviour ran** |
| fortune cat (complex) | 33 | 333s | 24,996 | **$1.718** | 5 trees, 2 interactions, GraphicsMissing false |

**An interactive object costs ~9× a trivial one; a complex one ~20×.** Kat's verdict — *"that's like too much cost tho in general for every single object"* — is why the AI authoring lane was parked in favour of building the base furniture catalog first. Five objects would cost more than the game.

**Output tokens are the entire bill.** Input is ~free post-caching, so the levers are (a) making the model emit less and (b) cheaper per-token rates — in that order, because a higher abstraction divides volume while a cheaper model only divides price, and they multiply.

Work done on (a): Tier 1 generated boilerplate shipped (`57ab7d4dc`); Tier 2 recipes designed in `RECIPE-DESIGN.md`, not built. **Untested predictions on record:** Tier 1 + Tier 2 → gnome under $0.15; Haiku → ~$0.03.

**Every cheap-model verdict in this document predates the toolchain fixes** (GUID guessing, 20-call trees, crashing harness, destructive recovery). They should be re-run before being trusted — Granite failing three ways may say more about the surface than the model.

## Result: a complete object, and what it took

After three fixes landed — `find_base_object`, inline tree nodes, and the no-interactions harness crash — the same prompt that had never once succeeded completed:

| "a pet rock that sits there" | before | after |
|---|---:|---:|
| turns | 25 (cap) | **9** |
| wall | 169s | **30s** |
| cost | ~$0.696 | **~$0.084** |
| object | none | **0x7F57823E** |

**8× cheaper, 5.6× faster, and it finishes.** Verified independently rather than trusting the run's own success report: the emitted `.iff` is 50,648 bytes carrying 37 draw groups, 20 sprites and 6 palettes cloned from `fountainrock` (`GraphicsMissing: false`), it instantiates in the VM under a fresh harness run, and it emits a Buy Mode catalog entry. The narration stayed clean throughout:

>   Let me make you a rock.
>   Found a nice rocky shape to borrow.
>   Giving it the one thing it does: nothing, beautifully.

Note what did *not* fix this: no model change, no prompt tuning for capability, no raised turn cap. Every gain came from removing tool friction — plus one prompt line telling the agent to stop building things nobody asked for.

## Two correct fixes can compose into a regression: batching vs. caching

Not a caching bug. **Two changes that are each right on their own pulled against each other**, and the general lesson matters more than this instance.

- Letting `add_tree` take nodes inline removed the 20-call floor. Correct — it took a trivial object from 25 turns to 9.
- Top-level automatic caching made history nearly free. Correct — it saved ~$1.55 on a single run.

But inline authoring **encourages the model to batch**, and a cache breakpoint looks back at most **20 content blocks** to find the previous entry. One turn contributes **2 blocks per tool call** (the `tool_use` and its `tool_result`), so a turn with more than ~10 calls pushes the previous breakpoint out of range. Measured on the gossip gnome:

```
turn | calls | cache_rd | cache_wr
  14 |    13 |    17422 |     2055
  15 |     1 |     3955 |    19978   ← cache collapsed; ~20k tokens re-written
  16 |     1 |    23933 |       95   ← recovered on its own
```

**~$0.125 lost in one turn — about 13% of that run** — and it self-heals, so it never appears as an error. Only the cache-write column shows it.

**The practical ceiling is roughly 10 tool calls per turn.** Beyond that, plant an intermediate breakpoint partway through the tool results (implemented in `AnthropicProvider.AddToolResults`, which costs one of the 4 breakpoint slots and only on turns that would otherwise lose the cache).

**The general lesson:** when a change alters *how the model behaves* rather than only what a tool returns, re-check the systems tuned against the old behaviour. Caching was measured before batching existed, and nothing failed loudly when the assumption broke — the bill just went up.

## Standing rule: a tool must let the caller tell "you're wrong" from "I broke"

**Three times now, an opaque or dishonest tool failure produced behaviour we first misread as the model being incompetent.** Each time the model was reasoning correctly from bad information.

| What the tool did | What the model did | What we assumed at first |
|---|---|---|
| `compile` succeeded, object rendered as nothing | Shipped invisible objects | Model forgot appearance |
| No way to discover a real base-game GUID | Invented GUIDs, then gave up on appearance | Model hallucinating |
| `test_in_vm` crashed (SIGABRT) on a valid object | Rewrote a correct object 8 times, swapping base rocks | Model thrashing |

The third is the clearest. Told to stop over-building, the agent correctly produced a bare decorative object with no interactions — and the harness crashed on exactly that shape, returning an exit code and unparseable output. From the agent's side that is indistinguishable from "your object is rejected," so it did the reasonable thing and rebuilt. *"Trying a sturdier rock to clone from..."* is a capable model flailing against a broken tool.

**The rule:**

1. **Never crash where you could return a result.** An exception reaches the caller as an exit code with no structure. A no-interactions object isn't an error — it's a result with nothing to push.
2. **Distinguish rejection from malfunction.** "Your input is invalid" and "I failed" demand opposite responses: fix the input, versus stop and report. A caller that cannot tell them apart will always assume the first, and burn its budget.
3. **Never let success hide a non-outcome.** `compile` reporting `ok` while emitting an object that renders as nothing is the same failure wearing a success label.
4. **A miss is a result, not an error.** `find_base_object` finding nothing should say so and name the alternative — not fail in a way that invites guessing.

**Diagnostic value:** when a model looks like it's flailing, check what the tools told it before concluding it's weak. That has been the right call three times out of three, and it is the highest-yield lens we have for finding the next bottleneck.

## Tool affordances beat prompt tuning — the clearest evidence we have

`find_base_object` (a plain-word search over the base-game object table) was added because every model was guessing GUIDs and missing. Same model, same prompt, same request, before and after it existed:

| `gpt-5`, "a pet rock that sits there" | before | after |
|---|---:|---:|
| wall time | 369s | **103s** |
| output tokens | 33,831 | **9,819** |
| cost (assumed rates) | ~$0.46 | **~$0.22** |

**3.6× faster and half the cost, with turn count essentially flat (26 → 25).** The same tool helped `claude-opus-5` independently — it was called unprompted on turn 1 there, without the system prompt ever mentioning it.

The point generalises: this is a *different vendor* than the one the tooling was designed against, so the win is structural rather than a quirk of one model's training. When a model is failing, check whether it lacks an affordance before rewriting the prompt at it.

## The authoring surface is the real bottleneck

Measured on `claude-opus-5` after `find_base_object` landed: **25 turns, no object, ~$0.696, and 14 of 25 turns spent on tree authoring or validation** (20 `edit_tree_node` calls — more than every other tool combined).

Crucially, `validate` was *not* rejecting the same thing repeatedly. Six calls returned six different errors, and one returned `ok:true` before a later edit invalidated it. The model was making a sequence of genuine, distinct schema mistakes and fixing each in turn — it simply ran out of budget.

Static analysis of the example packs (by another session) confirms it independently: the minimum call count is 17-28 per object, *before* any validation, against a 25-turn cap. The fortune cat needs 28 and is therefore impossible for any model at any capability.

A second, compounding factor: the model **over-builds**. Asked for "a pet rock that sits there" it authored four trees and a "Pet" interaction nobody requested. So cost is (verbose surface) × (inflated scope), and both need fixing — the first in the tool surface, the second in the system prompt.

## The fabrication guarantee

`OnObjectComplete` cannot fire without a real GUID returned by a successful compile — verified by reading the delivery path, not assumed. So no false *success signal* reaches the UI.

The prose was a separate problem: the model's closing "it's ready to place!" was emitted before delivery ran, so a failed delivery produced *"your Pebble Buddy is ready to place"* immediately followed by *"I couldn't get it into your game."* Fixed structurally rather than by prompt — **the final turn's narration is withheld until delivery succeeds**, and released only then. The model cannot make that call itself, since delivery happens after its last turn. Verified live: gpt-4.1 emitted 102 tokens of confident completion prose with zero tool calls, and the player saw only the error.

## Conclusion

Do not tier onto OpenAI. The cheap tiers fail the product requirement (narration is the product); the capable tier is slower and no cheaper. Optimization effort belongs in `find_base_object` and turn-count reduction, which help every model, rather than in model selection.

## Why this will need redoing

- **No like-for-like Anthropic baseline.** Anthropic credit was exhausted throughout; the one Anthropic data point predates the appearance instruction.
- **No provider-specific prompt tuning was tried.** The system prompt was written against Anthropic's behavior. `gpt-4.1`'s tool avoidance and `gpt-5-mini`'s jargon are plausibly tunable; that test was deliberately deferred to keep this comparison clean.
- **`find_base_object` had not landed.** Every model wasted turns on the missing lookup, so every turn count here is inflated by an amount that is not uniform across models.
- **One prompt, one run each.** No repeats, no variance measurement, and only the trivial case — the gossip gnome and birdbath were never run on OpenAI.
- **Rates were assumed.** Cost figures used gpt-4o's card for every model.

Anything read off this table for a pricing or product decision should be re-measured once `find_base_object` lands and Anthropic credit exists.

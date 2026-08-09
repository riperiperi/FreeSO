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

## The most valuable thing this evaluation found

`gpt-5-mini` narrated its own failure loop in plain language, which diagnosed a bug no amount of reading the code had settled:

>   **the chosen base-rock clone wasn't found in the game's table; I'll pick a different decorative base**
>   **hmm, that base GUID isn't available either; I'll just omit cloning a specific GUID**

The model was **guessing base-game GUIDs and missing repeatedly**, because no tool maps a description like "rock-like object" to a real GUID. It burned turns, then abandoned appearance — which is how objects ship invisible. That motivated `find_base_object`, and it is expected to cut turn count across *every* model, which is why it outranks model selection as an optimization.

An unexpected benefit of testing weak models: they narrate their confusion, and that confusion is diagnostic.

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

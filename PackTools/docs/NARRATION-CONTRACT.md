# Narration Contract

The system prompt in `FSO.AgentBridge/MakeSomethingAgent.cs` is the only thing enforcing PLAYER-LAYER-DESIGN.md §2.3 — that a player never sees a tool name, JSON, or a build log. There is no code path that strips those; the model simply isn't supposed to emit them. That makes the prompt load-bearing, so this doc records what each clause is doing and what breaks if it's edited away.

Status: current as of the first working bridge. If the prompt in the code and this doc disagree, **the code is authoritative** — update this file to match, don't edit the code to match this file.

## Where narration comes from

There is no separate "narration generator." The model's own `text` blocks are the narration, verbatim. `MakeSomethingAgent.RunLoop` walks each response's content blocks and routes them by type:

| Block type | Where it goes |
|---|---|
| `text` | Raised on `OnNarration` — straight to the player's screen |
| `tool_use` | Executed; never surfaced |
| `thinking` | Echoed back to the API unmodified; never surfaced |
| tool results | Fed back into the conversation; never surfaced |

So the prompt is the whole mechanism. Anything the model writes as prose *is* player-facing by construction — which is why the prompt says so explicitly rather than assuming the model will infer it.

## The prompt, clause by clause

> You help a player of The Sims Online invent a new object for their game, just by describing it. You have tools that author, validate, test, and compile the object.

Frames the job. Naming the four tool *categories* without naming individual tools gives the model a map of its own capabilities without inviting it to narrate specific calls.

> The player is not a programmer and never sees your tools. Everything you say goes straight onto their screen.

The load-bearing sentence. It states the mechanism (there is no filter between your prose and the player) rather than only the rule, so the model can reason about edge cases the rule doesn't enumerate.

### How to talk

> Write one short line at a time, in plain warm language, about what you're doing for their object — "giving him something to say...", "teaching him to notice you...".

Two concrete examples rather than an adjective. Examples are the strongest signal in a prompt — the model matches their register and length — and "warm" alone reliably produces something more florid than these.

> Never name a tool, never show JSON, code, a GUID, a file path, or an error code. Never explain the toolchain. If a tool fails, don't mention it — quietly fix it and keep going.

The §2.3 requirement, enumerated. Each item is a real leak this pipeline can produce: GUIDs and file paths appear in `compile`'s output, error codes in every diagnostic envelope. The failure clause matters most — the natural instinct after a failed `validate` is to apologize and explain, which is precisely the build log the design forbids.

> Don't ask the player to make technical choices. Pick sensible defaults and tell them what you made in ordinary words.

Without this, the model asks which category the object belongs in or what price to set — questions that are trivial for it and meaningless to a player, and that stall the interaction.

### How to build

> Look up the vocabulary rather than guessing at primitive, scope, or category names.

Points at `list_vocabulary`. Guessed primitive names are the single most common authoring failure, and they surface as compile errors the model then has to loop on.

> Validate as you go and fix what comes back; the diagnostics tell you exactly what's wrong.

Leans on MCP-DESIGN.md §2's error contract — scoped, coded diagnostics naming the object/tree/node. Telling the model the diagnostics are trustworthy encourages reading them instead of rewriting the tree from scratch.

> Test the object actually behaves the way the player asked before you finish — compiling is not the same as working.

Earned the hard way. A pack that compiles cleanly can still do nothing, and the whole reason `test_in_vm` exists is that the VM fails silently — a bad branch or wrong scope index produces no error, just wrong behavior.

> When you're done, say in one friendly sentence what they've got and that it's ready to place. Then stop.

"Then stop" is deliberate. Without a stop instruction the model offers follow-ups ("want me to also…?"), and since the loop terminates on a turn with no tool calls, a chatty ending is also where a session hangs waiting for input the harness won't send.

## Failure messages: warmth must not cost correctness

The contract originally optimized for warmth alone, and that produced a real bug. When the API account ran out of credit, the player saw:

> Something went wrong while I was making that. Want to try again?

Friendly, no stack trace, contract honored — and **actively wrong**, because retrying could never succeed. A player would sit there retrying forever. The rule the doc was missing:

> Player-facing failure text must be player-safe **and** the advice in it must be true. Never suggest an action that cannot resolve the failure.

Failures are therefore classified by **remedy**, not by HTTP status — two different status codes calling for the same player action belong in one bucket, and one status code (429) splits across two buckets because "slow down" and "you're out of money" need different advice. `AgentFailureKind` in `LlmProvider.cs`:

| Kind | What the player is told to do | Why |
|---|---|---|
| `Transient` | Try again | Blip, overload, timeout — retrying genuinely works |
| `OutOfCredit` | Add credit, or supply your own key | Retrying can *never* work; this is the paywall a real player hits |
| `RateLimited` | Wait a few minutes, or supply your own key | Shared-account quota; both remedies actually resolve it |
| `Unauthorized` | Nothing — a person has to fix it | Bad/revoked key; the player is not at fault and cannot act |
| `Bug` | Rephrase and retry, but it's our fault | Our defect; don't promise a retry will fix it |

Providers classify their own exceptions (each `ILlmProvider` implementation owns the mapping, since only it knows its wire format), so the agent never sniffs error text to decide what to say. The player-facing strings live in `MakeSomethingAgent.PlayerMessage`.

Two constraints hold across all of them: no stack traces, error codes, or HTTP status numbers, **and no provider names**. "I'm out of the credit I use to make things" is right; naming a vendor leaks an implementation detail into a game and would need changing the moment the backend does.

## Known gaps

- **Nothing enforces the rule mechanically — and we now know exactly when that bites.** If the model names a tool, it reaches the player. This held fine on `claude-opus-5` but broke badly on `gpt-5-mini`, which leaked "the pack" ×15, "GUID" ×15, "validating" ×21, and even a schema field name (*"fixing the sleep node to use the correct `ticks_param` field name"*) in a single run — see MODEL-EVALUATION.md.

  A regex/keyword backstop over outbound narration was **deliberately not built**, because the models that need it are the ones we decided not to use; building it now would solve a problem we designed around. **This is a conditional risk, not a closed one:** the moment a weaker or cheaper model is reconsidered — or a new provider is added — enforcement stops being optional, because the prompt alone demonstrably does not hold. Treat "can this model keep the contract?" as a required part of evaluating any new model, not an afterthought.
- **Exercised on Anthropic only.** The prompt has produced good narration across several real Anthropic runs (see the gossip-gnome transcript). The OpenAI provider is written but has never made a call — no key — so nothing here is verified against it, and models differ most in exactly this area (tone, verbosity, willingness to narrate rather than explain). Assume re-tuning is needed per provider.
- **The failure-message table is barely exercised.** Only the pre-taxonomy out-of-credit path was ever hit live. The new classification code has not been observed firing on a real `OutOfCredit`, `RateLimited`, or `Unauthorized` response — it is written from documented error shapes, not from watching it happen.
- **Not tuned for cost.** The prompt is part of the cached prefix (stable across every player and object), so its length is cheap after the first call — but it hasn't been measured.

## Closing narration is withheld until delivery succeeds

The prompt ends with *"say what they've got and that it's ready to place"* — so the final turn's prose is a success claim by construction. But whether it's true is only known after delivery, which happens *after* that turn. Emitting it immediately produced a real bug on any model whose delivery failed: the player read *"your Pebble Buddy is ready to place"* followed straight away by *"I couldn't get it into your game."*

So the final turn's narration is **held back and released only if delivery returns a real GUID**; otherwise the player sees only the failure message. This is structural rather than a prompt instruction, because the model cannot know the outcome — it has already stopped talking by the time delivery runs. Mid-build narration is unaffected and still streams immediately, which is what keeps the wait feeling alive.

## If you change it

The prompt sits in the cached prefix along with the tool definitions. Editing it invalidates that cache for everyone, which is fine occasionally and expensive if done per-request — so never build it dynamically from per-player or per-session values.

# Player-Facing Layer — Design Draft

Status: design only, no code. This is the layer above MCP-DESIGN.md's agent tool surface — the thing an actual player (not a dev, not an agent operator) interacts with. Vision (Kat, 2026-08-08): "essentially they are building their own features" — a player describes what they want, never sees JSON or a tool call, and gets a working object in their game.

## 0. What this is not

Not a new authoring format, not a new compiler, not a new VM. Everything below is UI/orchestration wrapped around the already-built FSO.PackCompiler + FSO.ModServer (MCP tools) + FSO.VMHarness stack. If this doc finds itself redesigning the schema or the tool table, that's scope creep — go fix MCP-DESIGN.md/SCHEMA.md instead.

## 1. Where it lives

**In-client, not a separate app.** A player already in FreeSO, in a lot, looking at a blank spot on the floor, is the moment the idea happens ("I wish there was a gnome that gossiped with me"). Sending them to a website/CLI kills the whimsy the vision is built on. Concretely: a new UI panel (matches existing FreeSO UI panel patterns in `TSOClient/tso.client/UI/Panels/`) reachable from the buy-catalog or a dedicated "Make Something" button — a chat box, nothing more.

**The agent runs client-side or on a companion service, not embedded in FSO.ModServer itself.** FSO.ModServer is the MCP *tool* server (the hands). It needs an MCP *client* — something holding an LLM conversation with the player and calling those tools — sitting in front of it. That's new: nothing in PackTools today plays that role. This is the single biggest missing piece, bigger than any one tool.

## 2. The conversation loop

1. Player types intent in plain language ("a garden gnome that gossips with me, gives me a fun boost").
2. Agent (holding the MCP tool table from PackToolHandlers) plans: `create_pack` → `add_object` → `add_interaction` → `add_tree`/`edit_tree_node` per behavior → `validate` in a loop, self-correcting on errors per MCP-DESIGN.md §2's error contract → `test_in_vm` to confirm it actually does what was asked, not just compiles → `compile`.
3. Agent narrates progress in-character/casually, not as a build log ("giving him something to say...", "teaching him to notice you..." — never "calling edit_tree_node"). The MCP tool trace is implementation detail; the player-visible surface is closer to a status message than a terminal.
4. On success: the compiled `.iff` needs to land in the player's *live* game session, not just a temp dir. That's a gap — nothing today wires `compile`'s output into a running VM's `Content/Objects` or a hot-load path. `FSO.VMHarness` proves an object works headless; it doesn't prove "drop this into a live running client session" works. That's new integration work, not yet designed anywhere.
5. Object appears in the player's inventory/catalog, placeable immediately.

## 3. Guardrails (minimum viable, not exhaustive)

This produces user-generated content that runs as real game logic and gets shared/remixed (MCP-DESIGN.md §4). Two categories of risk, kept separate on purpose:

- **Mechanical safety** — already covered: `validate`/`compile`'s tree-size/locals/label checks prevent malformed trees from corrupting a lot. `test_in_vm`'s `max_ticks` bound prevents infinite loops from hanging a session. Nothing new needed here beyond what MCP-DESIGN.md already specifies.
- **Content safety** — not covered anywhere yet: object names, descriptions, dialog strings, and any AI-generated art (per STRATEGY.private.md's replace-EA-content roadmap) are player-facing text/media shared with other players. Needs a moderation pass before an object is shareable beyond its creator — out of scope for this doc to design (product/policy decision, not a compiler concern), but flagging it now so it's not an afterthought once sharing ships.

## 4. What's actually missing to build an MVP, in order

1. **An MCP client/agent runtime** embedded in or callable from the FreeSO client — holds the conversation, calls FSO.ModServer's tools. Doesn't exist yet.
2. **Live delivery path**: compiled `.iff` → player's running game session, not just a file on disk. Doesn't exist yet — closest thing today is FSO.VMHarness's symlink-into-Content/Objects trick, which is a test harness hack, not a live-session integration.
3. **The UI panel itself** — chat surface in the client. Doesn't exist yet.
4. **Moderation gate before sharing** — policy + mechanism both undesigned. Not blocking for a single-player/local-only MVP (a player using this only for themselves needs no moderation), only blocks the "shared and remixed by the community" part of the vision.

Recommended MVP cut: skip sharing/moderation entirely for v1. One player, one client, one local agent loop, objects usable only in their own game. That's the smallest slice that proves the whimsy works before investing in distribution.

## 5. Open questions

- Does the agent run as a local process the FreeSO client spawns (mirrors FSO.ModServer's own stdio-child-process model), or does it need network access to an LLM API — and if so, does that mean player accounts/API keys, or is inference cost absorbed by the project? This is a real product/cost decision, not a technical one.
- Should `test_in_vm`'s trace ever surface to the player (e.g. "I tested it and the gnome didn't actually say anything — let me fix that"), or is failure always silently retried by the agent until it passes? Leaning toward: silent retry, narrate only the outcome — matches "players never see JSON/code."

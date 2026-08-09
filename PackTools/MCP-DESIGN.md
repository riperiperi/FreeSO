# MCP Layer Design — Agent-Facing Mod Authoring

Status: design only, no code. Wraps the pack compiler (`FSO.PackCompiler`, see `PackTools/SCHEMA.md`) and the VM test harness (`FSO.VMHarness`) behind an MCP server so any MCP-capable agent can author, compile, test, and remix FreeSO packs conversationally, without touching bytecode.

## 1. Tool surface

### Decision: fine-grained editing tools over one mega "write whole pack" tool

Argument, weighed against the alternative:

**Mega tool (`write_pack(json)`)** — agent emits the entire pack JSON in one call, every edit is "regenerate the whole document."
- Pro: simplest server, no partial-state management.
- Con: token cost scales with pack size on *every* edit — fixing one node in a 40-node tree re-sends the whole tree. Worse, it re-invites the exact failure mode `SCHEMA.md` calls out explicitly ("fail loud... because the VM itself fails silently" / "unknown fields are a compile error, catches LLM hallucination"): a large single JSON blob is where LLMs hallucinate a field name, drop a `then`/`else`, or silently truncate a big tree under context pressure. Errors surface only after a full-document compile, so the agent can't localize what it broke.

**Fine-grained tools** — `create_pack`, `add_object`, `add_interaction`, `edit_tree_node`, `compile`, `test_in_vm`, `decompile_object`, chosen here. Each call is a small, structurally-typed diff against server-held pack state (see §5).
- Pro: token cost per edit is O(change size), not O(pack size). Compile errors from `edit_tree_node` localize to the node just added. Self-correction loop (agent adds node → compiles → error → fixes just that node) stays cheap.
- Con: server must hold mutable pack state across calls (see state/session model below) — more server complexity, worth it because it's the thing that makes the self-correction loop affordable.

Verdict: fine-grained wins on the two axes that matter most for this use case — LLM reliability (smaller, typed edits are less hallucination-prone than a full document per turn) and token cost (edits don't restate what didn't change). The compiler's "fail loud" philosophy only pays off if failures are already scoped to a small diff when they happen.

### Proposed tools

| Tool | Purpose | Notes |
|---|---|---|
| `create_pack(id, name, author, version, description)` | Start a new pack session | Returns `pack_session_id`. Server holds working pack JSON in memory (or a scratch file) keyed by this id. |
| `add_object(pack_session_id, id, guid?, name, price, category, appearance, attributes?)` | Add/replace an object stub | `guid` optional — server can allocate from the community GUID range (SCHEMA.md) and report it back, since agents are bad at inventing valid unique GUIDs. |
| `add_interaction(pack_session_id, object_id, interaction)` | Add one TTAB/TTAs entry | Validates `action`/`test` reference existing or not-yet-defined tree names (allow forward reference; resolved at `compile`). |
| `set_dialog_string(pack_session_id, object_id, index, text)` | Set one entry (1-255) in an object's private dialog string table | Compiles to STR# 301. `dialog_private`/`dialog_global`/`dialog_semiglobal` nodes reference an entry by its integer index. |
| `add_tree(pack_session_id, object_id, tree_name, args?, locals?)` | Declare an empty named tree | Separates "declare a tree exists" from "fill in nodes" so `edit_tree_node` always has a target. |
| `edit_tree_node(pack_session_id, object_id, tree_name, node)` | Add or replace one node by `id` | The core authoring primitive. One node per call keeps diffs small and errors localized. Accepts the same per-primitive shape as SCHEMA.md's node objects. |
| `remove_tree_node(pack_session_id, object_id, tree_name, node_id)` | Delete a node | Server should warn (not error) if another node's `then`/`else` still points at it — surfaces dangling edges before compile. |
| `read_pack(pack_session_id)` | Return current working JSON | Lets the agent re-orient after several edits, or hand off to a human/another agent, without the server needing to restate it unprompted. |
| `validate(pack_session_id)` | Static checks without emitting `.iff` | Tree size ≤253, locals ≤255, label resolution, enum values, GUID collisions — same checks `compile` does, but cheap and side-effect-free, for a tight edit→validate loop before spending a full compile. |
| `compile(pack_session_id)` | Emit `.iff` + build report | See §2 for the error contract. |
| `test_in_vm(pack_session_id, scenario)` | Run compiled object(s) in `FSO.VMHarness` | See §3. |
| `decompile_object(guid_or_path)` | `.iff` → pack JSON | For remixing base-game or shared community objects (§4). Not scoped to a `pack_session_id` — reads standalone. |
| `list_vocabulary(kind)` | Return primitive/scope/operator/motive enum tables from `simantics-vocabulary.md` as structured data | Lets an agent self-serve "what primitives exist" / "what does scope 14 mean" instead of guessing or the human maintaining a duplicate copy of the vocabulary doc in a system prompt. Cheap to add, directly reduces hallucinated field names. |

Deliberately excluded from v1: a `remove_object` / `delete_pack` pair — packs are cheap to recreate via `create_pack`, and destructive tools add risk for little value at this stage.

### Session/state model

`pack_session_id` is required by every editing tool because trees are graphs built incrementally — `edit_tree_node` needs somewhere to attach the node. Server keeps working state in memory per session (or spills to a scratch `.json` file per `pack_session_id` under a temp dir, so a crashed server doesn't lose in-progress work). `read_pack` and `compile` are the two ways state becomes visible outside the server; `compile` is also the point at which the working JSON gets snapshotted to disk as the canonical pack file (so the artifact isn't only "whatever's in server memory").

## 2. Error-reporting shape

Every compiler error must be something an agent can act on without a human translating it — this is the single most load-bearing contract in the whole design, because `SCHEMA.md`'s "fail loud" only helps if the failure is legible to the thing that has to fix it.

Shape (returned by `compile`, `validate`, and inline by `edit_tree_node` when it runs cheap local checks eagerly):

```json
{
  "ok": false,
  "errors": [
    {
      "code": "unknown_field",
      "object_id": "gossip_gnome",
      "tree_name": "gossip_action",
      "node_id": "reward",
      "field": "opp",
      "message": "Unknown field 'opp' on primitive 'expression'. Did you mean 'op'?",
      "expected": ["prim", "lhs", "op", "rhs", "then", "else"]
    },
    {
      "code": "unresolved_label",
      "object_id": "gossip_gnome",
      "tree_name": "gossip_action",
      "node_id": "walk_over",
      "field": "then",
      "message": "Node 'chat_anim' referenced by 'then' does not exist in tree 'gossip_action'.",
      "known_node_ids": ["walk_over", "reward", "count_it"]
    }
  ],
  "warnings": []
}
```

Design rules for this shape:
- **Always scoped to `object_id` + `tree_name` + `node_id`** where applicable — matches the granularity of `edit_tree_node`, so the agent's next call is obvious (re-issue `edit_tree_node` for that one node).
- **`code` is a stable machine-readable enum**, not just prose — `unknown_field`, `unresolved_label`, `tree_too_large`, `guid_collision`, `locals_overflow`, `invalid_enum_value`, `duplicate_node_id`. Lets an agent (or a thin retry loop around the agent) pattern-match without parsing English.
- **`message` is the human/LLM-readable explanation**, including a "did you mean" suggestion when the error is a near-miss on a known field/enum name — directly targets the "unknown fields are an error, catches LLM hallucination" behavior in SCHEMA.md; a bare rejection teaches the agent nothing, a suggestion closes the loop in one retry.
- **`expected` / `known_node_ids` / similar enumerations** ride along on relevant error codes so the agent doesn't need a separate `list_vocabulary` round-trip mid-fix.
- Compiler warnings (non-fatal, e.g. "node unreachable") use the same envelope with `ok: true` and populated `warnings`, so an agent can decide whether to address them before calling `test_in_vm`.

## 3. `test_in_vm` — self-test loop contract

Purpose: let an agent verify behavior, not just successful compilation. A pack that compiles can still be wrong (SCHEMA.md's underlying point that "the VM itself fails silently" — a bad `then`/`else` wire or wrong scope index doesn't crash, it just does the wrong thing).

Request:
```json
{
  "pack_session_id": "...",
  "scenario": {
    "place_object": "gossip_gnome",
    "spawn_sim": { "motives": { "social": 0 } },
    "push_interaction": "Gossip",
    "max_ticks": 200,
    "assertions": [
      { "type": "motive_at_least", "target": "sim", "motive": "social", "value": 10 },
      { "type": "attribute_equals", "target": "gossip_gnome", "attribute": "times_gossiped", "value": 1 }
    ]
  }
}
```

Response — trace format modeled as an ordered event log, not a raw VM tick dump, so it stays legible to an agent without VM internals:
```json
{
  "ok": true,
  "ticks_run": 47,
  "trace": [
    { "tick": 0, "event": "interaction_pushed", "interaction": "Gossip", "sim": "sim_0" },
    { "tick": 3, "event": "tree_enter", "object": "gossip_gnome", "tree": "gossip_action" },
    { "tick": 3, "event": "node_enter", "node": "walk_over" },
    { "tick": 12, "event": "node_exit", "node": "walk_over", "branch": "then" },
    { "tick": 12, "event": "node_enter", "node": "chat_anim" },
    { "tick": 40, "event": "node_exit", "node": "chat_anim", "branch": "then" },
    { "tick": 40, "event": "expression", "node": "reward", "lhs": "my_motives.social", "op": "+=", "rhs": 15, "result": 15 },
    { "tick": 41, "event": "expression", "node": "count_it", "lhs": "my_attributes.times_gossiped", "op": "+=", "rhs": 1, "result": 1 },
    { "tick": 41, "event": "tree_exit", "tree": "gossip_action", "return": true }
  ],
  "assertions": [
    { "type": "motive_at_least", "target": "sim", "motive": "social", "value": 10, "actual": 15, "passed": true },
    { "type": "attribute_equals", "target": "gossip_gnome", "attribute": "times_gossiped", "value": 1, "actual": 1, "passed": true }
  ]
}
```

Notes:
- **Trace sampling blind spot (current implementation):** FSO.VMHarness samples the top of each thread's stack once per tick, so a tree that enters and completes within a single tick (e.g. expression-only trees) never appears in the trace at all. An empty trace does NOT mean the tree never ran — check `final_state` for its effects instead. Sub-tick visibility requires instrumenting the VM's instruction loop, not the tick loop.
- `node_enter`/`node_exit` events (with the branch taken) give the agent a step-through of exactly which path the tree took — the direct fix for "unknown opcodes silently no-op" and "wrong branch taken" classes of bug from `simantics-vocabulary.md` §5, which won't throw compiler errors but will show up as a trace that never reaches the expected node.
- `expression` events log lhs/op/rhs/result so a wrong scope or operator (e.g. `=` instead of `+=`) is visible as "result didn't change" rather than requiring the agent to re-derive VM semantics.
- Assertion vocabulary starts minimal: `motive_at_least/at_most/equals`, `attribute_equals`, `node_reached`, `node_not_reached`, `tree_returned`. Extend only when a real pack needs something else — no speculative assertion types.
- A run that never completes (infinite loop, or genuinely long `sleep`) hits `max_ticks` and returns `ok: false, reason: "tick_limit_exceeded"` with the trace so far — this is a distinct failure mode from a compile error and from a failed assertion, and the agent should be able to tell the three apart from `ok`/`reason` alone.
- `test_in_vm` requires a successful `compile` first (or performs one implicitly and folds compile errors into the same response envelope as §2) — an agent shouldn't need to remember to call both in the right order.

## 4. Pack sharing / remix flow

Goal (per SCHEMA.md's "round-trip goal"): an agent can read someone else's shared pack or a base-game object and build on it, the same way it reads its own.

Flow:
1. **Shared packs are just the pack JSON file** (plus any asset references) — no separate wire format. Sharing a pack means sharing/publishing the `.json` (this repo doesn't need to design a distribution channel; that's a product-layer concern outside the MCP server's scope).
2. **`decompile_object(guid_or_path)`** turns an existing `.iff` (base-game object or another agent's compiled output) into pack JSON, using the compiler's paired decompiler. Output uses the same schema an agent would author by hand, so it can be fed straight into a new `create_pack` session as a starting point (effectively "fork").
3. **Remix pattern**: agent calls `decompile_object` → gets pack JSON → calls `create_pack` with a new `id`/`author` → re-plays the object(s) it wants into the new session via `add_object`/`add_tree`/`edit_tree_node` calls seeded from the decompiled JSON → edits from there. The MCP layer doesn't need a dedicated "fork" tool; decompile + create_pack composes into one.
4. **Attribution stays in the JSON**: `pack.author` on the original vs. the fork is just data the agent should preserve/update, not something the server enforces — license/attribution policy is a product decision, not an MCP mechanism.
5. **Partial decompile is expected to be lossy** for anything outside v0.1 schema scope (custom art, patches — both explicitly out of scope per SCHEMA.md). `decompile_object` should report what it couldn't round-trip (e.g. `"warnings": ["custom sprite data not representable in schema v0.1, appearance approximated via clone_from_guid"]`) using the same error/warning envelope as §2, so an agent forking a complex object knows what got lossy up front instead of discovering it after publishing a broken remix.

## 5. Server stack (.NET, 2026)

Current options for building an MCP server in C#/.NET:

- **`ModelContextProtocol` (the official C# SDK, Microsoft + Anthropic collaboration, distributed via NuGet, namespace `ModelContextProtocol`)** — the maintained, spec-tracking SDK. Supports stdio and HTTP/SSE transports, has first-class ASP.NET Core integration (`AddMcpServer()`/`WithHttpTransport()` in `Program.cs` for a hosted server, or a plain console host for stdio), and uses attribute-based tool registration (`[McpServerTool]` on methods, with parameters/return types reflected into the JSON schema automatically). This is the right choice here: it's the one under active spec alignment, and attribute-based tools map cleanly onto the tool table in §1 — each tool in this doc becomes one `[McpServerToolType]` class method.
- **Transport choice**: stdio for local/dev use (an agent's harness spawns the server as a child process — matches how this repo's other tools run) vs. HTTP/SSE if the server needs to be shared across multiple concurrent agent sessions (e.g. two peer sessions both testing packs against the same running VM harness instance). Start with stdio — matches `FSO.VMHarness` and `FSO.PackCompiler` being invoked as local processes/libraries already, and avoids standing up auth/multi-tenancy concerns for a single-player-facing feature. Revisit HTTP only if a concrete multi-agent-concurrent-session need shows up.
- **Hosting the compiler/VM**: the MCP server should reference `FSO.PackCompiler` and `FSO.VMHarness` as in-process library dependencies (both are already .NET projects in this solution), not shell out to their CLIs — avoids process-spawn overhead per tool call and lets error/trace objects be passed as typed C# objects and serialized directly to the JSON shapes in §2/§3, rather than round-tripping through stdout parsing.
- **Session state (§1)**: a simple `ConcurrentDictionary<string, PackSession>` in the server process is sufficient for v1 — no external store needed unless the server needs to survive restarts mid-session, which isn't a stated requirement yet.

## 6. Open questions for whoever picks this up next

- Should `test_in_vm` support multi-sim scenarios (two sims interacting via the object) in v1, or is single-sim sufficient until a pack actually needs it? Leaning toward: wait for the need.
- GUID allocation (§1, `add_object`) needs a real registry/range policy decided at the compiler level (`FSO.PackCompiler`) before the MCP server can just "ask the compiler for one" — check with the PackCompiler lane owner.
- `list_vocabulary` is scoped to reading `simantics-vocabulary.md`'s tables as structured data at server startup; if that doc format changes, the tool's parser needs to track it (or vocabulary should move to a machine-readable source the doc is generated from — out of scope for this design, flagging for later).

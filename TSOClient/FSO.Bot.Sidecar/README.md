# FSO.Bot.Sidecar

Go sidecar that bridges the C# `FSO.Bot.Headless` bot to a private campfire.
Scaffold — no convention handlers yet (those land in `freesoexperiment-d87-d-*`).

## Operator cheat sheet (5 lines)

1. Run the sidecar: `./bin/freeso-sidecar --bot path/to/FSO.Bot.Headless`
2. Copy the `Campfire:` id from the admission block it prints on stdout.
3. Admit an agent: `scripts/admit-agent.sh my-agent --cf-home ./bot-data`
4. Paste the emitted "Agent commands" block on the agent host.
5. Agent joins, reads `convention:operation` tags to discover the verbset, reads `freeso:perception --follow` to perceive.

## What it does

- Launches the C# bot as a child process with credentials routed exclusively through `exec.Cmd.Env` (never the CLI — see `TestFSOPassNotOnCLI`).
- Creates a private **invite-only** campfire on first run (or reuses one via `--campfire-id` / `FREESO_CF_CAMPFIRE`).
- Publishes every `conventions/*.json` declaration to the campfire with tags `convention:operation` + `freeso:<op>`.
- Bridges each NDJSON line from the bot's stdout (`kind: perception | dialog | system | response`) into a campfire broadcast tagged `freeso:<kind>` + `sim:<persist_id>`.
- Writes `.admit-info` to the CF_HOME so the helper can auto-discover the campfire id.

## Regenerating convention skeletons

When the verb catalog changes:

```bash
python3 conventions/_gen.py
```

Regenerates one JSON per verb except the two curated declarations (`walk-to.json`, `speak.json`). `d87-d-*` children replace the skeletons with real arg schemas when they land handlers.

## Build

```bash
go build -o ./bin/freeso-sidecar .
```

## Tests

```bash
go test ./...               # unit + integration (requires cf + go on PATH)
FREESO_SKIP_INTEGRATION=1 go test ./...   # unit only
```

Integration test spins up a stub bot, creates a real invite-only campfire, and asserts declarations + perception + dialog broadcasts are visible via a separate `cf read` process.

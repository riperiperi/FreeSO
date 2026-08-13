# Session lanes (live)

Same worktree: `packtools-on-archive`. One folder. Do not cross lanes.

| Lane | Chat | Touch | Do not touch |
|---|---|---|---|
| **Phase F — browser** | parked 2026-08-12 | — | `FSO.HouseGen`, `FSO.VMHarness`, `examples/layouts` |
| **Phase A — house** | this window (handoff 2026-08-12 23:21) | `FSO.HouseGen`, `FSO.VMHarness`, `examples/layouts`, house examples, `STATE.md`, `task_plan.md` | `FSO.Browser*`, LotView, `:5259`, KNI rebuild paths |

## Live status (2026-08-12 ~23:24 PDT)

### Goal
Floor plan / photo → AI builds house on a lot → friends hang out there. Desktop path is fine for the video. Browser polish after that looks like Sims.

### Phase F
Parked. Join-city scaffolding, KNIF, Mario-optional, LotView net8 dual-target, `?lot=real` all landed. Diamond / debug Blazor is a tech spike, not the product. Leave `:5259` alone if still listening.

### Phase A (this window)
- Committed: `4caba4c23` — windows on generated houses (local **ahead 1**, not pushed).
- Uncommitted WIP: `PackTools/FSO.VMHarness/Program.cs` — `--walk`. Routing frame vanishes at tick 1. **Leave it.** Occupancy proof, not the north-star.
- **Now:** floor-plan image → layout JSON (`FSO.HouseGen --from-image`). Converter + lot load already proven.

## Shared collision surface
`STATE.md`, `task_plan.md` — re-read HEAD before editing; throwaway-index + **exact paths only**. Never `git add -A`.

## Build note
Phase A harness: always `-p:FSO_GRAPHICS=MonoGame`. Bare `dotnet build` can pick up KNI node state from the browser lane and fail on `TouchPanel.EnableMouseTouchPoint`. HouseGen itself does not use XNA.

Remove this file when both lanes are idle.

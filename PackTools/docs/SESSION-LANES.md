# Session lanes (live)

Same worktree: `packtools-on-archive`. Do not cross lanes.

| Lane | Owner | Touch | Do not touch |
|---|---|---|---|
| **Phase F — browser** | other session (LotView / `?lot=real`, BrowserClient :5259) | `FSO.Browser*`, `FSO.BrowserEffects`, `TSOClient/tso.world`, KNI docs | HouseGen, VMHarness house/walk/vision |
| **Phase A — house** | this session (windows → walk → vision) | `FSO.HouseGen`, `FSO.VMHarness`, `examples/layouts`, house examples | anything under Browser*, LotView, :5259 |

Shared collision surface: `STATE.md`, `task_plan.md`, `START-HERE.md` — re-read HEAD, throwaway-index exact paths only.

Build note: Phase A harness must use `-p:FSO_GRAPHICS=MonoGame`. A bare `dotnet build` can pick up Kni node state from the browser lane and fail on `TouchPanel.EnableMouseTouchPoint`.

Updated: 2026-08-12 ~21:30 PDT. Remove this file when both lanes are idle.

# FSO.CityEditor

Standalone city-map authoring tool for FreeSO. Mirrors the architecture
of `FSO.IDE` (Volcanic): a small WinForms wrapper that loads
`FSO.Client.dll` at runtime via reflection and runs the FreeSO client in
a special "city-editor" mode.

## Why a wrapper, not a separate engine

Loading `FSO.Client.dll` at runtime means:

- The editor uses the **same renderer** the live game uses, so what you
  see while editing is what players will see.
- **No copyrighted content ships with the editor.** The user must already
  have a legal FreeSO install (which itself requires the original TSO
  game files). FSO.CityEditor.exe is just an alternate launcher — it
  inherits whatever content the install already has.
- Renderer improvements to FreeSO are picked up automatically; no
  duplicated city-rendering codebase to maintain.

## Loading a specific city

By default the editor opens `city_0100` (Alphaville) for editing. Two CLI
args override this:

```
FSO.CityEditor.exe --city /path/to/city_directory   # load from disk path
FSO.CityEditor.exe --cityid 200                      # load a built-in ID
```

`--city` accepts an absolute or relative path. The directory must contain
the seven PNG layers (`elevation`, `terraintype`, `roadmap`, `forestdensity`,
`foresttype`, `vertexcolor`, `thumbnail`). Use this to edit maps generated
by `Other/tools/CityMapGenerator/` without first installing them under
`Content/Cities/`.

## Distribution

Release builds drop `FSO.CityEditor.exe` into the same `publish/client/`
directory as `FSO.exe` and `FSO.IDE.exe`. Players double-click whichever
launcher they want.

## Building

Builds from `TSOClient/FreeSO.sln` with the `FSO_CityEditor` MSBuild
target on Windows (matches the workflow that already builds FSO.IDE).
Targets .NET Framework 4.6.1, AnyCPU, WinExe.

## Status

Stage 1 — project skeleton. The `.exe` builds, loads `FSO.Client.dll`,
and registers a `CityEditorHook` for the running client to detect. The
client-side handling of editor mode (skip-login → city-editor screen,
auto-enable map painter, bake-on-save) lands in subsequent commits.

Roadmap:

- Stage 2 — client-side `CityEditorScreen` that the screen transition
  detects via `CityEditorHook.IsActive`. Loads a city dir directly,
  enables `MapPainterPlugin` automatically.
- Stage 3 — bake hook in `MapPainterPlugin` so saving writes
  `vertexcolor.png` and `thumbnail.png` rendered by the live engine,
  alongside the existing five input layers.
- Stage 4+ — WinForms tool palette, procedural-island generator, lot
  placer, multi-camera thumbnail capture, validation lint.
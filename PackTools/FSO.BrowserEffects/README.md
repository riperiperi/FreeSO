# FSO.BrowserEffects

Rebuild FreeSO `.fx` shaders with **KNI MGCB** for **BlazorGL** (KNIF XNBs that
`Content.Load<Effect>` accepts).

## Why not Mac?

Verified 2026-08-12 on Apple Silicon with
`nkast.Xna.Framework.Content.Pipeline.Builder` **4.2.9001**:

| Step | Result |
|---|---|
| `dotnet MGCB.dll /help` | Works (managed) |
| MSBuild → `MGCB.exe` | **cannot execute binary file** (PE32+, exit 126) |
| `build.sh` → `mgcb-dotnet.sh` → `dotnet MGCB.dll` + `colorpoly2D.fx` | **Fails**: `Unable to load shared library 'd3dcompiler_47.dll'` |
| Wine (`brew install --cask wine-stable`) | Blocked here (sudo for gstreamer) |

Maintainer confirmation: [kni#2012](https://github.com/kniEngine/kni/discussions/2012) —
content builder/editor do not support macOS/Linux (missing native importer libs).
`Builder.Windows` also lacks a bundled `d3dcompiler_47.dll`; Windows hosts use
the system DirectX runtime.

**Blocker (exact):** EffectProcessor → SharpDX.D3DCompiler → native
`d3dcompiler_47.dll` (+ later `libmojoshader_64.dll`). No Mac dylibs ship in the
package.

## Build (Windows)

```powershell
# From repo root
.\PackTools\FSO.BrowserEffects\build.ps1
```

Or:

```powershell
cd PackTools\FSO.BrowserEffects
dotnet build -c Release
# KNI writes BlazorGL output under wwwroot/Content/
Copy-Item -Recurse -Force wwwroot\Content\Effects\*.xnb `
  ..\FSO.BrowserClient\wwwroot\Content\Effects\
```

CI: `.github/workflows/kni-effects-blazor.yml` (windows-latest) uploads
`kni-effects-blazorgl` artifact.

## Consume in BrowserClient

Place `colorpoly2D.xnb` at:

`PackTools/FSO.BrowserClient/wwwroot/Content/Effects/colorpoly2D.xnb`

Then open `http://localhost:5259/?lot=1` — green status
**effect OK (Content.Load colorpoly2D)** when KNIF loads; otherwise BasicEffect
fallback. Stock FreeSO MGFX 11 probe: `?lot=1&effect=1` (sample-content).

## Source

`Content/Effects/colorpoly2D.fx` — copy of
`TSOClient/tso.content/ContentSrc/Effects/colorpoly2D.fx` (46 lines).

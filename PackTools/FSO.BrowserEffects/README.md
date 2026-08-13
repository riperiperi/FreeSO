# FSO.BrowserEffects

Rebuild FreeSO `.fx` shaders with **KNI MGCB** for **BlazorGL** (KNIF XNBs that
`Content.Load<Effect>` accepts).

## Minimum lot FX set (GLVer=2 / WorldContent.LoadEffects)

Built from `TSOClient/tso.content/ContentSrc/Effects/` into
`Content/Effects/` (plus `#include` siblings: `GrassShader.fx`, `RCObject.fx`,
`Vitaboy.fx`, `SpriteEffects.fx`, `LightingCommon.fx`):

| Asset name (Content.Load) | Source `.fx` |
|---|---|
| `Effects/colorpoly2D` | `colorpoly2D.fx` (BrowserClient probe) |
| `Effects/GrassShaderiOS` | `GrassShaderiOS.fx` → includes `GrassShader.fx` |
| `Effects/2DWorldBatchiOS` | `2DWorldBatchiOS.fx` |
| `Effects/gradpoly2D` | `gradpoly2D.fx` |
| `Effects/LightMap2D` | `LightMap2D.fx` |
| `Effects/SSAA` | `SSAA.fx` |
| `Effects/RCObjectiOS` | `RCObjectiOS.fx` → includes `RCObject.fx` |
| `Effects/ParticleShader` | `ParticleShader.fx` |
| `Effects/VitaboyiOS` | `VitaboyiOS.fx` → includes `Vitaboy.fx` |
| `Effects/SpriteEffectsiOS` | `SpriteEffectsiOS.fx` → includes `SpriteEffects.fx` |
| `Effects/MapGeneration` | `MapGeneration.fx` (no iOS variant; Init falls back) |

`build.ps1` compiles each effect separately so a single EffectProcessor failure
does not block the rest. `colorpoly2D` must succeed. See `BUILD-RESULTS.md`
after a CI/Windows run for per-effect status.

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

Place rebuilt XNBs at:

`PackTools/FSO.BrowserClient/wwwroot/Content/Effects/*.xnb`

Then open `http://localhost:5259/?lot=1` — green status
**effect OK (Content.Load colorpoly2D)** when KNIF loads; otherwise BasicEffect
fallback. Stock FreeSO MGFX 11 probe: `?lot=1&effect=1` (sample-content).

LotView is **not** wired into BrowserClient yet; these XNBs are staged for that.

## Source

Copies under `Content/Effects/` from
`TSOClient/tso.content/ContentSrc/Effects/`. Tiny `*iOS.fx` wrappers need their
sibling `.fx` files beside them for `#include`.

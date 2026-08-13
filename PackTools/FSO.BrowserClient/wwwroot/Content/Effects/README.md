# Built KNIF XNBs (from FSO.BrowserEffects / CI)

Minimum `WorldContent.LoadEffects` set for GLVer=2 (prefer `*iOS` variants),
plus `colorpoly2D` BrowserClient probe. Rebuilt via
`.github/workflows/kni-effects-blazor.yml` → artifact `kni-effects-blazorgl`
(run [31663582278](https://github.com/katrinalaszlo/FreeSO/actions/runs/31663582278)).

Landed (all 11 — see `BUILD-RESULTS.md`):

- `colorpoly2D.xnb`
- `GrassShaderiOS.xnb`
- `2DWorldBatchiOS.xnb`
- `gradpoly2D.xnb`
- `LightMap2D.xnb`
- `SSAA.xnb`
- `RCObjectiOS.xnb`
- `ParticleShader.xnb`
- `VitaboyiOS.xnb`
- `SpriteEffectsiOS.xnb`
- `MapGeneration.xnb`

Stock FreeSO MGFX 11 copies live under `wwwroot/sample-content/effects/`.
LotView is not wired into BrowserClient yet.

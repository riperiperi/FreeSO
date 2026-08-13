# Built KNIF XNBs (from FSO.BrowserEffects / CI)

Minimum `WorldContent.LoadEffects` set for GLVer=2 (prefer `*iOS` variants),
plus `colorpoly2D` BrowserClient probe. Rebuilt via
`.github/workflows/kni-effects-blazor.yml` → artifact `kni-effects-blazorgl`.

Expected names (after a successful CI expand):

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

See `BUILD-RESULTS.md` (copied from CI) for which effects EffectProcessor
accepted. Stock FreeSO MGFX 11 copies live under `wwwroot/sample-content/effects/`.

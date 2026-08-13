# FSO.BrowserClient

KNI / BlazorGL spike for **Phase F** (browser FreeSO). Loads a content texture over
HTTP and runs the Archive city→lot Aries join through `FSO.WsGateway` (see
`FSO.BrowserAries`).

Template: `nkast.Kni.Templates` → `kni-blazor-gl` (net8.0, KNI 4.2.9001).

## Prerequisites

- .NET SDK 8+ (`~/.dotnet/dotnet` is fine)
- WASM workload: `dotnet workload install wasm-tools`
- For join demo: gateway + fake city/lot (below)

## Run (texture only)

```sh
cd PackTools/FSO.BrowserClient
dotnet run
```

http://localhost:5259 — canvas with `HttpContentStore` → `Texture2D`.
No auto-join (avoids a red Failed bar when the gateway is down). Press **Space**
to join via the default gateway `http://127.0.0.1:8087`.

## Run (texture + Aries join)

```sh
# terminals 1–3
python3 PackTools/FSO.WsGateway/tools/fake-city-server.py 33101
python3 PackTools/FSO.WsGateway/tools/fake-lot-server.py 34101
dotnet run --project PackTools/FSO.WsGateway -- --listen http://127.0.0.1:8087

# terminal 4
cd PackTools/FSO.BrowserClient && dotnet run
# open: http://localhost:5259/?gateway=ws://127.0.0.1:8087
```

With `?gateway=…` in the URL (or `?join=1`), the client auto-joins after ~1.5s;
**Space** always starts a join. Use `?join=0` to disable auto-join even when
`gateway` is set. On `LotJoined`, the UI switches to an **isometric grass lot
placeholder** (WASD / arrows to pan). This is not real `FSO.LotView` yet — see
`../docs/KNI-MIGRATION.md` S5.

### Lot placeholder + S3 BasicEffect (no gateway)

```
http://localhost:5259/?lot=1
```

Isometric floor + green status strip **effect OK (BasicEffect)** and a small
colored triangle (GPU effect path). Built-in `BasicEffect` — not a FreeSO XNB.

### FreeSO XNB negative probe (S3 format wall)

```
http://localhost:5259/?lot=1&effect=1
```

Also runs `Content.Load<Effect>("Effects/colorpoly2D")` against
`wwwroot/Content/Effects/colorpoly2D.xnb` (copy of iOS stock). **Expected fail:**
KNI 4.2 only accepts MGFX 10 / KNIF 11–12; FreeSO ships MGFX 11. Red pill next to
the green BasicEffect strip; exact message in the browser console
(`FreeSO XNB blocked: …`). Same bytes also under `wwwroot/sample-content/effects/`.

## Content seam

- `FSO.BrowserContent` (`net8`/`net9`) — `HttpContentStore` / `FileContentStore` / composite
- Sample texture: `wwwroot/sample-content/textures/squares.png`
- Sample FreeSO effects (transport only): `wwwroot/sample-content/effects/*.xnb`
- ContentManager probe: `wwwroot/Content/Effects/colorpoly2D.xnb`

## Networking

- `FSO.BrowserAries` — WASM-safe Aries framer + `ArchiveJoinDemo` (no Mina)
- Gateway: [`../FSO.WsGateway`](../FSO.WsGateway)

## Next — real LotView checklist (ordered)

Placeholder diamonds stay until this passes. Do **not** wire `ExternalWorld` early.

1. **KNI MGCB rebuild** of lot `*iOS.fx` → KNIF XNBs (`GrassShaderiOS`, `2DWorldBatchiOS`, …). Stock FreeSO MGFX 11 XNBs will not `Content.Load` on KNI 4.2.
2. **Mario / SM64 optional** — `Blueprint` must not construct `SM64Component` on WASM.
3. **`WorldContent.Init` MapGeneration** — fall back when `MapGenerationiOS` missing (landed in LotView).
4. **Dual-target LotView closure to net8** (Common/Files/Content/HIT/Vitaboy*/LotView) — BrowserClient is net8; libs are net9.
5. **Thin WASM seam** — gate Mina/HIT/Threads/File scans as needed.
6. **Wire `ExternalWorld` + `TerrainComponent.UpdateTerrain`** behind a flag; keep diamond fallback.
7. Real VM tick payload; live Archive RSA path.

See `../docs/KNI-MIGRATION.md` and root `task_plan.md` Phase F.

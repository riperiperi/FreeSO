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

### Lot placeholder + S3 effects (no gateway)

```
http://localhost:5259/?lot=1
```

Isometric floor + effect status strip + small triangle. BrowserClient always tries
`Content.Load<Effect>("Effects/colorpoly2D")` from `wwwroot/Content/Effects/`
(KNIF rebuild). If missing/unreadable → **BasicEffect fallback** (still green).
Teal half-pill = KNIF load succeeded. Rebuild on Windows/CI:
`PackTools/FSO.BrowserEffects` (Mac MGCB blocked — needs `d3dcompiler_47.dll`).

### FreeSO XNB negative probe (S3 format wall)

```
http://localhost:5259/?lot=1&effect=1
```

Also loads stock FreeSO MGFX 11 from `sample-content/effects/colorpoly2D.xnb`.
**Expected fail** on KNI 4.2 (MGFX 10 / KNIF 11–12 only). Red pill; console
`FreeSO XNB blocked: …`.

## Content seam

- `FSO.BrowserContent` (`net8`/`net9`) — `HttpContentStore` / `FileContentStore` / composite
- Sample texture: `wwwroot/sample-content/textures/squares.png`
- Stock FreeSO effects (transport / negative probe): `wwwroot/sample-content/effects/*.xnb`
- KNIF Content.Load target: `wwwroot/Content/Effects/colorpoly2D.xnb` (from FSO.BrowserEffects)

## Networking

- `FSO.BrowserAries` — WASM-safe Aries framer + `ArchiveJoinDemo` (no Mina)
- Gateway: [`../FSO.WsGateway`](../FSO.WsGateway)

## Next — real LotView checklist (ordered)

Placeholder diamonds stay until this passes. Do **not** wire `ExternalWorld` early.

1. **KNI MGCB rebuild** — `colorpoly2D` KNIF **DONE** (CI + committed under `wwwroot/Content/Effects/`). Still need lot set: `GrassShaderiOS`, `2DWorldBatchiOS`, …
2. **Mario / SM64 optional** — **DONE** (`FSO_NO_SM64` / `BLAZORGL` stub).
3. **`WorldContent.Init` MapGeneration** — **DONE** (fallback when `MapGenerationiOS` missing).
4. **Dual-target LotView closure to net8** — **DONE** (`net8.0;net9.0` on Common/Files/Content/HIT/Vitaboy*/LotView + TargaImagePCL). BrowserClient still not ProjectReferenced.
5. **Thin WASM seam** — gate Mina/HIT/Threads/File scans as needed.
6. **Wire `ExternalWorld` + `TerrainComponent.UpdateTerrain`** behind a flag; keep diamond fallback.
7. Real VM tick payload; live Archive RSA path.

See `../docs/KNI-MIGRATION.md` and root `task_plan.md` Phase F.

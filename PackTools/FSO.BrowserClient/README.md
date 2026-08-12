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
`gateway` is set. Stage bars turn green on `LotJoined`.
## Content seam

- `FSO.BrowserContent` (`net8`/`net9`) — `HttpContentStore` / `FileContentStore` / composite
- Sample asset: `wwwroot/sample-content/textures/squares.png`

## Networking

- `FSO.BrowserAries` — WASM-safe Aries framer + `ArchiveJoinDemo` (no Mina)
- Gateway: [`../FSO.WsGateway`](../FSO.WsGateway)

## Next

Lot view / effects in Blazor; real VM tick payload; live Archive RSA path.

See `../docs/KNI-MIGRATION.md` and root `task_plan.md` Phase F.

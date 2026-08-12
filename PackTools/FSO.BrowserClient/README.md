# FSO.BrowserClient

KNI / BlazorGL spike for **Phase F** (browser FreeSO). Not a FreeSO client port — a
minimal WebGL game host that builds, runs in the browser, and proves the KNI path works
in this tree.

Template: `nkast.Kni.Templates` → `kni-blazor-gl` (net8.0, KNI 4.2.9001).

## Prerequisites

- .NET SDK 8+ (`~/.dotnet/dotnet` is fine)
- WASM workload: `dotnet workload install wasm-tools`
- Spike omits `nkast.Xna.Framework.Content.Pipeline.Builder` (ships `MGCB.exe`, Windows-only).
  Runtime-drawn graphics only. Re-add the builder when real `.mgcb` content lands (or use a
  cross-platform MGCB host).

## Run (dev server)

```sh
cd PackTools/FSO.BrowserClient
dotnet run
```

Opens a Blazor WASM host at **http://localhost:5259** (see
`Properties/launchSettings.json`). Dark-blue canvas + a texture loaded through
`HttpContentStore` from `wwwroot/sample-content/textures/squares.png` (same FreeSO
`Content/Textures/squares.png` used on desktop).

## Publish (static files)

```sh
cd PackTools/FSO.BrowserClient
dotnet publish -c Release
```

Output lands under:

```
bin/Release/net8.0/publish/wwwroot/
```

Serve that folder with any static file server (or Kestrel). For local smoke:

```sh
dotnet serve -d bin/Release/net8.0/publish/wwwroot -p 5500
# or: python3 -m http.server 5500 -d bin/Release/net8.0/publish/wwwroot
```

## Networking

This spike does **not** talk to the game yet. The proven WS↔TCP bridge is
[`../FSO.WsGateway`](../FSO.WsGateway) (Aries handshake decoded in a real browser against
Kat's Archive server). Wire the KNI client to `ws://…/city` and `ws://…/lot` next —
gateway demo lives at `FSO.WsGateway/wwwroot/`.

## Content seam (S2)

- `FSO.BrowserContent` is multi-targeted (`net8.0;net9.0`) and referenced here.
- Game ctor takes an absolute content base URL (`Navigation.BaseUri + "sample-content/"`).
- Load path: `HttpContentStore.OpenAsync("textures/squares.png")` → `Texture2D.FromStream`.
- Desktop `Content.GetResource` uses the same `IContentStore` abstraction (default
  `FileContentStore`). See `../docs/CONTENT-HTTP-SEAM.md`.

## Next steps (real client)

1. Grow game code inside `FSO.BrowserClientGame` (lot view, effects).
2. Serve real FreeSO `Content/` + game data from a static host; swap sample URL.
3. Speak Aries over the gateway (reuse `FSO.Server.Protocol` in WASM, or grow from the
   JS seed in `FSO.WsGateway/wwwroot`).

See `../docs/KNI-MIGRATION.md`, `../docs/BROWSER-VIABILITY.md`, and root `task_plan.md` Phase F.

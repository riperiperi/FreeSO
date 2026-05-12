# FSO.LotRenderer — headless lot renderer (Linux/Mesa)

A Linux-portable headless renderer that fetches a live lot snapshot (FSOV) from the
FreeSO API and writes a PNG thumbnail.  No display required.

## Platform requirements

| Component | Version tested |
|-----------|---------------|
| Ubuntu | 24.04 x86_64 |
| Mesa (llvmpipe) | LLVM 20.1.2, 256 bits |
| .NET SDK | 9.0 (build only — binary is self-contained) |
| MonoGame | DesktopGL 3.8.5-preview.2 |

Mesa llvmpipe ships with Ubuntu 24.04 as part of `libgl1-mesa-dri`.
No GPU or X server required — set `SDL_VIDEODRIVER=offscreen` (see below).

## Build (workshop / any x86_64 Linux with Docker)

```bash
cd /home/baron/projects/freeso-experiment/FreeSO/TSOClient/FSO.LotRenderer

# Self-contained linux-x64 publish (no .NET 9 install needed on target):
docker run --rm \
  -v "$(pwd)/../../..:/src" \
  -w /src/TSOClient/FSO.LotRenderer \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  dotnet publish -c Release -r linux-x64 --self-contained \
    -o /src/TSOClient/FSO.LotRenderer/publish/linux-x64

sudo chmod -R a+rw publish/linux-x64
```

Output binary: `publish/linux-x64/freeso-renderer`

## Run

```bash
export SDL_VIDEODRIVER=offscreen   # headless — no display needed
./freeso-renderer \
  --api-url  http://workshop:9000 \
  --user     baron \
  --password test1234 \
  --game-path /home/baron/projects/freeso-experiment/GameAssets/TSOClient/ \
  --debug-lot 16318812 \
  --out /tmp/lot2-test.png
```

### Flags

| Flag | Env var | Default | Notes |
|------|---------|---------|-------|
| `--api-url` | `FSO_RENDERER_API_URL` | `http://workshop:9000` | FreeSO city API |
| `--user` | `FSO_RENDERER_USER` | `baron` | Admin account |
| `--password` | `FSO_RENDERER_PASS` | `test1234` | |
| `--game-path` | `FSO_GAME_LOCATION` | `/home/baron/projects/freeso-experiment/GameAssets/TSOClient/` | Must contain `tuning.dat` |
| `--debug-lot` | — | `16318812` | Packed lot **location** (`x<<16|y`), NOT lot_id |
| `--out` | — | `/tmp/lot2-test.png` | Output PNG path |
| `--level` | — | (ignored, S2) | Floor level |
| `--angle` | — | (ignored, S2) | Camera angle |

### Packed location vs lot_id

The FSOV API uses packed map coordinates, not the `lot_id` column:

```
location = (x << 16) | y
```

Lot 2 (baron's Main at X=249, Y=348): `(249 << 16) | 348 = 16318812 = 0x00F9015C`

### SDL_VIDEODRIVER

- `offscreen` — Mesa llvmpipe renders off-screen; no display needed. Preferred.
- `x11` — requires a running X server or `xvfb-run -a`.

## Integration test

```bash
SDL_VIDEODRIVER=offscreen \
  FSO_GAME_LOCATION=/home/baron/projects/freeso-experiment/GameAssets/TSOClient/ \
  dotnet test /home/baron/projects/freeso-experiment/FreeSO/TSOClient/FSO.LotRenderer.Tests \
    --logger:"console;verbosity=detailed"
```

The test (`RendererIntegrationTest.RenderLot2_ProducesValidPng`) spawns the renderer binary,
waits up to 5 minutes, then asserts:

1. Exit code 0
2. Output file exists
3. File size >= 10 KB
4. First 8 bytes match the PNG magic header `89 50 4E 47 0D 0A 1A 0A`

## Architecture notes

- **MonoGame on the calling thread**: OpenGL contexts are thread-affine. `HeadlessGraphicsDeviceService`
  calls `Game.RunOneFrame()` on the main thread to initialize the GL context; all GL work
  (world loading, rendering, PNG export) happens on that same thread.

- **`gd.Present()` before render targets**: Mesa llvmpipe holds an internal GL fence from
  the initial frame. Calling `GraphicsDevice.Present()` before creating any `RenderTarget2D`
  releases the fence. Without this, `PPXDepthEngine.InitScreenTargets()` blocks indefinitely.
  This matches the pattern in `FSOFacadeWorker/Program.cs` (line 193).

- **GameThread pump**: `ApiClient` callbacks are dispatched via `GameThread.OnWork` / `DigestUpdate`.
  The main render loop calls `GameThread.OnWork.WaitOne(500)` + `GameThread.DigestUpdate(null)`
  to service login and FSOV-fetch callbacks before issuing render calls.

## Spike outcome

PASS — Mesa llvmpipe (LLVM 20.1.2, 256 bits) renders lot 2 as a 288x288 PNG (51,852 bytes)
with `SDL_VIDEODRIVER=offscreen`, no display required.

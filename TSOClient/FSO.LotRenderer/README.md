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
| `--level` | — | top floor | Floor to render. `0` = terrain only, `1` = ground floor, `2` = 2nd floor, etc. Max: `bp.Stories`. When omitted, renders the topmost floor (same as `GetLotThumb` default). |
| `--angle` | — | `iso-ne` | Isometric camera angle. See table below. |
| `--zoom` | — | `far` | Zoom level. See table below. |

### Angle matrix

| `--angle` value | WorldRotation | Camera position | Output size |
|-----------------|---------------|-----------------|-------------|
| `iso-ne` | TopLeft | NE of lot | 576×576 (far/med), 1024×1024 (near) |
| `iso-nw` | TopRight | NW of lot | 576×576 (far/med), 1024×1024 (near) |
| `iso-se` | BottomLeft | SE of lot | 576×576 (far/med), 1024×1024 (near) |
| `iso-sw` | BottomRight | SW of lot | 576×576 (far/med), 1024×1024 (near) |

### Zoom matrix

| `--zoom` value | WorldZoom | Buffer size | PreciseZoom | Notes |
|----------------|-----------|-------------|-------------|-------|
| `far` | Far | 576×576 | 0.25 | Default; matches S1 / FacadeWorker output |
| `med` | Medium | 576×576 | 0.50 | Each tile 2× larger — fewer tiles, more detail |
| `near` | Near | 1024×1024 | 1.00 | Full sprite resolution; clamped at 1024×1024 |

The output PNG is decimated 2× (to half dimensions) before writing, matching the existing
`GetLotThumb` pipeline. Buffer size is the render-target size; final PNG is half that.

### Example — per-floor walkthrough of lot 2

```bash
export SDL_VIDEODRIVER=offscreen
export GAME=/home/baron/projects/freeso-experiment/GameAssets/TSOClient/
export API=http://workshop:9000

for level in 1 2 3; do
  for angle in iso-ne iso-nw iso-se iso-sw; do
    ./freeso-renderer \
      --api-url $API --user baron --password test1234 \
      --game-path "$GAME" --debug-lot 16318812 \
      --level $level --angle $angle --zoom far \
      --out "/tmp/lot2_L${level}_${angle}.png"
  done
done
```

### Packed location vs lot_id

The FSOV API uses packed map coordinates, not the `lot_id` column:

```
location = (x << 16) | y
```

Lot 2 (baron's Main at X=249, Y=348): `(249 << 16) | 348 = 16318812 = 0x00F9015C`

### SDL_VIDEODRIVER

- `offscreen` — Mesa llvmpipe renders off-screen; no display needed. Preferred.
- `x11` — requires a running X server or `xvfb-run -a`.

## Integration tests

### S1 smoke test — single render

```bash
SDL_VIDEODRIVER=offscreen \
  FSO_GAME_LOCATION=/home/baron/projects/freeso-experiment/GameAssets/TSOClient/ \
  dotnet test /home/baron/projects/freeso-experiment/FreeSO/TSOClient/FSO.LotRenderer.Tests \
    --filter "RenderLot2" \
    --logger:"console;verbosity=detailed"
```

The test (`RendererIntegrationTest.RenderLot2_ProducesValidPng`) spawns the renderer binary,
waits up to 5 minutes, then asserts:

1. Exit code 0
2. Output file exists
3. File size >= 10 KB
4. First 8 bytes match the PNG magic header `89 50 4E 47 0D 0A 1A 0A`

### S2 per-floor / rotation / zoom test

```bash
SDL_VIDEODRIVER=offscreen \
  FSO_GAME_LOCATION=/home/baron/projects/freeso-experiment/GameAssets/TSOClient/ \
  dotnet test /home/baron/projects/freeso-experiment/FreeSO/TSOClient/FSO.LotRenderer.Tests \
    --filter "PerFloorRotation" \
    --logger:"console;verbosity=detailed"
```

The test (`RendererIntegrationTest.PerFloorRotation_AllCombinations_ProduceDistinctValidPngs`)
renders 7 representative combinations (4 angles × 1 zoom at level 1, then 2 additional zooms,
then level 2) and asserts:

1. Each output PNG is >= 10 KB
2. Every pair of outputs has > 1% byte-difference (no two combos produce identical images)

Combinations rendered:

| Level | Angle | Zoom |
|-------|-------|------|
| 1 | iso-ne | far |
| 1 | iso-nw | far |
| 1 | iso-se | far |
| 1 | iso-sw | far |
| 1 | iso-ne | med |
| 1 | iso-ne | near |
| 2 | iso-ne | far |

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

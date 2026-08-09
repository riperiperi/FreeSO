# In-Game Capture — Scope

> **Status: scoped only, not built.** The original reason to wait — "the client is about
> to migrate onto `upstream/archive`, so building now would be throwaway" — has expired;
> that migration is done and `packtools-on-archive` is the base. Nothing here is blocked
> any more, it is simply unbuilt and unscheduled (no roadmap phase in `../task_plan.md`
> claims it). The `net8.0`-compatibility note in §"Pull in a managed NuGet encoder" now
> reads `net9.0`. No code has ever been changed by this doc.

## 0. Why this matters (context from the research that prompted it)

Roblox: 930M in-app screenshots + 240M in-app videos → 1T+ cumulative YouTube views. BeReal (external-virality model): 73M → ~20M MAU. The evidence points at capture-*in-client* as the loop, not "share a link to another platform." For this project specifically: a pack file is a download; **an object doing its thing is a video.** Nothing in the engine produces that today — this doc scopes what it would take.

## 1. What already exists — checked before designing anything

**A real, working, dual-mode "isolated object" renderer already exists**: `IWorldPlatform.GetObjectThumb(ObjectComponent[] objects, Vector3[] positions, GraphicsDevice gd, WorldState state)`, implemented separately for both render paths:

- **3D mode** (`TSOClient/tso.world/Platform/WorldPlatform3D.cs:221`): sets up a dedicated `WorldCamera3D`, renders the object(s) alone (transparent background, forced `Direction.NORTH`, room/container stripped out so nothing else in the scene draws) into a `RenderTarget2D` — **1024×1024**, not a tiny icon. The downscale to a small catalog thumbnail happens *after*, in `CatThumbGenerator.cs` (`FSO.UI/Utils/`), which composites the 1024×1024 render down to 74×37 for the Buy Mode catalog tile.
- **2D mode** (`TSOClient/tso.world/Platform/WorldPlatform2D.cs:203`): same idea via the sprite-based renderer — forces `_2D` camera mode, `WorldZoom.Near`, `state.RenderingThumbnail = true`.

This is a genuinely mature, already-proven render-to-texture pipeline for "a clean, framed shot of one object" — it's been rendering every catalog icon in the game. **This is most of the hard part of a stills feature already done.**

**What does NOT exist**:
- No screenshot feature (searched `Screenshot`/`screenshot` across `tso.client`/`FSO.UI` — zero hits).
- `FSO.Files.ImageLoaderHelpers.BitmapFunction`/`SavePNGFunc` — hooks for loading/saving images, but they're empty function pointers the platform head must fill in (Windows does it via `System.Drawing.Bitmap`; our new `FSO.Mac` head leaves them `null`, noted explicitly in that port's report). Only one call site (`UIGraphicsProvider.cs`, for exporting/caching UI graphics assets) — not a player-facing capture path.
- No GIF, video, or animated-image encoding anywhere in the codebase or its dependencies (checked `.csproj`s and source for `gif`/`ffmpeg`/`mp4`/video-encoder references — nothing).
- No full-backbuffer/whole-screen capture pattern (checked for `GetBackBufferData` usage — the only hits are reading `PresentationParameters.BackBufferWidth/Height` for layout math, not capture).

**One directly reusable asset from tonight's other work**: `PackTools/FSO.PackCompiler/ArtGen/PngWriter.cs` — a from-scratch RGBA PNG encoder using only `System.IO.Compression.ZLibStream` (no `System.Drawing`, no new dependency), already built and proven tonight writing the chair's PNG dumps. Directly reusable for real in-game screenshots — the PNG-encoding half of this feature is **done**, not scoped.

## 2. Stills — two different shots, two different difficulty levels

### 2a. Isolated object shot (reuses `GetObjectThumb`)

Skip `CatThumbGenerator`'s downscale-to-74×37 step, keep the full 1024×1024 (or whatever size) render, read it back via `RenderTarget2D.GetData<Color>()` (the exact same call `CatThumbGenerator` already makes), encode with `PngWriter`. **Low effort** — every hard piece (isolated render, readback, encode) is already proven in this repo.

**Caveat**: this shot is *isolated* by design — no Sim, no scene, no context, matching a catalog icon's purpose. It is not "an object doing its thing." Per §4, this is probably not the shot worth shipping first.

### 2b. Live moment shot (Sim actually using the object)

No existing helper does this — it needs a genuine whole-scene capture, not the isolated-object trick. But it's *simpler* than 2a in one respect: no camera setup, no stripping the object out of its context, no forcing a clean background — just read back whatever's already rendered to the screen's `RenderTarget2D`/back buffer at a chosen moment (e.g. when an interaction fires). Standard MonoGame pattern (`GraphicsDevice.SetRenderTarget` to an offscreen target before the normal draw, or read the back buffer directly if `PreserveContents` is set), same `GetData<Color>()` + `PngWriter` encode as above. **Low-to-medium effort** — no isolation gymnastics needed, but does need a hook into the interaction-fired event to decide *when* to capture (not deeply investigated this pass — `VMEventScript`/interaction-push points would be the place to look).

## 3. Clips (GIF) — real, bounded, not a rabbit hole, not small either

Two separable problems:

**Frame capture during live play**: read `RenderTarget2D`/back-buffer snapshots at intervals (e.g. every 3rd frame at 30fps → ~10fps capture) into an in-memory ring buffer for a fixed window (a few seconds either side of the interaction firing). Memory cost is bounded and modest — e.g. 3 seconds at 10fps, 640×480, RGBA: 30 frames × ~1.2MB ≈ 36MB, fine to hold in memory before encoding. This part is mechanically similar to §2b's single-frame capture, just repeated on a timer — no new hard problem, just needs care not to stall the render thread (capture off the critical path, encode *after* the window closes, not during).

**GIF encoding**: nothing exists in-repo. Two real options, not a false choice:
- **Hand-roll a minimal GIF89a encoder** — bounded, well-documented format (LZW compression + a 256-color-per-frame palette + Graphic Control Extension blocks for timing). Same category of effort as `PngWriter.cs` was tonight (a from-scratch encoder against a public spec, no new dependency) — proven this is a viable pattern for this project, not just theoretical. Real effort, not a rabbit hole: color quantization is the fiddly part (GIF's 256-color-per-frame ceiling), everything else is direct.
- **Pull in a managed NuGet encoder** (e.g. `SixLabors.ImageSharp`, actively maintained, fully managed, no native interop, net9.0-compatible) — meaningfully faster to ship, at the cost of one new dependency the project doesn't currently carry. Given the project already takes NuGet dependencies pragmatically elsewhere (Newtonsoft.Json, MonoGame.Framework.DesktopGL itself), this isn't obviously against house style — flagging as the faster path, not asserting it's the "right" one; that's a call for whoever owns this next.

**Not recommended for v1**: real video (MP4/H.264) — needs either a native encoder or an `ffmpeg` binary dependency, meaningfully heavier than what a "GIF-length shareable clip" actually requires. Skip.

## 4. What the capture should contain — product question, answered directionally

Agree with the framing this was scoped under: **the isolated object shot (§2a) is the weak version of this feature, not the target.** The interesting frame is the object *in use* — a Sim mid-interaction, ideally with the interaction name and/or the creator's original prompt visible. That points at §2b/§3 (live-scene capture) as the real target, not §2a (isolated catalog-style render) — even though §2a is the easiest thing to ship, it's closest to "a fancier catalog icon," not "a shareable moment."

Overlay/caption (object name, interaction name, creator's prompt) is a compositing step *after* raw frame capture, not part of capture itself — cheap once frames exist (burn text into the final PNG/GIF frames, or render a UI overlay into the same frame the capture reads back). Not scoped further here since it's downstream of the capture mechanism, not a blocker to it.

## 5. Effort estimate

| Piece | Estimate | Why |
|---|---|---|
| §2a isolated object still (PNG) | Small (~1-2 hrs) | Both hard parts (render-to-texture, PNG encode) already proven in this repo tonight. |
| §2b live-moment still (PNG) | Small-to-medium | Simpler render than 2a (no isolation setup), but needs a real interaction-fired hook not yet investigated. |
| §3 GIF clip of a live moment | Medium (a real chunk of a day, not multi-day) | Frame-capture-on-a-timer is mechanically simple; GIF encoding is the real work, bounded either way (hand-rolled vs. NuGet). |
| Real video (MP4) | Larger, not recommended for v1 | Native encoder or ffmpeg dependency; skip unless a strong reason emerges. |

**Recommended path if greenlit**: §2b (live-moment PNG) first — smallest genuinely-new piece, and it's the shot that actually matters per §4, not the easier-but-weaker §2a. GIF (§3) as the follow-up once the interaction-fired hook and frame-capture-on-a-timer plumbing exist from §2b — most of §3's frame-capture half is "do what 2b does, repeatedly," so 2b isn't wasted work if §3 comes next.

## Files referenced

- `TSOClient/tso.world/Platform/WorldPlatform3D.cs:221`, `WorldPlatform2D.cs:203` — existing `GetObjectThumb` implementations.
- `TSOClient/FSO.UI/Utils/CatThumbGenerator.cs` — existing consumer, shows the readback + compositing pattern.
- `PackTools/FSO.PackCompiler/ArtGen/PngWriter.cs` — the reusable, already-proven PNG encoder.
- `TSOClient/FSO.Files/ImageLoaderHelpers.cs` — the currently-unfilled `SavePNGFunc`/`BitmapFunction` hooks (not the right integration point for this feature, but worth knowing about — see §1).

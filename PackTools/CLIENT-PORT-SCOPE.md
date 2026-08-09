> **Historical, 2026-08-08 — the .NET 8 port it describes is superseded.**
>
> This documents porting the client to **net8.0** on top of a fork of stale `master`.
> Upstream had already migrated the whole solution to **.NET 9** (PR #283), and we now
> build on `upstream/archive` as `packtools-on-archive`, at net9.0, with upstream's own
> native macOS CI and code signing. The survey in `UPSTREAM-BRANCHES.md` said as much at
> the time; this document's outcome section contradicted it, and the contradiction went
> unresolved for a day.
>
> Kept because the *method* is still useful — which projects the client actually depends
> on, the WinForms cut and how it was confirmed safe, the dead-code exclusions, and the
> MonoGame DesktopGL swap. Do not follow its target-framework conclusions.

# .NET 8 Client Port — Scope

Status: **Compiles clean AND launches — real window/audio subsystem init succeeds, the game boots and reaches content loading, then hits one precisely-identified blocker (a lost custom MonoGame content reader — see "Launch attempt outcome" at the bottom).** `FSO.Client.csproj` (the actual client, previously .NET Framework 4.5) targets net8.0 and builds clean, along with all its Framework-era dependencies, plus a new macOS platform head (`FSO.Mac`). See §"Outcome" and §"Launch attempt outcome" at the bottom for exactly what changed, what ran, and where it stopped.

This scoping pass also checked out the previously-uninitialized `Other/libs/FSOMonoGame` git submodule for inspection (inert, safe, just populates a designated path already declared in `.gitmodules`).

**Key correction to the framing below**: this is not a platform port, it's a **Mono → .NET 8 runtime migration**. The shipped `/Applications/FreeSO.app/Contents/MacOS/` contains `libMono.Unix.dylib`, `libSDL2-2.0.0.dylib`, and `libopenal.dylib` — checkable in ten seconds — proving the client already runs on macOS today, on Mono, with MonoGame DesktopGL as the rendering path in both the old and new worlds. Windowing, input, GL rendering, and audio on this platform are proven, not unknown-unknowns. What's actually changing underneath is the runtime and the project file format — and that conversion has already been done successfully 7+ times in this same solution (see "already done" list below). Read "Risk #3" below with that correction in mind: it's real but substantially smaller than a from-scratch platform port would be. MonoGame 3.8 targeting .NET 8 is a first-class, well-trodden scenario — worth checking MonoGame's own migration guidance and other Mono→.NET 8 MonoGame game migrations for a known-good pattern before treating anything here as novel.

## Why this is bigger than it first looks

`FSO.Windows` is **not** in the port surface — it's a Windows-specific *entry point* that references `FSO.Client.csproj`, not the reverse (same relationship as `FSO.iOS`/`FSODroid` to the client). A macOS build needs a **new** platform head, not a port of this one.

The real remaining Framework-era surface, read directly off `FSO.Client.csproj`'s `ProjectReference` list — **~10 projects, not 2-3**:

| Project | Current target | Notes |
|---|---|---|
| `tso.client` (`FSO.Client.csproj`) | .NET Framework 4.5 | The client itself — windowing, input, rendering, UI. The one unknown-unknowns item (see below). |
| `FSO.UI` | .NET Framework 4.5 | Base UI framework (`UIDialog`, `GameFacade`, all controls). Also where the WinForms debug tooling lives — see below. |
| `FSO.Common.DatabaseService` | .NET Framework 4.5 | Not yet inspected in depth. |
| `FSO.Common.Domain` | .NET Framework 4.5 | Not yet inspected in depth. |
| `FSO.Content.TSO` | .NET Framework 4.7.2 | Turned out to be nearly empty — 1 file (`AssemblyInfo.cs`). Near-zero risk despite the scary-looking target version. |
| `FSO.Patcher` | .NET Framework 4.5 | WinForms-based updater tool. Separate standalone utility, not needed by the running client. |
| `FSO.Server.Clients` | .NET Framework 4.5 | Not yet inspected in depth. |
| `FSO.Server.DataService` (`FSO.Common.DataService.csproj`) | .NET Framework 4.5 | Not yet inspected in depth. |
| `FSO.Server.Protocol` | .NET Framework 4.5 | Not yet inspected in depth. |
| `tso.debug` (`FSO.Debug.csproj`) | .NET Framework 4.5 | Whole project of WinForms inspector windows (Vitaboy, Simantics/BHAV, content browser). See WinForms section. |
| `Other/libs/FSOMonoGame` (vendored submodule) | — | See MonoGame section. |
| `Other/libs/MSDFData`, `Other/libs/VoronoiLib` | .NET Framework 4.5 | Small, pure-logic utility libs. No Windows-only API usage found. Low risk. |

**Already done and confirmed clean** (checked every `.csproj`'s `TargetFramework` directly, not just trusted the wiki): `tso.files`, `tso.common`, `tso.content`, `tso.simantics`, `tso.world` (`FSO.LotView`), `tso.sound` (`FSO.HIT`), `tso.vitaboy.model`, `tso.vitaboy.engine` — all net8.0, all building. `tso.world` already renders via GL successfully in whatever proved that, which retires the "does the shader pipeline survive DX→GL" risk before the client port even starts.

**Note on this table**: as of this scoping pass, this porting work (the 8 net8.0 libraries above) existed only as uncommitted working-tree changes — flagged separately and since committed by the requesting session in three commits (engine port, agent bridge, UI panel — the panel commit explicitly marked unverified/uncompiled). Confirm this file's project list against current `git log` if reading much later, since more may have landed since.

## Three distinct problems, not one

### 1. MonoGame — fix already proven, just needs repeating

`FSO.Client.csproj` references `MonoGame.Framework.Net.WindowsGL.csproj` / `MonoGame.Framework.WindowsGL.csproj` inside the vendored `Other/libs/FSOMonoGame` submodule. Checked it out to verify: those `.csproj` files don't exist as literal files in that fork — old MonoGame used a custom project-generation tool instead of plain SDK-style `.csproj`s, and that tool isn't part of this build.

**The fix is already proven 7 times over**: every already-ported library dropped this vendored reference and switched to the `MonoGame.Framework.DesktopGL` NuGet package (`Version="3.8.*"`) instead. Same swap for `tso.client`/`FSO.UI`, not a new pattern to invent.

### 2. WinForms — a real structural wall, not a port task

`System.Windows.Forms` doesn't run on macOS at all — this isn't a portability gap to close, it's a different OS UI toolkit. Two places it shows up in the client's actual dependency tree:

- **`tso.debug`** (`FSO.Debug.csproj`) — an entire separate project of Form-based inspector windows (Vitaboy inspector, Simantics/BHAV routine inspector, content browser).
- **`FSO.UI/Debug/*`** — WinForms debug tools (asset search, UI inspector, exception display, scene inspector) bundled *inside* the main `FSO.UI` library itself, not separated into an optional project.

**Open question, not yet confirmed**: whether the shipped client flow actually instantiates any of `tso.debug`'s `Form` classes. A grep across `tso.client` found no call sites — only the bare `ProjectReference` — suggesting these are dead weight from the running client's perspective and could simply be excluded/cut for a v1 port rather than ported. But that grep wasn't exhaustive (didn't check indirect invocation via reflection, debug-menu wiring, etc.) — **someone should confirm this before deleting or excluding anything**.

`FSO.Patcher` is also WinForms but is a separate standalone updater tool, not something the running client needs at runtime — can likely be left behind entirely for a v1 native build (an installer/updater is a separate concern).

### 3. Small, narrow platform-specific spots — not blockers

- `TSOClient/tso.client/Utils/GameLocator/WindowsLocator.cs` — P/Invoke, Windows-only by name, likely already gated to the Windows platform head only.
- `TSOClient/FSO.UI/Model/DiscordRpc.cs` — P/Invoke to a Windows Discord Rich Presence DLL. Stub or disable for v1.
- `TSOClient/tso.client/UI/Screens/LotScreenNew.cs:138-139` — one `System.Windows.Forms.Form.FromHandle(GameFacade.Game.Window.Handle)` call, used only to grab the native window handle. Narrow, should be replaceable with a MonoGame-native or platform-conditional equivalent.

## Is there a shortcut via a different toolchain?

No mono or msbuild installed in this dev environment, so couldn't test directly (and didn't want to install new tooling unprompted for a scoping-only pass). Structurally, though: even with mono available, the same missing-MonoGame-project-files problem would block a build — an alternate toolchain doesn't sidestep the actual work, since the fix (swap to the DesktopGL NuGet package) is required either way.

## Estimate

- **The ~9 remaining support-library conversions**: genuinely mechanical, proven pattern (7 libraries already did exactly this: SDK-style csproj + `TargetFramework net8.0` + swap vendored MonoGame ref for the NuGet package). Rough estimate: about a day of focused work total, probably less.
- **`tso.client` itself**: the real unknown. It's the single biggest project in the solution, and the *first* ported piece that actually touches windowing/input/rendering/asset-loading at the OS boundary — every already-ported library is bottom-up logic with no OS-surface exposure. The proven pattern de-risks the mechanical conversion; it says nothing about what breaks once a window is actually expected to open and render on the new stack (DPI scaling, keyboard/input differences, path-separator assumptions, whatever else only shows up once it's running). Realistic estimate: multi-day, not a quick unblock.

## Top 3 risks

1. **(Resolved as of this writing, but worth remembering)** The completed engine-port work existed only as uncommitted working-tree changes for hours — a stray `git reset --hard`/`checkout .`/bad stash could have erased it. Now committed; keep committing incrementally rather than letting new port work accumulate uncommitted again.
2. **The WinForms-cut decision needs real confirmation**, not just a grep. If something in the actual game flow does depend on `tso.debug`/`FSO.UI/Debug` (debug-menu wiring, reflection-based invocation, etc.), that's a bigger job than "just exclude it."
3. **`tso.client`'s unknown-unknowns.** The support libraries prove the mechanical conversion works; they prove nothing about what breaks once an actual window, input loop, and asset pipeline are running on the new stack. That's where a "should be a day" estimate turns into a week if something fundamental doesn't translate cleanly.

## Files referenced

- `TSOClient/tso.client/FSO.Client.csproj` — the client project, source of the `ProjectReference` list above.
- `Other/libs/FSOMonoGame/` — vendored MonoGame fork (git submodule, was uninitialized, now checked out for inspection).
- Any already-net8.0 `.csproj` under `TSOClient/tso.*` — the template to copy for the mechanical conversions.

## Outcome (port executed, this session)

**`dotnet build TSOClient/tso.client/FSO.Client.csproj` succeeds, 0 errors.** 15 projects converted from old-style Framework csproj to SDK-style net8.0, all following the proven in-repo pattern (no external migration guide needed — the pattern was already established 7+ times before this session started). Existing `FSO.PackCompiler.Tests`/`FSO.ModServer.Tests` still 31/31 and 32/32 after — no regression.

**Projects converted**: `FSO.Content.TSO`, `Other/libs/MSDFData`, `Other/libs/VoronoiLib`, `FSO.Server.Common` (a dependency this scoping pass hadn't originally found), `FSO.Server.Protocol`, `FSO.Server.Clients`, `FSO.Common.Domain`, `FSO.Common.DatabaseService`, `FSO.Server.DataService`, `FSO.UI`, `tso.client` (`FSO.Client.csproj`) itself. Each got: `Sdk="Microsoft.NET.Sdk"`, `<TargetFramework>net8.0</TargetFramework>`, the vendored/old-Framework `<Reference>`s replaced with modern `PackageReference`s (`MonoGame.Framework.DesktopGL 3.8.*` where MonoGame was used, `Mina 2.0.11`, `Common.Logging 3.4.1`, `Newtonsoft.Json 13.0.3`, `Ninject 3.3.6`, `NLog 5.3.4`, `RestSharp 110.2.0`, `Portable.BouncyCastle 1.9.0`, `JWT 10.0.3`, `MIConvexHull 1.1.19.1019`, `System.Collections.Immutable 8.0.0` — versions picked to match what already-ported libraries used where precedent existed).

**Confirmed and executed rather than just guessed**:
- **WinForms cut, confirmed safe first.** Both `tso.debug` (`FSO.Debug.csproj`) and `FSO.Patcher` were dropped from `FSO.Client.csproj`'s references entirely, but only after confirming via grep that no `using` in `tso.client` actually references their namespaces (`FSO.Debug`, `FSO.Patcher`) — the one namespace that looked similar, `FSO.Client.Debug`, turned out to be `IDEHook.cs` inside `FSO.UI` (not WinForms, kept). Also confirmed `FSO.UI`'s own `Debug/*.cs` WinForms tooling (asset search, UI inspector, exception display, scene inspector) was **already excluded** from the old csproj's explicit `Compile` list — never part of the shipping `FSO.UI.dll` to begin with, so excluding it in the new SDK-style project (which needed explicit `<Compile Remove>` since SDK-style defaults to implicit globbing) preserves the exact same compiled surface, not a new decision.
- **Dead code found and excluded the same way, project by project.** `tso.client` had ~40 more files never in the old explicit `Compile` list: the entire old "House"-prefixed 2D/3D lot renderer (`Rendering/Lot/**`, fully superseded by `tso.world`/`FSO.LotView`, already in active use), the old packet-based network layer (`Network/PacketHandlers.cs` etc., superseded by the data-service layer that IS compiled), and two dead UI screens (`LotScreen.cs`, `LotScreenNew.cs` — the latter was where the one `System.Windows.Forms.Form.FromHandle` call lived, so that concern is moot, it's dead code). All excluded via the same "diff disk files against the old csproj's explicit list" method as `FSO.UI`, not guessed.
- **The `MonoGame.Framework.DesktopGL` swap worked as predicted.** No structural surprises — dropped the vendored `FSOMonoGame` submodule project references and the `OpenTK.dll` direct reference (confirmed unused directly, only ever an internal MonoGame dependency) everywhere they appeared, replaced with the NuGet package, matching what the 7 already-ported libraries did.

**Small mechanical API fixes needed along the way** (old package/API version → new, not structural):
- `JWT.JsonWebToken.Encode/Decode` (old `JWT` package API) → `JwtBuilder` fluent API (modern `JWT` package) — 2 call sites, `FSO.Server.Common`.
- `RestSharp`'s callback-style `ExecuteAsync(request, callback)` doesn't exist in 110.x (Task-based only now) — added a small local extension method adapting the new Task API back to the old callback shape, rather than rewriting ~10 call sites in `ApiClient.cs`. `Method.GET`/`Method.POST` → `Method.Get`/`Method.Post` (casing only). `RestClient.CookieContainer` moved into `RestClientOptions`.
- `Color.TransparentBlack` doesn't exist in modern MonoGame — replaced with `Color.Transparent` (same value) at 5 call sites across `tso.client`/`FSO.UI`.
- `Game.OnExiting`'s second parameter changed from `EventArgs` to `ExitingEventArgs` in modern MonoGame — 1 call site, `TSOGame.cs`.
- `Microsoft.Xna.Framework.GamerServices`/`Guide` (Xbox Live-era on-screen keyboard API) removed entirely from modern MonoGame, no replacement — cut rather than ported, since the call site was already gated behind `FSOEnvironment.SoftwareKeyboard`, which desktop platforms never set. Nothing to port to.
- `FSO.Client.csproj`'s `OutputType` was briefly set to `WinExe` by mistake during conversion — corrected back to `Library` (matching the original; `Main` lives in the platform-head project, e.g. the new macOS entry point still to be written, not in the client library itself).

**Still open from the compile pass**:
- `RestSharp 110.2.0` has a known moderate-severity NuGet advisory (`GHSA-4rr6-2v9v-wcpc`) — flagged by the build itself as a warning, not fixed, worth a version bump before this ships anywhere real.
- `FSO.Common.Domain`, `FSO.Common.DatabaseService`, `FSO.Server.Protocol`, `FSO.Server.Clients`, `FSO.Server.DataService` compile clean but weren't inspected line-by-line for runtime correctness the way `ApiClient.cs`/`JWTokenFactory.cs` were — those two got real API-migration attention because the compiler forced it; the rest may have subtler behavioral differences under the new package versions that only a running server-communication test would surface.

## Launch attempt outcome (this session, second pass)

**New project**: `TSOClient/FSO.Mac/` (`FSO.Mac.csproj` + `Program.cs`) — the macOS platform head, same role as `FSO.Windows`: references `FSO.Client` (kept as `Library`, confirmed correct earlier), hosts `TSOGame` via `FSOProgram.InitWithArguments` + `GameStartProxy.Start`. Net8.0, `MonoGame.Framework.DesktopGL` PackageReference (brings its own bundled `libSDL2-2.0.0.dylib`/`libopenal.dylib` via the NuGet package's `runtimes/osx/native/` assets — matches what the shipped `/Applications/FreeSO.app` bundles, confirmed by name).

**Real bug found and fixed, not a platform-port issue**: `Utils/GameLocator/MacOSLocator.cs` (already existed, already wired into `FSOProgram.InitWithArguments`'s platform switch) only checked `../The Sims Online/TSOClient/` (relative) or `~/Documents/The Sims Online/TSOClient/` — neither matches where TSO actually installs on this machine (`~/Library/Application Support/The Sims Online/TSOClient/`, confirmed by checking for `tuning.dat` there). Added that path as a checked fallback. Without this fix, the game wouldn't find the TSO install at all and would fail immediately with a "game not found" dialog — this was hiding an entirely separate class of issue behind it.

**Content resolution**: `FSOEnvironment.ContentDir = "Content/"` is relative, resolved against `AppDomain.CurrentDomain.BaseDirectory` (the build output dir), not whatever directory you launch from — confirmed by reading `FSOProgram.InitWithArguments`. `FSO.Content.TSO`'s own `Content/**` copy-to-output only ships a partial tree (missing `3D/`, `Audio/`, `DX/`/`OGL/` compiled shader content, `Fonts/`, `MeshCache/`, the `.uis` parser grammar, etc. — confirmed by diffing against the real one). Replaced the build output's `Content/` with a symlink to `/Applications/FreeSO.app/Contents/MacOS/Content` — the real, complete, already-proven-working tree — rather than trying to reconstruct or copy it. This is a local dev-environment workaround, not a repo change; a real distributable build still needs its own proper content packaging step, not addressed here.

**Result of the actual launch attempt** (`dotnet FreeSO.dll` from the build output dir): **native subsystem init succeeded** — no crash on SDL2/OpenAL loading, no window-creation failure. `TSOGame` booted and reached `LoadContent()` (`TSOGame.cs:324`), which is real, meaningful progress — everything up to and including MonoGame's own `Game.Run()` lifecycle, graphics device creation, and audio device creation worked. It then failed on the very first content load:

```
Content could not be loaded. Make sure that the FreeSO content has been compiled! (ContentSrc/TSOClientContent.mgcb)
Microsoft.Xna.Framework.Content.ContentLoadException: Could not find ContentTypeReader Type... MSDFData.FieldFontReader, MSDFData...
```

**Precisely identified, not a guess**: `Content.Load<FieldFont>("../Fonts/simdialogue")` (loading the game's main vector font, `GameFacade.VectorFont` — used for essentially all text rendering, not skippable) needs a custom MonoGame `ContentTypeReader<FieldFont>` class named `MSDFData.FieldFontReader`, baked into the compiled `.xnb` binary by assembly-qualified name at content-compile time. **That reader class does not exist anywhere in this checked-out source tree** — grepped the whole repo (`ContentTypeReader`/`ContentTypeWriter`) and found nothing outside the vendored MonoGame fork itself. It must have been part of a separate Content Pipeline Extension project used to originally compile these `.xnb` files on Windows, whose source was never checked into this repository (or was lost/is elsewhere). The `.xnb` files themselves are present and fine (confirmed `simdialogue.xnb` exists in the real Content tree, in 4 variants for different render paths) — it's specifically the reader class that's missing.

**Not attempted**: reverse-engineering the `.xnb` binary format to hand-write a compatible reader (the exception message suggests `ContentTypeReaderManager.AddTypeCreator()` as a registration point, but that only helps once you already have a working reader implementation — writing one from scratch requires knowing the exact byte layout MSDFData's original writer used, which isn't derivable from what's checked in). This is a real, bounded, well-understood *next* problem, not something papered over or guessed at.

**Stubs/cuts made to get this far, listed explicitly per the ask**:
- `ClipboardHandler.Default` — left at its built-in no-op default. No clipboard copy/paste.
- `ITTSContext.Provider` — left null. No text-to-speech (matches how the original Windows head already behaves on Linux/Mac anyway).
- `FSO.Files.ImageLoaderHelpers.BitmapFunction`/`SavePNGFunc` — left null. Only consulted conditionally by `ImageLoader` (confirmed via source), so this is safe (no crash), but some non-standard image load paths (custom skins, certain screenshot saves) will silently no-op. A real implementation would need a cross-platform image library (e.g. ImageSharp) in place of Windows' `System.Drawing.Bitmap`-based one.
- The `Content/` symlink itself is a local workaround, not a real content-packaging solution — noted above.

No screenshot — the window/render loop was never reached (content loading happens before the first frame draws). Next session picking this up should start at the `FieldFontReader` problem specifically, not re-verify anything upstream of it — window creation, audio, and TSO-install discovery are all confirmed working.

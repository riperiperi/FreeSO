> **Re-sequenced again 2026-08-11: Phase F pulled forward by Kat, and the networking spike
> this doc recommended has landed.** `PackTools/FSO.WsGateway` is a WS↔TCP byte gateway in
> front of the existing Archive ports (33101/34101) — zero FreeSO changes, because Aries is
> a length-prefixed byte stream and `CustomCumulativeProtocolDecoder` reassembles from any
> chunking. Proven by piping a real `RequestClientSessionArchive` through the bridge and
> deserializing it with the shipping protocol code (3/3 tests). The "open-ended unknown"
> below is now a bounded engineering task; still unproven against a *live* archive server.
> Legal gating (§4) is explicitly waived by Kat for now — CC0 catalog work proceeds in
> parallel rather than as a prerequisite.
>
> **KNI + content + Aries (2026-08-11, later same day):** `FSO.BrowserClient` loads a real
> PNG via `HttpContentStore` → `Texture2D`. Lib chain builds with `-p:FSO_GRAPHICS=Kni`.
> Gateway demo reaches **lot joined** (city FindLot + `/lot` HostOnline/ClientOnline/tick)
> against fake city+lot. Still open: real VM ticks, effects, lot view in Blazor. See `KNI-MIGRATION.md`.

> **Older note, 2026-08-08.** Browser is decided but moved to the tail
> of the roadmap (`../task_plan.md` Phase F) — it lowers install friction rather than
> proving the idea, and upstream's new built-in TSO installer softens the bring-your-own
> objection considerably. The conclusion below is unchanged and load-bearing: **rendering
> is the solved part, the raw-TCP→WebSocket gateway is the only open-ended unknown in the
> whole plan.**

# Browser viability — scoping, not building

Status: assessment only, no code. Scope per Kat: **browser plays, desktop app authors/mods.**
No compiler, no agent loop, no LLM calls, no toolchain in the browser — the browser target
is the game client + content rendering, full stop. That removes filesystem-heavy authoring
paths, native-toolchain dependencies, and API-key handling from the question entirely; they
stay desktop-side regardless of what this doc concludes.

## Bottom line

**Not viable yet, for a reason unrelated to rendering.** The rendering gap (desktop OpenGL →
WebGL) has a real, maintained answer — adopting **KNI**'s Blazor/WebGL target — and is
probably a 2-4 week job once someone's done it once. The actual blocker is **networking**:
FreeSO's live client-server protocol runs on raw TCP sockets (Mina.NET), and browsers cannot
open a raw TCP socket at all. That's not a porting difficulty, it's a hard platform wall, and
nothing in this codebase or its upstream branches has started on it.

So: rendering is solved (elsewhere), networking is not (anywhere in this project), and legal
distribution is gated on original art regardless of either. Sequencing, not a verdict on
whether it's ever possible.

## 1. Prior art — named, not categorical

**KNI** (github.com/kniEngine/kni) is the one that matters most here, because it's not a
different engine to port to — it's a MonoGame-API-compatible fork. Per its own docs and
community reports (DarkGenesis blog, MonoGame community forum threads on "KNI Engine +
BlazorGL"): swap `MonoGame.Framework.DesktopGL` → the matching `nkast.Xna.Framework.*`
packages, and `Game`/`SpriteBatch`/`GraphicsDeviceManager`/input APIs work largely unchanged.
It ships a `BlazorGL` platform target (the enum was literally renamed `BLAZOR` → `BlazorGL`)
using Blazor WebAssembly as the browser host. Used in production-ish by SadConsole,
FlatRedBall, Apos.Gui per the MonoGame community.

Known limits reported by people who've actually shipped on it:
- **Gamepad input is not supported on the web target** — needs conditional compilation.
- **Some shaders don't work or need feature reduction** going through WebGL's tighter subset.
- **File access has browser permission complications** — content has to arrive via fetch/HTTP, not local disk.
- Explicit warning from someone who's done it: "the Web is NOT your desktop... start with the minimum, add until it breaks."

**FNA-WASM-Build** (github.com/r58Playz/FNA-WASM-Build) is the other real lineage — it's how
**Celeste** and **Terraria** run in-browser, via Emscripten compiling FNA3D to WebGL2 rather
than a Blazor host. Not directly applicable here (FreeSO is on MonoGame's own API, not FNA),
but the Celeste-WASM writeup (velzie.rip/blog/celeste-wasm) is the best available account of
what actually breaks when a real, shipped 2D game hits the browser runtime, and it's a useful
stress test for claims below:
- **Threading**: .NET's WASM threading model runs code in web workers, but the `<canvas>`
  API only exists on the DOM thread — needed an OpenGL proxy (`emscripten_proxy_sync`) to
  marshal every GL call back to the main thread.
- **Audio**: FMOD (Celeste's proprietary audio middleware) doesn't support running in a
  worker either — required extracting and recompiling object files with wrapper code. Not our
  problem directly (FreeSO doesn't use FMOD) but the audio-doesn't-work-off-the-main-thread
  problem is general to the WASM threading model, not FMOD-specific.
- Runtime-level bugs: "wasm .NET is just straight up broken in a lot of cases" — missing
  crypto, broken reflection, ~200 lines of patches to the Mono runtime itself were needed.
  This was on a heavier, modded, MonoMod-instrumented game; expect less of this for FreeSO,
  but the ceiling on "you might have to patch the runtime" is real and not zero.
- **Took about a year** for a small, motivated team on a much harder game (native audio
  middleware, runtime bytecode patching for mod support, multiplayer via CelesteNet).

MonoGame itself has no official web/WASM target — the community tried a Bridge.NET-based
web build years ago; it's gone, the demo's been pulled down, and the GitHub issue asking for
WebAssembly support (MonoGame/MonoGame#8102) is old and unresolved. KNI is the maintained
answer to the gap MonoGame itself never closed.

## 2. Fit against this codebase specifically

Checked by reading (not editing) `TSOClient/`, scoped to what actually ships in the client
process (excluding `tso.debug`, `FSO.IDE` — Windows Forms dev tools never shipped to players).

**Client target today**: `FSO.Mac.csproj` / presumably `FSO.Windows.csproj` reference
`MonoGame.Framework.DesktopGL 3.8.*` on net8.0, rendering through desktop OpenGL. `tso.sound`
(the audio project, `FSO.HIT`) *also* depends on `MonoGame.Framework.DesktopGL` for its audio
backend — so this isn't a "swap the renderer, leave audio alone" move; a KNI migration means
retargeting every MonoGame.Framework reference across the client and audio projects to the
matching `nkast.Xna.Framework.*` package, consistently. That's mechanical but not small — it
touches every project that references MonoGame, not just the rendering-facing ones.

**Networking — the actual blocker.** `FSO.Server.Clients/AriesClient.cs` opens its connection
via `Mina.Transport.Socket.AsyncSocketConnector` — raw TCP. `TSOClient/tso.client/Network/`
has an even older raw-socket layer (`NetworkClient.cs`, `PacketHandlers.cs`) that's already
dead code (explicitly `<Compile Remove>`'d in `FSO.Client.csproj`, superseded by the
data-service layer) — but the thing that superseded it, `ClientDataService` →
`AriesClient`, is *still* raw TCP, just wrapped in a nicer API. No WebSocket transport exists
anywhere in this project. A browser tab cannot open a raw TCP socket, full stop — this isn't
a WASM runtime limitation like the threading/audio issues above, it's a browser sandboxing
rule with no workaround at the client. The fix is a server-side WebSocket-to-TCP gateway (or
adding a native WebSocket transport to the Mina-based protocol stack) — real, buildable, but
a second project of its own, not a side effect of the rendering port.

**Filesystem.** Content loading (FAR archives, `.iff` files) is read via `FileStream`/
`File.ReadAllBytes` in ~86 files across the client projects — normal for a desktop game, but
means every content-load path assumes local disk. In a browser, content has to be fetched
over HTTP into memory (this is exactly what KNI's content pipeline / Blazor template
expects) — so this needs adaptation, but it's the same "fetch into a byte array instead of
opening a file handle" pattern every one of these ports does, not a novel problem for FreeSO.

**`System.Drawing`.** 29 files reference it, but essentially all are `tso.debug` (WinForms
debug tool) and `FSO.IDE` (the Windows Forms object editor) — dev tooling, never shipped to
players, irrelevant to a browser client. One hit is in `tso.client/GameContent/
ContentManager.cs`, which is *also* already excluded from compilation
(superseded, per `FSO.Client.csproj`'s own comment). Net effect: essentially zero
`System.Drawing` exposure in the actual shipped client path — a genuinely light risk, not a
blocker, which is worth saying plainly since it could easily have gone the other way.

**Threading.** `new Thread(...)` appears in five files that do ship
(`tso.common/Utils/Cache/FileSystemCache.cs`, `tso.common/Audio/MP3Player.cs`,
`tso.content/ContentPreloader.cs`, `tso.simantics/NetPlay/Drivers/VMServerDriver.cs`, plus the
already-dead `ContentManager.cs`). Each would need to become cooperative
(Task-based/single-threaded-friendly) for a WASM target without opting into WASM's
multi-threaded mode — which itself requires `SharedArrayBuffer` and the COOP/COEP response
headers Celeste-WASM had to fight with. Not huge in raw file count, but each one is a
"figure out what this thread is really doing and whether it can be a Task instead" job, and
`VMServerDriver` in particular (networking-adjacent, in the simulation layer) deserves a
closer look before anyone assumes it's trivial.

## 3. Hybrid model: play in browser, download to create

This is the shape Kat's proposing, and it changes the sequencing question, not the technical
one. Two parts:

**Would it work mechanically?** Yes, with the caveat that "receiving" is a distribution
question, not a technical one. A desktop-authored pack compiles to a real `.iff` +
catalog entries (`PackTools/SCHEMA.md`'s compiler contract) — the same artifact a browser
client would need to load. If the browser client can load *any* object content over HTTP
(which it must, per the filesystem point above), it can load a compiled pack's `.iff` the
same way, once the object is registered server-side. Nothing about the pack compiler or its
output format is desktop-specific; it emits standard `.iff` chunks either way. So the hybrid
model doesn't need new object-format work — it needs the browser client's content pipeline
(whatever loads `Objects/*.iff` today) to fetch pack-authored objects the same way it fetches
base-game ones, and the server to serve them.

**What actually gates it:** the same two things above, unconditionally — the browser build
needs the WebGL rendering path (KNI) to exist before it can render anything at all, and it
needs a WebSocket-reachable server before a browser player can join a lot with anyone else on
it. Neither is specific to "hybrid" vs. "browser-only"; hybrid doesn't dodge either
requirement, it just changes who's allowed to *create* content once those exist. So hybrid is
viable in principle and doesn't add new technical risk on top of the browser-client question
— but it inherits both of that question's blockers in full, not a discounted version of them.

## 4. Legal gating (context, not a technical constraint)

A browser build means the game serves its own content directly rather than requiring a
separate TSO install — that's distribution, and only clean once the art is original. The
ArtGen work (parametric chair/table/bed/lamp/storage generators, already producing real
sprites — see `PackTools/ART-PIPELINE-DESIGN.md`) is exactly what unblocks this, and it's
progressing in parallel. This gates the whole effort regardless of technical viability —
factor it into *when*, not *whether*.

## 5. Estimate and risk, honestly

| Piece | Confidence it's solvable | Rough effort | Prior art |
|---|---|---|---|
| Rendering (DesktopGL → KNI/BlazorGL) | High | 2-4 weeks | KNI itself, SadConsole/FlatRedBall/Apos.Gui shipping on it |
| Audio retarget (tso.sound → KNI) | High | included above (same migration) | same |
| Content loading (disk → fetch) | High | 1-2 weeks | every WASM game port does this |
| Threading cleanup (5 files) | Medium | 1-2 weeks, more if `VMServerDriver` fights back | Celeste-WASM's proxy pattern, if multi-threaded WASM is needed at all |
| **Networking (TCP → WebSocket gateway)** | **Unknown — no prior art in this project, not attempted upstream either** | **Weeks to months; genuinely open-ended without a design spike** | None found — this is the frontier, not KNI's or FNA's solved problem |
| Legal/content gating | N/A (policy, not code) | Gated on ArtGen maturity | — |

**Single biggest risk: networking, not rendering.** Every piece of "make MonoGame render in
a browser" has a name attached to it and people who've shipped it. Nothing found — in this
project's history, upstream, or the wider MonoGame/FNA community search — addresses turning
a raw-TCP Mina-based multiplayer protocol into something a browser tab can speak. That's the
actual unknown, and it's not a rendering problem at all, so no amount of KNI expertise closes
it. Recommend a narrow design spike specifically on "WebSocket gateway in front of the
existing lot/city server" before sizing this further — that's the piece nobody has derisked
yet, upstream or here.

## Recommendation

Sequence as: ArtGen maturity (already in motion, gates distribution) → a short networking
spike to derisk the actual unknown → then the KNI rendering port, which is comparatively
well-trodden ground. Doing the rendering port first would produce a browser tab that can
render a lot but can't join one with anyone else on it — technically impressive, not the
five-second pitch ("type a sentence, watch an object appear" implies *other people see it*).

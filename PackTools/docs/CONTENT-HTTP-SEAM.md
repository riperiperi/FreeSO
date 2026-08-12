# Content HTTP seam (spike)

Status: **wired into `Content.GetResource` + BasePath `FileProvider`** (2026-08-11).
`Content.Store` defaults to `FileContentStore(BasePath)`; browser swaps `HttpContentStore`
via `Content.SetStore`. FAR3 archives load once through the store into a seekable stream
(`FAR3Archive(Stream)`). Content/ and TS1 overlays in `FileProvider` still use
`File.OpenRead`. Pair with `BROWSER-VIABILITY.md` / `FSO.WsGateway`.

## Why

Browser clients cannot `File.OpenRead`. FreeSO content today lives on disk under the
client base path (`Content.BasePath`) and `Content/` (FSO overlay). In-browser, the same
relative paths must be fetched over HTTP into memory. This package is the thinnest
abstraction that lets desktop keep `FileStream` and browser use `HttpClient`, without a
wholesale rewrite of `tso.content`.

## What landed

| Piece | Path |
|---|---|
| `IContentStore` | `PackTools/FSO.BrowserContent/IContentStore.cs` |
| `FileContentStore` | desktop — rooted directory, sync + async |
| `HttpContentStore` | browser — `HttpClient` + base URL, bytes → `MemoryStream` |
| Tests | `PackTools/FSO.BrowserContent.Tests` |

API surface:

```csharp
Task<Stream> OpenAsync(string relativePath, CancellationToken ct = default);
Task<byte[]> ReadAllBytesAsync(string relativePath, CancellationToken ct = default);
Task<bool> ExistsAsync(string relativePath, CancellationToken ct = default);
```

`FileContentStore` also exposes sync `Open` / `ReadAllBytes` for today’s sync call sites.

Paths are content-relative (`pet-rock.json`, `uigraphics/foo.png`). `..` is rejected.
`HttpContentStore.OpenAsync` buffers to a `MemoryStream` (browser-friendly; no seeking over
the wire).

## Where FreeSO should plug in next

**Primary choke point:** `FSO.Content.Content.GetResource(string path, ulong assetID)` in
`TSOClient/tso.content/Content.cs`.

That method already funnels archive and loose-file opens:

1. `*.dat` → `new FAR3Archive(GetPath(path))` then entry by ID
2. else → `File.OpenRead(GetPath(path))`

**Recommended first wiring (still a follow-up PR, not this spike):**

1. Add an optional `IContentStore? Store` (or required field set in `Content.Init`) on
   `Content`, defaulting to `new FileContentStore(BasePath)` so desktop behaviour is identical.
2. Change `GetResource`’s loose-file branch to `Store.Open(...)` / `OpenAsync` (sync wrapper
   fine for desktop).
3. Give `FAR3Archive` / `FAR1Archive` a `Stream`- or `byte[]`-based constructor (they already
   wrap a `BinaryReader` over a stream) so archive open goes through the store once, then
   in-memory random access — **do not** make every FAR entry an HTTP GET.
4. Next: `FileProvider<T>.Get` (`Framework/FileProvider.cs`) — the other high-traffic
   loose-file decoder path (`File.OpenRead(fullPath)`).

`Content.Get()` / `Content.Init(basepath, device)` stay the public entry; only the byte
source behind `GetPath` + open changes.

### Content roots to serve over HTTP

When hosting for a browser build, expose (same relative layout the client expects):

- FreeSO overlay: `Content/` (UI, patches, FSODataDefinition, upgrades, …)
- Game data: TSO client directory (`objectdata/`, `uigraphics/`, `*.dat` FARs, cities, …)

A single static host with both trees (or two base URLs / a composite store) is enough for
the next spike. Do not invent a new pack format.

## Top 10 hotspots (read path)

Ordered by “plug store here first,” not raw `FileStream` count (many hits are write/debug):

| # | File | Why |
|---|---|---|
| 1 | `tso.content/Content.cs` | `GetResource`, data definition, version, `GetPath` |
| 2 | `tso.content/Framework/FileProvider.cs` | Generic loose-file decode (`File.OpenRead`) |
| 3 | `tso.files/FAR3/FAR3Archive.cs` | FAR3 open — needs stream ctor from store bytes |
| 4 | `tso.files/FAR1/FAR1Archive.cs` | FAR1 open — same |
| 5 | `tso.content/Framework/FAR3Provider.cs` | Opens FARs via path during avatar/UI init |
| 6 | `tso.content/Framework/FAR1Provider.cs` | Same for FAR1 |
| 7 | `tso.files/Formats/IFF/IffFile.cs` | Direct `.iff` path open |
| 8 | `tso.content/UIGraphicsProvider.cs` | UI textures from disk |
| 9 | `tso.content/Model/TextureRef.cs` | Texture `FileStream` |
| 10 | `tso.files/Formats/DBPF/DBPFFile.cs` | DBPF (`File.OpenRead`) |

Honourable mentions once the above work: `RCDBPFContent.cs`, `ImageLoader.cs`,
`OTFFile.cs`, HIT audio loaders (`HITFile`, `EVT`, `FSC`, …), `IniFile.cs`.

`Directory.GetFiles` scanning in `Content._ScanFiles` is a **separate** problem (need a
manifest or `index.json` over HTTP). Do not solve that in the first wiring.

## What NOT to rewrite yet

- Whole `tso.content` / provider graph (`Avatar*Provider`, world providers, codecs).
- MonoGame → KNI / BlazorGL rendering port.
- Mina.NET / Aries transport (use `FSO.WsGateway`; already spiked).
- Write paths (`ChangeManager`, PIFF save, neighbourhood save, mesh export).
- `System.Drawing` / `FSO.IDE` / `tso.debug`.
- Making every FAR entry a separate HTTP request.
- Async-everywhere churn across SimAntics / world load on desktop.

## Verify

```sh
dotnet test PackTools/FSO.BrowserContent.Tests/FSO.BrowserContent.Tests.csproj
```

File tests read `PackTools/examples/pet-rock.json`. HTTP tests spin a loopback
`HttpListener` serving `PackTools/examples/`.

## Done looks like (later)

Browser client constructed with `new HttpContentStore("https://content.example/")`,
desktop still `FileContentStore(BasePath)`, and `Content.GetResource` + one FAR archive
type load a real UI graphic or object IFF without `File.OpenRead` on the browser target.

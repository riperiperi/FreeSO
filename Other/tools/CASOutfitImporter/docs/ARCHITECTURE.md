# Architecture

How FreeSO loads avatar content, how the two tools fit together, and where
the moving parts live.

## The five layers

```
   ┌─────────────────────────────────────────────────────────────────────┐
   │ 1. INPUT — TS1 skin folder                                          │
   │    .skn mesh + .bmp textures + .cmx metadata                        │
   └─────────────────────────────┬───────────────────────────────────────┘
                                 │  CASOutfitImporter
                                 ▼
   ┌─────────────────────────────────────────────────────────────────────┐
   │ 2. CONTENT-REPO — managed library                                   │
   │    content-repo/<category>/<name>/{pack.json, Content/...}          │
   │    Each pack carries its own files + a single-entry .col            │
   └─────────────────────────────┬───────────────────────────────────────┘
                                 │  manage-packs install/uninstall/reset
                                 ▼
   ┌─────────────────────────────────────────────────────────────────────┐
   │ 3. SOURCE TREE — TSOClient/FSO.Content.TSO/Content/Avatar/...       │
   │    Merged .col + every installed pack's individual files            │
   └─────────────────────────────┬───────────────────────────────────────┘
                                 │  GitHub Actions builds → Releases →
                                 │  update-server.sh → freeso-portal
                                 │  → game's built-in updater
                                 ▼
   ┌─────────────────────────────────────────────────────────────────────┐
   │ 4. PROVIDERS — TSOAvatarContentProvider chain (at runtime)          │
   │    Composite of [FAR3 archives, loose Files, runtime patches]       │
   └─────────────────────────────┬───────────────────────────────────────┘
                                 │
                                 ▼
   ┌─────────────────────────────────────────────────────────────────────┐
   │ 5. CONSUMERS                                                        │
   │    CAS UI (PersonSelectionEdit) → ea_*[_heads].col                  │
   │    Wedding trunk (VMEODTrunkPlugin) → wedding_<gender>.col          │
   │    Clothes rack (RackOutfitsProvider) → purchasable.xml + ea_*.col  │
   └─────────────────────────────────────────────────────────────────────┘
```

## Vitaboy file relationships

Each layer points down at the layer beneath it via a `(TypeID, FileID)` pair:

```
   .col          (collection — what UI lists)
     │  Index  │  CollectionItem → PurchasableOutfitId
     ▼
   .po           (purchasable — gender + outfit reference)
     │  Version │  Gender │  OutfitAssetID
     ▼
   .oft          (outfit — three skin-tone appearances + region + handgroup)
     │  LightAprId │ MediumAprId │ DarkAprId │ HandGroup │ Region(1=head,2=body)
     ▼
   .apr          (appearance — list of bindings)
     │  Bindings[]: BindingId
     ▼
   .bnd          (binding — bone → mesh + texture)
     │  Bone string │ MeshId │ TextureId
     ▼
   .mesh + .png  (geometry + texture)
```

A complete CAS or trunk outfit is a tree of 7+ files (one mesh, three
textures, three bindings, three appearances, one outfit, one purchasable,
plus an entry inside one collection file). The importer emits all of them
with internally consistent IDs and the manage-packs tool keeps them grouped
as a unit.

## How the engine resolves a content lookup

When the wedding trunk plugin runs `Content.AvatarCollections.Get("wedding_female.col")`,
the call goes to `TSOAvatarContentProvider<Collection>`, which is a
`CompositeProvider` over three sub-providers in order:

```csharp
SetProviders(new List<IContentProvider<T>> {
    FAR,     // FAR3 archives matching .*/collections/.*\.dat
    Files,   // loose files matching Avatar/Collections/.*\.col
    Runtime  // in-memory edits from FSO.IDE
});
```

`CompositeProvider.Get(name)` returns the first sub-provider that has it.
**FAR3 archives are checked before loose files.** So if a `wedding_female.col`
exists in both, the FAR3 version wins and the loose copy is silently ignored.

This is why the **source patch** in `WORKFLOW.md` is required. Without it,
the merged `.col` files manage-packs writes have no effect at runtime.

## ID encoding for loose content files

When the engine looks up a content file by `(TypeID, FileID)` rather than by
filename — which is how `.po` references its `.oft`, etc. — the loose
`FileProvider` parses the IDs out of a special filename convention:

```
  baseName.HEXID16.ext

  HEXID16 = (FileID << 32 | TypeID) printed as 16 lowercase hex chars
```

Example: `k8groupie_body_f.b3445c6b4f465432.oft`
- HEXID16 = `b3445c6b4f465432`
- FileID  = `0xb3445c6b`
- TypeID  = `0x4f465432`  (ASCII "OFT2")

The importer generates random 32-bit FileIDs (with the high bit set to stay
clear of low-numbered Maxis IDs) and stable TypeIDs per content kind.

## Why content-repo + manage-packs?

Three properties drove the design:

### 1. Versioning via git

`content-repo/` is tracked in git. Every pack is a directory with a
manifest. Adding a pack is a commit; removing one is a commit; the audit
log is `git log content-repo/`. No external manifest, no DB, no lock files.

### 2. Reversibility via uninstall

Each pack's `pack.json` declares exactly which files it contributes and
which `.col` entry it adds. `manage-packs uninstall` reads the manifest and
reverses the operation precisely — files deleted, `.col` entry removed.
Resetting a category is just "uninstall every installed pack here".

### 3. Composability via deterministic merging

Multiple packs targeting the same `.col` (e.g. three wedding-trunk packs)
coexist via merge: the live `wedding_female.col` is rebuilt from `vanilla +
union of every installed pack's entry`, sorted deterministically by
`(typeId, fileId)`. Re-applying the same set of packs produces byte-identical
output — clean diffs, no churn.

## How the runtime sees content from a build

The FreeSO source tree has a **shared content project** at
`TSOClient/FSO.Content.TSO/`. Its csproj has:

```xml
<Content Include="Content\**\*.*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

Both `FSO.Client.csproj` and `FSO.Server.csproj` reference `FSO.Content.TSO`,
so MSBuild copies the entire `Content/**/*.*` tree into both build outputs
automatically. `manage-packs install` writes into this exact tree. The
GitHub Actions CI then bakes the content into the released zips. The
existing `update-server.sh` deploys to the running server. The freeso-portal
admin "Publish Update" hands the new zip to all connected clients.

The CI's path filters
(`build-{client,server}.yml: paths: TSOClient/FSO.Content.TSO/**`) ensure
that touching content alone triggers a build — no source code change needed.

## Two distinct in-game flows for outfits

| Concept | Where the engine reads from | Money? | Persistence |
|---|---|---|---|
| **CAS selectable** | `ea_<gender>[_heads].col` | n/a — picked at character creation | Becomes the avatar's body/head |
| **Trunk costume** | `<type>_<gender>.col` | Free | Transient costume (`VMPersonSuits.DynamicCostume`); reverts when leaving lot |
| **Clothes rack purchase** | `packingslips/purchasable.xml` + `ea_*.col` | Costs simoleons | Inserts row in `fso_outfits`; permanent ownership |

Today the importer covers the first two via `--mode cas` and
`--mode trunk:wedding`. The third is described in `LIMITATIONS.md`.

## Server-side validation differs by mode

| Flow | Server validates? | Where |
|---|---|---|
| CAS body/head | **Yes** — strict whitelist | `RegistrationHandler.cs:49-85` builds a set from `ea_*.col` at startup |
| Trunk costume | **No** | Server stores nothing for transient costumes |
| Clothes rack purchase | Trusts the client | Inserts whatever `asset_id` the client paid for into `fso_outfits` |

Practical implication: a CAS pack must reach **both** client and server
binaries (so build-server.yml + the server deploy step matter). A trunk
pack only needs the client binary.

## Where everything lives in the repo

```
FreeSO/                                   ← the source-tree fork
├── content-repo/                         ← managed pack library + config
│   ├── config.yaml
│   ├── _vanilla/                         ← gitignored, regenerable
│   ├── trunks/<type>/<name>/
│   └── cas/<heads|bodies>/<gender>/<name>/
├── manage-packs                          ← bash wrapper (top-level convenience)
├── Other/
│   └── tools/
│       ├── CASOutfitImporter/            ← C# .NET Core 3.1 generator
│       └── ManagePacks/
│           └── manage_packs.py           ← Python install/uninstall/reset
├── scripts/
│   ├── update-server.sh                  ← (existing) deploys server zip
│   └── update-client.sh                  ← (existing) updates a client install
├── .github/workflows/
│   ├── build-client.yml                  ← (existing) CI for client
│   └── build-server.yml                  ← (existing) CI for server
└── TSOClient/
    ├── FSO.Content.TSO/Content/          ← managed by manage-packs install
    └── tso.content/Framework/
        └── TSOAvatarContentProvider.cs   ← needs the one-line source patch
```
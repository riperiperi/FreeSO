# CASOutfitImporter

CLI that turns a folder of The Sims 1 skin assets (`.skn` mesh + `.bmp`
textures + `.cmx` metadata) into a Vitaboy-format content pack the FreeSO
engine can load — either as a Create-A-Sim selectable head/body, or as a
costume wearable at an in-game wedding trunk.

The importer is the **first half** of the modding pipeline. The companion
`manage-packs` tool (at the FreeSO source-tree root) installs the generated
packs into your fork's source tree, where they get picked up by the existing
GitHub Actions / `update-server.sh` / freeso-portal release pipeline.

```
   TS1 skin folder
        │
        │  CASOutfitImporter --save-to-repo …
        ▼
   content-repo/<category>/<name>/
        │
        │  ./manage-packs install …
        ▼
   TSOClient/FSO.Content.TSO/Content/Avatar/…
        │
        │  git push → CI → release → admin "Publish Update" → clients
        ▼
   live game
```

## Quick start

```bash
cd /srv/dev_projects/personal/FreeSO/Other/tools/CASOutfitImporter

# 1. Build the tool (first time only)
dotnet build

# 2. Generate a pack into the content-repo
dotnet bin/Debug/netcoreapp3.1/CASOutfitImporter.dll \
  --save-to-repo /srv/dev_projects/personal/FreeSO/content-repo --verify \
  --input /srv/dev_projects/personal/FreeSO/test_skin_head/b076fa_k8groupie \
  --type body --gender f --name k8groupie \
  --mode trunk:wedding

# 3. Install into the source tree (interactive or scripted)
cd /srv/dev_projects/personal/FreeSO
./manage-packs list
./manage-packs install trunks/wedding/k8groupie
```

> The native apphost is broken on snap-isolated dotnet on this machine, so
> examples invoke the dll directly. On a normal dotnet install, `dotnet run`
> works as expected.

## Documentation

| File | Contents |
|---|---|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | How FreeSO loads avatar content, file relationships, FAR3 vs loose, ID encoding |
| [docs/INPUT_FORMAT.md](docs/INPUT_FORMAT.md) | What a TS1 skin folder must contain |
| [docs/CLI_REFERENCE.md](docs/CLI_REFERENCE.md) | Every flag, every mode, full examples |
| [docs/CONTENT_REPO.md](docs/CONTENT_REPO.md) | content-repo layout + pack.json schema |
| [docs/MANAGE_PACKS.md](docs/MANAGE_PACKS.md) | The install/uninstall/reset tool reference |
| [docs/WORKFLOW.md](docs/WORKFLOW.md) | End-to-end pipeline from skin to live game |
| [docs/LIMITATIONS.md](docs/LIMITATIONS.md) | What works, what doesn't, what's planned |

## Modes supported today

- **`--mode cas`** — outfit becomes a selectable head/body in Create-A-Sim
- **`--mode trunk:wedding`** — outfit appears as a free costume at any wedding trunk

## Layout

```
CASOutfitImporter/
├── CASOutfitImporter.csproj
├── Program.cs
├── Formats/        # binary readers + writers (.mesh, .col, FAR3, RefPack, …)
├── Imaging/        # 8-bit BMP reader, RGBA PNG writer, magenta key, tone synth
├── Importer/       # the SkinImporter pipeline (input folder → staged pack)
├── Verify/         # cross-link verifier
├── Packaging/      # README generator + zip bundler (legacy --staging mode)
├── Tests/          # synthetic FAR3 generator for round-trip testing
└── docs/           # this directory
```
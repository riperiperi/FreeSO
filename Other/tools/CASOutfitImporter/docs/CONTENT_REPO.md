# content-repo

Managed library of generated content packs. The `CASOutfitImporter` deposits
packs here via `--save-to-repo`; `manage-packs` reads from here when listing,
installing, uninstalling, or resetting.

## Layout

```
/srv/dev_projects/personal/FreeSO/content-repo/
├── config.yaml                       # paths + categories the manager knows
├── .gitignore                        # _vanilla/ is local-only
├── _vanilla/                         # FAR3-extracted baseline collections
│   ├── ea_female.col
│   ├── ea_female_heads.col
│   ├── wedding_female.col
│   └── ...
├── trunks/
│   ├── wedding/
│   │   └── <name>/
│   │       ├── pack.json
│   │       └── Content/Avatar/...    # the staged file tree
│   ├── scifi/
│   ├── vegas/
│   └── ...
└── cas/
    ├── heads/<gender>/<name>/
    └── bodies/<gender>/<name>/
```

The directory under `trunks/` or `cas/` mirrors the **category** that
appears in pack.json. Browsing is hierarchical: a human can `ls
content-repo/trunks/wedding/` to see all wedding-trunk packs.

## config.yaml

Two paths and a list. Edit once after cloning, never again.

```yaml
# Path to the FreeSO source-tree fork. Files installed by manage-packs land
# under <freeso_source>/TSOClient/FSO.Content.TSO/Content/Avatar/...
freeso_source: /srv/dev_projects/personal/FreeSO

# Path to a working FreeSO game install. Used by `manage-packs refresh-vanilla`
# to extract baseline collection files from FAR3 archives.
game_dir: /home/ian/games/edenso/game/TSOClient

# Trunk types known to VMEODTrunkPlugin. Manage-packs treats each as a
# separate management category.
trunk_types:
  - wedding
  # - scifi
  # ... uncomment as you author packs for them
```

## pack.json schema

Every pack carries a `pack.json` manifest. The manager uses it to know what
files to copy/remove and which `.col` entry to merge or strip.

```json
{
  "name": "k8groupie",
  "category": "trunks/wedding",
  "gender": "f",
  "type": "body",
  "mode": "trunk:wedding",
  "created_at": "2026-05-06T22:30:00Z",
  "tool_version": "0.3.0",
  "outfit_id": "BA5BF97F4F465432",
  "purchasable_id": "BE7F4044504F5446",
  "collection_entry": {
    "target": "wedding_female.col",
    "type_id": "0x504F5446",
    "file_id": "0xBE7F4044"
  },
  "files": [
    "Content/Avatar/Meshes/User/k8groupie_body_f.<id>.mesh",
    "Content/Avatar/Textures/User/k8groupie_body_f_lgt.<id>.png",
    "Content/Avatar/Textures/User/k8groupie_body_f_med.<id>.png",
    "Content/Avatar/Textures/User/k8groupie_body_f_drk.<id>.png",
    "Content/Avatar/Bindings/User/k8groupie_body_f_lgt.<id>.bnd",
    "Content/Avatar/Bindings/User/k8groupie_body_f_med.<id>.bnd",
    "Content/Avatar/Bindings/User/k8groupie_body_f_drk.<id>.bnd",
    "Content/Avatar/Appearances/User/k8groupie_body_f_lgt.<id>.apr",
    "Content/Avatar/Appearances/User/k8groupie_body_f_med.<id>.apr",
    "Content/Avatar/Appearances/User/k8groupie_body_f_drk.<id>.apr",
    "Content/Avatar/Outfits/User/k8groupie_body_f.<id>.oft",
    "Content/Avatar/Purchasables/User/k8groupie_body_f.<id>.po",
    "Content/Avatar/Collections/wedding_female.col"
  ]
}
```

| Field | Meaning |
|---|---|
| `name` | Short identifier; used in filenames + as directory name in repo |
| `category` | Hierarchical category; matches the directory under content-repo |
| `gender` | `"f"` or `"m"` — used to pick which `.col` we target |
| `type` | `"body"` or `"head"` |
| `mode` | CLI-shaped mode string (round-trips through `--mode`) |
| `created_at` | ISO-8601 UTC timestamp |
| `tool_version` | Importer version that produced the pack |
| `outfit_id` / `purchasable_id` | 64-bit packed `(fileId<<32 | typeId)`, hex |
| `collection_entry.target` | The `.col` file this pack adds an entry to |
| `collection_entry.type_id` / `file_id` | The 32-bit IDs for that entry |
| `files` | All files (relative to the pack dir) that this pack contributes |

The `files` list is the source of truth for uninstall: `manage-packs` deletes
exactly those files (minus `Content/Avatar/Collections/*.col`, which it
rebuilds from vanilla + remaining packs).

## _vanilla/ snapshots

`manage-packs refresh-vanilla` reads the FAR3 archives under your `game_dir`
(typically `<game>/avatardata*/bodies/collections/*.dat` and the matching
`heads/`) and extracts every `.col` we know about into `_vanilla/`. These
serve two purposes:

- **Display baseline** — `manage-packs list` shows "vanilla: 37 outfits" so
  you can see what FreeSO ships before any of your packs.
- **Reset target** — `manage-packs reset` rebuilds the live `.col` from
  `_vanilla/` plus whatever packs are still installed; if no packs remain, it
  removes the loose `.col` entirely so the engine falls back to FAR3.

`_vanilla/` is **gitignored**: it varies per game install and can always be
regenerated. Don't commit it.

## Why store the .col inside each pack

Each pack ships its own single-entry `.col` file (just its contribution).
At install time, `manage-packs` produces the live merged `.col` by reading:

1. The vanilla snapshot from `_vanilla/<target>`
2. The single-entry `.col` of every currently-installed pack targeting the
   same collection
3. Plus the new pack's entry (for an install) or minus the removed pack's
   entry (for an uninstall)

Output is sorted by `(typeId, fileId)` so re-applying the same set of packs
always produces an identical merged `.col` — clean diffs, no spurious churn.
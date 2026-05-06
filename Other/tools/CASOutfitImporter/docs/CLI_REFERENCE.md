# CLI reference (CASOutfitImporter)

For `manage-packs` (the install/uninstall/reset tool), see
[MANAGE_PACKS.md](MANAGE_PACKS.md).

## Synopsis

```
# Recommended — save into a content-repo (managed by manage-packs):
CASOutfitImporter --save-to-repo <repo-dir> [--verify]
                  --input <dir> --type head|body --gender m|f --name <id>
                                [--mode cas|trunk:wedding]
                  [--input ... ] ...

# One-off staging dir / zip (legacy):
CASOutfitImporter [--staging <dir>] [--zip <path.zip>] [--verify]
                  --input <dir> --type head|body --gender m|f --name <id>
                                [--mode cas|trunk:wedding]
                                [--source-collection <path.col-or-FAR3>]
                  [--input ... ] ...

# Round-trip verify an already-staged tree:
CASOutfitImporter --verify-only <staged-dir>
```

## Global flags

| Flag | Argument | Default | Purpose |
|---|---|---|---|
| `--save-to-repo` | dir path | (none) | Save pack into `<dir>/<category>/<name>/` + write `pack.json`. Single-entry `.col` only — manage-packs handles merging at install time. Mutually exclusive with `--source-collection`. |
| `--staging` | dir path | `./out` | (legacy mode) Where to write the staged `Content/...` tree |
| `--zip` | path | (none) | (legacy mode) Also bundle the staged tree into a zip |
| `--verify` | — | off | After import, run cross-link verification on the staged output |
| `--verify-only` | dir path | — | Skip import; verify an existing staged directory |
| `-h`, `--help` | — | — | Print usage and exit |

## Per-input flags

Each `--input <dir>` introduces a new outfit. Subsequent flags up to the next
`--input` apply to that item.

| Flag | Argument | Required | Purpose |
|---|---|---|---|
| `--input` | folder | yes | Path to a TS1 skin folder (see `INPUT_FORMAT.md`) |
| `--type` | `head` \| `body` | yes | Whether this is a head or body outfit |
| `--gender` | `m` \| `f` (or `male`/`female`) | yes | Avatar gender |
| `--name` | short id | yes | Identifier baked into output filenames AND the pack subdirectory name. Non-alphanumeric chars become `_` |
| `--mode` | see Modes below | `cas` | Where the outfit gets registered |
| `--source-collection` | path | optional | (legacy mode only) Existing collection to merge with (raw `.col` or FAR3 `.dat`) |

## Modes

| Mode | content-repo subdir | Collection target | Server validation? |
|---|---|---|---|
| `cas` | `cas/heads/<g>/<name>/` or `cas/bodies/<g>/<name>/` | `ea_<gender>[_heads].col` | **Yes** — server `RegistrationHandler` whitelists from this file |
| `trunk:wedding`  | `trunks/wedding/<name>/`  | `wedding_<gender>.col`  | No |
| `trunk:scifi`    | `trunks/scifi/<name>/`    | `scifi_<gender>.col`    | No |
| `trunk:vegas`    | `trunks/vegas/<name>/`    | `vegas_<gender>.col`    | No |
| `trunk:vaudwest` | `trunks/vaudwest/<name>/` | `vaudwest_<gender>.col` | No |
| `trunk:uniforms` | `trunks/uniforms/<name>/` | `uniforms_<gender>.col` | No |
| `trunk:costumes` | `trunks/costumes/<name>/` | `costumes_<gender>.col` | No |
| `trunk:oldworld` | `trunks/oldworld/<name>/` | `oldworld_<gender>.col` | No |
| `trunk:sports`   | `trunks/sports/<name>/`   | `sports_<gender>.col`   | No |
| `trunk:toga`     | `trunks/toga/<name>/`     | `toga_<gender>.col`     | No |

The 9 trunk modes correspond 1:1 to the values of
`VMEODTrunkPluginCollections` in `tso.simantics`. CAS bodies + heads use
the `RegistrationHandler` whitelist — **server must rebuild + redeploy** for
CAS packs. Trunk modes never touch the server-side whitelist; the client
binary is sufficient.

`--type head` is incompatible with any `trunk:*` mode (heads aren't wearable
costumes; the trunk plugin only swaps body outfits).

## --source-collection auto-detect (legacy mode)

The flag accepts:

| Input | Behavior |
|---|---|
| Raw `.col` file | Read bytes directly |
| FAR3 `.dat` archive | Sniff `FAR!byAZ` magic, look up the entry whose filename matches the resolved collection name, decompress, parse |

In `--save-to-repo` mode this flag is rejected — packs ship single-entry
`.col`s and `manage-packs` does the merging at install time using
`content-repo/_vanilla/`.

## Examples

### Save into content-repo (recommended)

```bash
dotnet bin/Debug/netcoreapp3.1/CASOutfitImporter.dll \
  --save-to-repo /srv/dev_projects/personal/FreeSO/content-repo --verify \
  --input /srv/dev_projects/personal/FreeSO/test_skin_head/b076fa_k8groupie \
  --type body --gender f --name k8groupie \
  --mode trunk:wedding
```

Output: `content-repo/trunks/wedding/k8groupie/{pack.json, Content/...}`.

Then install with `./manage-packs install trunks/wedding/k8groupie`.

### Multiple outfits, one invocation

```bash
dotnet …/CASOutfitImporter.dll \
  --save-to-repo /srv/.../content-repo --verify \
  --input ./skin_a --type body --gender f --name skin_a --mode trunk:wedding \
  --input ./skin_b --type body --gender f --name skin_b --mode trunk:wedding \
  --input ./skin_c --type body --gender m --name skin_c --mode trunk:wedding
```

All three packs land in the appropriate subdirectories. Install them
individually or in batch via `manage-packs`.

### One-off zip for direct copy onto an arbitrary install (legacy)

```bash
dotnet …/CASOutfitImporter.dll \
  --staging ./out --zip ./out.zip --verify \
  --input ./skin --type body --gender f --name k8groupie \
  --mode trunk:wedding \
  --source-collection /home/ian/games/edenso/game/TSOClient/avatardata/bodies/collections/collections.dat
```

Produces `out.zip` containing a merged `Content/Avatar/...` tree ready to
extract on top of an installation. Useful when you want a portable zip but
not the content-repo overhead — e.g. testing a single change on a different
machine.

### Verify an existing staged tree

```bash
dotnet …/CASOutfitImporter.dll --verify-only ./out
# or
dotnet …/CASOutfitImporter.dll --verify-only /srv/.../content-repo/trunks/wedding/k8groupie
```

## Verifier output

```
Collection: wedding_female.col
  entries: 1
  [0] po typeId=0x504F5446 fileId=0xCDDB185A  → k8groupie_body_f.<id>.po
        gender=F oft typeId=0x4F465432 fileId=0xC86BFE5E
          region=body hand=1
            light: k8groupie_body_f_lgt.<id>.apr
              bone=PELVIS  mesh: v2 bones=19 faces=572 verts=406+106 (23782/23782 B)
            medium: ...
            dark: ...

OK — 14 cross-links verified, 0 failures
```

| Marker | Meaning |
|---|---|
| `→ <filename>` | Resolved a cross-link to a package-local file |
| `(inherited from source archive)` | (legacy mode) Entry came from `--source-collection`, will resolve from FAR3 at runtime |
| `mesh: v<N> bones=… (X/Y B)` | Mesh parsed; `X` bytes consumed must equal `Y` file size |

In `--save-to-repo` mode, every pack carries exactly one collection entry
that resolves locally to its own `.po` — there are no inherited entries.

## Exit codes

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | Verification failed, or invalid CLI arguments |
| 2 | Uncaught exception (bug or bad input file format) |
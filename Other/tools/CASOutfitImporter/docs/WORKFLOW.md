# Deployment workflow

End-to-end pipeline from "I have a TS1 skin folder" to "all my players see
the new outfit on next connect."

## The full picture

```
   ┌─────────────────────────┐
   │ TS1 skin folder         │
   │  .skn + .bmp(s) + .cmx  │
   └────────────┬────────────┘
                │  CASOutfitImporter --save-to-repo
                ▼
   ┌─────────────────────────┐
   │ content-repo/           │
   │  trunks/wedding/<name>/ │   pack.json + Content/...
   │  cas/<kind>/<g>/<name>/ │
   └────────────┬────────────┘
                │  ./manage-packs install trunks/wedding/<name>
                ▼
   ┌─────────────────────────┐
   │ TSOClient/.../Content/  │   merged .col + per-pack files
   └────────────┬────────────┘
                │  git add + git commit + git push
                ▼
   ┌─────────────────────────┐
   │ GitHub Actions          │
   │  build-client.yml       │   path filter includes
   │  build-server.yml       │   FSO.Content.TSO/**
   └────────────┬────────────┘
                │  PublishBuildArtifacts → GitHub Releases
                ▼
   ┌─────────────────────────┐
   │ latest-client / latest- │
   │   server tags / zips    │
   └────────────┬────────────┘
                │  ssh server, sudo /opt/freeso/update-server.sh
                ▼
   ┌─────────────────────────┐
   │ Running city server     │
   │  (new binaries + content│
   │   on disk)              │
   └────────────┬────────────┘
                │  freeso-portal admin → "Publish Update"
                │  POSTs /admin/updates → GenerateUpdateService
                ▼
   ┌─────────────────────────┐
   │ Update row inserted     │
   │  in fso_updates table   │
   │  + zips uploaded via    │
   │  IUpdateUploader        │
   └────────────┬────────────┘
                │  client connects → game's built-in updater
                ▼
   ┌─────────────────────────┐
   │ Players see new outfit  │
   └─────────────────────────┘
```

Steps the user runs locally are circles 1, 2, 3 (importer + manage-packs +
git). Step 4 (CI) is automatic. Step 5 (`update-server.sh`) is one ssh
command. Step 6 (`Publish Update` in freeso-portal) is one click. Step 7
is automatic.

## Step 0 — prerequisites

| What | Why |
|---|---|
| .NET Core 3.1 SDK | To build the importer |
| Python 3 (stdlib only) | To run `manage-packs` |
| FreeSO source-tree fork | The deployment target |
| Read access to a FreeSO content install | Source for FAR3 archives that hold vanilla collections |
| One TS1 skin folder per outfit | The input — see `INPUT_FORMAT.md` |
| **Source patch applied once** | `TSOAvatarContentProvider.cs:46` provider order flip — see below |

## Step 0.5 — apply the source patch (one-time)

Stock FreeSO loads FAR3 archives **before** loose `.col` files, so the
merged collections this pipeline writes have no effect at runtime without
this patch.

File: `TSOClient/tso.content/Framework/TSOAvatarContentProvider.cs:46`

```diff
        SetProviders(new List<IContentProvider<T>> {
-           FAR,
            Files,
+           FAR,
            Runtime
        });
```

```bash
cd /srv/dev_projects/personal/FreeSO
# edit the file
git commit -am "patch: loose Avatar/Collections/*.col override FAR3 archives"
git push   # triggers a CI build + release; deploy via update-server.sh
```

This change is idempotent — re-running the diff only matters for upstream
merge conflict resolution.

## Step 1 — build the importer (once)

```bash
cd /srv/dev_projects/personal/FreeSO/Other/tools/CASOutfitImporter
dotnet build
```

Output: `bin/Debug/netcoreapp3.1/CASOutfitImporter.dll`. Only re-run when
the importer source changes.

## Step 2 — populate the vanilla snapshot (once after first setup, or after
each game-content patch)

```bash
cd /srv/dev_projects/personal/FreeSO
./manage-packs refresh-vanilla
```

Reads `content-repo/config.yaml`, walks `<game_dir>/avatardata*/...`,
extracts every known `.col` (ea_*, wedding_*, scifi_*, etc.) into
`content-repo/_vanilla/`.

`_vanilla/` is gitignored — local cache, regenerable.

## Step 3 — generate a pack into the content-repo

```bash
dotnet /srv/.../CASOutfitImporter/bin/Debug/netcoreapp3.1/CASOutfitImporter.dll \
  --save-to-repo /srv/dev_projects/personal/FreeSO/content-repo --verify \
  --input /srv/dev_projects/personal/FreeSO/test_skin_head/b076fa_k8groupie \
  --type body --gender f --name k8groupie \
  --mode trunk:wedding
```

Output: `content-repo/trunks/wedding/k8groupie/{pack.json, Content/...}`.
The pack carries its own files + a single-entry `.col` — no merging at
generation time.

## Step 4 — install the pack

```bash
cd /srv/dev_projects/personal/FreeSO
./manage-packs install trunks/wedding/k8groupie
```

Or interactively: `./manage-packs` and walk the menus.

This:
- Copies the pack's mesh/texture/binding/apr/oft/po into
  `TSOClient/FSO.Content.TSO/Content/Avatar/.../User/`
- Rebuilds `TSOClient/FSO.Content.TSO/Content/Avatar/Collections/wedding_female.col`
  from `vanilla + every installed pack's entry + this pack's entry`,
  sorted deterministically

After this, `git status` shows the new files staged.

## Step 5 — commit + push

```bash
cd /srv/dev_projects/personal/FreeSO
git add TSOClient/FSO.Content.TSO/Content/ content-repo/
git commit -m "add wedding-trunk outfit: k8groupie"
git push
```

Two locations get committed:
- `content-repo/trunks/wedding/k8groupie/` — the pack itself (so other
  source trees of yours can install it later)
- `TSOClient/FSO.Content.TSO/Content/Avatar/...` — the materialized state
  baked into the build

Some teams gitignore the materialized state and treat it as a build
artifact (regen via `manage-packs sync` before each push). For a single-dev
single-server setup, committing both is simplest.

## Step 6 — wait for GitHub Actions (automatic)

`.github/workflows/build-client.yml` and `.../build-server.yml` trigger on
push to master/dev when paths under `TSOClient/FSO.Content.TSO/**` change.
They build the client + server, upload zips to GitHub Releases as
`latest-client` / `latest-server`.

Watch in the Actions tab if you want; or just wait ~5 minutes.

## Step 7 — deploy the new server

```bash
ssh user@your-server
sudo /opt/freeso/update-server.sh
```

Your script:
- Downloads the latest server zip from `latest-server` GitHub Release
- Archives it under `/opt/freeso/releases/`
- Stops `freeso-server.service`
- Extracts to `/opt/freeso/server/` (preserving `config.json`)
- Starts the service

The server now has the new content baked into its `Content/Avatar/...`.
For trunk outfits this is mostly cosmetic — the server doesn't validate
trunk costumes — but a coherent build is better.

## Step 8 — publish the client update via freeso-portal

Open the freeso-portal admin panel. Navigate to Updates / Publish Update.
This calls `/admin/updates` on the FSO API, which runs
`GenerateUpdateService.BuildUpdate()`:

- Downloads the latest client zip from the GitHub Release
- Diffs it against the previous published update → produces incremental zip
- Uploads zips via the configured `IUpdateUploader` (S3, GitHub, filesystem)
- Inserts a new row in `fso_updates`
- Marks the update visible (`publish_date = NOW()`,
  `fso_update_branch.current_dist_id = <new-update-id>`)

After this completes, all clients on next connect will see a new version
available.

## Step 9 — clients auto-update (automatic)

Each connecting client queries the FSO API for its current branch, gets the
latest visible update, follows the `UpdatePath` chain (incremental or full),
downloads + applies via `FSOUpdateManifest`, restarts. Players see the new
outfit at the wedding trunk on their next session.

## Removing an outfit

```bash
cd /srv/dev_projects/personal/FreeSO
./manage-packs uninstall trunks/wedding/k8groupie
git add TSOClient/FSO.Content.TSO/Content/ content-repo/
git commit -m "remove k8groupie wedding outfit"
git push
```

Then deploy as steps 7–8. The pack stays in `content-repo/` (so you can
re-install later). To delete the pack itself, also `rm -rf
content-repo/trunks/wedding/k8groupie/` and commit that.

## Resetting a category to vanilla

```bash
./manage-packs reset trunks/wedding
```

Uninstalls every wedding-trunk pack we'd added. The merged `.col` is
removed if it would equal the vanilla snapshot, so the engine falls back
to FAR3 cleanly.

## Rolling back a release

The rollback story is just git:

```bash
cd /srv/dev_projects/personal/FreeSO
git revert <bad-commit>
git push
# wait for CI
ssh user@server; sudo /opt/freeso/update-server.sh
# freeso-portal admin → Publish Update again
```

Each `Publish Update` produces a forward-only delta. Reverting the source
commit + publishing again is the canonical undo.

## Multiple outfits in one push

Either run `manage-packs install` multiple times before committing:

```bash
./manage-packs install trunks/wedding/skin_a
./manage-packs install trunks/wedding/skin_b
./manage-packs install trunks/wedding/skin_c
git add ... && git commit -m "add 3 wedding outfits" && git push
```

Or use the importer in batch with multiple `--input` blocks (creates the
packs in content-repo) and then install each one via manage-packs. Both
ways the merged `wedding_female.col` ends up with vanilla + all three new
entries.

## Drift detection (manual today)

```bash
cd /srv/dev_projects/personal/FreeSO
./manage-packs list
git status TSOClient/FSO.Content.TSO/Content/Avatar/
```

A discrepancy between "manage-packs says installed" and "what git status
shows" means someone hand-edited the source tree. `./manage-packs uninstall
<pack>` followed by `./manage-packs install <pack>` reconciles cleanly.

## Why two repos co-located

`content-repo/` (the pack library) and `TSOClient/FSO.Content.TSO/Content/`
(the deployed state) are both inside the FreeSO source tree but serve
different roles:

- The library is the **source of truth** — packs you've authored.
- The deployed state is the **materialized output** — what actually ships.

You could split them into two git repos for cleaner separation. For a
single-dev single-server setup, co-location is simpler and one push
covers both.
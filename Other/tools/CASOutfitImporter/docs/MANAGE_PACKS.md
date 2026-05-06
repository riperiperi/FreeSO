# manage-packs

Interactive + scriptable tool that installs/uninstalls/resets content packs
against a FreeSO source tree. Pure Python 3, stdlib only.

Located at `/srv/dev_projects/personal/FreeSO/manage-packs` (a thin bash
wrapper around `Other/tools/ManagePacks/manage_packs.py`).

## Subcommands

```
./manage-packs                         # interactive TUI
./manage-packs list
./manage-packs refresh-vanilla
./manage-packs install <category>/<name>
./manage-packs uninstall <category>/<name>
./manage-packs reset <category-prefix>
```

All operations are idempotent. Re-running `install` is a no-op. `uninstall`
on a pack that isn't installed is a no-op. Mid-operation crashes leave the
source tree in a recoverable state (next `install`/`uninstall` will re-do
or undo cleanly).

## Interactive flow

`./manage-packs` with no args drops into a TUI:

```
FreeSO content-pack manager

   1) cas/bodies/f (f)                  vanilla:226  installed:0  available:0
   2) cas/bodies/m (m)                  vanilla:172  installed:0  available:0
   3) cas/heads/f (f)                   vanilla:235  installed:0  available:0
   4) cas/heads/m (m)                   vanilla:189  installed:0  available:0
   5) trunks/wedding (f)                vanilla: 37  installed:0  available:1 *
   6) trunks/wedding (m)                vanilla: 24  installed:0  available:0

  r) refresh-vanilla
  q) quit
> 5
```

Each category screen shows what's vanilla, what's installed (loose, ours),
what's available in the repo, and offers install/uninstall/reset.

## list (per-category state)

`./manage-packs list` prints exactly the same table the TUI shows, plus the
configured paths. Useful for quick scripted snapshots.

```
content-repo: /srv/dev_projects/personal/FreeSO/content-repo
freeso_source: /srv/dev_projects/personal/FreeSO
   1) cas/bodies/f (f)                  vanilla:226  installed:0  available:0
   ...
   5) trunks/wedding (f)                vanilla: 37  installed:1  available:0 *
```

| Column | Meaning |
|---|---|
| vanilla | Number of entries in `_vanilla/<target>.col` (`—` if no snapshot exists) |
| installed | Number of packs from this category currently materialized in the source tree |
| available | Packs sitting in `content-repo/<category>/` but not yet installed |
| `*` | Indicator: this category has activity (≥1 installed or ≥1 available) |

## refresh-vanilla

Rebuilds `content-repo/_vanilla/*.col` from the FAR3 archives under your
configured `game_dir`. Run this once after first setup, and again any time
your FreeSO install gets patched with new content.

```
$ ./manage-packs refresh-vanilla
  ea_female.col: 226 entries  (from collections.dat)
  ea_female_heads.col: 235 entries  (from collections.dat)
  ea_male.col: 172 entries  (from collections.dat)
  ea_male_heads.col: 189 entries  (from collections.dat)
  wedding_female.col: 37 entries  (from collections.dat)
  wedding_male.col: 24 entries  (from collections.dat)
```

If a known collection isn't found, it logs a `WARN` line — usually means the
trunk type isn't shipped in your install (e.g. you have `scifi` listed in
`trunk_types` but the archive doesn't contain `scifi_female.col`).

## install

```
$ ./manage-packs install trunks/wedding/k8groupie
installing trunks/wedding/k8groupie
  → wedding_female.col: 38 entries

review with: cd /srv/dev_projects/personal/FreeSO && git status
```

What it does:
1. Copies every file in the pack's `Content/Avatar/...` (mesh, textures,
   bindings, appearances, outfit, purchasable) into the source tree's
   `TSOClient/FSO.Content.TSO/Content/Avatar/...`.
2. Rebuilds the target `.col` from
   `vanilla + (every currently installed pack's entry) + this pack's entry`,
   sorted by `(typeId, fileId)`.
3. Writes the rebuilt `.col` to `Avatar/Collections/<target>`.

Idempotent: installing a pack that's already installed leaves files intact
and writes the same `.col` bytes (sorted output makes the byte stream
deterministic).

## uninstall

```
$ ./manage-packs uninstall trunks/wedding/k8groupie
uninstalling trunks/wedding/k8groupie
  removed loose wedding_female.col (matches vanilla)
```

What it does:
1. Deletes every file the pack's `pack.json` declared.
2. Cleans up empty `User/` directories where applicable.
3. Rebuilds the target `.col` from `vanilla + (other installed packs' entries)`.
4. **If the rebuilt `.col` would equal the vanilla snapshot, deletes the loose
   file entirely** — the engine then falls back to FAR3 cleanly.

After an uninstall of the last pack in a category, `git status` is clean
(provided you started from a clean tree).

## reset

```
$ ./manage-packs reset trunks/wedding
uninstalling trunks/wedding/k8groupie
  removed loose wedding_female.col (matches vanilla)
```

Equivalent to `uninstall` for every pack matching the given category prefix.
Useful for "throw away every wedding-trunk customization in one go".

## Where the manager looks for things

| Knob | Source |
|---|---|
| Where the source tree is | `freeso_source` in `config.yaml` |
| Where the FreeSO game install is | `game_dir` in `config.yaml` |
| Which categories are tracked | derived from `trunk_types` + every pack's `category` field |
| What "installed" means for pack X | every file in pack X's `pack.json` exists under `<freeso_source>/TSOClient/FSO.Content.TSO/...` |
| What's in vanilla | `_vanilla/<target>.col` (run `refresh-vanilla` to populate) |

## Source patch reminder

Stock FreeSO loads FAR3 archives **before** loose `.col` files. Without a
one-line source patch, every install operation produces files that the
engine will silently ignore.

```diff
// TSOClient/tso.content/Framework/TSOAvatarContentProvider.cs:46
        SetProviders(new List<IContentProvider<T>> {
-           FAR,
            Files,
+           FAR,
            Runtime
        });
```

Apply once, commit, never think about it again. The manager doesn't apply
the patch — that's a one-time decision for the operator. See
`WORKFLOW.md` for full deployment context.

## Exit codes

| Code | Meaning |
|---|---|
| 0 | Operation succeeded (idempotent no-ops included) |
| 1 | Bad CLI args, missing config, missing game_dir, etc. |
| 2 | Uncaught Python exception |
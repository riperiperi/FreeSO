# Limitations and future work

What works, what doesn't, what's planned.

## What works today

- **Two-tool pipeline**: `CASOutfitImporter` produces packs into a content-repo;
  `manage-packs` installs/uninstalls/resets them against a FreeSO source tree.
- **CAS heads + bodies** (`--mode cas`) — server requires a rebuild because
  `RegistrationHandler` whitelists from `ea_*.col`.
- **All 9 trunk types** (`--mode trunk:wedding`, `trunk:scifi`, `trunk:vegas`,
  `trunk:vaudwest`, `trunk:uniforms`, `trunk:costumes`, `trunk:oldworld`,
  `trunk:sports`, `trunk:toga`) — no server validation. Matches the full set of
  `VMEODTrunkPluginCollections` values.
- **TS1 mesh import** via plain-text `.skn` files. Coordinate flip and
  bone-weight reordering match `FSO.Vitaboy.Mesh.Read` exactly.
- **8/24/32-bit BMP** texture import with magenta color-keying for body outfits.
- **Skin-tone synthesis** for inputs that ship only `lgt` (multiplies RGB).
- **Round-trip verification** (`--verify`) for every file the importer writes.
- **FAR3 read + RefPack decompression** — used by the importer's `--source-collection`
  legacy flag and by `manage-packs refresh-vanilla` to extract baselines.
- **Idempotent install / uninstall / reset** in `manage-packs` — operations
  produce deterministic byte-identical output for the same input set.
- **Vanilla restoration** — uninstalling the last pack in a category drops the
  loose `.col` so the engine falls back to FAR3 cleanly.
- **Multi-pack composition** — install two packs targeting `wedding_female.col`
  and they coexist; uninstall one and the other survives.

## What doesn't work yet

### Clothes-rack mode (purchasable)

The store path uses `packingslips/purchasable.xml` (gendered category entries
with prices) plus `ea_*.col`. Adding `--mode purchasable:<rack-type>` would
need:

- New CLI flag for **price** and **rack type** (`Daywear`, `Formalwear`, etc.)
- The importer to merge into a copy of `purchasable.xml`
- `manage-packs` to handle the XML merge alongside `.col` merges

Server-side this is permissive — the server doesn't validate purchase IDs
against a whitelist.

### Custom new trunk objects

A truly separate trunk with its own outfit pool requires:

1. New entry in `VMEODTrunkPluginCollections` enum (1-line C# change in
   `tso.simantics`)
2. New IFF object cloned from an existing trunk (FSO.IDE / Iffinator),
   with sprites + a BHAV that passes the new enum value
3. New `<typeName>_<gender>.col` for its outfit pool

Step 3 is trivial with the existing pipeline. Steps 1 and 2 require IFF
authoring outside this importer's scope.

## Trunk capacity

There's no hardcoded limit. The `.col` file format uses a signed `int32`
count (~2 billion theoretical max). `VMEODTrunkPlugin` and
`UITrunkEOD.CollectionToDataProvider` iterate the list with a `foreach` and
feed a `UICollectionViewer` that paginates. Practical ceilings are real but
loose — memory (~1 MB resident per resolved outfit chain), UI scroll
performance over a few hundred items, player usability at thousands. Hundreds
per trunk are fine; thousands would technically work but degrade UX.

## Known caveats

### Skin-tone synthesis is a stop-gap

When a head ships only a `lgt` BMP, the importer multiplies RGB by 0.78 (med)
and 0.55 (drk). Looks visibly wrong on faces (saturated hair → muddy, eye
whites → beige). Replace `_med.png`/`_drk.png` in
`Avatar/Textures/User/` with hand-painted versions for shippable quality.

### Mesh validation is structural-only

The verifier confirms each `.mesh` parses cleanly under FSO's `Mesh.Read`
semantics. It cannot detect:

- Inverted face winding (mesh appears inside-out)
- UV coordinate mismatches
- Vertex weights pointing at bones the engine's skeleton doesn't have
- Overlapping geometry

In-game testing remains the canonical check.

### Bone bindings rely on the `.skn` filename

The primary bone is parsed from `xskin-<name>-<BONE>-<GROUP>.skn`. If your
input uses a different convention, the mesh binds to the wrong bone or falls
back to `HEAD`/`PELVIS` defaults. Fix is to rename the input file.

### TypeID choices are arbitrary

The importer uses self-consistent but fabricated TypeIDs per content kind.
They don't match canonical Maxis IDs (which would matter only if external
tooling cross-references via TypeID).

### Random FileIDs can collide on re-run

The importer generates random 32-bit FileIDs (high bit set) per pack
generation. Re-generating the same pack produces *different* IDs each time —
which means `manage-packs` will see "new pack, install fresh" rather than
"upgrade existing".

For a v1 fix, regenerating a pack with the same name should produce stable
IDs (deterministic from the name). The current behavior means: regenerate
only when you intend to replace the previous version, and uninstall the
previous version first.

### No animation, skeleton, or hand-group import

Static skin imports only. Out of scope:

- Avatar animations (`.anim`, `.cfp`)
- New skeletons
- Custom hand groups (the body outfits get a default female/male hand-group
  reference; if not resolvable at runtime, hands fall back to the avatar's defaults)

### The native apphost is broken in this dev environment

Snap-isolated dotnet 3.1 produces a published exe that fails with a glibc
symbol mismatch:

```
relocation error: ... symbol _dl_audit_symbind_alt version GLIBC_PRIVATE not defined
```

Workaround: invoke through the dotnet host (`dotnet bin/.../*.dll`).
A normal dotnet install on the deploy server doesn't have this issue.

## Out of scope by design

- **Sound, music, radio packs** — the importer is avatar-content only.
- **Lot/object content** (furniture, structures, gameplay objects) — adjacent
  project; FSO.IDE / `Other/tools/Iffinator/` are more relevant.
- **Server-side game-rule changes** (jobs, debug menus, cheats) — unrelated.
- **Cross-game asset migration** (TS2/TS3/TS4 → TSO) — different mesh and
  animation systems; not portable through this pipeline.

## What might come next

In rough priority order:

1. **Stable pack IDs from name+content hash** — kills the "regenerate produces
   collision-prone IDs" caveat and enables clean upgrade-in-place.
2. **Other trunk types** — wire up `scifi`, `vegas`, etc.
3. **Purchasable mode** — clothes-rack distribution with prices.
4. **`manage-packs status`** — show every commit that touched a pack
   (audit timeline), not just the current state.
5. **`manage-packs check`** — drift detection: warn if files exist in the
   source tree that no installed pack claims, or if a pack's claimed files
   are missing.
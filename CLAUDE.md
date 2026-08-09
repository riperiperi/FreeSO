# FreeSO — standing rules

## Do not build what already exists

Not ours, not upstream's, not the community's. On 2026-08-08 this was violated
four separate times in one day, by different sessions, independently:

1. The client was ported to .NET 8 — upstream had already merged a .NET 9
   migration (PR #283) and ships native macOS builds weekly.
2. `AllowedHeightFlags` was diagnosed from scratch by reading `VMContext.cs`.
   The community decor tutorial documents it as a known pain point.
3. A second branch re-did PackTools onto the archive base while the first branch
   kept going — 60 of its 90 files were byte-identical to the other branch.
4. The `AllowedHeightFlags` fix was nearly implemented a second time, because
   `RENDER-VERIFICATION-STATUS.md` still said "not yet implemented" hours after
   the fix landed.

Every one was individually reasonable. That is the point: this failure does not
feel like a mistake while it is happening.

### Before starting anything non-trivial

- `git log --all --oneline -- <path>` — has any branch already touched this?
- `git branch -a` and `git worktree list` — is another branch or working
  directory already on it?
- Check the actual code, not a status line in a doc. Docs here go stale within
  hours; several already have. **A doc saying "not yet implemented" is a lead to
  verify, never a fact.**
- For engine-level work, check upstream first: `riperiperi/FreeSO`.

### Upstream branches

`master` is stale (last commit Aug 2025) and is the trap — it looks canonical
and is a year behind. **`upstream/archive` is where development actually
happens**, ships weekly beta releases, and has working native macOS CI.
`packtools-on-archive` is our base. `mac-port` forked from `master` and is
superseded — do not build on it.

## Verify by running, not by reasoning

Compiling clean is not rendering. Chunk presence is not pixels. Three bugs
shipped in one day because a test asserted a chunk existed. If a claim is about
behaviour, run it and watch; if it is about bytes, assert the bytes.

Do not send Kat into the game to test something you have not verified yourself
first. It happened four times in one day.

## Committing

Sessions share this working tree, so a plain `git add` can sweep in another
session's staged files. Use a throwaway index:

```sh
TMPIDX=$(mktemp); export GIT_INDEX_FILE="$TMPIDX"
git read-tree HEAD; git add <exact paths>; git commit -F -
unset GIT_INDEX_FILE; rm -f "$TMPIDX"; git reset
```

Commit incrementally. Work has sat staged-but-uncommitted for hours here more
than once.

## Where things live

- Repo: `~/Desktop/Github-Wiki/GitHub/FreeSO` (one folder, one worktree — keep
  it that way)
- Plan/findings/progress: repo root, single copy each, no duplicates elsewhere
- Project docs: `PackTools/` — `START-HERE.md` first

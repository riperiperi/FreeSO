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

Check all four places, in this order. All of them, every time — the four
duplications above each came from checking some but not others.

**1. This working tree — did we already build it?**
```sh
grep -rn "<symbol or concept>" PackTools/ TSOClient/   # does it exist right now?
```
Includes code an earlier session in *this* conversation wrote. Do not trust
memory of what was built; look.

**2. Other branches and worktrees — did another session build it?**
```sh
git log --all --oneline -- <path>     # any branch ever touched this file?
git log --all -S "<symbol>" --oneline # any branch ever contained this code?
git branch -a && git worktree list    # what else is checked out right now?
```
Parallel sessions share this machine. `-S` is the important one: it finds code
by content across all history, including branches you did not know existed.

**3. Upstream — did the maintainer already build it?**
```sh
git log upstream/archive --oneline -S "<symbol>"
gh pr list --repo riperiperi/FreeSO --state all --search "<keyword>"
```
Upstream is active and ships weekly. Check `archive`, not just `master`.

**4. The wider world — did someone else already build it?**
Forks (`gh api repos/riperiperi/FreeSO/forks`), the community
(`forum.freeso.org`, `freesoeod.wordpress.com`, tutorials), and general prior
art. The `AllowedHeightFlags` bug was documented publicly for years before it
was rediscovered here from engine source.

**Then: check the actual code, not a status line in a doc.** Docs here go stale
within hours; several already have. **A doc saying "not yet implemented" is a
lead to verify, never a fact.**

If it already exists: use it, extend it, or fix it. Do not re-create it. If it
exists but is wrong, say so and fix that — a rewrite disguised as a fix is still
duplication.

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

Three files at the repo root, read in this order. They are the only documents
you need to start work:

1. **`CLAUDE.md`** (this file) — the rules
2. **`STATE.md`** — what exists today: what FreeSO gives us, what we added, what's ahead
3. **`task_plan.md`** — what to do next and why it's in that order

Everything else is in **`PackTools/docs/`** — 26 files of specs, design proposals
for unbuilt things, and postmortems. Open one when you need it; do not read them
to orient yourself, and treat any status line in them as a lead to verify rather
than a fact. `docs/SCHEMA.md` and `docs/simantics-vocabulary.md` are the two you
will actually need to author content.

Repo lives at `~/Desktop/Github-Wiki/GitHub/FreeSO` — one folder, one worktree.
Keep it that way.

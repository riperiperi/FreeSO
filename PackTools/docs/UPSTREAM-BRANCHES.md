# Upstream branch landscape

Read this before implementing anything non-trivial against the engine. It exists
because two days of work were spent porting the client to .NET 8 without noticing
upstream had already done it — see "How this was missed" below.

Surveyed 2026-08-08. Re-run the survey rather than trusting these dates if it has
been a while:

```sh
git fetch --all
for b in $(git branch -r --format='%(refname:short)' | grep -v HEAD); do
  printf "%-40s %s %s\n" "$b" \
    "$(git log -1 --format='%ci' "$b" | cut -d' ' -f1)" \
    "$(git show "$b:TSOClient/tso.client/FSO.Client.csproj" 2>/dev/null \
       | grep -oE 'net[0-9]+\.[0-9]|v4\.[0-9]' | head -1)"
done | sort -k2 -r
```

## The branches that matter

| Branch | Last commit | Client target | Notes |
|---|---|---|---|
| `upstream/archive` | 2026-08-07 | net9.0 | **Active development.** The live branch. |
| `upstream/archive-experiment` | 2026-07-27 | net9.0 | |
| `upstream/archive-experiment-temp` | 2026-07-26 | net9.0 | |
| `upstream/dotnet9-opt` | 2025-09-28 | net9.0 | Where the .NET modernization landed. |
| `upstream/master` | 2025-08-22 | v4.5 | **Stale.** Not where development happens. |
| `packtools-on-archive` (**ours**) | — | net9.0 | Based on `upstream/archive`. The branch to work on. |
| `mac-port` (superseded) | — | net8.0 | Forked from `master`, so it predates all of the above. Kept for reference; do not build on it. |

Everything else is a dependabot branch or a feature branch last touched between
2015 and 2019.

**`master` being stale is the trap.** It is the branch a newcomer assumes is
current, it is what `mac-port` forked from, and it is roughly a year behind the
branch that is actually maintained.

## Consequences for this fork

**Resolved 2026-08-08:** we moved onto `upstream/archive` as `packtools-on-archive`, so
this fork no longer predates the upstream .NET port. The lesson below still stands for
anything else, and the two cases it cost us are kept as evidence.

Our old branch predated the upstream .NET port, so anything absent there may exist
upstream rather than not existing at all. Two concrete cases already hit:

- `Other/libs/MSDFData/FieldFontReader.cs` — required to load the game's vector
  font, absent here, present upstream (added in `3494da87a`, Sep 2025). Its
  absence blocked the client from rendering a single frame.
- The whole SDK-style/.NET port of the client and UI projects, reimplemented here
  before anyone noticed upstream had it.

Assume more of both. A missing file on this branch is not evidence the file does
not exist.

## How this was missed

The dependency check was scoped to the local branch — "has *mac-port* already
started this?" — which cannot surface work done anywhere else. Every downstream
symptom then reinforced the wrong conclusion: the client not building read as
"nobody has ported this" rather than "nobody has ported this *here*", and missing
files read as upstream gaps rather than as a stale fork.

**So: before implementing anything substantial, check whether it exists on another
branch first.** `git log --all --oneline -- <path>` and the survey above both
take seconds. Recovering a file from another branch is cheaper than rebuilding it,
and far cheaper than rebuilding it without knowing you did.

## Migration assessment (re-surveyed 2026-08-08, later same day)

Scoping only — no history rewritten, nothing pushed. `mac-port`'s actual fork
point, verified via `git merge-base mac-port upstream/master`, is
`4c6b3e8f5` ("Fix 2d thumbnail lighting at night", 2025-08-22) — matches
`upstream/master`'s tip exactly, confirming `mac-port` forked from `master`,
not from anything more current.

**Corrected commit count**: `upstream/archive` is **371 commits** ahead of that
fork point, not ~156 — re-measured with
`git log --oneline $(git merge-base mac-port upstream/master)..upstream/archive | wc -l`
rather than trusting an earlier estimate, per the standing rule above. `mac-port`
itself has 14 commits since the fork, all from this session.

**`upstream/dotnet9-opt` is not an ancestor of `upstream/archive`** — checked with
`git merge-base --is-ancestor`. They're separate lines; despite the name,
`dotnet9-opt` (2025-09-28) is not what `archive`'s net9 port descends from.
`archive` is the one to target — it's both the most current and the one
actually receiving ongoing development.

### What's genuinely ours vs. duplicate work, checked file-by-file (not assumed)

| Our commit | Verdict | Evidence |
|---|---|---|
| `52c6ee494` Add PackTools | **Keep, applies cleanly** | Archive has no `PackTools/` directory at all — this is virgin territory upstream, zero conflict surface possible. |
| `0a1d64246`, `a677d5d31`, `4eccc9386`, `eef2f7d45`, `e08daa799`, `54f3bdeeb` (PackTools-only commits) | **Keep, applies cleanly** | Same reason — all under `PackTools/`, a directory upstream has never touched. |
| `09f2083b1` `WorldObjectCatalog.AddLive` | **Keep, genuinely ours** | Diffed `WorldObjectCatalog.cs` against `upstream/archive` directly — no `AddLive` method exists there. Small, clean, 11-line diff; easy to reapply by hand or cherry-pick. |
| `4f9902d47` MacOSLocator + EmojiCache fixes | **Drop both — already fixed upstream, independently** | Checked both files in `upstream/archive` directly, not assumed: `MacOSLocator.cs` there uses `Environment.SpecialFolder.LocalApplicationData` instead of our manual `UserProfile`-based path — **empirically verified on this machine that `LocalApplicationData` resolves to `~/Library/Application Support`**, i.e. archive's version already finds the real TSO install correctly, via a cleaner API than ours. `EmojiCache.cs` in archive already has the exact `ServicePointManager.SecurityProtocol`/`Ssl3` lines **commented out** — same bug, independently found and fixed. Carrying our versions forward would silently reintroduce a worse fix over a better one. |
| `4f9902d47` `TSOClient/FSO.Mac/` (the platform head itself) | **Keep, genuinely ours** | Confirmed via `git ls-tree upstream/archive` — no `FSO.Mac`, `FSO.OSX`, or any non-Windows desktop platform head exists upstream. Only `FSO.Windows/`. This is real, unique work with no upstream equivalent. |
| `c746ba4a0` Port engine libraries to .NET 8 | **Discard entirely** | Directly duplicate of upstream's own (more complete, net9) engine port. Don't replay — see rebase-vs-fresh-branch reasoning below for why replaying it at all is the risky move, not just the redundant one. |
| `76e809e5c` Port client and UI to .NET 8 | **Discard entirely** | Same — duplicate of archive's own client/UI port. |
| `48144a5bf` Add in-game Make Something panel (UNVERIFIED) | **Partial — new files clean, two hook points need manual reapplication, one trivially and one not** | Checked the two files it hooks into, not assumed: `UIBuyMode.cs` is **byte-identical** between our fork point and `upstream/archive` (zero-diff) — our added button reapplies cleanly. `CoreGameScreen.cs` diffs at **387 insertions / 109 deletions** between the same two points — substantial upstream rework, so our small `OpenMakeSomething()`/`CloseMakeSomething()`/field addition needs a manual, deliberate re-hook there, not a mechanical reapply. The three new standalone files (`IMakeSomethingAgent.cs`, `StubMakeSomethingAgent.cs`, `UIMakeSomethingDialog.cs`) don't touch anything upstream changed — copy across cleanly regardless. Was already marked UNVERIFIED/uncompiled in its own commit message; still true here, no new information changes that. |
| `08a68d67d` Document the upstream branch landscape | **Keep, this file** | Self-referential — update in place, which is what this section is doing. |

### net8.0 → net9.0 for `PackTools/`

Confirmed `upstream/archive`'s `FSO.Client.csproj` targets `net9.0`. Every
`PackTools/` project currently targets `net8.0`. This is a low-risk bump (.NET
8→9 doesn't remove APIs the way Framework→Core did), and it's the kind of thing
that should be *verified by building*, not asserted from reasoning about
version numbers — bump `TargetFramework` in each `PackTools/*.csproj` after the
migration and run both test suites; if `MonoGame.Framework.DesktopGL 3.8.*`
and the other NuGet packages already in use don't have net9-specific issues,
this should be a one-line-per-project change. Not verified by actually building
against net9 in this pass — flagging as the first thing to confirm once the
branch exists, not asserting it's fine.

### Rebase vs. fresh branch from `archive` — recommendation: **fresh branch, not a rebase**

A literal `git rebase mac-port --onto upstream/archive` would replay all 14
commits in original order, hitting its worst conflicts *inside* the two commits
being discarded anyway (`c746ba4a0`, `76e809e5c` — dozens of files each,
independently rewritten upstream) before ever reaching the commits that matter.
That's conflict-resolution effort spent entirely on work being thrown away, and
it's the kind of git operation where a mid-rebase mistake (`--skip` on the
wrong commit, a bad conflict resolution accepted by habit) risks the exact
"lose work" failure mode this session has already caught twice.

**Recommended instead**: branch fresh from `upstream/archive`, then bring across
only the pieces in the "keep" rows above, deliberately:
1. `git checkout -b <new-branch> upstream/archive`
2. `git checkout mac-port -- PackTools/` — one shot, zero conflict risk, since
   archive never touched that directory.
3. Reapply `WorldObjectCatalog.AddLive` by hand (11 lines, trivial).
4. Copy `TSOClient/FSO.Mac/` across as new files (no upstream equivalent to
   conflict with).
5. Cherry-pick or hand-apply the `UIBuyMode.cs` button addition (clean, file is
   identical to our fork point).
6. Manually re-hook `CoreGameScreen.cs` (the one piece needing real judgment,
   not mechanical reapplication) plus copy the three new panel files across.
7. Do **not** reapply `4f9902d47`'s `MacOSLocator.cs`/`EmojiCache.cs` changes —
   already superseded, confirmed above.
8. Bump `PackTools/` to net9.0 project-by-project, verify both test suites
   green and the client still builds.

Every step above is either a clean copy (zero conflict possible) or a small,
reviewable, deliberate change — no step requires resolving a conflict inside
a commit that's being discarded. This is slower than one rebase command but
each step is independently verifiable, which matters more here than speed:
nothing is pushed yet, so a wrong rebase would be locally recoverable via
reflog, but "recoverable in principle" isn't the same as "worth risking" when
the safe alternative costs maybe 30 extra minutes.

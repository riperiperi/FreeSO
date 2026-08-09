# Findings & Decisions — Upstream Duplication Research

## Requirements
- Find the most up-to-date, feature-complete FreeSO base before resuming any building.
- Do not duplicate work that already exists upstream, in forks, or in the community.
- Research only — no code/branch changes without explicit approval.

## Research Findings

### The wiki's repo pointer was wrong
- `Github-Wiki/GitHub/FreeSO` was an empty placeholder (never cloned) — only contained a stray `.claude/settings.local.json`.
- Real work lives at `/Users/katlaszlo/Desktop/Github-Wiki/GitHub/FreeSO` (751M `.git`, `origin` = `katrinalaszlo/FreeSO` fork, `upstream` = `riperiperi/FreeSO`, branch `mac-port`, **never pushed**).
- A second worktree, `FreeSO-archive-migration`, exists at `.git/worktrees/FreeSO-archive-migration`, checked out to branch `macos-archive-migration` — a separate, unfinished porting lane per `START-HERE.md`.
- Fixed: `Github-Wiki/GitHub/FreeSO` is now a symlink to `/Users/katlaszlo/Desktop/Github-Wiki/GitHub/FreeSO`.

### `master` is stale — `archive` is the real active upstream line
- `upstream/master` last commit: **2025-08-22** (`4c6b3e8f5`, "Fix 2d thumbnail lighting at night").
- `upstream/archive` last commit: **2026-08-09** (today), 373 commits ahead of master, actively pushed to by riperiperi himself.
- `upstream/archive-experiment` (Simitone side of the same effort): last commit 2026-07-27.
- `dotnet9-opt` branch (standalone .NET 9 upgrade) is already folded into master via merged PR #283 — but **`mac-port` does not contain that merge** (`git merge-base --is-ancestor d5485b8ad mac-port` → false). `mac-port` independently ported to **.NET 8** instead, redoing work upstream already solved on .NET 9.
- The "FreeSO Archive Beta" GitHub releases (v0.1.0-beta → v0.5.3-beta, weekly cadence June–Aug 2026) are `archive` branch's own release line.
- `~/FreeSO-runtime/installer-mac-v0.5.3-beta.dmg`, already downloaded on this machine, is that pipeline's actual native macOS output.

### What "Archive Mode" (PR #282, riperiperi) actually is
- "Large scale changes to the client and server to support running in 'archive mode' — a mode that uses a local sqlite database cloned from a live server to provide a simple self-hosted exploration experience... a bit like those videos of abandoned shopping malls."
- Self-hosted / no external server required by design — this likely obsoletes the host-vs-join failure class documented in `RENDER-VERIFICATION-STATUS.md` (Quick Start silently joining instead of hosting cost several hours today).
- Currently "doesn't support running with a regular server — it's just here to keep track of diffs vs master" per the PR description (as of when written; may have changed given 373 commits since).
- `archive` branch has dedicated, working macOS CI: `build: add macos ci`, `build: attempt to fix macos code signing`, `build: macos publish`, `fix: some macos issues`, `Only use the 2d uv offset on macos`. This is a solved, shipping problem — not something `mac-port`'s independent .NET 8 macOS build work needed to re-solve.

### Fork landscape — confirmed no real competing implementations
- All ~40 forks of `riperiperi/FreeSO` checked via GitHub API `compare`: **0 commits ahead of master**, including the two that sounded most threatening on name alone — `groxaxo/LLM-Seems` ("blended with LLM personas") and `simulatedsuburbia/SimulatedSuburbia` ("the persistent online suburb"). Aspirational renames, no code.
- Simitone forks (~30 checked): same pattern, except:
  - **`devloic/Simitone`** — real, published .NET 9 + MonoGame DesktopGL port, Linux/macOS, no Wine. Small clean diff: conditional `TargetFramework`, WinForms/`System.Drawing` stripped from entry project, `SixLabors.ImageSharp` swap, `FSOEnvironment.Linux` real platform detection, `FSO.IDE` gated off non-Windows gracefully. Worth diffing against `mac-port`'s own port work.
  - `BlackRoad-Forge/RoadSimitone` ("agent habitation") — 0 commits ahead, no real code, same as LLM-Seems pattern.
- `riperiperi/Breakin-In` (PS2 Bustin' Out lobby emulator) — unrelated to either product pillar. Most-forked fork (`Zero1UP`, 3★) is a Dockerized variant, only relevant as a packaging reference if ever needed.

### Upstream PRs — merged features relevant to current work
- **#283** "Upgrade FreeSO to .NET 9.0" (SegerEnd, merged) — multi-commit series: `FSO.Patcher`, `FSO.Watchdog`, MP3Player async refactor, Volcanic/FSO.IDE, VoronoiLib, MSDFExtension. `mac-port` does not have this.
- **#286** "Adding in support for non-windows OSes for Simitone" (alexjyong, merged) — tested Ubuntu/Linux Mint via WSL/QEMU, explicitly not macOS.
- **#292** "Add server connection, Archive maker and Keyboard navigation" (SegerEnd) — on the `archive` branch.
- Nothing in the open or merged PR list touches AI-authored content, MCP, or scripted object generation — that space is confirmed clear.

### No official or community precedent for AI-assisted object authoring
- FreeSO community forum (`forum.freeso.org`): zero threads on AI-assisted content, procedural terrain, or real-world map conversion.
- `freesoeod.wordpress.com` (EOD/custom-object dev guide): fully manual workflow — C# server handler + client UI + hand-authored `.iff`, wired by shared GUID. No AI-assisted or automated authoring mentioned anywhere.
- `freeso.woobsha.com/MyFirstDecor` (community decor tutorial): confirms the exact bug class the team found today (`AllowedHeightFlags`) is a **known, long-standing community pain point** — *"Height flag values [are] manual — must copy from existing objects... no documentation provided."* This was discoverable by reading the tutorial, not just by reading `VMContext.cs`/`UIObjectHolder.cs` from scratch. Direct evidence for the "research before building" post-mortem finding.
- Original EA-era TSO *did* have an official "Custom Content Creator" program (via EA-Land): in-game upload tool, brand/collection/artist metadata, age rating, Maxis moderation for duplicates, **no direct creator payment** — monetization ran through club-gated fan-site access instead. Real historical precedent for `vibecode-sims.md`'s deferred sharing/moderation lane, if/when that reopens.
- `Documentation/Initial Setup.md` (upstream, current on master): confirms `config.json`/`gameLocation`/`simNFS` setup, F1-to-repoint-server client flow. Does **not** mention `lastJoinedHost` or Quick-Start-vs-Host-Server at all — the host/join bug found and fixed today is genuinely undocumented upstream, not a miss.

### City-from-geography precedent (outside FreeSO)
- FreeSO's own `Crafting a City.md` (on `archive`'s Documentation, read earlier) fully specifies the mechanism: stacked image layers — elevation, terrain type (grass/water/rock/snow/sand), roads on tile edges, forest type/density, vertex-color.
- General OSM-to-terrain pattern is well-trodden elsewhere (not FreeSO-specific): Osmundi, CityGen3D, Cities:Skylines' OSM import mod, `osm2terrn2` (Rigs of Rods) all do real-elevation/OSM-road → in-game terrain today. Worth referencing the OSM-import mod's road-snapping logic instead of writing that transform from zero.

### Phase 2 — API compatibility check: `mac-port` vs `upstream/archive`
Diffed the exact files PackTools depends on, not just file lists — checked whether signatures/behavior PackCompiler, ModServer, VMHarness, and LiveInject actually call have changed.

**Safe / unaffected:**
- `TSOClient/tso.content/ChangeManager.cs` (`RegisterObjects`, what `FSO.LiveInject` calls) — **0-line diff, byte-identical** between `mac-port` and `archive`.
- `AbstractObjectProvider.AddObject` (both overloads `LiveInject` and the content pipeline use) — signatures unchanged. Archive only *adds* a new `IconCache` dict + `GetOrAddGeneratedIcon(guid, generator)` helper.
- `VMStackObjectVariable.AllowedHeightFlags = 4` — same scope index on both branches. The open placement-height bug fix (`PackBuilder.cs` needs to set `my_object[4] = 1` in generated `init` trees) applies identically regardless of base; not a reason to delay the fix.
- `VM.UseWorld` — public getter/setter signature unchanged (`VMHarness/Program.cs`'s `VM.UseWorld = false` still works); internally became `[ThreadStatic]` and no longer pushes into `VMContext.UseWorld`/`VMEntity.UseWorld` directly (those now read through `VM.UseWorld` themselves) — an internal refactor, not a breaking one for this usage.
- `OBJD.cs` (object definition chunk — GUID, catalog fields, etc.) — diff is confined entirely to the **Read** path (more defensive `numFields` bounds-checking against malformed/older files). **No changes to the Write/serialization path**, which is what `PackCompiler` actually calls to emit `.iff`. Safe.
- `STR.cs` (dialog/catalog strings, what `set_dialog_string` writes to STR# 301) — purely defensive parsing changes (skip zero-length language sets, avoid reallocating on same-size read). No format or writer change. Safe.

**Possible risk, not yet confirmed either way — needs a real build+test run, not just diffing:**
- `SPR2.cs` (sprite chunk format, used by every generated/cloned appearance) has a real behavioral change on `archive`: sprite header reading (`Width`/`Height`/`Flags`/`PaletteID`) was split out into a new `ReadHead()` called eagerly (for async loading) with an accompanying byte-offset change (`ToDecode = io.ReadBytes(spriteSize - 10)` vs. the old full-size read), plus palette resolution moved to an eager `Parent.ChunkParent.Get<PALT>()` lookup with a null-safe fallback, and `ContainsNoZ` changed from a stored field to a computed property off `Flags`. This is exactly the chunk type the art pipeline and PackCompiler's appearance-writing path touch most. **Don't assume compatible — this is the one thing to actually build and round-trip-test against `archive` before trusting it**, not reason further from diffs alone.
- `tso.simantics/VM.cs` has substantial internal refactors beyond `UseWorld` (Entities changed from `List<VMEntity>` to a new `VMObjectList<VMEntity>` type, `HollowAdj` changed from `byte[][]` to `VMHollowAdjEntry[]`, `VMLotSwitchHandler` delegate gained a `LotTransitionInfo` parameter, ambience/sound handling reworked). Grepped all of PackTools for direct usage of `.Entities`, `AddToObjList`, `HollowAdj`, `VMLotSwitchHandler` — **none found**, so none of this should affect PackTools directly, but it's a large enough refactor (124 changed lines) that a full build is the only real confirmation.

**Overall read:** the surface PackTools actually touches (object/string/sprite chunk I/O, object registration, VM harness) is mostly either identical or additively/defensively changed, not restructured. Nothing found that looks like a hard blocker to moving onto `archive`. The one item worth hands-on verification before committing to the move is `SPR2.cs` — build PackCompiler against `archive`'s `tso.files` and run the existing round-trip test suite; that will settle it faster than more diff-reading.

### A parallel session already did this reconciliation — found at `Github-Wiki/GitHub/FreeSO-archive-migration`
This is a **git worktree of the same repo** (`/Users/katlaszlo/Desktop/Github-Wiki/GitHub/FreeSO`), checked out to branch `macos-archive-migration`. It is NOT a separate clone — same `.git`, same object store, just a different working directory/branch pair.

**It is correctly based on the current line**: `git status` shows it is 2 commits behind `upstream/archive`, fast-forwardable, no divergence. Unlike `mac-port` (forked from year-stale `master`), this branch's foundation is right.

**CORRECTION (2026-08-08, later same session): two claims in this section were wrong, and they mattered.** A file-by-file hash comparison of all 90 staged files against `mac-port` found:
- **60 of 90 were byte-identical to `mac-port`.** The remaining 30 were an *earlier* version of the same files, not different work.
- `UPSTREAM-BRANCHES.md` and `CLIENT-PORT-SCOPE.md` **do exist on `mac-port`** — the claim below that they "don't exist on `mac-port`" was false.
- `WorldObjectCatalog.AddLive()` **also already exists on `mac-port`** (line 92) — the claim further down that it was "not present anywhere on `mac-port`" was false.
- The only genuinely distinct content was the `net9.0` csproj variants (vs `mac-port`'s `net8.0`), i.e. mechanical re-targeting for the archive base.

**Revised picture:** `mac-port` is effectively a superset — same work, plus the later fixes. The worktree was a stale snapshot of it re-targeted onto `archive`. The earlier "neither branch is strictly ahead, reconcile both" conclusion was built on those two false claims and is **superseded**.

**Root cause of the error:** existence was inferred from the *narrative* in the other session's docs rather than checked against the actual tree. Exactly the same failure mode this whole investigation is about — asserting from a plausible story instead of verifying. Verified afterward with `git rev-parse`/`git cat-file` per file; that's what should have happened first.

**Resolution:** worktree state committed (`41d955187`) so nothing is lost, then the second working directory was removed via `git worktree remove`. Branch `macos-archive-migration` still exists and holds the full archive-based port. One FreeSO folder now, not two.

---

*Original (partly incorrect) text follows, kept for the record:*

**It already contains its own version of exactly the reconciliation this plan was built to figure out**, written up in two docs that don't exist on `mac-port`:
- `PackTools/UPSTREAM-BRANCHES.md` — a branch-freshness survey, dated 2026-08-08, that reaches the *same conclusion* this plan reached (archive is current, master is a stale trap) but independently and earlier, and states the lesson explicitly: *"two days of work were spent porting the client to .NET 8 without noticing upstream had already done it."*
- `PackTools/CLIENT-PORT-SCOPE.md` — a full scoping + execution log of porting `FSO.Client` and ~10 dependent Framework-era projects to SDK-style/net8.0, done *on top of* `upstream/archive`, including a clean, minimal `TSOClient/FSO.Mac` platform head (distinct from `FSO.Windows`, matching the `FSO.iOS`/`FSODroid` pattern) — a narrower, better-scoped macOS port than `mac-port`'s broader in-place client changes. Confirms `FSO.PackCompiler.Tests`/`FSO.ModServer.Tests` still green (31/31, 32/32) after the port.
- **Open inconsistency, not yet resolved**: `UPSTREAM-BRANCHES.md`'s own survey table says `upstream/archive`'s `tso.client` already targets `net9.0` — but `CLIENT-PORT-SCOPE.md`'s "Outcome" section describes this same session porting `FSO.Client.csproj` *to* `net8.0` as new work. Those two claims don't reconcile at face value (why port to net8.0 something the survey says is already net9.0?). Didn't chase this further — flagging rather than guessing; worth a direct `git show upstream/archive:TSOClient/tso.client/FSO.Client.csproj | grep TargetFramework` before trusting either claim.

**Uncommitted work sitting here right now, at risk:** the entire `PackTools/` directory (compiler, ModServer, ModServer.Tests, PackCompiler.Tests, AgentBridge, ArtCalibration, LiveInject, LiveInject.Proof, VMHarness, all example packs, all design docs) plus `TSOClient/FSO.Mac` are **`git add`-staged but not committed**. `CLIENT-PORT-SCOPE.md`'s own Risk #1 already flagged this exact danger for the engine-port portion and says it "Now committed" — but that note is stale relative to the current `git status`: PackTools itself is still sitting staged, uncommitted, exposed to the same `git reset --hard`/bad-stash risk the doc warned about. This machine runs many parallel Claude Code sessions sharing working trees (per `START-HERE.md`'s own commit-with-throwaway-index warning) — this is real, present risk, not hypothetical.

**Also found: an unstaged fix not present anywhere on `mac-port`.** `TSOClient/tso.content/WorldObjectCatalog.cs` has an uncommitted `AddLive(ObjectCatalogItem item)` method — registers a catalog item into the *already-initialized* live catalog (`ItemsByCategory`/`ItemsByGUID`, otherwise only populated once in `Init()`), explicitly for "objects compiled and injected after startup (see FSO.LiveInject)." This may be directly relevant to the open catalog-thumbnail bug from `mac-port`'s `START-HERE.md` — worth checking whether it's already the fix, a partial fix, or unrelated, before writing new code for that bug.

**Neither branch is strictly ahead of the other — they diverged from a shared point and each accumulated different fixes after.** Diffed `PackBuilder.cs` between the two: this worktree's copy (file timestamps ~15:32 today) is *missing* fixes `mac-port` has from later in the day — the GraphicsMissing-hard-fail-on-`Install()` check and the GameDir(sprite-source)-vs-gameDir(install-target) disambiguation comments/docs that came out of the render-verification work. So: `mac-port` is ahead on PackCompiler correctness fixes; this worktree is ahead on being based on the right upstream commit and on the platform-head/live-catalog work. A migration plan now needs to merge the good parts of both, not just adopt one wholesale.

## Technical Decisions
| Decision | Rationale |
|----------|-----------|
| Treat `upstream/archive`, not `master`, as the real upstream base going forward | `master` is a year stale; `archive` is where riperiperi is actually committing, has native macOS CI already, and the self-hosted mode may obsolete the host/join bug class |
| Hold all further building until Phase 3 (migration plan) is approved | User explicitly said research-only; a branch-base change is hard to reverse if done on the wrong assumption |
| Planning files live at `PackTools/` (real files), not duplicated at the wiki root | One source of truth; this is where the rest of today's docs already are |

## Issues Encountered
| Issue | Resolution |
|-------|------------|
| `WebFetch` returns 403 on `freeso.org` (both `/` and `/news/`) | Used `WebSearch` for site content instead; likely blocks non-browser user agents |
| `git compare` API showed some forks (e.g. `alexjyong/Simitone`, 15★) as 0/0 ahead-behind despite being an active community fork | Likely a default-branch mismatch in the compare call, not proof of zero divergence for that specific fork — flagged as unconfirmed, not asserted as fact |
| Planning files briefly duplicated at both `PackTools/` and `Github-Wiki/` root | Consolidated to one real copy at `PackTools/`, reachable via the `Github-Wiki/GitHub/FreeSO/` symlink alias — no second copy anywhere |

## Resources
- Upstream repo: `riperiperi/FreeSO` (branches: `master` stale, `archive` active, `archive-experiment`, `dotnet9-opt` merged into master)
- Simitone: `riperiperi/Simitone`; relevant fork `devloic/Simitone` (native Linux/macOS .NET 9 port)
- `riperiperi/Breakin-In` — unrelated (PS2 lobby emulator)
- Local repo: `/Users/katlaszlo/Desktop/Github-Wiki/GitHub/FreeSO` (branch `mac-port`, unpushed) + worktree `FreeSO-archive-migration` (branch `macos-archive-migration`)
- `~/FreeSO-runtime/installer-mac-v0.5.3-beta.dmg` — upstream's own native macOS build output, already on this machine
- Key docs already read: `PackTools/START-HERE.md`, `PRODUCT-DIRECTION.md`, `RENDER-VERIFICATION-STATUS.md`, `DEV-ENVIRONMENT-NOTES.md`, `MODEL-EVALUATION.md`, upstream `Documentation/Crafting a City.md` and `Initial Setup.md`

## Visual/Browser Findings
- None requiring separate capture — all fetched content was text/markdown, logged inline above.

---
*Last updated: 2026-08-08, end of upstream-duplication research pass.*

---

## Closed 2026-08-08 (evening)

The migration question this file existed to answer is resolved: PackTools now builds on
`upstream/archive` as `packtools-on-archive` (56/56, 48/48 on net9.0), and the Make
Something panel was ported after being found missing from the first pass.

The product direction then changed underneath it — see `task_plan.md` and `STATE.md`,
which supersede the roadmap thinking here. This file is kept for the *evidence*: the
branch survey, the fork audit, the API compatibility checks, and the record of two claims
I asserted without verifying and had to retract.

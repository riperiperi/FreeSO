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
| `mac-port` (ours) | — | net8.0 | Forked from `master`, so it predates all of the above. |

Everything else is a dependabot branch or a feature branch last touched between
2015 and 2019.

**`master` being stale is the trap.** It is the branch a newcomer assumes is
current, it is what `mac-port` forked from, and it is roughly a year behind the
branch that is actually maintained.

## Consequences for this fork

Our branch predates the upstream .NET port, so anything absent here may exist
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

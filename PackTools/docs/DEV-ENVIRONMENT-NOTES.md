# Dev environment notes

Short-lived gotchas about the local toolchain that aren't about the FreeSO
codebase itself — kept separate from the port/migration docs so they don't get
lost when those get superseded.

## Freshly-installed .NET SDK gets SIGKILL'd on this macOS build

**Symptom**: `dotnet --version` (or any `dotnet` invocation) dies instantly
with exit code 137, no error output. This isn't specific to one SDK version —
it broke the *previously-working* `dotnet` muxer too, since a new SDK install
overwrites the shared `~/.dotnet/dotnet` binary.

**Confirmed cause, not a corrupted download**: checked
`~/Library/Logs/DiagnosticReports/dotnet-*.ips` — `"signal":"SIGKILL (Code
Signature Invalid)"`, `"termination":{"namespace":"CODESIGNING"}`. The
`dotnet-install.sh` script's own download-size check passed (remote and local
file sizes matched), so this isn't a truncated/corrupted transfer — it's macOS
(this machine is on 26.3, a very new build) rejecting the binary's original
Apple-notarized signature for some environment-specific reason, not a broken
binary.

**Fix**:
```sh
codesign --force --sign - ~/.dotnet/dotnet
```
Ad-hoc re-signs the muxer locally, which macOS accepts. Confirmed both the
pre-existing SDK and the newly-installed one work immediately after (verify
with `dotnet --list-sdks`). Standard, low-risk fix for a "signature became
invalid after extraction/on this OS build" situation — not a security
workaround for anything actually wrong with the binary's contents.

**If this happens again**: check the crash log first
(`~/Library/Logs/DiagnosticReports/dotnet-*.ips`, most recent one) to confirm
it's the same `CODESIGNING`/`Invalid Page` signature rather than assuming and
re-signing blindly — worth ruling out an actually-corrupted download before
treating it as this same known issue.

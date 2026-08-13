#!/usr/bin/env bash
# Mac/Linux entry point — documents the blocker. Real build is build.ps1 / CI on Windows.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
DOTNET="${DOTNET:-$HOME/.dotnet/dotnet}"
if [[ ! -x "$DOTNET" ]]; then DOTNET="$(command -v dotnet)"; fi
WRAPPER="$ROOT/mgcb-dotnet.sh"
chmod +x "$WRAPPER" "$ROOT/build.sh" 2>/dev/null || true

echo "FSO.BrowserEffects: attempting KNI MGCB on $(uname -s)/$(uname -m)…"
echo "Using managed MGCB via mgcb-dotnet.sh (MGCB.exe is Windows PE)."
echo "Expected: EffectProcessor needs Windows d3dcompiler_47.dll (see README.md)."
echo

# Restore so mgcb-dotnet.sh can find MGCB.dll in the NuGet cache.
"$DOTNET" restore "$ROOT/FSO.BrowserEffects.csproj" --nologo -v q

set +e
"$DOTNET" build "$ROOT/FSO.BrowserEffects.csproj" -c Release --nologo \
  -p:KniContentBuilderExe="$WRAPPER"
code=$?
set -e

if [[ $code -ne 0 ]]; then
  cat <<'EOF'

--- Mac/Linux blocker (verified 2026-08-12) ---
1) MSBuild default invokes Tools/MGCB.exe → "cannot execute binary file" (PE32+).
2) With mgcb-dotnet.sh → `dotnet MGCB.dll` runs, but EffectProcessor fails:
   Unable to load shared library 'd3dcompiler_47.dll'
   (SharpDX.D3DCompiler; also ships libmojoshader_64.dll as Windows PE).
nkast: content builder does not support macOS/Linux (kni#2012).

Build on Windows instead:
  PackTools/FSO.BrowserEffects/build.ps1
Or GitHub Actions:
  .github/workflows/kni-effects-blazor.yml
Then ensure XNBs are at:
  PackTools/FSO.BrowserClient/wwwroot/Content/Effects/

EOF
  exit "$code"
fi

BUILT="$ROOT/wwwroot/Content/Effects"
DEST="$(cd "$ROOT/../FSO.BrowserClient/wwwroot/Content/Effects" && pwd)"
mkdir -p "$DEST"
cp -f "$BUILT"/*.xnb "$DEST/"
echo "Copied XNBs → $DEST"
ls -la "$DEST"/*.xnb

#!/usr/bin/env bash
# Wrapper so MSBuild KniContentReference can invoke managed MGCB on non-Windows.
# Usage: -p:KniContentBuilderExe=/abs/path/mgcb-dotnet.sh
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DOTNET="${DOTNET:-$HOME/.dotnet/dotnet}"
if [[ ! -x "$DOTNET" ]]; then DOTNET="$(command -v dotnet)"; fi

find_mgcb() {
  if [[ -n "${KNIMGCB_DLL:-}" && -f "$KNIMGCB_DLL" ]]; then
    echo "$KNIMGCB_DLL"
    return 0
  fi
  local assets="$SCRIPT_DIR/obj/project.assets.json"
  if [[ -f "$assets" ]]; then
    python3 - "$assets" <<'PY'
import json, os, sys
a = json.load(open(sys.argv[1]))
rel = "nkast.xna.framework.content.pipeline.builder/4.2.9001/tools/MGCB.dll"
for folder in a.get("packageFolders", {}):
    cand = os.path.join(folder, rel)
    if os.path.isfile(cand):
        print(cand)
        sys.exit(0)
print("", end="")
sys.exit(1)
PY
    return $?
  fi
  return 1
}

MGCB="$(find_mgcb || true)"
if [[ -z "${MGCB:-}" || ! -f "$MGCB" ]]; then
  echo "mgcb-dotnet.sh: could not find MGCB.dll (restore FSO.BrowserEffects first; or set KNIMGCB_DLL)" >&2
  exit 1
fi
exec "$DOTNET" "$MGCB" "$@"

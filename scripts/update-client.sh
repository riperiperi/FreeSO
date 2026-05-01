#!/usr/bin/env bash
# Update a FreeSO client install from the latest-client GitHub Release.
# Preserves the TSO game data folder and local cache/config.
set -euo pipefail

REPO="TheGreatCodeholio/FreeSO"
RELEASE_TAG="latest-client"
ARTIFACT_ZIP="freeso-client-windows-ogl.zip"
CLIENT_DIR="${CLIENT_DIR:-$HOME/games/edenso}"
GH_USER="${GH_USER:-$(whoami)}"

# Folders/files inside CLIENT_DIR that are never overwritten
PRESERVE=( game fso_cache PatchFiles FreeSO.exe.config )

STAGING="$(mktemp -d /tmp/freeso-client-update.XXXXXX)"
trap 'rm -rf "$STAGING"' EXIT

echo "==> Downloading $RELEASE_TAG release"
mkdir -p "$STAGING/dl"
gh release download "$RELEASE_TAG" \
    -R "$REPO" \
    -p "$ARTIFACT_ZIP" \
    -D "$STAGING/dl" \
    --clobber

echo "==> Extracting"
mkdir -p "$STAGING/build"
unzip -q "$STAGING/dl/$ARTIFACT_ZIP" -d "$STAGING/build"

[[ -f "$STAGING/build/FreeSO.exe" ]] \
    || { echo "artifact looks wrong (no FreeSO.exe)" >&2; exit 1; }

echo "==> Deploying to $CLIENT_DIR"
EXCLUDES=()
for f in "${PRESERVE[@]}"; do EXCLUDES+=( --exclude="/$f" --exclude="/$f/" ); done
rsync -a --delete "${EXCLUDES[@]}" "$STAGING/build/" "$CLIENT_DIR/"

echo "==> Done. Client updated to $(cat "$CLIENT_DIR/version.txt" 2>/dev/null || echo unknown)"

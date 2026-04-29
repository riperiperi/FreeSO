#!/usr/bin/env bash
# Update FreeSO server from the latest-server GitHub Release (master only).
set -euo pipefail

REPO="TheGreatCodeholio/FreeSO"
RELEASE_TAG="latest-server"
ARTIFACT_ZIP="freeso-server-linux-x64.zip"
SERVER_DIR="/opt/freeso/server"
BACKUP_ROOT="/opt/freeso/backups"
GH_USER="adminlocal"

PRESERVE_TOP=( config.json appsettings.json appsettings.Development.json updateUrl.txt )

[[ $EUID -eq 0 ]] || { echo "must run as root" >&2; exit 1; }

STAGING="$(mktemp -d /tmp/freeso-update.XXXXXX)"
chmod 755 "$STAGING"
trap 'rm -rf "$STAGING"' EXIT

echo "==> Downloading $RELEASE_TAG release"
mkdir -p "$STAGING/dl"
chown "$GH_USER" "$STAGING/dl"
sudo -u "$GH_USER" gh release download "$RELEASE_TAG" \
        -R "$REPO" \
        -p "$ARTIFACT_ZIP" \
        -D "$STAGING/dl" \
        --clobber

echo "==> Extracting"
mkdir -p "$STAGING/build"
unzip -q "$STAGING/dl/$ARTIFACT_ZIP" -d "$STAGING/build"

[[ -f "$STAGING/build/FSO.Server.Core.dll" ]] \
    || { echo "artifact looks wrong (no FSO.Server.Core.dll)" >&2; exit 1; }

echo "==> Stopping freeso.service"
systemctl stop freeso.service

TS=$(date -u +%Y%m%d-%H%M%S)
BACKUP="$BACKUP_ROOT/server-$TS"
echo "==> Backing up to $BACKUP"
mkdir -p "$BACKUP_ROOT"
cp -a "$SERVER_DIR" "$BACKUP"

echo "==> Deploying"
EXCLUDES=()
for f in "${PRESERVE_TOP[@]}"; do EXCLUDES+=( --exclude="/$f" ); done
rsync -a --delete "${EXCLUDES[@]}" "$STAGING/build/" "$SERVER_DIR/"

echo "==> Restoring exec bits"
for bin in FSO.Server.Core FSO.Server.Api.Core createdump; do
    [[ -f "$SERVER_DIR/$bin" ]] && chmod +x "$SERVER_DIR/$bin"
done

echo "==> Fixing ownership"
chown -R freeso:freeso "$SERVER_DIR"

echo "==> Starting freeso.service"
systemctl start freeso.service
sleep 5
if systemctl is-active --quiet freeso.service; then
    echo "==> Done. Backup at: $BACKUP"
else
    echo "FAILED to start; see: journalctl -u freeso -n 100" >&2
    exit 1
fi
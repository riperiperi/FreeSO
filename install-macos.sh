#!/usr/bin/env bash
#
# EdenSO client installer for macOS. Downloads the FreeSO client + TSO
# game files + remesh package + macextras (.dylib bundle), lays them out
# under ~/Library/Application Support, writes config.ini pointed at
# api.edenso.net, and creates a launcher.
#
# Idempotent — re-running offers update-keeping-config or wipe-and-reinstall.
#
# Override URLs / install dir via env vars:
#   FREESO_SERVER_URL=https://my.shard ./install-macos.sh
#   GAME_NAME=MyShard ./install-macos.sh

set -euo pipefail

if [[ "$EUID" -eq 0 ]]; then
    printf "Please run as your normal user, not via sudo.\n"
    exit 1
fi

if [[ "$(uname)" != "Darwin" ]]; then
    printf "This installer is for macOS. Use install-linux.sh on Linux.\n"
    exit 1
fi

# ── Configuration ────────────────────────────────────────────────────────────
GAME_NAME="${GAME_NAME:-EdenSO}"
SERVER_URL="${FREESO_SERVER_URL:-https://api.edenso.net}"
CLIENT_URL="${FREESO_CLIENT_URL:-https://github.com/TheGreatCodeholio/FreeSO/releases/download/latest-client/freeso-client-windows-ogl.zip}"
TSO_URL="${FREESO_TSO_URL:-https://archive.org/download/TheSimsOnline_201802/TSO.zip}"
REMESH_URL="${FREESO_REMESH_URL:-https://github.com/ItsSim/fsolauncher/releases/download/1.12.1-prod.24/remeshes-1.0.0-1726774408.zip}"
MACEXTRAS_URL="${FREESO_MACEXTRAS_URL:-https://github.com/TheGreatCodeholio/FreeSO/releases/download/latest-client/macextras.zip}"

# Apple-specified location for app data — same place Steam, Discord,
# and most native apps put their large support files.
DEFAULT_DIR="$HOME/Library/Application Support/$GAME_NAME"
TEMPDIR="$HOME/.${GAME_NAME,,}_temp"

# ── Helpers ──────────────────────────────────────────────────────────────────
log()  { printf '\n[%s] %s\n' "$(date +%H:%M:%S)" "$*"; }
fail() { printf '\nERROR: %s\n' "$*" >&2; exit 1; }

# ── Banner ───────────────────────────────────────────────────────────────────
printf "\n  %s Installer for macOS\n" "$GAME_NAME"
printf "  ====================================\n"

# ── Install directory prompt ─────────────────────────────────────────────────
read -r -p "Install location [$DEFAULT_DIR]: " GAMEDIR
GAMEDIR="${GAMEDIR:-$DEFAULT_DIR}"
[[ "$GAMEDIR" = /* ]] || fail "$GAMEDIR is not an absolute path."
mkdir -p "$GAMEDIR" || fail "Could not create $GAMEDIR"

# ── Detect existing install + offer update or fresh ──────────────────────────
PRESERVE_CONFIG=0
if [[ -f "$GAMEDIR/FreeSO.exe" ]]; then
    log "Existing install detected at $GAMEDIR"
    read -r -p "Update keeping config (u), Reinstall fresh (r), or Cancel (c)? [u]: " choice
    choice="${choice:-u}"
    case "$choice" in
        c|C) printf "Cancelled.\n"; exit 0 ;;
        r|R) log "Wiping $GAMEDIR/*"; rm -rf "${GAMEDIR:?}"/* ;;
        *)
            mkdir -p "$TEMPDIR"
            if [[ -f "$GAMEDIR/Content/config.ini" ]]; then
                cp "$GAMEDIR/Content/config.ini" "$TEMPDIR/config.ini.backup"
                PRESERVE_CONFIG=1
                log "Backed up your existing Content/config.ini"
            fi
            ;;
    esac
fi

mkdir -p "$TEMPDIR"
cd "$TEMPDIR" || fail "Could not enter $TEMPDIR"

# ── Dependency installation via Homebrew ────────────────────────────────────
log "[1/5] Installing dependencies via Homebrew"
if ! command -v brew >/dev/null 2>&1; then
    fail "Homebrew is required but not installed.
Install it from https://brew.sh, then re-run this script."
fi

brew update
brew install mono cabextract curl unzip || fail "Homebrew install failed."

command -v unzip      >/dev/null || fail "unzip not found after brew install"
command -v cabextract >/dev/null || fail "cabextract not found after brew install"
command -v curl       >/dev/null || fail "curl not found after brew install"
command -v mono       >/dev/null || fail "mono not found after brew install"

# ── Download client + TSO + remesh + macextras ──────────────────────────────
log "[2/5] Downloading $GAME_NAME client"
curl -fL --retry 3 --retry-delay 5 -o "$TEMPDIR/client.zip" "$CLIENT_URL"

log "[3/5] Downloading TSO game files (large — ~350 MB)"
curl -fL --retry 3 --retry-delay 5 -o "$TEMPDIR/TSO.zip" "$TSO_URL"

log "[3/5] Downloading 3D remesh package"
curl -fL --retry 3 --retry-delay 5 -o "$TEMPDIR/RemeshPackage.zip" "$REMESH_URL"

log "[3/5] Downloading macOS native libraries (macextras)"
curl -fL --retry 3 --retry-delay 5 -o "$TEMPDIR/macextras.zip" "$MACEXTRAS_URL"

# ── Extract everything ──────────────────────────────────────────────────────
log "[4/5] Extracting client → $GAMEDIR"

# Apply CLEANUP.txt manifest from the new client zip BEFORE extracting.
# Same logic as install-linux.sh — see comment there for the rationale.
CLEANUP_TMP="$TEMPDIR/CLEANUP.txt"
if unzip -p "$TEMPDIR/client.zip" CLEANUP.txt > "$CLEANUP_TMP" 2>/dev/null && [[ -s "$CLEANUP_TMP" ]]; then
    log "  Applying CLEANUP.txt manifest"
    removed=0
    while IFS= read -r path; do
        path="${path%%$'\r'}"
        [[ -z "$path" || "$path" =~ ^[[:space:]]*# || "$path" == /* || "$path" == *..* ]] && continue
        if [[ -e "$GAMEDIR/$path" ]]; then
            rm -rf -- "$GAMEDIR/$path"
            removed=$((removed + 1))
        fi
    done < "$CLEANUP_TMP"
    log "  Removed $removed stale path(s)"
fi

unzip -q -o "$TEMPDIR/client.zip" -d "$GAMEDIR"

log "[4/5] Extracting TSO content → $GAMEDIR/game"
unzip -q -o "$TEMPDIR/TSO.zip" -d "$TEMPDIR/tso"
mkdir -p "$GAMEDIR/game"
cabextract -qq -d "$GAMEDIR/game" "$TEMPDIR/tso/Data1.cab"

log "[4/5] Extracting remesh package → $GAMEDIR/Content/MeshReplace"
mkdir -p "$GAMEDIR/Content/MeshReplace"
unzip -q -o "$TEMPDIR/RemeshPackage.zip" -d "$GAMEDIR/Content/MeshReplace"

log "[4/5] Extracting macextras → $GAMEDIR (overlays into install root)"
unzip -q -o "$TEMPDIR/macextras.zip" -d "$GAMEDIR"

# ── Write config.ini ────────────────────────────────────────────────────────
log "[5/5] Configuring client"
mkdir -p "$GAMEDIR/Content"
if (( PRESERVE_CONFIG )); then
    cp "$TEMPDIR/config.ini.backup" "$GAMEDIR/Content/config.ini"
    printf "       Restored your existing Content/config.ini\n"
else
    # Absolute StartupPath so launching via the .command (which already
    # cds to its own dir) AND any future direct-execute path both work.
    cat > "$GAMEDIR/Content/config.ini" <<EOF
# $GAME_NAME Settings File
CurrentLang=english
StartupPath=$GAMEDIR/game/TSOClient/
UseCustomServer=True
GameEntryUrl=$SERVER_URL
CitySelectorUrl=$SERVER_URL
EOF
fi

# ── Launcher: .command files (open in Terminal on double-click) ─────────────
log "Creating launcher scripts"
cat > "$GAMEDIR/$GAME_NAME.command" <<EOF
#!/bin/bash
# Launcher for $GAME_NAME — double-click in Finder to play.
cd "\$(dirname "\$0")"
exec mono FreeSO.exe
EOF

cat > "$GAMEDIR/$GAME_NAME (3D).command" <<EOF
#!/bin/bash
# Launcher for $GAME_NAME in 3D mode.
cd "\$(dirname "\$0")"
exec mono FreeSO.exe -3d
EOF

chmod +x "$GAMEDIR/$GAME_NAME.command" "$GAMEDIR/$GAME_NAME (3D).command"

# Symlink onto Desktop so users have a clear entry point. Ignore failures
# if Desktop doesn't exist (e.g. user moved it).
if [[ -d "$HOME/Desktop" ]]; then
    ln -sf "$GAMEDIR/$GAME_NAME.command" "$HOME/Desktop/$GAME_NAME.command"
    ln -sf "$GAMEDIR/$GAME_NAME (3D).command" "$HOME/Desktop/$GAME_NAME (3D).command"
fi

# ── Cleanup ─────────────────────────────────────────────────────────────────
log "Cleaning up temporary files"
rm -rf "$TEMPDIR"

# ── Done ────────────────────────────────────────────────────────────────────
printf "\n"
printf "  ====================================\n"
printf "  %s installed at: %s\n" "$GAME_NAME" "$GAMEDIR"
printf "  Launch by double-clicking the Desktop shortcut, or:\n"
printf "    \"%s/%s.command\"\n" "$GAMEDIR" "$GAME_NAME"
printf "  ====================================\n"
printf "\nNote: macOS Gatekeeper may prompt the first time you launch.\n"
printf "If blocked, right-click the .command file and choose Open.\n"
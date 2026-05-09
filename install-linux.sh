#!/usr/bin/env bash
#
# EdenSO client installer for Linux. Downloads the FreeSO client + TSO
# game files + remesh package, lays them out under XDG_DATA_HOME, writes
# config.ini pointed at api.edenso.net, and creates a .desktop entry.
#
# Idempotent — re-running offers update-keeping-config or wipe-and-reinstall.
#
# Override URLs / install dir via env vars:
#   FREESO_SERVER_URL=https://my.shard ./install-linux.sh
#   GAME_NAME=MyShard ./install-linux.sh

set -euo pipefail

if [[ "$EUID" -eq 0 ]]; then
    printf "Please run as your normal user, not root or via sudo.\n"
    printf "The script will sudo for the apt install step only.\n"
    exit 1
fi

if [[ "$(uname)" != "Linux" ]]; then
    printf "This installer is for Linux. Use install-macos.sh on macOS.\n"
    exit 1
fi

# ── Configuration ────────────────────────────────────────────────────────────
GAME_NAME="${GAME_NAME:-EdenSO}"
SERVER_URL="${FREESO_SERVER_URL:-https://api.edenso.net}"
CLIENT_URL="${FREESO_CLIENT_URL:-https://github.com/TheGreatCodeholio/FreeSO/releases/download/latest-client/freeso-client-windows-ogl.zip}"
TSO_URL="${FREESO_TSO_URL:-https://archive.org/download/TheSimsOnline_201802/TSO.zip}"
REMESH_URL="${FREESO_REMESH_URL:-https://github.com/ItsSim/fsolauncher/releases/download/1.12.1-prod.24/remeshes-1.0.0-1726774408.zip}"

# Default install dir under XDG_DATA_HOME (or ~/.local/share). Keeps the
# install out of the user's $HOME root and follows freedesktop conventions.
DEFAULT_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/$GAME_NAME"
TEMPDIR="$HOME/.${GAME_NAME,,}_temp"

# ── Helpers ──────────────────────────────────────────────────────────────────
log()  { printf '\n[%s] %s\n' "$(date +%H:%M:%S)" "$*"; }
fail() { printf '\nERROR: %s\n' "$*" >&2; exit 1; }

require_cmd() {
    command -v "$1" >/dev/null 2>&1 || fail "Required command not found: $1"
}

# ── Banner ───────────────────────────────────────────────────────────────────
printf "\n  %s Installer for Linux\n" "$GAME_NAME"
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
            if [[ -f "$GAMEDIR/Content/config.ini" ]]; then
                cp "$GAMEDIR/Content/config.ini" "$TEMPDIR/config.ini.backup" 2>/dev/null \
                    || mkdir -p "$TEMPDIR" && cp "$GAMEDIR/Content/config.ini" "$TEMPDIR/config.ini.backup"
                PRESERVE_CONFIG=1
                log "Backed up your existing Content/config.ini"
            fi
            ;;
    esac
fi

mkdir -p "$TEMPDIR"
cd "$TEMPDIR" || fail "Could not enter $TEMPDIR"

# ── Dependency installation ──────────────────────────────────────────────────
log "[1/5] Installing dependencies via your distro's package manager"

PKG_UPDATE=""
PKG_INSTALL=""
if   command -v apt    >/dev/null; then PKG_UPDATE="apt update -y";    PKG_INSTALL="apt install -y unzip cabextract curl mono-complete libgdiplus libsdl2-2.0-0 libopenal1"
elif command -v dnf    >/dev/null; then PKG_UPDATE="dnf check-update"; PKG_INSTALL="dnf install -y unzip cabextract curl mono-core mono-devel libgdiplus SDL2 openal-soft"
elif command -v pacman >/dev/null; then PKG_UPDATE="pacman -Syy";       PKG_INSTALL="pacman -S --noconfirm unzip cabextract curl mono libgdiplus sdl2 openal"
elif command -v zypper >/dev/null; then PKG_UPDATE="zypper refresh";   PKG_INSTALL="zypper install -y unzip cabextract curl mono-core mono-devel libgdiplus libSDL2-2_0-0 libopenal1"
elif command -v yum    >/dev/null; then PKG_UPDATE="yum check-update"; PKG_INSTALL="yum install -y unzip cabextract curl mono-core mono-devel libgdiplus SDL2 openal-soft"
else
    fail "Unsupported distro. Install unzip + cabextract + curl + mono-complete + libgdiplus + libsdl2 + libopenal manually, then re-run."
fi

# Run as root since most package managers need it.
sudo $PKG_UPDATE  || true   # update may "fail" with check-update on rpm distros — non-fatal
sudo $PKG_INSTALL || fail "Dependency install failed."

require_cmd unzip
require_cmd cabextract
require_cmd curl
require_cmd mono

# ── Download client + TSO + remesh ──────────────────────────────────────────
log "[2/5] Downloading $GAME_NAME client"
curl -fL --retry 3 --retry-delay 5 -o "$TEMPDIR/client.zip" "$CLIENT_URL"

log "[3/5] Downloading TSO game files (large — ~350 MB)"
curl -fL --retry 3 --retry-delay 5 -o "$TEMPDIR/TSO.zip" "$TSO_URL"

log "[3/5] Downloading 3D remesh package"
curl -fL --retry 3 --retry-delay 5 -o "$TEMPDIR/RemeshPackage.zip" "$REMESH_URL"

# ── Extract everything into the install dir ─────────────────────────────────
log "[4/5] Extracting client → $GAMEDIR"
unzip -q -o "$TEMPDIR/client.zip" -d "$GAMEDIR"

log "[4/5] Extracting TSO content → $GAMEDIR/game"
unzip -q -o "$TEMPDIR/TSO.zip" -d "$TEMPDIR/tso"
mkdir -p "$GAMEDIR/game"
cabextract -qq -d "$GAMEDIR/game" "$TEMPDIR/tso/Data1.cab"

log "[4/5] Extracting remesh package → $GAMEDIR/Content/MeshReplace"
mkdir -p "$GAMEDIR/Content/MeshReplace"
unzip -q -o "$TEMPDIR/RemeshPackage.zip" -d "$GAMEDIR/Content/MeshReplace"

# ── Write config.ini (or restore preserved one) ─────────────────────────────
log "[5/5] Configuring client"
mkdir -p "$GAMEDIR/Content"
if (( PRESERVE_CONFIG )); then
    cp "$TEMPDIR/config.ini.backup" "$GAMEDIR/Content/config.ini"
    printf "       Restored your existing Content/config.ini\n"
else
    # Absolute StartupPath so launches via .desktop (where the WD is
    # typically the user's home, not the install dir) still find the
    # TSO game files. Also writes the path into other fields so the
    # client doesn't fall back to defaults.
    cat > "$GAMEDIR/Content/config.ini" <<EOF
# $GAME_NAME Settings File
CurrentLang=english
StartupPath=$GAMEDIR/game/TSOClient/
UseCustomServer=True
GameEntryUrl=$SERVER_URL
CitySelectorUrl=$SERVER_URL
EOF
fi

# ── Desktop entry / launcher ────────────────────────────────────────────────
log "Creating .desktop entry"
APPS="$HOME/.local/share/applications"
mkdir -p "$APPS"

# Path= sets the working directory the binary launches from. Without
# this, Exec=mono ... would inherit the user's $HOME as cwd and any
# relative paths in the FSO client (e.g. logs, default save dirs) would
# end up scattered there.
cat > "$APPS/${GAME_NAME,,}.desktop" <<EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=$GAME_NAME
Comment=Launch $GAME_NAME
Path=$GAMEDIR
Exec=mono $GAMEDIR/FreeSO.exe
Terminal=false
StartupNotify=false
Categories=Game
EOF

cat > "$APPS/${GAME_NAME,,}-3d.desktop" <<EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=$GAME_NAME (3D)
Comment=Launch $GAME_NAME in 3D mode
Path=$GAMEDIR
Exec=mono $GAMEDIR/FreeSO.exe -3d
Terminal=false
StartupNotify=false
Categories=Game
EOF

# Refresh the desktop database so the new entries appear without a relog.
command -v update-desktop-database >/dev/null && update-desktop-database "$APPS" || true

# ── Cleanup ─────────────────────────────────────────────────────────────────
log "Cleaning up temporary files"
rm -rf "$TEMPDIR"

# ── Done ────────────────────────────────────────────────────────────────────
printf "\n"
printf "  ====================================\n"
printf "  %s installed at: %s\n" "$GAME_NAME" "$GAMEDIR"
printf "  Launch from your applications menu, or run:\n"
printf "    mono %s/FreeSO.exe        (2D)\n" "$GAMEDIR"
printf "    mono %s/FreeSO.exe -3d    (3D)\n" "$GAMEDIR"
printf "  ====================================\n"
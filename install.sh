#!/bin/bash

if [ "$EUID" -eq 0 ]; then
    printf "Please run as a local user, not sudo\n"
    exit 1
fi

# ── Configuration ────────────────────────────────────────────────────────────
# Each URL is overridable via env var so devs / community hosts don't have
# to fork the script just to point at a test server. Run with e.g.
#   FREESO_SERVER_URL=https://localhost:8080 ./install.sh
GAME_NAME="${GAME_NAME:-FreeSO}"
SERVER_URL="${FREESO_SERVER_URL:-https://api.edenso.net}"
# Use the explicit release tag rather than `releases/latest/...` — when both
# `latest-client` and `latest-server` tags exist, GitHub's "latest" is
# whichever was published most recently and may resolve to the wrong one.
CLIENT_URL="${FREESO_CLIENT_URL:-https://github.com/TheGreatCodeholio/FreeSO/releases/download/latest-client/freeso-client-windows-ogl.zip}"
TSO_URL="${FREESO_TSO_URL:-https://archive.org/download/TheSimsOnline_201802/TSO.zip}"
REMESH_URL="${FREESO_REMESH_URL:-https://github.com/ItsSim/fsolauncher/releases/download/1.12.1-prod.24/remeshes-1.0.0-1726774408.zip}"
MACEXTRAS_URL="${FREESO_MACEXTRAS_URL:-https://github.com/TheGreatCodeholio/FreeSO/releases/download/latest-client/macextras.zip}"
# ─────────────────────────────────────────────────────────────────────────────

# ── Platform detection ───────────────────────────────────────────────────────
OS="linux"
if [[ "$(uname)" == "Darwin" ]]; then
    OS="macos"
fi

TEMPDIR="$HOME/.freeso_temp"
GAMEDIR="none"

printf "%s Installer for %s\n" "$GAME_NAME" "$([ "$OS" = "macos" ] && echo "macOS" || echo "Linux")"
printf "Based on the DramaSO installer by Tom\n\n"

# ── Install directory prompt ─────────────────────────────────────────────────
while [ "$GAMEDIR" == "none" ]; do
    read -r -p "Directory to install ${GAME_NAME} [${HOME}/freeso]: " GAMEDIR
    GAMEDIR=${GAMEDIR:-${HOME}/freeso}

    if [ -z "${GAMEDIR%%/*}" ] && pathchk -pP "$GAMEDIR"; then
        mkdir -p "$GAMEDIR"
        if [ -d "$GAMEDIR" ]; then
            printf "Installing to %s.\n" "$GAMEDIR"
        else
            printf "%s is not a valid directory.\n" "$GAMEDIR"
            GAMEDIR="none"
        fi
    else
        printf "%s is not a full absolute path (e.g. %s/freeso)\n" "$GAMEDIR" "$HOME"
        GAMEDIR="none"
    fi
done

printf "Temporary install file location: %s\n" "$TEMPDIR"
printf "Game file location: %s\n" "$GAMEDIR"

mkdir -p "$TEMPDIR" && cd "$TEMPDIR" || exit 1

# ── Dependency installation ──────────────────────────────────────────────────
printf "\nInstalling dependencies...\n"

if [ "$OS" = "macos" ]; then
    if ! which brew > /dev/null 2>&1; then
        printf "\nHomebrew is required but not installed.\n"
        printf "Install it from https://brew.sh then re-run this script.\n"
        exit 1
    fi
    brew update
    brew install mono cabextract curl unzip
else
    PACKAGEUPDATE="none"
    PACKAGEINSTALL="none"

    printf "Determining package manager...\n"
    if which apt    > /dev/null 2>&1; then PACKAGEUPDATE="apt update -y";   PACKAGEINSTALL="apt install -y unzip cabextract curl mono-complete libgdiplus libsdl2-2.0-0 libopenal1"; fi
    if which pacman > /dev/null 2>&1; then PACKAGEUPDATE="pacman -Syy";      PACKAGEINSTALL="pacman -S --noconfirm unzip cabextract curl mono"; fi
    if which yum    > /dev/null 2>&1; then PACKAGEUPDATE="yum check-update"; PACKAGEINSTALL="yum install -y unzip cabextract curl mono-core mono-devel"; fi
    if which dnf    > /dev/null 2>&1; then PACKAGEUPDATE="dnf check-update"; PACKAGEINSTALL="dnf install -y unzip cabextract curl mono-core mono-devel"; fi
    if which zypper > /dev/null 2>&1; then PACKAGEUPDATE="zypper refresh";   PACKAGEINSTALL="zypper install -y unzip cabextract curl mono-core mono-devel"; fi

    if [ "$PACKAGEUPDATE" == "none" ]; then
        printf "\nPackage manager not supported. Install unzip, cabextract, curl and mono manually, then re-run.\n"
        exit 1
    fi

    sudo ${PACKAGEUPDATE}
    sudo ${PACKAGEINSTALL}
fi

# ── Downloads ────────────────────────────────────────────────────────────────
printf "\nDownloading: TSO game package\n"
curl -# -L -o "${TEMPDIR}/TSO.zip" "$TSO_URL"

printf "\nDownloading: %s client\n" "$GAME_NAME"
curl -# -L -o "${TEMPDIR}/client.zip" "$CLIENT_URL"

printf "\nDownloading: Remesh package\n"
curl -# -L -o "${TEMPDIR}/RemeshPackage.zip" "$REMESH_URL"

if [ "$OS" = "macos" ]; then
    printf "\nDownloading: macOS native libraries\n"
    curl -# -L -o "${TEMPDIR}/macextras.zip" "$MACEXTRAS_URL"
fi

# ── Extraction ───────────────────────────────────────────────────────────────
printf "\nExtracting client...\n"
unzip -q -o "${TEMPDIR}/client.zip" -d "$GAMEDIR"

printf "\nExtracting TSO game files...\n"
unzip -q -o "${TEMPDIR}/TSO.zip" -d "${TEMPDIR}/tso"
cabextract -qq -d "${GAMEDIR}/game" "${TEMPDIR}/tso/Data1.cab"

printf "\nExtracting remesh package...\n"
mkdir -p "${GAMEDIR}/Content/MeshReplace"
unzip -q -o "${TEMPDIR}/RemeshPackage.zip" -d "${GAMEDIR}/Content/MeshReplace"

if [ "$OS" = "macos" ]; then
    printf "\nExtracting macOS native libraries...\n"
    unzip -q -o "${TEMPDIR}/macextras.zip" -d "$GAMEDIR"
fi

# ── Client configuration ─────────────────────────────────────────────────────
printf "\nConfiguring client...\n"
cat > "${GAMEDIR}/Content/config.ini" << EOL
# FreeSO Settings File
CurrentLang=english
StartupPath=game/TSOClient/
UseCustomServer=True
GameEntryUrl=${SERVER_URL}
CitySelectorUrl=${SERVER_URL}
EOL

# ── Launcher ─────────────────────────────────────────────────────────────────
printf "\nCreating launcher...\n"

if [ "$OS" = "macos" ]; then
    # .command files open in Terminal on double-click on macOS
    cat > "${GAMEDIR}/${GAME_NAME}.command" << EOL
#!/bin/bash
cd "\$(dirname "\$0")"
mono FreeSO.exe
EOL
    cat > "${GAMEDIR}/${GAME_NAME} (3D).command" << EOL
#!/bin/bash
cd "\$(dirname "\$0")"
mono FreeSO.exe -3d
EOL
    chmod +x "${GAMEDIR}/${GAME_NAME}.command" "${GAMEDIR}/${GAME_NAME} (3D).command"
    printf "Launcher scripts created in %s\n" "$GAMEDIR"
    printf "Double-click %s.command to launch.\n" "$GAME_NAME"
else
    mkdir -p "${HOME}/.local/share/applications"

    cat > "${HOME}/.local/share/applications/${GAME_NAME}.desktop" << EOL
[Desktop Entry]
Version=1.0
Type=Application
Name=${GAME_NAME}
Comment=Launch ${GAME_NAME}
Exec=mono ${GAMEDIR}/FreeSO.exe
Terminal=false
StartupNotify=false
Categories=Game
EOL

    cat > "${HOME}/.local/share/applications/${GAME_NAME}-3d.desktop" << EOL
[Desktop Entry]
Version=1.0
Type=Application
Name=${GAME_NAME} (3D)
Comment=Launch ${GAME_NAME} in 3D mode
Exec=mono ${GAMEDIR}/FreeSO.exe -3d
Terminal=false
StartupNotify=false
Categories=Game
EOL
fi

# ── Cleanup ──────────────────────────────────────────────────────────────────
printf "\nCleaning up temporary files...\n"
rm -rf "$TEMPDIR"

printf "\nInstall complete!\n"
if [ "$OS" = "macos" ]; then
    printf "Double-click %s/%s.command to launch.\n" "$GAMEDIR" "$GAME_NAME"
    printf "Or run: mono %s/FreeSO.exe\n" "$GAMEDIR"
else
    printf "Run with:  mono %s/FreeSO.exe\n" "$GAMEDIR"
    printf "Or:        mono %s/FreeSO.exe -3d  (3D mode)\n" "$GAMEDIR"
    printf "Or launch from your applications menu (may need to log out and back in).\n"
fi
#!/bin/bash

if [ "$EUID" -eq 0 ]; then
    printf "Please run as a local user, not sudo\n"
    exit 1
fi

# ── Configuration ────────────────────────────────────────────────────────────
GAME_NAME="FreeSO"
SERVER_URL="https://fso.icarey.net"
CLIENT_URL="https://github.com/TheGreatCodeholio/FreeSO/releases/latest/download/freeso-client-windows-ogl.zip"
TSO_URL="https://archive.org/download/TheSimsOnline_201802/TSO.zip"
REMESH_URL="https://github.com/ItsSim/fsolauncher/releases/download/1.12.1-prod.24/remeshes-1.0.0-1726774408.zip"
# ─────────────────────────────────────────────────────────────────────────────

TEMPDIR="$HOME/.freeso_temp"
GAMEDIR="none"

printf "%s Installer for Linux\n" "$GAME_NAME"
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

# ── Package manager detection ────────────────────────────────────────────────
PACKAGEUPDATE="none"
PACKAGEINSTALL="none"

printf "\nDetermining package manager...\n"
if which apt    > /dev/null 2>&1; then PACKAGEUPDATE="apt update -y";     PACKAGEINSTALL="apt install -y unzip cabextract curl mono-complete libgdiplus libsdl2-2.0-0 libopenal1"; fi
if which pacman > /dev/null 2>&1; then PACKAGEUPDATE="pacman -Syy";        PACKAGEINSTALL="pacman -S --noconfirm unzip cabextract curl mono"; fi
if which yum    > /dev/null 2>&1; then PACKAGEUPDATE="yum check-update";   PACKAGEINSTALL="yum install -y unzip cabextract curl mono-core mono-devel"; fi
if which dnf    > /dev/null 2>&1; then PACKAGEUPDATE="dnf check-update";   PACKAGEINSTALL="dnf install -y unzip cabextract curl mono-core mono-devel"; fi
if which zypper > /dev/null 2>&1; then PACKAGEUPDATE="zypper refresh";     PACKAGEINSTALL="zypper install -y unzip cabextract curl mono-core mono-devel"; fi

if [ "$PACKAGEUPDATE" == "none" ]; then
    printf "\nPackage manager not supported. Install unzip, cabextract, curl and mono manually, then re-run.\n"
    exit 1
fi

printf "\nUpdating package sources...\n"
sudo ${PACKAGEUPDATE}

printf "\nInstalling dependencies...\n"
sudo ${PACKAGEINSTALL}

# ── Downloads ────────────────────────────────────────────────────────────────
printf "\nDownloading: TSO game package\n"
curl -# -L -o "${TEMPDIR}/TSO.zip" "$TSO_URL"

printf "\nDownloading: %s client\n" "$GAME_NAME"
curl -# -L -o "${TEMPDIR}/client.zip" "$CLIENT_URL"

printf "\nDownloading: Remesh package\n"
curl -# -L -o "${TEMPDIR}/RemeshPackage.zip" "$REMESH_URL"

# ── Extraction ───────────────────────────────────────────────────────────────
printf "\nExtracting client...\n"
unzip -q -o "${TEMPDIR}/client.zip" -d "$GAMEDIR"

printf "\nExtracting TSO game files...\n"
unzip -q -o "${TEMPDIR}/TSO.zip" -d "${TEMPDIR}/tso"
cabextract -qq -d "${GAMEDIR}/game" "${TEMPDIR}/tso/Data1.cab"

printf "\nExtracting remesh package...\n"
mkdir -p "${GAMEDIR}/Content/MeshReplace"
unzip -q -o "${TEMPDIR}/RemeshPackage.zip" -d "${GAMEDIR}/Content/MeshReplace"

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

# ── Desktop entries ──────────────────────────────────────────────────────────
printf "\nCreating desktop launcher...\n"
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

# ── Cleanup ──────────────────────────────────────────────────────────────────
printf "\nCleaning up temporary files...\n"
rm -rf "$TEMPDIR"

printf "\nInstall complete!\n"
printf "Run with:  mono %s/FreeSO.exe\n" "$GAMEDIR"
printf "Or:        mono %s/FreeSO.exe -3d  (3D mode)\n" "$GAMEDIR"
printf "Or launch from your applications menu (may need to log out and back in).\n"
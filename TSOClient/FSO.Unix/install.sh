#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

PUBLISH_DIR = SCRIPT_DIR

INSTALL_DIR="$HOME/.local/share/FreeSO"
mkdir -p "$INSTALL_DIR"
rm -rf "$INSTALL_DIR"/*
cp -R "$PUBLISH_DIR"/* "$INSTALL_DIR/"
chmod +x "$INSTALL_DIR/FreeSO"

mkdir -p "$HOME/.local/share/icons"
cp "$SCRIPT_DIR/fso.png" "$HOME/.local/share/icons/freeso.png"

cat > "$HOME/.local/share/applications/FreeSO.desktop" << EOF
[Desktop Entry]
Name=FreeSO
Comment=Free re-implementation of The Sims Online
Exec=$INSTALL_DIR/FreeSO
Icon=$HOME/.local/share/icons/freeso.png
Terminal=false
Type=Application
Categories=Game;
EOF

echo "Done!"
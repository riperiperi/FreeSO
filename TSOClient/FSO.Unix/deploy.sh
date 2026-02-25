#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

OS="$(uname -s)"

if [ "$OS" = "Darwin" ]; then
    RID="osx-arm64"
elif [ "$OS" = "Linux" ]; then
    RID="linux-x64"
else
    echo "Unsupported platform: $OS" && exit 1
fi

PUBLISH_DIR="$SCRIPT_DIR/bin/Release/net9.0/$RID/publish"

echo "Building FSO.Unix for $OS..."
dotnet publish -c Release -r "$RID" --self-contained true -p:PublishSingleFile=true

if [ "$OS" = "Darwin" ]; then
    rm -rf /Applications/FreeSO.app
    cp -R "$PUBLISH_DIR/FreeSO.app" /Applications/
    open /Applications/FreeSO.app
else
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

    "$INSTALL_DIR/FreeSO" &
fi

echo "Done."

#!/usr/bin/env bash
# One-command browser multiplayer demo: content bundle → lot host → gateway →
# static server, then open http://127.0.0.1:5259/?vm=1&name=you in two tabs.
#
#   TSO_DIR=/path/to/TSOClient ./run_browser_demo.sh
#
# Env / flags:
#   TSO_DIR       real TSO game tree (the dir containing objectdata/, avatardata/…)
#                 — only needed the first time, to build the content bundle
#   PACKS_DIR     compiled pack .iffs           (default: ~/packs-out)
#   OUT_DIR       content bundle output         (default: ~/browser-content)
#   PUBLISH_DIR   BrowserClient publish output  (default: ~/browser-publish)
#   PORT_HTTP=5259  PORT_GATEWAY=8087  PORT_SANDBOX=37564
#   --rebuild     force bundle + builds + publish even if outputs exist
#
# Ctrl-C stops everything.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$SCRIPT_DIR/../.." && pwd)"
PACKS_DIR="${PACKS_DIR:-$HOME/packs-out}"
OUT_DIR="${OUT_DIR:-$HOME/browser-content}"
PUBLISH_DIR="${PUBLISH_DIR:-$HOME/browser-publish}"
PORT_HTTP="${PORT_HTTP:-5259}"
PORT_GATEWAY="${PORT_GATEWAY:-8087}"
PORT_SANDBOX="${PORT_SANDBOX:-37564}"
REBUILD=0
[ "${1:-}" = "--rebuild" ] && REBUILD=1

LOG_DIR="${LOG_DIR:-$OUT_DIR/logs}"
HOUSE="$REPO/PackTools/FSO.BrowserClient/wwwroot/houses/grove.xml"
FURNISH="$REPO/PackTools/FSO.BrowserClient/wwwroot/houses/grove-furnish.json"
MANIFEST="$REPO/PackTools/FSO.BrowserClient/wwwroot/packs/manifest.json"
HOST_BIN="$REPO/PackTools/FSO.LotHostLite/bin/Debug/net9.0/FSO.LotHostLite"
GATEWAY_BIN="$REPO/PackTools/FSO.WsGateway/bin/Debug/net9.0/FSO.WsGateway"

# dotnet: PATH first, then conventional install locations.
if ! command -v dotnet >/dev/null 2>&1; then
    for cand in "$HOME/.dotnet" /home/user/.dotnet /usr/local/share/dotnet /usr/share/dotnet; do
        if [ -x "$cand/dotnet" ]; then
            export DOTNET_ROOT="$cand"
            export PATH="$cand:$PATH"
            break
        fi
    done
    command -v dotnet >/dev/null 2>&1 || { echo "FATAL: dotnet not found (install .NET 9 SDK or add it to PATH)" >&2; exit 1; }
fi
export DOTNET_ROOT="${DOTNET_ROOT:-$(dirname "$(command -v dotnet)")}"

step() { echo; echo "==> $*"; }

# 1. Content bundle (needs TSO_DIR only when building it).
if [ "$REBUILD" = 1 ] || [ ! -f "$OUT_DIR/content.tar.gz" ]; then
    [ -n "${TSO_DIR:-}" ] || { echo "FATAL: no bundle at $OUT_DIR and TSO_DIR not set" >&2; exit 1; }
    # Empty is as fatal as missing: a bundle without packs means an unfurnished
    # lot and every furnish entry failing at host boot.
    if ! ls "$PACKS_DIR"/*.iff >/dev/null 2>&1; then
        echo "FATAL: no .iff files in PACKS_DIR $PACKS_DIR — build them first:" >&2
        echo "  for j in \"$REPO\"/PackTools/examples/*.json; do dotnet run --project \"$REPO\"/PackTools/FSO.PackCompiler -- build \"\$j\" -o \"$PACKS_DIR\" --tso-dir \"\$TSO_DIR\"; done" >&2
        exit 1
    fi
    step "building content bundle → $OUT_DIR (a few minutes)"
    python3 "$SCRIPT_DIR/make_browser_content.py" \
        --tso-dir "$TSO_DIR" --repo "$REPO" --packs "$PACKS_DIR" --out "$OUT_DIR"
else
    step "content bundle exists ($OUT_DIR/content.tar.gz) — skipping (use --rebuild to force)"
fi

# 2. Builds.
if [ "$REBUILD" = 1 ] || [ ! -x "$HOST_BIN" ]; then
    step "building FSO.LotHostLite"
    dotnet build -c Debug "$REPO/PackTools/FSO.LotHostLite" | tail -2
fi
if [ "$REBUILD" = 1 ] || [ ! -x "$GATEWAY_BIN" ]; then
    step "building FSO.WsGateway"
    dotnet build -c Debug "$REPO/PackTools/FSO.WsGateway" | tail -2
fi
if [ "$REBUILD" = 1 ] || [ ! -f "$PUBLISH_DIR/wwwroot/index.html" ]; then
    step "publishing FSO.BrowserClient → $PUBLISH_DIR (a few minutes)"
    rm -rf "$PUBLISH_DIR"
    dotnet publish -c Debug "$REPO/PackTools/FSO.BrowserClient" -o "$PUBLISH_DIR" | tail -2
fi

# 3. Stage the bundle into the served tree.
mkdir -p "$PUBLISH_DIR/wwwroot/tso-content" "$LOG_DIR"
ln -sf "$OUT_DIR/content.tar.gz" "$PUBLISH_DIR/wwwroot/tso-content/content.tar.gz"
ln -sf "$OUT_DIR/content-manifest.json" "$PUBLISH_DIR/wwwroot/tso-content/content-manifest.json"

# 4. Start everything; kill the whole tree on exit.
PIDS=()
cleanup() {
    trap - EXIT INT TERM
    echo; echo "==> stopping demo"
    for pid in "${PIDS[@]:-}"; do kill "$pid" 2>/dev/null || true; done
    wait 2>/dev/null || true
}
trap cleanup EXIT INT TERM

step "starting lot host (sandbox :$PORT_SANDBOX)"
"$HOST_BIN" --house "$HOUSE" --tso-dir "$OUT_DIR/tso/" --bare-objects \
    --furnish "$FURNISH" --manifest "$MANIFEST" --packs "$PACKS_DIR" \
    --port "$PORT_SANDBOX" > "$LOG_DIR/host.log" 2>&1 &
PIDS+=($!)

step "starting gateway (ws :$PORT_GATEWAY)"
"$GATEWAY_BIN" --listen "http://127.0.0.1:$PORT_GATEWAY" \
    --sandbox "127.0.0.1:$PORT_SANDBOX" > "$LOG_DIR/gateway.log" 2>&1 &
PIDS+=($!)

step "starting static server (http :$PORT_HTTP)"
python3 -m http.server "$PORT_HTTP" --bind 127.0.0.1 \
    --directory "$PUBLISH_DIR/wwwroot" > "$LOG_DIR/http.log" 2>&1 &
PIDS+=($!)

# 5. Readiness: the host is the slow one (content boot ~15s).
for i in $(seq 1 60); do
    grep -q "ticking 30Hz" "$LOG_DIR/host.log" 2>/dev/null && break
    kill -0 "${PIDS[0]}" 2>/dev/null || { echo "FATAL: lot host died — $LOG_DIR/host.log:" >&2; tail -15 "$LOG_DIR/host.log" >&2; exit 1; }
    sleep 2
done
grep -q "ticking 30Hz" "$LOG_DIR/host.log" || { echo "FATAL: lot host never became ready" >&2; exit 1; }
grep -m1 "furnished" "$LOG_DIR/host.log" || true

echo
echo "============================================================"
echo "  Demo is up. Open (twice, for multiplayer):"
echo
echo "      http://127.0.0.1:$PORT_HTTP/?vm=1&name=you"
echo
echo "  Click furniture for its pie menu; chat box bottom-left."
echo "  Logs: $LOG_DIR/{host,gateway,http}.log — Ctrl-C stops all."
echo "============================================================"
wait

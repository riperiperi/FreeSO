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
# Republish whenever the checked-out code changed — a stale publish dir served
# a two-day-old app once and cost hours of "same thing" debugging.
REV="$(git -C "$REPO" rev-parse --short HEAD 2>/dev/null || echo unknown)"
PUB_REV="$(cat "$PUBLISH_DIR/wwwroot/build-rev.txt" 2>/dev/null || echo none)"
if [ "$REBUILD" = 1 ] || [ ! -f "$PUBLISH_DIR/wwwroot/index.html" ] || [ "$PUB_REV" != "$REV" ]; then
    step "publishing FSO.BrowserClient @ $REV → $PUBLISH_DIR (a few minutes; was: $PUB_REV)"
    rm -rf "$PUBLISH_DIR"
    dotnet publish -c Debug "$REPO/PackTools/FSO.BrowserClient" -o "$PUBLISH_DIR" | tail -2
    echo "$REV" > "$PUBLISH_DIR/wwwroot/build-rev.txt"
    # Stamp the page so the browser shows which build it's running.
    sed -i.bak "s/__FSO_BUILD_REV__/$REV/g" "$PUBLISH_DIR/wwwroot/index.html" && rm -f "$PUBLISH_DIR/wwwroot/index.html.bak"
else
    step "publish is current (build $PUB_REV)"
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

step "starting static server (http :$PORT_HTTP, no-store)"
# Cache-Control: no-store — a browser-cached stale app is indistinguishable
# from a broken one; never let the demo be served from cache.
cat > "$LOG_DIR/serve.py" <<'PYEOF'
import functools, http.server, sys
class NoStoreHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header('Cache-Control', 'no-store')
        super().end_headers()
    def log_message(self, fmt, *args):
        sys.stderr.write("%s - %s\n" % (self.address_string(), fmt % args))
http.server.ThreadingHTTPServer(
    ('127.0.0.1', int(sys.argv[1])),
    functools.partial(NoStoreHandler, directory=sys.argv[2])).serve_forever()
PYEOF
python3 "$LOG_DIR/serve.py" "$PORT_HTTP" "$PUBLISH_DIR/wwwroot" > "$LOG_DIR/http.log" 2>&1 &
PIDS+=($!)

# The port must be OURS serving THIS build: a leftover server from an old demo
# session squatting the port serves a stale app and looks exactly like "nothing
# changed". curl build-rev.txt and require this publish's rev.
sleep 2
SERVED_REV="$(curl -sf "http://127.0.0.1:$PORT_HTTP/build-rev.txt" 2>/dev/null | tr -d '[:space:]' || true)"
if [ "$SERVED_REV" != "$REV" ]; then
    echo "FATAL: :$PORT_HTTP is serving build '${SERVED_REV:-nothing}' instead of '$REV'." >&2
    echo "  Another (old) server owns the port — dev servers show up as plain" >&2
    echo "  'dotnet', so kill by PORT, not by name, then rerun:" >&2
    echo "    lsof -ti :$PORT_HTTP | xargs kill -9" >&2
    exit 1
fi

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

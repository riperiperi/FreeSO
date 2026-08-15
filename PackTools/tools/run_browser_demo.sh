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
DOCTOR=0
case "${1:-}" in
    --rebuild) REBUILD=1 ;;
    --doctor)  DOCTOR=1 ;;
esac

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

# --- ports -------------------------------------------------------------------
# Leftover processes from earlier runs owning these ports caused every
# "nothing changed / not reachable" failure in the field. The demo owns these
# three ports, so free them ourselves — but only when the squatter is
# recognisably part of this demo; anything else is reported, never killed.
port_pids() {
    if command -v lsof >/dev/null 2>&1; then
        lsof -ti ":$1" 2>/dev/null || true
    else
        ss -tlnp 2>/dev/null | grep ":$1 " | grep -oE 'pid=[0-9]+' | cut -d= -f2 | sort -u
    fi
}

free_our_port() {
    local port="$1" pid args
    for pid in $(port_pids "$port"); do
        args="$(ps -p "$pid" -o args= 2>/dev/null || true)"
        case "$args" in
            *FSO.WsGateway*|*FSO.LotHostLite*|*serve.py*|*http.server*|*browser-publish*)
                echo "  :$port was held by an earlier demo process ($pid) — stopping it"
                kill -9 "$pid" 2>/dev/null || true
                ;;
            "")  ;; # vanished between listing and inspection
            *)
                echo "FATAL: :$port is held by something that is not part of this demo:" >&2
                echo "    pid $pid: $args" >&2
                echo "  Quit that program (or set PORT_HTTP/PORT_GATEWAY/PORT_SANDBOX) and rerun." >&2
                exit 1
                ;;
        esac
    done
}

doctor() {
    echo "FreeSO browser demo — status"
    echo "  repo:     $REPO @ $(git -C "$REPO" rev-parse --short HEAD 2>/dev/null || echo '?') on $(git -C "$REPO" branch --show-current 2>/dev/null || echo '?')"
    echo "  packs:    $(ls "$PACKS_DIR"/*.iff 2>/dev/null | wc -l | tr -d ' ') iff files in $PACKS_DIR"
    echo "  bundle:   $([ -f "$OUT_DIR/content.tar.gz" ] && echo present || echo MISSING) ($OUT_DIR/content.tar.gz)"
    echo "  publish:  build $(cat "$PUBLISH_DIR/wwwroot/build-rev.txt" 2>/dev/null || echo NONE)"
    local p name
    for p in "$PORT_HTTP:web" "$PORT_GATEWAY:gateway" "$PORT_SANDBOX:lot host"; do
        name="${p#*:}"; p="${p%%:*}"
        if (exec 3<>"/dev/tcp/127.0.0.1/$p") 2>/dev/null; then
            exec 3<&- 3>&-
            echo "  :$p       LISTENING ($name)"
        else
            echo "  :$p       DOWN      ($name)"
        fi
    done
    local log
    for log in host gateway http; do
        if [ -s "$LOG_DIR/$log.log" ]; then
            echo "  --- last line of $log.log:"
            echo "      $(tail -1 "$LOG_DIR/$log.log")"
        fi
    done
}

if [ "$DOCTOR" = 1 ]; then doctor; exit 0; fi

# Everything that went wrong on a second machine was detectable in seconds, but
# surfaced twenty minutes later (or in the browser, in another program). Check it
# all up front, and say exactly which one failed.
preflight() {
    local problems=0
    note() { echo "  ✗ $*" >&2; problems=$((problems + 1)); }

    command -v python3 >/dev/null 2>&1 || note "python3 not found (needed for the bundle and the web server)"

    if ! command -v dotnet >/dev/null 2>&1; then
        note "dotnet not found on PATH — this silently produces an empty furniture folder.
      try: export PATH=\"\$PATH:/usr/local/share/dotnet:\$HOME/.dotnet\""
    fi

    local need_bundle=0
    [ "$REBUILD" = 1 ] && need_bundle=1
    [ -s "$OUT_DIR/content.tar.gz" ] || need_bundle=1
    if [ "$need_bundle" = 1 ]; then
        if [ -z "${TSO_DIR:-}" ]; then
            note "no content bundle at $OUT_DIR and TSO_DIR is not set.
      TSO_DIR must point at your TSO install (the folder containing objectdata/).
      find it: find ~ -maxdepth 6 -type d -name objectdata"
        elif [ ! -f "$TSO_DIR/objectdata/globals/global.iff" ]; then
            note "TSO_DIR does not look like a TSO install: $TSO_DIR
      expected: \$TSO_DIR/objectdata/globals/global.iff"
        fi
        # The bundle needs room for the tree plus the tarball, and the publish
        # another few hundred MB.
        local avail
        avail="$(df -Pk "$(dirname "$OUT_DIR")" 2>/dev/null | awk 'NR==2 {print int($4/1024)}')"
        [ -n "$avail" ] && [ "$avail" -lt 1200 ] && \
            note "only ${avail}MB free near $OUT_DIR; the bundle + publish need ~1.2GB"
    fi

    if ! ls "$PACKS_DIR"/*.iff >/dev/null 2>&1; then
        note "no compiled furniture in $PACKS_DIR — build it first (~10 min):
      for j in \"$REPO\"/PackTools/examples/*.json; do dotnet run --project \"$REPO\"/PackTools/FSO.PackCompiler -- build \"\$j\" -o \"$PACKS_DIR\" --tso-dir \"\$TSO_DIR\"; done"
    fi

    for f in "$HOUSE" "$FURNISH" "$MANIFEST"; do
        [ -f "$f" ] || note "missing repo file: $f"
    done

    if [ "$problems" -gt 0 ]; then
        echo >&2
        echo "FATAL: $problems problem(s) above — nothing was built or started." >&2
        exit 1
    fi
    echo "preflight ok (dotnet $(dotnet --version 2>/dev/null || echo '?'), $(ls "$PACKS_DIR"/*.iff 2>/dev/null | wc -l | tr -d ' ') furniture files)"
}

step "preflight"
preflight

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
# Always build (incremental — seconds when nothing changed). "Build only if the
# binary is missing" shipped a real failure: a gateway binary left over from an
# older checkout predated the --sandbox flag, so it exited 2 on an unknown
# argument and nothing ever listened on the WS port.
step "building FSO.LotHostLite"
dotnet build -c Debug "$REPO/PackTools/FSO.LotHostLite" | tail -2
step "building FSO.WsGateway"
dotnet build -c Debug "$REPO/PackTools/FSO.WsGateway" | tail -2
# Republish whenever the checked-out code changed — a stale publish dir served
# a two-day-old app once and cost hours of "same thing" debugging.
REV="$(git -C "$REPO" rev-parse --short HEAD 2>/dev/null || echo unknown)"
PUB_REV="$(cat "$PUBLISH_DIR/wwwroot/build-rev.txt" 2>/dev/null || echo none)"
# Rev alone isn't enough: uncommitted edits (the normal state while working on
# the client) would keep serving the previous publish and look like the change
# had no effect. Any newer source file forces a republish too.
STAMP="$PUBLISH_DIR/wwwroot/build-rev.txt"
NEWER_SRC=""
[ -f "$STAMP" ] && NEWER_SRC="$(find "$REPO/PackTools/FSO.BrowserClient" "$REPO/TSOClient/tso.world" \
    "$REPO/TSOClient/tso.simantics" "$REPO/TSOClient/tso.content" \
    \( -name '*.cs' -o -name '*.html' -o -name '*.csproj' \) -newer "$STAMP" -print -quit 2>/dev/null)"
if [ "$REBUILD" = 1 ] || [ ! -f "$PUBLISH_DIR/wwwroot/index.html" ] || [ "$PUB_REV" != "$REV" ] || [ -n "$NEWER_SRC" ]; then
    [ -n "$NEWER_SRC" ] && echo "  (source newer than publish: ${NEWER_SRC#$REPO/})"
    step "publishing FSO.BrowserClient @ $REV → $PUBLISH_DIR (a few minutes; was: $PUB_REV)"
    rm -rf "$PUBLISH_DIR"
    dotnet publish -c Debug "$REPO/PackTools/FSO.BrowserClient" -o "$PUBLISH_DIR" | tail -2
    echo "$REV" > "$PUBLISH_DIR/wwwroot/build-rev.txt"
    # Stamp the page so the browser shows which build it's running.
    sed -i.bak "s/__FSO_BUILD_REV__/$REV/g" "$PUBLISH_DIR/wwwroot/index.html" && rm -f "$PUBLISH_DIR/wwwroot/index.html.bak"
else
    step "publish is current (build $PUB_REV)"
fi

# 3. The bundle is served straight from $OUT_DIR by the route in serve.py — no
# staging, no symlinks. Remove any links left by older versions of this script so
# they can't shadow the route with a dangling target.
mkdir -p "$LOG_DIR"
rm -rf "$PUBLISH_DIR/wwwroot/tso-content"
if [ ! -s "$OUT_DIR/content.tar.gz" ]; then
    echo "FATAL: content bundle missing or empty: $OUT_DIR/content.tar.gz" >&2
    ls -l "$OUT_DIR" 2>/dev/null >&2 || echo "  (no $OUT_DIR at all — rerun with TSO_DIR set to rebuild)" >&2
    exit 1
fi

# 4. Start everything; kill the whole tree on exit.
PIDS=()
cleanup() {
    trap - EXIT INT TERM
    echo; echo "==> stopping demo"
    for pid in "${PIDS[@]:-}"; do kill "$pid" 2>/dev/null || true; done
    wait 2>/dev/null || true
}
trap cleanup EXIT INT TERM

step "freeing demo ports"
free_our_port "$PORT_SANDBOX"
free_our_port "$PORT_GATEWAY"
free_our_port "$PORT_HTTP"
sleep 1

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
import functools, http.server, os, sys, posixpath, urllib.parse

WWWROOT, BUNDLE = sys.argv[2], sys.argv[3]

class DemoHandler(http.server.SimpleHTTPRequestHandler):
    """Serves the published app, plus /tso-content/ mapped straight onto the
    bundle directory. The bundle used to be symlinked into wwwroot, which
    reached one machine as a bare 404 inside the running game; a route has no
    filesystem hop to get wrong."""

    def translate_path(self, path):
        clean = posixpath.normpath(urllib.parse.unquote(path.split('?', 1)[0].split('#', 1)[0]))
        if clean.startswith('/tso-content/'):
            rel = clean[len('/tso-content/'):]
            # normpath already collapsed '..'; refuse anything still escaping.
            if rel.startswith('/') or rel.startswith('..'):
                return os.path.join(WWWROOT, 'nonexistent')
            return os.path.join(BUNDLE, rel)
        return super().translate_path(path)

    def end_headers(self):
        # Cache-Control: no-store — a browser-cached stale app is
        # indistinguishable from a broken one.
        self.send_header('Cache-Control', 'no-store')
        super().end_headers()

    def log_message(self, fmt, *args):
        sys.stderr.write("%s - %s\n" % (self.address_string(), fmt % args))

http.server.ThreadingHTTPServer(
    ('127.0.0.1', int(sys.argv[1])),
    functools.partial(DemoHandler, directory=WWWROOT)).serve_forever()
PYEOF
python3 "$LOG_DIR/serve.py" "$PORT_HTTP" "$PUBLISH_DIR/wwwroot" "$OUT_DIR" > "$LOG_DIR/http.log" 2>&1 &
PIDS+=($!)

# The port must be OURS serving THIS build: a leftover server from an old demo
# session squatting the port serves a stale app and looks exactly like "nothing
# changed". curl build-rev.txt and require this publish's rev.
sleep 2
SERVED_REV="$(curl -sf "http://127.0.0.1:$PORT_HTTP/build-rev.txt" 2>/dev/null | tr -d '[:space:]' || true)"
# The game content must be fetchable over HTTP, not merely present on disk:
# symlink handling and server roots have both broken this, and the only symptom
# is a 404 inside the running game.
BUNDLE_CODE="$(curl -s -o /dev/null -w '%{http_code}' -r 0-1 \
    "http://127.0.0.1:$PORT_HTTP/tso-content/content.tar.gz" 2>/dev/null)"
BUNDLE_CODE="${BUNDLE_CODE:-000}"
case "$BUNDLE_CODE" in
    200|206) ;;
    *)
        echo "FATAL: the game content bundle is not being served (HTTP $BUNDLE_CODE)." >&2
        echo "  url:  http://127.0.0.1:$PORT_HTTP/tso-content/content.tar.gz" >&2
        echo "  file: $OUT_DIR/content.tar.gz" >&2
        ls -l "$OUT_DIR" 2>&1 | sed 's/^/    /' >&2
        tail -3 "$LOG_DIR/http.log" 2>/dev/null | sed 's/^/    /' >&2
        exit 1
        ;;
esac

if [ "$SERVED_REV" != "$REV" ]; then
    echo "FATAL: :$PORT_HTTP is serving build '${SERVED_REV:-nothing}' instead of '$REV'." >&2
    echo "  Another (old) server owns the port — dev servers show up as plain" >&2
    echo "  'dotnet', so kill by PORT, not by name, then rerun:" >&2
    echo "    lsof -ti :$PORT_HTTP | xargs kill -9" >&2
    exit 1
fi

# 5. Readiness. Every service must be PROVEN listening: a gateway that lost its
# port to a leftover process used to die here while the script still announced
# "Demo is up", and the only symptom was ws errors in the browser minutes later.
wait_tcp() { # host port seconds
    local end=$((SECONDS + $3))
    while [ $SECONDS -lt $end ]; do
        (exec 3<>"/dev/tcp/$1/$2") 2>/dev/null && { exec 3<&- 3>&-; return 0; }
        sleep 1
    done
    return 1
}
service_died() { # name logfile
    echo "FATAL: $1 died — last lines of $2:" >&2
    tail -15 "$2" >&2
    echo "  If it says 'address already in use', a process from an earlier run owns" >&2
    echo "  the port (dev servers show up as plain 'dotnet' — kill by PORT, not name):" >&2
    echo "    lsof -ti :$PORT_HTTP :$PORT_GATEWAY :$PORT_SANDBOX | xargs kill -9" >&2
    exit 1
}

# The lot host is the slow one (content boot ~15s).
for i in $(seq 1 60); do
    grep -q "ticking 30Hz" "$LOG_DIR/host.log" 2>/dev/null && break
    kill -0 "${PIDS[0]}" 2>/dev/null || service_died "lot host" "$LOG_DIR/host.log"
    sleep 2
done
grep -q "ticking 30Hz" "$LOG_DIR/host.log" || { echo "FATAL: lot host never became ready" >&2; exit 1; }
grep -m1 "furnished" "$LOG_DIR/host.log" || true

kill -0 "${PIDS[1]}" 2>/dev/null || service_died "gateway" "$LOG_DIR/gateway.log"
wait_tcp 127.0.0.1 "$PORT_SANDBOX" 15 || service_died "lot host (not listening on $PORT_SANDBOX)" "$LOG_DIR/host.log"
wait_tcp 127.0.0.1 "$PORT_GATEWAY" 15 || service_died "gateway (not listening on $PORT_GATEWAY)" "$LOG_DIR/gateway.log"

echo
echo "============================================================"
echo "  Demo is up. Open (twice, for multiplayer):"
echo
echo "      http://127.0.0.1:$PORT_HTTP/?vm=1&name=you"
echo
echo "  Click furniture for its pie menu; chat box bottom-left."
echo
echo "  THIS TERMINAL IS NOW BUSY running the demo. Anything you type here is"
echo "  queued, not run — open a new tab (Cmd+T) for other commands, e.g."
echo "      ./PackTools/tools/run_browser_demo.sh --doctor"
echo "  Ctrl-C here stops everything. Logs: $LOG_DIR/{host,gateway,http}.log"
echo "============================================================"
wait

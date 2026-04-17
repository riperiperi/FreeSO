#!/usr/bin/env bash
# admit-agent.sh — one-command admission helper for freeso-sidecar.
#
# Usage:
#   scripts/admit-agent.sh <agent-name> [--cf-home <path>] [--campfire-id <hex>]
#
# What it does (on the sidecar host):
#   1) Reads the sidecar's campfire id from FREESO_CF_CAMPFIRE or from an
#      .admit-info file the sidecar writes at $CF_HOME/.admit-info.
#   2) Creates a fresh identity for the agent under ./agents/<agent-name>/cf.
#   3) Reads that identity's public key.
#   4) Runs `cf admit <campfire-id> <pk>` against the sidecar's CF_HOME.
#   5) Emits a ready-to-run `cf join <campfire-id>` line with the right
#      CF_HOME= exported for the agent shell.
#
# The ergonomic contract: one command, zero hand-assembled cf invocations.
# Operator copies the final "agent commands" block into the agent machine's
# shell and the agent is online.
set -euo pipefail

err() { printf 'admit-agent.sh: %s\n' "$*" >&2; }

AGENT_NAME=${1:-}
if [ -z "$AGENT_NAME" ]; then
    err "usage: $0 <agent-name> [--cf-home <path>] [--campfire-id <hex>]"
    exit 2
fi
shift || true

SIDECAR_CF_HOME=${FREESO_SIDECAR_CF_HOME:-./bot-data}
CAMPFIRE_ID=${FREESO_CF_CAMPFIRE:-}

while [ $# -gt 0 ]; do
    case "$1" in
        --cf-home) SIDECAR_CF_HOME="$2"; shift 2;;
        --campfire-id) CAMPFIRE_ID="$2"; shift 2;;
        *) err "unknown flag: $1"; exit 2;;
    esac
done

if ! command -v cf >/dev/null 2>&1; then
    err "cf not on PATH"
    exit 1
fi

if [ -z "$CAMPFIRE_ID" ]; then
    # Try to recover id from the sidecar log / admit-info file.
    if [ -f "$SIDECAR_CF_HOME/.admit-info" ]; then
        CAMPFIRE_ID=$(grep -E '^CAMPFIRE_ID=' "$SIDECAR_CF_HOME/.admit-info" | cut -d= -f2)
    fi
fi

if [ -z "$CAMPFIRE_ID" ]; then
    err "no campfire id: set FREESO_CF_CAMPFIRE or pass --campfire-id <hex>"
    exit 2
fi

AGENT_DIR="./agents/$AGENT_NAME/cf"
mkdir -p "$AGENT_DIR"

# Create the agent identity if absent (idempotent — `cf init` is a no-op if
# identity.json already exists).
CF_HOME="$AGENT_DIR" cf init >/dev/null 2>&1 || true

AGENT_PK=$(CF_HOME="$AGENT_DIR" cf id)
if [ -z "$AGENT_PK" ]; then
    err "cf id returned empty key in $AGENT_DIR"
    exit 1
fi

printf 'Admitting agent %s (%s) into campfire %s...\n' "$AGENT_NAME" "${AGENT_PK:0:16}..." "$CAMPFIRE_ID"
CF_HOME="$SIDECAR_CF_HOME" cf admit "$CAMPFIRE_ID" "$AGENT_PK"

cat <<EOF

=== Agent commands (copy-paste on the agent host) ===
export CF_HOME=$(readlink -f "$AGENT_DIR")
cf join $CAMPFIRE_ID
# Now the agent can:
#   cf read $CAMPFIRE_ID --all --tag convention:operation   # discover the verb surface
#   cf read $CAMPFIRE_ID --tag freeso:perception --follow   # perceive the body
#   cf $CAMPFIRE_ID <op> --arg ...                          # act
=== / Agent commands ===
EOF

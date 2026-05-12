#!/usr/bin/env bash
#
# One-time destructive reset for the EdenSO shard.
#
# What this does:
#   1. Snapshots a list of avatars who currently own / co-own a lot
#   2. Truncates fso_objects (clears every owned object — on lots AND in inventories)
#   3. Clears all lot-ownership tables (fso_lots / fso_roommates / fso_lot_admit /
#      fso_lot_visits / fso_lot_visit_totals / fso_lot_top_100 / fso_lot_claims)
#   4. Credits each former lot-owning avatar with +100,000 simoleons
#   5. Wipes daily-quest progress (fso_daily_quests / fso_action_log)
#   6. Removes the persisted lot files under /opt/freeso/nfs/Lots/
#
# What this preserves:
#   - fso_avatars (avatar exists, budget gets the +100k where applicable)
#   - fso_users (accounts intact)
#   - fso_inbox (mail history)
#   - fso_neighborhoods (city geography)
#   - fso_bonus (mayor / property bonus history — leaving payouts intact)
#   - Avatar outfits, skills, motives — all kept
#
# Stops the freeso-server service before changing DB, restarts after.
# Takes a full mysqldump beforehand. Asks for explicit yes-confirmation.

set -euo pipefail

DB="fso"
BACKUP_DIR="/opt/freeso/backups"
LOT_DATA_DIR="/opt/freeso/nfs/Lots"
SERVICE="freeso-server.service"

red()    { printf '\033[31m%s\033[0m\n' "$*"; }
yellow() { printf '\033[33m%s\033[0m\n' "$*"; }
green()  { printf '\033[32m%s\033[0m\n' "$*"; }

if [[ $EUID -ne 0 ]]; then
    red "Must be run as root (sudo)." >&2
    exit 1
fi

red    "=========================================================="
red    " EdenSO shard state reset — DESTRUCTIVE."
red    "=========================================================="
yellow ""
yellow "  This will:"
yellow "    * Wipe every owned object on lots AND in player inventories"
yellow "    * Wipe every lot ownership (roommates, admit lists, visit history)"
yellow "    * Delete all lot save files under $LOT_DATA_DIR"
yellow "    * Truncate the daily-quest tables for a clean start"
yellow ""
yellow "  Player accounts, avatars, skills, motives, outfits, mail, and"
yellow "  budgets are PRESERVED. Each former lot owner gets +§100,000."
yellow ""
read -r -p "Type 'reset' to continue, anything else to abort: " confirm
if [[ "$confirm" != "reset" ]]; then
    echo "Aborted."
    exit 0
fi

# Pre-flight ----------------------------------------------------------
mkdir -p "$BACKUP_DIR"
TS=$(date -u +%Y%m%dT%H%M%SZ)
DUMP="$BACKUP_DIR/pre-reset-${TS}.sql.gz"

green ""
green "[1/6] Taking full DB backup → $DUMP"
mysqldump --single-transaction --quick --routines "$DB" | gzip > "$DUMP"
ls -lh "$DUMP"

green ""
green "[2/6] Stopping $SERVICE"
systemctl stop "$SERVICE"

# DB wipe ------------------------------------------------------------
SQL_FILE="$(mktemp)"
cat > "$SQL_FILE" <<'SQL'
START TRANSACTION;

-- (a) snapshot former owners — anyone with a roommate row counts as a
-- co-owner. Visitors / past-visitors don't count.
DROP TEMPORARY TABLE IF EXISTS _former_owners;
CREATE TEMPORARY TABLE _former_owners AS
SELECT DISTINCT avatar_id FROM fso_roommates WHERE is_pending = 0;

-- (b) compensate former owners with §100,000
UPDATE fso_avatars a
  JOIN _former_owners f ON f.avatar_id = a.avatar_id
   SET a.budget = a.budget + 100000;

-- (c) wipe every owned object — both lot inventories and avatar
-- inventories live here
DELETE FROM fso_objects;

-- (d) wipe lot membership / visit history / rankings
DELETE FROM fso_roommates;
DELETE FROM fso_lot_admit;
DELETE FROM fso_lot_visits;
DELETE FROM fso_lot_visit_totals;
DELETE FROM fso_lot_top_100;
DELETE FROM fso_lot_claims;
DELETE FROM fso_lots;

-- (e) wipe daily-quest state so post-reset quests roll cleanly
DELETE FROM fso_daily_quests;
DELETE FROM fso_action_log;

DROP TEMPORARY TABLE _former_owners;

COMMIT;
SQL

green ""
green "[3/6] Applying reset SQL"
mysql "$DB" < "$SQL_FILE"
rm -f "$SQL_FILE"

green ""
green "[4/6] Verifying:"
mysql "$DB" -e "
SELECT 'fso_objects rows'       AS what, COUNT(*) AS n FROM fso_objects
UNION ALL SELECT 'fso_lots rows',           COUNT(*) FROM fso_lots
UNION ALL SELECT 'fso_roommates rows',      COUNT(*) FROM fso_roommates
UNION ALL SELECT 'fso_daily_quests rows',   COUNT(*) FROM fso_daily_quests
UNION ALL SELECT 'fso_action_log rows',     COUNT(*) FROM fso_action_log;
"

green ""
green "[5/6] Deleting lot save files under $LOT_DATA_DIR"
if [[ -d "$LOT_DATA_DIR" ]]; then
    FILES_BEFORE=$(find "$LOT_DATA_DIR" -type f | wc -l)
    find "$LOT_DATA_DIR" -mindepth 1 -delete
    FILES_AFTER=$(find "$LOT_DATA_DIR" -type f | wc -l)
    echo "  removed $((FILES_BEFORE - FILES_AFTER)) files (was $FILES_BEFORE)"
else
    yellow "  $LOT_DATA_DIR doesn't exist — skipping"
fi

green ""
green "[6/6] Restarting $SERVICE"
systemctl start "$SERVICE"
sleep 3
if systemctl is-active --quiet "$SERVICE"; then
    green "  service is up"
else
    red   "  service failed to start — check journalctl -u $SERVICE"
    exit 1
fi

echo
green "Reset complete. Backup retained at $DUMP."
green "Players will see a fresh city next time they log in."
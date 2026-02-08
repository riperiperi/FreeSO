#!/usr/bin/env python3
"""Create an archive template from a running FreeSO Docker server.

Usage: ./create-archive.py [archive-name]

Prerequisites:
  - Docker compose stack running (mariadb + freeso-server)

Output:
  - docker/archives/archive.zip                        (distributable archive)
  - ArchiveCities/<name>/archive.ini                   (archive manifest)
"""

import functools
import http.server
import re
import shutil
import sqlite3
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
REPO_DIR = SCRIPT_DIR.parent
COMPOSE_FILE = SCRIPT_DIR / "docker-compose.yml"
NFS_DIR = SCRIPT_DIR / "nfs"
ARCHIVE_CITIES_DIR = REPO_DIR / "TSOClient" / "tso.client" / "Content" / "ArchiveCities"

DB_USER, DB_PASS, DB_NAME = "fsoserver", "password", "fso"
SERVE_PORT = 8080

# True color (24-bit) ANSI
BOLD, DIM, RESET = "\033[1m", "\033[2m", "\033[0m"
MINT = "\033[38;2;102;217;178m"
SKY = "\033[38;2;125;196;253m"
LAVENDER = "\033[38;2;180;160;255m"
CORAL = "\033[38;2;255;107;107m"
AMBER = "\033[38;2;255;206;84m"
SLATE = "\033[38;2;140;150;168m"
WHITE = "\033[38;2;235;235;240m"


def box(title, color=SKY):
    w = len(title) + 4
    print(f"  {color}\u256d{'\u2500' * w}\u256e{RESET}")
    print(f"  {color}\u2502{RESET}  {WHITE}{BOLD}{title}{RESET}  {color}\u2502{RESET}")
    print(f"  {color}\u2570{'\u2500' * w}\u256f{RESET}")


def header(text):   print(f"  {LAVENDER}\u25b6{RESET} {BOLD}{text}{RESET}")
def item(text, color=SLATE): print(f"  {SLATE}\u2502{RESET}   {color}{text}{RESET}")
def success(text):  print(f"  {MINT}\u2714{RESET} {text}")
def warning(text):  print(f"  {AMBER}\u26a0{RESET} {AMBER}{text}{RESET}")
def fail_msg(text): print(f"  {CORAL}\u2718{RESET} {CORAL}{BOLD}{text}{RESET}")
def info(label, value): print(f"  {SLATE}{label}:{RESET} {SKY}{value}{RESET}")
def divider(): print(f"  {DIM}{'\u2500' * 36}{RESET}")

# Table import order (respects foreign key dependencies)
IMPORT_ORDER = [
    "fso_auth_tickets", "fso_db_changes", "fso_dyn_payouts", "fso_email_confirm",
    "fso_events", "fso_lot_claims", "fso_relationships", "fso_shard_tickets",
    "fso_transactions", "fso_tuning", "fso_update_addons", "fso_update_branch",
    "fso_updates", "fso_users", "fso_shards", "fso_user_authenticate",
    "fso_auth_attempts", "fso_lots", "fso_election_cycles", "fso_avatars",
    "fso_event_participation", "fso_neighborhoods",
    "fso_bonus", "fso_bookmarks", "fso_avatar_claims", "fso_bulletin_posts",
    "fso_election_candidates", "fso_election_cyclemail", "fso_election_freevotes",
    "fso_election_votes", "fso_generic_avatar_participation", "fso_global_cooldowns",
    "fso_inbox", "fso_ip_ban", "fso_joblevels", "fso_lot_admit", "fso_lot_top_100",
    "fso_lot_visit_totals", "fso_lot_visits", "fso_mayor_ratings", "fso_nhood_ban",
    "fso_objects", "fso_roommates", "fso_tasks",
    "fso_object_attributes", "fso_lot_server_tickets",
    "fso_outfits", "fso_hosts", "fso_tuning_presets", "fso_tuning_preset_items",
]

# MySQL -> SQLite type mapping (order: longest prefix first to avoid partial matches)
TYPE_RULES = [
    (r'\bbigint\(\d+\)', 'INTEGER'),   (r'\bsmallint\(\d+\)', 'INTEGER'),
    (r'\btinyint\(\d+\)', 'INTEGER'),   (r'\bint\(\d+\)', 'INTEGER'),
    (r'\bvarbinary\(\d+\)', 'BLOB'),    (r'\bbinary\(\d+\)', 'BLOB'),
    (r'\bvarchar\(\d+\)', 'TEXT'),      (r'\bmediumblob\b', 'BLOB'),
    (r'\blongblob\b', 'BLOB'),          (r'\bblob\b', 'BLOB'),
    (r'\bmediumtext\b', 'TEXT'),        (r'\blongtext\b', 'TEXT'),
    (r'\btext\b', 'TEXT'),              (r'\bdouble\b', 'REAL'),
    (r'\bfloat\b', 'REAL'),            (r'\bdatetime\b', 'TEXT'),
]

# SQLite triggers (from SqliteFunctions.cs, minus the avatar count limit which archive removes)
SQLITE_TRIGGERS = [
    """CREATE TRIGGER IF NOT EXISTS `fso_avatars_BEFORE_UPDATE` BEFORE UPDATE ON `fso_avatars` FOR EACH ROW BEGIN
     SELECT CASE WHEN NEW.budget<0 THEN RAISE (ABORT, 'Transaction would cause avatar to have negative budget.') END;
    END""",
    """CREATE TRIGGER IF NOT EXISTS `fso_objects_BEFORE_UPDATE` BEFORE UPDATE ON `fso_objects` FOR EACH ROW BEGIN
     SELECT CASE WHEN NEW.budget<0 THEN RAISE (ABORT, 'Transaction would cause object to have negative budget.') END;
    END""",
    """CREATE TRIGGER IF NOT EXISTS `fso_roommates_BEFORE_INSERT` BEFORE INSERT ON `fso_roommates` FOR EACH ROW BEGIN
     SELECT CASE WHEN (SELECT COUNT(*) FROM fso_roommates a WHERE NEW.avatar_id = a.avatar_id) > 0 THEN
      RAISE (ABORT, 'Cannot be a roommate of more than one lot.') END;
     SELECT CASE WHEN (SELECT COUNT(*) FROM fso_roommates a WHERE NEW.lot_id = a.lot_id) >= 8 THEN
      RAISE (ABORT, 'Cannot have more than 8 roommates in a lot.') END;
    END""",
    """CREATE TRIGGER IF NOT EXISTS `fso_outfits_before_insert` BEFORE INSERT ON `fso_outfits` FOR EACH ROW BEGIN
     SELECT CASE WHEN NEW.object_owner IS NOT NULL AND (SELECT COUNT(*) FROM fso_outfits o WHERE NEW.object_owner = o.object_owner) >= 20 THEN
      RAISE (ABORT, 'Cannot have more than 20 outfits in a rack.') END;
    END""",
    """CREATE TRIGGER IF NOT EXISTS `fso_outfits_before_update` BEFORE UPDATE ON `fso_outfits` FOR EACH ROW BEGIN
     SELECT CASE WHEN NEW.avatar_owner IS NOT NULL AND (SELECT COUNT(*) FROM fso_outfits o WHERE NEW.avatar_owner = o.avatar_owner AND o.outfit_type = NEW.outfit_type) >= 5 THEN
      RAISE (ABORT, 'Cannot have more than 5 outfits per category in backpack.') END;
    END""",
    """CREATE TRIGGER IF NOT EXISTS `fso_bonus_after_insert` AFTER INSERT ON `fso_bonus` FOR EACH ROW BEGIN
     UPDATE fso_avatars SET budget = (budget + IFNULL(NEW.bonus_visitor,0) + IFNULL(NEW.bonus_property,0) + IFNULL(NEW.bonus_sim,0)) WHERE avatar_id = NEW.avatar_id;
    END""",
    """CREATE TRIGGER IF NOT EXISTS `fso_election_votes_BEFORE_INSERT` BEFORE INSERT ON `fso_election_votes` FOR EACH ROW BEGIN
     SELECT CASE WHEN (SELECT COUNT(*) from fso_election_votes v INNER JOIN fso_avatars va ON v.from_avatar_id = va.avatar_id
      WHERE v.election_cycle_id = NEW.election_cycle_id AND v.type = NEW.type AND va.user_id IN
       (SELECT user_id FROM fso_users WHERE last_ip =
        (SELECT last_ip FROM fso_avatars a JOIN fso_users u on a.user_id = u.user_id WHERE avatar_id = NEW.from_avatar_id)
       )) > 0 THEN RAISE (ABORT, 'A vote from this person or someone related already exists for this cycle.') END;
    END""",
]

# Archive conversion SQL (from ToolArchiveConvert.cs)
ARCHIVE_SQL = """
ALTER TABLE `fso_users` ADD COLUMN `display_name` TEXT NOT NULL DEFAULT '0';
ALTER TABLE `fso_users` ADD COLUMN `is_verified` INTEGER NOT NULL DEFAULT 1;
ALTER TABLE `fso_users` ADD COLUMN `shared_user` INTEGER NOT NULL DEFAULT 1;
CREATE INDEX IF NOT EXISTS `fso_users_display_name` ON `fso_users`(`display_name`);

CREATE TABLE IF NOT EXISTS `fso_archive_featured` (
    `id` INTEGER, `name` TEXT NOT NULL, `lot_id` INTEGER NOT NULL,
    `category` INTEGER NOT NULL, `description` TEXT NOT NULL, `shard_id` INTEGER NOT NULL,
    PRIMARY KEY(`id` AUTOINCREMENT),
    CONSTRAINT `fso_featured_shard_fk` FOREIGN KEY(`shard_id`) REFERENCES `fso_shards`(`shard_id`) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS fso_archive_featured_category_shard_idx ON fso_archive_featured (category, shard_id);

CREATE TABLE IF NOT EXISTS `fso_archive_recents` (
    `user_id` INTEGER NOT NULL, `avatar_id` INTEGER NOT NULL,
    `last_timestamp` datetime NOT NULL DEFAULT current_timestamp,
    PRIMARY KEY(`user_id`, `avatar_id`),
    CONSTRAINT `fso_recent_user_fk` FOREIGN KEY(`user_id`) REFERENCES `fso_users`(`user_id`) ON DELETE CASCADE,
    CONSTRAINT `fso_recent_avatar_fk` FOREIGN KEY(`avatar_id`) REFERENCES `fso_avatars`(`avatar_id`) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS fso_archive_recents_user_idx ON fso_archive_recents (user_id);

DROP TRIGGER IF EXISTS `fso_avatars_BEFORE_INSERT`;
UPDATE `fso_users` SET username='archive', register_date=0, email='unused', register_ip='0', last_ip='0', client_id='0', last_login=0 WHERE user_id=1;
UPDATE `fso_avatars` SET user_id=1;
DELETE FROM `fso_event_participation`; DELETE FROM `fso_global_cooldowns`;
DELETE FROM `fso_users` WHERE user_id != 1; DELETE FROM `fso_user_authenticate`;
DELETE FROM `fso_auth_attempts`; DELETE FROM `fso_auth_tickets`;
DELETE FROM `fso_lot_server_tickets`; DELETE FROM `fso_shard_tickets`;
DELETE FROM `fso_tasks`; DELETE FROM `fso_transactions`;
DELETE FROM `fso_avatar_claims`; DELETE FROM `fso_lot_claims`;
VACUUM;
"""


def docker_compose(*args, **kwargs):
    return subprocess.run(
        ["docker", "compose", "-f", str(COMPOSE_FILE), *args],
        capture_output=True, **kwargs
    )


def mariadb_exec(sql):
    r = docker_compose("exec", "-T", "mariadb", "mariadb",
                        f"-u{DB_USER}", f"-p{DB_PASS}", "-N", "-e", sql, DB_NAME, text=True)
    return r.stdout.strip().split('\n') if r.stdout.strip() else []


def mariadb_dump(table):
    r = docker_compose("exec", "-T", "mariadb", "mariadb-dump",
                        f"-u{DB_USER}", f"-p{DB_PASS}", "--skip-comments",
                        "--hex-blob", DB_NAME, table)
    return r.stdout.decode('utf-8', errors='replace')


def convert_column(name, rest, has_ai):
    """Convert a MySQL column definition to SQLite."""
    rest = rest.rstrip(',')

    # Strip COMMENT first (always last clause, may contain escaped quotes)
    rest = re.sub(r"\s*COMMENT\s+'.*", '', rest)

    if 'AUTO_INCREMENT' in rest:
        rest = re.sub(r'\w+\([^)]*\)', 'INTEGER', rest, count=1)
        rest = re.sub(r'\s*unsigned\b', '', rest)
        rest = rest.replace('AUTO_INCREMENT', 'PRIMARY KEY AUTOINCREMENT')
        return f'  {name} {re.sub(r" +", " ", rest).strip()},', True

    # Enum before type rules
    m = re.search(r"enum\(([^)]+)\)", rest)
    if m:
        col = name.strip('`')
        rest = re.sub(r"enum\([^)]+\)", f"TEXT CHECK(`{col}` IN ({m.group(1)}))", rest)

    for pattern, repl in TYPE_RULES:
        rest = re.sub(pattern, repl, rest)

    rest = re.sub(r'\s*unsigned\b', '', rest)
    rest = rest.replace(' zerofill', '')
    rest = re.sub(r'\s*ON UPDATE current_timestamp\(\)', '', rest)
    rest = rest.replace('current_timestamp()', 'current_timestamp')

    return f'  {name} {re.sub(r" +", " ", rest).strip()},', has_ai


def fix_hex(s):
    """Convert MySQL 0xABCD hex literals to SQLite X'ABCD' format."""
    return re.sub(r"0x([0-9A-Fa-f]+)", lambda m: f"X'{m.group(1)}'", s)


def mysql_to_sqlite(sql):
    """Convert a MariaDB table dump to SQLite-compatible SQL."""
    lines = sql.replace('\r', '').split('\n')
    out, indexes = [], []
    in_create = in_delim = in_insert = False
    table_id = ""
    has_ai = False

    for line in lines:
        s = line.strip()

        # Skip DELIMITER blocks (MySQL trigger definitions)
        if s.startswith('DELIMITER'):
            in_delim = not in_delim
            continue
        if in_delim:
            continue

        # Skip MySQL control lines
        if s.startswith(('/*', '--', 'LOCK ', 'UNLOCK ', 'DROP TABLE', 'set auto', 'commit')):
            continue
        if s in ('', ';', ';;'):
            continue

        # Multi-line INSERT continuation
        if in_insert:
            out.append(fix_hex(s.replace("\\'", "''").replace('\\"', '"').replace('\\\\', '\\')))
            if s.endswith(';'):
                in_insert = False
            continue

        if in_create:
            if s.startswith(') ENGINE=') or s.startswith(') DEFAULT'):
                in_create = False
                if out and out[-1].endswith(','):
                    out[-1] = out[-1][:-1]
                out.append(');')
                for idx_name, idx_cols in indexes:
                    out.append(f"CREATE INDEX {idx_name} ON {table_id}{idx_cols};")
                continue

            # KEY / INDEX lines
            km = re.match(r'\s*((?P<kt>[A-Z]+)\s)?KEY(\s(?P<n>`[^`]+`))?\s(?P<c>\([^)]+\)),?$', s)
            if km:
                kt, n, c = km.group('kt') or '', km.group('n') or '', re.sub(r'`\(\d+\)', '`', km.group('c'))
                if kt == 'PRIMARY' and not has_ai:
                    out.append(f'  PRIMARY KEY {c},')
                elif kt == 'UNIQUE':
                    out.append(f'  UNIQUE {c},')
                elif kt == '':
                    indexes.append((n, c))
                continue

            # CONSTRAINT (foreign keys)
            if re.match(r'\s*CONSTRAINT\s', s):
                out.append(f'  {s.rstrip(",")},')
                continue

            # Column definition
            cm = re.match(r'\s*(`[^`]+`)\s+(.+)$', s)
            if cm:
                col_line, has_ai = convert_column(cm.group(1), cm.group(2), has_ai)
                out.append(col_line)
            continue

        # CREATE TABLE
        ct = re.match(r'CREATE TABLE\s+(`[^`]+`)\s*\(', s)
        if ct:
            in_create, table_id, has_ai, indexes = True, ct.group(1), False, []
            out.append(f'CREATE TABLE IF NOT EXISTS {table_id} (')
            continue

        # INSERT INTO
        if s.startswith('INSERT INTO'):
            out.append(fix_hex(s.replace("\\'", "''").replace('\\"', '"').replace('\\\\', '\\')))
            if not s.endswith(';'):
                in_insert = True
            continue

    return '\n'.join(out)


def main():
    args = sys.argv[1:]
    if "-h" in args or "--help" in args:
        print()
        print(f"  {WHITE}{BOLD}Usage:{RESET}")
        print(f"    ./create-archive.py {SLATE}[options] [name]{RESET}")
        print()
        print(f"  {WHITE}{BOLD}Arguments:{RESET}")
        print(f"    {SKY}name{RESET}              {SLATE}Archive name (default: 'FreeSO Archive'){RESET}")
        print()
        print(f"  {WHITE}{BOLD}Options:{RESET}")
        print(f"    {SKY}--no-serve{RESET}        {SLATE}Don't start HTTP server after building{RESET}")
        print(f"    {SKY}--dry-run{RESET}         {SLATE}Show what would be done without making changes{RESET}")
        print(f"    {SKY}-h, --help{RESET}        {SLATE}Show this help{RESET}")
        print()
        print(f"  {WHITE}{BOLD}Requires:{RESET}")
        print(f"    Docker compose stack running {SLATE}(mariadb + freeso-server){RESET}")
        print(f"    Start with: {MINT}docker compose -f docker/docker-compose.yml up -d{RESET}")
        print()
        print(f"  {WHITE}{BOLD}Note:{RESET}")
        print(f"    This script places archive.ini in the ArchiveCities/ folder.")
        print(f"    Rebuild the game so it gets included in the build output.")
        print()
        print(f"  {WHITE}{BOLD}Output:{RESET}")
        print(f"    {SLATE}docker/archives/archive.zip{RESET}            {LAVENDER}Archive served to clients over HTTP{RESET}")
        print(f"    {SLATE}ArchiveCities/<name>/archive.ini{RESET}   {LAVENDER}Manifest in ArchiveCities/ folder{RESET}")
        print()
        sys.exit(0)

    no_serve = "--no-serve" in args
    dry_run = "--dry-run" in args
    name_args = [a for a in args if not a.startswith("--")]
    name = name_args[0] if name_args else "FreeSO Archive"
    out_dir = SCRIPT_DIR / "archives"
    out_dir.mkdir(exist_ok=True)
    zip_path = out_dir / "archive.zip"
    install_dir = ARCHIVE_CITIES_DIR / name

    print()
    box("FreeSO Archive Creator")
    info("Archive", name)
    if dry_run:
        print(f"  {AMBER}{BOLD}DRY RUN{RESET} {SLATE}\u2014 no changes will be made{RESET}")
    print()

    # Check Docker
    r = docker_compose("ps", "--status", "running", text=True)
    if "mariadb" not in r.stdout:
        fail_msg("MariaDB container not running")
        sys.exit(f"  {DIM}Start with: docker compose up -d{RESET}")
    success("Docker stack running")

    if dry_run:
        tables = [t.strip() for t in mariadb_exec("SHOW TABLES") if t.strip()]
        divider()
        header(f"Would import {len(tables)} tables")
        for t in IMPORT_ORDER:
            if t in tables:
                item(t)
        divider()
        success(f"Would add {len(SQLITE_TRIGGERS)} triggers")
        success("Would convert to archive format")
        divider()
        header("Would copy NFS data")
        for folder in ("Lots", "Objects"):
            src = NFS_DIR / folder
            if src.exists() and any(src.iterdir()):
                item(f"{folder}/")
        divider()
        success(f"Would create {SKY}{zip_path}{RESET}")
        success(f"Would install to {SKY}{install_dir}/{RESET}")
        print()
        print(f"  {MINT}{BOLD}\u2714 Dry run complete{RESET}")
        print()
        sys.exit(0)

    divider()

    # Build in a temp directory, output only zip + archive.ini
    with tempfile.TemporaryDirectory() as tmp:
        tmp_dir = Path(tmp)
        (tmp_dir / "Lots").mkdir()
        (tmp_dir / "Objects").mkdir()
        db_path = tmp_dir / "fsoarchive.db"

        # Dump MariaDB and import into SQLite
        tables = [t.strip() for t in mariadb_exec("SHOW TABLES") if t.strip()]
        header(f"Importing {len(tables)} tables")

        conn = sqlite3.connect(str(db_path))
        conn.execute("PRAGMA journal_mode=WAL")
        conn.execute("PRAGMA foreign_keys=OFF")

        dumps = {}
        for t in tables:
            dumps[t] = mariadb_dump(t)

        ok, errors = 0, 0
        for t in IMPORT_ORDER:
            if t not in dumps:
                continue
            sql = mysql_to_sqlite(dumps[t])
            if not sql.strip():
                continue
            try:
                conn.executescript(sql)
                ok += 1
                item(t)
            except Exception as e:
                errors += 1
                item(f"{t} \u2014 {e}", CORAL)
                (out_dir / f"{t}.debug.sql").write_text(sql)

        try:
            conn.execute("ALTER TABLE fso_objects ADD COLUMN inventory_state BLOB DEFAULT NULL")
        except sqlite3.OperationalError:
            pass  # column already exists

        if errors:
            warning(f"{ok}/{ok+errors} tables ({errors} failed \u2014 see *.debug.sql)")
        else:
            success(f"{ok}/{ok+errors} tables imported")
        divider()

        # Add triggers
        for trigger in SQLITE_TRIGGERS:
            conn.execute(trigger)
        success(f"{len(SQLITE_TRIGGERS)} triggers added")

        # Archive conversion
        conn.execute("PRAGMA foreign_keys=OFF")
        conn.executescript(ARCHIVE_SQL)
        conn.execute("PRAGMA foreign_keys=ON")
        conn.commit()
        conn.close()
        success("Converted to archive format")
        divider()

        # Copy NFS data
        header("Copying NFS data")
        copied_any = False
        for folder in ("Lots", "Objects"):
            src = NFS_DIR / folder
            if src.exists() and any(src.iterdir()):
                shutil.copytree(src, tmp_dir / folder, dirs_exist_ok=True)
                count = sum(1 for _ in (tmp_dir / folder).rglob('*') if _.is_file())
                item(f"{folder}/ ({count} files)")
                copied_any = True
        if not copied_any:
            item("(no NFS data found)")
        divider()

        # Create archive.zip
        with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zf:
            for f in tmp_dir.rglob('*'):
                if f.is_file():
                    zf.write(f, f.relative_to(tmp_dir))
        zip_mb = zip_path.stat().st_size / (1024 * 1024)
        success(f"Created archive.zip ({zip_mb:.1f} MB)")

        # Write archive.ini to ArchiveCities
        install_dir.mkdir(parents=True, exist_ok=True)
        for f in (install_dir / "archive.ini", install_dir / "data"):
            if f.is_file():
                f.unlink()
            elif f.is_dir():
                shutil.rmtree(f)

        data_size = sum(f.stat().st_size for f in tmp_dir.rglob('*') if f.is_file())
        zip_url = f"http://localhost:{SERVE_PORT}/archive.zip"
        archive_ini = (
            f"# Archive manifest\n[Default]\nName={name}\nDescription=Empty archive template for FreeSO\n"
            f"Size={data_size}\nMap=0100\nZipLocation={zip_url}\nZipHash=\nLocalDir="
        )
        (install_dir / "archive.ini").write_text(archive_ini)

    success(f"Installed to {SKY}{install_dir}/{RESET}")
    divider()

    if errors:
        print()
        box("\u26a0 Completed with errors", color=AMBER)
        print()
        sys.exit(1)

    print()
    print(f"  {MINT}{BOLD}\u2714 Archive ready!{RESET}")
    print()

    if no_serve:
        sys.exit(0)

    # Serve archive.zip over HTTP
    print(f"  {LAVENDER}\u25cf{RESET} Serving on {BOLD}http://localhost:{SERVE_PORT}/{RESET}")
    print(f"  {SLATE}Press Ctrl+C to stop{RESET}\n")
    handler = functools.partial(http.server.SimpleHTTPRequestHandler, directory=str(out_dir))
    with http.server.HTTPServer(("", SERVE_PORT), handler) as srv:
        try:
            srv.serve_forever()
        except KeyboardInterrupt:
            print(f"\n  {SLATE}Stopped.{RESET}")


if __name__ == "__main__":
    main()

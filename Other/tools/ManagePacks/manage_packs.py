#!/usr/bin/env python3
"""manage-packs — list/install/uninstall/reset content packs against a FreeSO source tree.

Reads packs from a content-repo (populated by CASOutfitImporter --save-to-repo),
inspects the source tree's TSOClient/FSO.Content.TSO/Content/Avatar/... to
determine what's installed, and applies idempotent operations.

Subcommands (all of which can also run interactively when called without args):
    refresh-vanilla     Re-extract baseline .col files from the user's FAR3
                        archives into content-repo/_vanilla/
    list                Print categories + state (installed / available / vanilla counts)
    install <pack>      Install a single pack by `<category>/<name>` path
    uninstall <pack>    Remove a previously installed pack
    reset <category>    Remove every pack we installed for this category, restore vanilla

Usage:
    ./manage-packs                              # interactive TUI
    ./manage-packs refresh-vanilla
    ./manage-packs list
    ./manage-packs install trunks/wedding/k8groupie
    ./manage-packs uninstall trunks/wedding/k8groupie
    ./manage-packs reset trunks/wedding
"""
from __future__ import annotations

import argparse
import dataclasses
import json
import os
import shutil
import struct
import sys
from pathlib import Path
from typing import Dict, List, Optional, Tuple


# ───────────────────────────────────────────────────────── config ──

DEFAULT_REPO = Path(__file__).resolve().parent.parent.parent.parent / "content-repo"


def load_config(repo: Path) -> dict:
    cfg_path = repo / "config.yaml"
    if not cfg_path.exists():
        die(f"no config.yaml at {cfg_path}; create one first (see template in docs)")
    return parse_yaml(cfg_path.read_text())


# Tiny YAML reader — keeps the tool stdlib-only. Handles only the subset our
# config uses: top-level scalars and a list of strings under `trunk_types:`.
def parse_yaml(text: str) -> dict:
    out: dict = {}
    cur_list: Optional[str] = None
    for raw in text.splitlines():
        line = raw.split("#", 1)[0].rstrip()
        if not line.strip():
            continue
        if line.startswith("  - "):
            if cur_list is None:
                die(f"YAML: list item without parent key: {raw!r}")
            out.setdefault(cur_list, []).append(line[4:].strip())
            continue
        if line.startswith(" "):
            die(f"YAML: unexpected indentation: {raw!r}")
        if ":" not in line:
            die(f"YAML: expected key:value, got {raw!r}")
        key, _, val = line.partition(":")
        key = key.strip()
        val = val.strip()
        if not val:
            cur_list = key
            continue
        cur_list = None
        out[key] = val.strip('"').strip("'")
    return out


# ───────────────────────────────────────────────────── format I/O ──

# Big-endian Collection (.col) format:
#   i32 count
#   count × (i32 index, u32 fileId, u32 typeId)
ColEntry = Tuple[int, int, int]  # (index, fileId, typeId)


def read_col(data: bytes) -> List[ColEntry]:
    n = struct.unpack(">i", data[:4])[0]
    out = []
    for i in range(n):
        idx, fid, tid = struct.unpack(">iII", data[4 + i * 12 : 4 + (i + 1) * 12])
        out.append((idx, fid, tid))
    return out


def write_col(entries: List[ColEntry]) -> bytes:
    # Sort deterministically by (typeId, fileId) so re-applying the same packs
    # always produces an identical .col — clean diffs.
    entries = sorted(entries, key=lambda e: (e[2], e[1]))
    buf = struct.pack(">i", len(entries))
    for i, (_, fid, tid) in enumerate(entries):
        buf += struct.pack(">iII", i, fid, tid)
    return buf


# FAR3 archive reader (read-only, raw + RefPack-compressed entries).
def read_far3_entries(path: Path) -> List[dict]:
    data = path.read_bytes()
    if data[:8] != b"FAR!byAZ":
        raise ValueError(f"{path} is not a FAR3 archive")
    if struct.unpack("<I", data[8:12])[0] != 3:
        raise ValueError(f"{path} unsupported FAR version")
    manifest_offset = struct.unpack("<I", data[12:16])[0]
    n = struct.unpack("<I", data[manifest_offset : manifest_offset + 4])[0]
    p = manifest_offset + 4
    entries = []
    for _ in range(n):
        decomp = struct.unpack("<I", data[p : p + 4])[0]
        p += 4
        comp = data[p] | (data[p + 1] << 8) | (data[p + 2] << 16)
        p += 3
        data_type = data[p]
        p += 1
        data_offset = struct.unpack("<I", data[p : p + 4])[0]
        p += 4
        is_compressed = data[p]
        p += 1
        access_number = data[p]
        p += 1
        fn_len = struct.unpack("<H", data[p : p + 2])[0]
        p += 2
        type_id = struct.unpack("<I", data[p : p + 4])[0]
        p += 4
        file_id = struct.unpack("<I", data[p : p + 4])[0]
        p += 4
        filename = data[p : p + fn_len].decode("ascii", errors="replace")
        p += fn_len
        entries.append({
            "filename": filename,
            "type_id": type_id, "file_id": file_id,
            "data_offset": data_offset,
            "is_compressed": is_compressed,
            "decomp_size": decomp, "comp_size": comp,
        })
    return entries


def far3_payload(path: Path, entry: dict) -> bytes:
    data = path.read_bytes()
    p = entry["data_offset"]
    if entry["is_compressed"] == 1:
        # Persist header: 9 reserved bytes + u32 filesize + u16 compressionId.
        filesize = struct.unpack("<I", data[p + 9 : p + 13])[0]
        compression_id = struct.unpack("<H", data[p + 13 : p + 15])[0]
        if compression_id == 0xFB10:
            d = data[p + 15 : p + 18]
            decomp_size = (d[0] << 16) | (d[1] << 8) | d[2]
            payload = data[p + 18 : p + 18 + filesize]
            return refpack_decompress(payload, decomp_size)
        # Compression flag set but actually raw — Maxis quirk.
        return data[p : p + entry["decomp_size"]]
    return data[p : p + entry["decomp_size"]]


def refpack_decompress(src: bytes, decompressed_size: int) -> bytes:
    """RefPack/QFS decoder. Mirror of the C# port we did earlier."""
    dst = bytearray(decompressed_size)
    sp = 0
    dp = 0
    while sp < len(src):
        c1 = src[sp]
        sp += 1
        if c1 <= 0x7F:
            c2 = src[sp]; sp += 1
            plain = c1 & 0x03
            dst[dp:dp + plain] = src[sp:sp + plain]
            dp += plain; sp += plain
            if dp == decompressed_size: break
            offset = ((c1 & 0x60) << 3) + c2 + 1
            n = ((c1 & 0x1C) >> 2) + 3
            _offset_copy(dst, dp - offset, dp, n); dp += n
            if dp == decompressed_size: break
        elif c1 <= 0xBF:
            c2 = src[sp]; sp += 1
            c3 = src[sp]; sp += 1
            plain = (c2 >> 6) & 0x03
            dst[dp:dp + plain] = src[sp:sp + plain]
            dp += plain; sp += plain
            if dp == decompressed_size: break
            offset = ((c2 & 0x3F) << 8) + c3 + 1
            n = (c1 & 0x3F) + 4
            _offset_copy(dst, dp - offset, dp, n); dp += n
            if dp == decompressed_size: break
        elif c1 <= 0xDF:
            c2 = src[sp]; sp += 1
            c3 = src[sp]; sp += 1
            c4 = src[sp]; sp += 1
            plain = c1 & 0x03
            dst[dp:dp + plain] = src[sp:sp + plain]
            dp += plain; sp += plain
            if dp == decompressed_size: break
            offset = ((c1 & 0x10) << 12) + (c2 << 8) + c3 + 1
            n = ((c1 & 0x0C) << 6) + c4 + 5
            _offset_copy(dst, dp - offset, dp, n); dp += n
            if dp == decompressed_size: break
        elif c1 <= 0xFB:
            plain = ((c1 & 0x1F) << 2) + 4
            dst[dp:dp + plain] = src[sp:sp + plain]
            dp += plain; sp += plain
            if dp == decompressed_size: break
        else:
            plain = c1 & 0x03
            dst[dp:dp + plain] = src[sp:sp + plain]
            dp += plain; sp += plain
            if dp == decompressed_size: break
    if dp != decompressed_size:
        raise ValueError(f"RefPack underflow: {dp}/{decompressed_size}")
    return bytes(dst)


def _offset_copy(buf: bytearray, src: int, dst: int, n: int) -> None:
    # Byte-by-byte so overlapping ranges resolve as RLE (the algo's defining trick).
    for i in range(n):
        buf[dst + i] = buf[src + i]


# ───────────────────────────────────────────────────────── model ──

@dataclasses.dataclass
class Pack:
    """A pack on disk under content-repo/."""
    name: str
    category: str          # e.g. "trunks/wedding", "cas/heads/f"
    pack_dir: Path
    manifest: dict

    @property
    def collection_target(self) -> str:
        return self.manifest["collection_entry"]["target"]

    @property
    def collection_entry(self) -> Tuple[int, int]:
        ce = self.manifest["collection_entry"]
        return (int(ce["type_id"], 16), int(ce["file_id"], 16))

    @property
    def files(self) -> List[str]:
        return [f for f in self.manifest["files"] if f != "pack.json"]


def load_packs(repo: Path) -> List[Pack]:
    """Walks content-repo/ for any directory containing pack.json."""
    out: List[Pack] = []
    if not repo.exists():
        return out
    for pj in sorted(repo.rglob("pack.json")):
        if pj.parent.name.startswith("_"):
            continue  # skip _vanilla and friends
        try:
            manifest = json.loads(pj.read_text())
        except json.JSONDecodeError as e:
            warn(f"skipping {pj}: invalid JSON ({e})")
            continue
        out.append(Pack(
            name=manifest["name"],
            category=manifest["category"],
            pack_dir=pj.parent,
            manifest=manifest,
        ))
    return out


@dataclasses.dataclass
class CategoryState:
    category: str
    collection_target: str        # e.g. "wedding_female.col"
    vanilla_count: int            # entries in the vanilla snapshot (or -1 if missing)
    installed_packs: List[Pack]
    available_packs: List[Pack]


def category_collection_target(category: str, gender: str) -> Optional[str]:
    """Predicts what .col file packs in this category target. Used to enumerate
    categories before any pack exists for them."""
    parts = category.split("/")
    if parts[0] == "trunks" and len(parts) == 2:
        # gender-specific; there are two .col files per trunk type.
        return f"{parts[1]}_{'female' if gender == 'f' else 'male'}.col"
    if parts[0] == "cas":
        # cas/heads/f, cas/bodies/m, etc.
        if len(parts) == 3:
            kind = parts[1]
            g = parts[2]
            suffix = "_heads" if kind == "heads" else ""
            return f"ea_{'female' if g == 'f' else 'male'}{suffix}.col"
    return None


def survey(cfg: dict, repo: Path) -> List[CategoryState]:
    """Builds the canonical view: every category we track, its install state,
    and what's in content-repo waiting to be installed."""
    freeso_source = Path(cfg["freeso_source"])
    content_avatar = freeso_source / "TSOClient" / "FSO.Content.TSO" / "Content" / "Avatar"
    collections_dir = content_avatar / "Collections"

    all_packs = load_packs(repo)

    # Discover categories from packs + from configured trunk types.
    categories: Dict[Tuple[str, str], List[Pack]] = {}
    for p in all_packs:
        # Each pack belongs to <category, gender>.
        gender = p.manifest.get("gender", "f")
        categories.setdefault((p.category, gender), []).append(p)
    for trunk_type in cfg.get("trunk_types", []) or []:
        for g in ("f", "m"):
            categories.setdefault((f"trunks/{trunk_type}", g), [])
    for kind in ("heads", "bodies"):
        for g in ("f", "m"):
            categories.setdefault((f"cas/{kind}/{g}", g), [])

    states: List[CategoryState] = []
    for (cat, gender), packs in sorted(categories.items()):
        target = category_collection_target(cat, gender)
        if target is None:
            continue

        # Vanilla snapshot
        vanilla_path = repo / "_vanilla" / target
        vanilla_count = -1
        if vanilla_path.exists():
            vanilla_count = len(read_col(vanilla_path.read_bytes()))

        # Installed: a pack is "installed" if every file it claims is present
        # in the source tree.
        installed: List[Pack] = []
        available: List[Pack] = []
        for p in packs:
            if all((freeso_source / "TSOClient" / "FSO.Content.TSO" / Path(f)).exists()
                   for f in p.files):
                installed.append(p)
            else:
                available.append(p)

        states.append(CategoryState(
            category=f"{cat} ({gender})",
            collection_target=target,
            vanilla_count=vanilla_count,
            installed_packs=installed,
            available_packs=available,
        ))
    return states


# ────────────────────────────────────────────────── operations ──

def install_pack(cfg: dict, repo: Path, pack: Pack) -> None:
    """Copies the pack's content files into the source tree and merges its .col
    entry with whatever's already there (vanilla + previously installed packs)."""
    freeso_source = Path(cfg["freeso_source"])
    fso_content_root = freeso_source / "TSOClient" / "FSO.Content.TSO"
    log(f"installing {pack.category}/{pack.name}")

    # 1. Copy non-collection files verbatim. They have unique IDs in their
    #    filenames so no conflicts.
    for rel in pack.files:
        if rel.startswith("Content/Avatar/Collections/"):
            continue
        src = pack.pack_dir / rel
        dst = fso_content_root / rel
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src, dst)

    # 2. Merge .col. Start from vanilla, fold in every installed pack's entry,
    #    then add this pack's entry. Sorted-by-id output keeps diffs clean.
    target = pack.collection_target
    new_entry = pack.collection_entry  # (typeId, fileId)
    merged = build_merged_collection(cfg, repo, target, extra_entries=[new_entry])
    col_path = fso_content_root / "Content" / "Avatar" / "Collections" / target
    col_path.parent.mkdir(parents=True, exist_ok=True)
    col_path.write_bytes(write_col(merged))
    log(f"  → {target}: {len(merged)} entries")


def uninstall_pack(cfg: dict, repo: Path, pack: Pack) -> None:
    """Removes the pack's content files from the source tree and rebuilds the
    target .col without this pack's entry."""
    freeso_source = Path(cfg["freeso_source"])
    fso_content_root = freeso_source / "TSOClient" / "FSO.Content.TSO"
    log(f"uninstalling {pack.category}/{pack.name}")

    for rel in pack.files:
        if rel.startswith("Content/Avatar/Collections/"):
            continue
        target = fso_content_root / rel
        if target.exists():
            target.unlink()
            # Best-effort cleanup of empty parent dirs (only User/, never higher).
            parent = target.parent
            if parent.name == "User" and not any(parent.iterdir()):
                parent.rmdir()

    # Rebuild the .col without this pack's entry.
    rebuild_collection(cfg, repo, pack.collection_target, exclude_pack=pack)


def reset_category(cfg: dict, repo: Path, category_pattern: str) -> None:
    """Removes every installed pack matching the given category prefix."""
    states = survey(cfg, repo)
    matching = [s for s in states if s.category.startswith(category_pattern)]
    if not matching:
        warn(f"no categories matched '{category_pattern}'")
        return
    for s in matching:
        for p in s.installed_packs:
            uninstall_pack(cfg, repo, p)
        # If no packs remain installed for this collection, drop the loose .col
        # entirely so the engine falls back to FAR3.
        target = s.collection_target
        col_path = (Path(cfg["freeso_source"])
                    / "TSOClient" / "FSO.Content.TSO" / "Content" / "Avatar" / "Collections" / target)
        if col_path.exists():
            still_used = any(p.collection_target == target
                             for ss in states for p in ss.installed_packs
                             if ss.category != s.category)
            if not still_used:
                col_path.unlink()
                log(f"  removed loose {target} (engine will use FAR3 vanilla)")


def build_merged_collection(cfg: dict, repo: Path, target: str,
                            extra_entries: Optional[List[Tuple[int, int]]] = None) -> List[ColEntry]:
    """Builds the live .col contents = vanilla + (entries from every currently
    installed pack targeting this collection) + extra_entries."""
    extra_entries = extra_entries or []

    # Vanilla baseline.
    entries: List[ColEntry] = []
    vanilla = repo / "_vanilla" / target
    if vanilla.exists():
        entries.extend(read_col(vanilla.read_bytes()))
    else:
        warn(f"no vanilla snapshot for {target} — run `manage-packs refresh-vanilla` first. "
             "Proceeding with empty baseline; this will hide vanilla outfits.")

    # Currently installed packs (from disk inspection — survey-style logic inline
    # to avoid a circular call). This is the post-this-operation projection because
    # the caller has already copied files in by the time we run.
    states = survey(cfg, repo)
    seen_ids = {(tid, fid) for _, fid, tid in entries}
    for s in states:
        if s.collection_target != target:
            continue
        for p in s.installed_packs:
            tid, fid = p.collection_entry
            if (tid, fid) in seen_ids:
                continue
            entries.append((0, fid, tid))
            seen_ids.add((tid, fid))

    for tid, fid in extra_entries:
        if (tid, fid) in seen_ids:
            continue
        entries.append((0, fid, tid))
        seen_ids.add((tid, fid))

    return entries


def rebuild_collection(cfg: dict, repo: Path, target: str, exclude_pack: Optional[Pack] = None) -> None:
    """Rewrites the live .col from vanilla + currently-installed packs (minus exclude_pack)."""
    freeso_source = Path(cfg["freeso_source"])
    col_path = (freeso_source / "TSOClient" / "FSO.Content.TSO"
                / "Content" / "Avatar" / "Collections" / target)

    states = survey(cfg, repo)
    entries: List[ColEntry] = []
    vanilla = repo / "_vanilla" / target
    if vanilla.exists():
        entries.extend(read_col(vanilla.read_bytes()))

    seen_ids = {(tid, fid) for _, fid, tid in entries}
    for s in states:
        if s.collection_target != target:
            continue
        for p in s.installed_packs:
            if exclude_pack and p.pack_dir == exclude_pack.pack_dir:
                continue
            tid, fid = p.collection_entry
            if (tid, fid) in seen_ids:
                continue
            entries.append((0, fid, tid))
            seen_ids.add((tid, fid))

    if entries == read_col(vanilla.read_bytes()) if vanilla.exists() else (not entries):
        # If we'd write the vanilla bytes verbatim, just delete the loose .col
        # so the engine falls back to FAR3 cleanly.
        if col_path.exists():
            col_path.unlink()
            log(f"  removed loose {target} (matches vanilla)")
        return

    col_path.parent.mkdir(parents=True, exist_ok=True)
    col_path.write_bytes(write_col(entries))
    log(f"  → {target}: {len(entries)} entries")


# ──────────────────────────────────────── refresh-vanilla ──

# The .col files we know how to extract — by predictable names rather than by
# walking every FAR3 entry, so noise stays low.
def known_collection_names(cfg: dict) -> List[str]:
    names = ["ea_male.col", "ea_female.col", "ea_male_heads.col", "ea_female_heads.col"]
    for trunk in cfg.get("trunk_types", []) or []:
        names.append(f"{trunk}_male.col")
        names.append(f"{trunk}_female.col")
    return names


def refresh_vanilla(cfg: dict, repo: Path) -> None:
    """Re-extract every known .col from the user's installed FAR3 archives."""
    game = Path(cfg["game_dir"])
    if not game.exists():
        die(f"game_dir does not exist: {game}")
    out = repo / "_vanilla"
    out.mkdir(parents=True, exist_ok=True)

    # Look at all .dat files under avatardata*/{bodies,heads}/collections/.
    archives: List[Path] = []
    for sub in game.glob("avatardata*"):
        for kind in ("bodies", "heads"):
            for dat in (sub / kind / "collections").glob("*.dat"):
                archives.append(dat)
    if not archives:
        die(f"no FAR3 collection archives found under {game}")

    wanted = set(name.lower() for name in known_collection_names(cfg))
    found: Dict[str, Path] = {}
    for far in archives:
        try:
            entries = read_far3_entries(far)
        except Exception as e:
            warn(f"skipping {far}: {e}")
            continue
        for entry in entries:
            fn = entry["filename"].lower()
            if fn in wanted and fn not in found:
                payload = far3_payload(far, entry)
                (out / fn).write_bytes(payload)
                found[fn] = far

    for name in sorted(known_collection_names(cfg)):
        n = name.lower()
        if n in found:
            n_entries = len(read_col((out / n).read_bytes()))
            log(f"  {name}: {n_entries} entries  (from {found[n].name})")
        else:
            warn(f"  {name}: not found in any FAR3 archive")


# ──────────────────────────────────────────────────── TUI ──

def banner() -> None:
    print()
    print("FreeSO content-pack manager")
    print()


def render_categories(states: List[CategoryState]) -> None:
    for i, s in enumerate(states, 1):
        v = "—" if s.vanilla_count < 0 else str(s.vanilla_count)
        ins = len(s.installed_packs)
        avail = len(s.available_packs)
        marker = " *" if ins or avail else ""
        print(f"  {i:>2}) {s.category:<32}  vanilla:{v:>3}  installed:{ins}  available:{avail}{marker}")


def menu(prompt: str, choices: List[str]) -> Optional[int]:
    while True:
        ans = input(prompt).strip().lower()
        if not ans or ans in {"q", "quit", "exit"}:
            return None
        if ans == "b" or ans == "back":
            return -1
        try:
            i = int(ans)
        except ValueError:
            print("  ?")
            continue
        if 1 <= i <= len(choices):
            return i - 1
        print(f"  pick 1..{len(choices)}, b=back, q=quit")


def category_screen(cfg: dict, repo: Path, state: CategoryState) -> None:
    while True:
        banner()
        print(f"=== {state.category} ===")
        v = "—" if state.vanilla_count < 0 else state.vanilla_count
        print(f"vanilla outfits in FAR3: {v}")
        print()
        if state.installed_packs:
            print("installed (loose, ours):")
            for p in state.installed_packs:
                print(f"  ✓ {p.name}")
        else:
            print("installed: none")
        print()
        if state.available_packs:
            print("available in repo:")
            for p in state.available_packs:
                print(f"  ◯ {p.name}")
        else:
            print("available: none")
        print()
        actions = [
            "install one or more from repo",
            "uninstall one or more",
            "reset to vanilla (uninstall everything we added)",
        ]
        for i, a in enumerate(actions, 1):
            print(f"  {i}) {a}")
        print("  b) back")
        choice = menu("> ", actions)
        if choice in (None, -1):
            return

        if choice == 0:
            do_install_prompt(cfg, repo, state)
        elif choice == 1:
            do_uninstall_prompt(cfg, repo, state)
        elif choice == 2:
            confirm = input(f"reset {state.category}? type 'yes': ").strip()
            if confirm == "yes":
                for p in state.installed_packs:
                    uninstall_pack(cfg, repo, p)
            else:
                print("  aborted")
        # Refresh state after any operation.
        state = next((s for s in survey(cfg, repo) if s.category == state.category), state)


def do_install_prompt(cfg: dict, repo: Path, state: CategoryState) -> None:
    if not state.available_packs:
        print("  nothing to install")
        return
    print()
    for i, p in enumerate(state.available_packs, 1):
        print(f"  {i}) {p.name}")
    sel = input("pick numbers (e.g. 1,3 or 'all'): ").strip().lower()
    if not sel:
        return
    if sel == "all":
        chosen = state.available_packs
    else:
        try:
            idxs = [int(x) - 1 for x in sel.split(",")]
            chosen = [state.available_packs[i] for i in idxs]
        except (ValueError, IndexError):
            print("  invalid selection")
            return
    for p in chosen:
        install_pack(cfg, repo, p)


def do_uninstall_prompt(cfg: dict, repo: Path, state: CategoryState) -> None:
    if not state.installed_packs:
        print("  nothing to uninstall")
        return
    print()
    for i, p in enumerate(state.installed_packs, 1):
        print(f"  {i}) {p.name}")
    sel = input("pick numbers (e.g. 1,3 or 'all'): ").strip().lower()
    if not sel:
        return
    if sel == "all":
        chosen = state.installed_packs
    else:
        try:
            idxs = [int(x) - 1 for x in sel.split(",")]
            chosen = [state.installed_packs[i] for i in idxs]
        except (ValueError, IndexError):
            print("  invalid selection")
            return
    for p in chosen:
        uninstall_pack(cfg, repo, p)


def interactive(cfg: dict, repo: Path) -> None:
    while True:
        banner()
        states = survey(cfg, repo)
        render_categories(states)
        print()
        print("  r) refresh-vanilla (re-extract from your installed FAR3)")
        print("  q) quit")
        ans = input("> ").strip().lower()
        if ans in {"q", "quit", "exit", ""}:
            return
        if ans == "r":
            refresh_vanilla(cfg, repo)
            input("press enter to continue ")
            continue
        try:
            idx = int(ans) - 1
        except ValueError:
            continue
        if 0 <= idx < len(states):
            category_screen(cfg, repo, states[idx])


# ───────────────────────────────────────────── subcommand entry ──

def cmd_list(args, cfg, repo) -> int:
    states = survey(cfg, repo)
    print(f"content-repo: {repo}")
    print(f"freeso_source: {cfg['freeso_source']}")
    render_categories(states)
    return 0


def cmd_install(args, cfg, repo) -> int:
    pack = find_pack(repo, args.path)
    if pack is None:
        die(f"no pack at {args.path} under content-repo")
    install_pack(cfg, repo, pack)
    print()
    print("review with: cd " + cfg["freeso_source"] + " && git status")
    return 0


def cmd_uninstall(args, cfg, repo) -> int:
    pack = find_pack(repo, args.path)
    if pack is None:
        die(f"no pack at {args.path} under content-repo")
    uninstall_pack(cfg, repo, pack)
    print()
    print("review with: cd " + cfg["freeso_source"] + " && git status")
    return 0


def cmd_reset(args, cfg, repo) -> int:
    reset_category(cfg, repo, args.category)
    print()
    print("review with: cd " + cfg["freeso_source"] + " && git status")
    return 0


def cmd_refresh_vanilla(args, cfg, repo) -> int:
    refresh_vanilla(cfg, repo)
    return 0


def find_pack(repo: Path, path: str) -> Optional[Pack]:
    target = (repo / path).resolve()
    for p in load_packs(repo):
        if p.pack_dir.resolve() == target:
            return p
    return None


# ──────────────────────────────────────────── helpers ──

def log(msg: str) -> None:
    print(msg)


def warn(msg: str) -> None:
    print(f"WARN: {msg}", file=sys.stderr)


def die(msg: str) -> None:
    print(f"ERROR: {msg}", file=sys.stderr)
    sys.exit(1)


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    p.add_argument("--repo", default=str(DEFAULT_REPO),
                   help="path to content-repo (default: ../../../content-repo relative to script)")
    sub = p.add_subparsers(dest="cmd")

    sub.add_parser("list", help="show categories + state")
    sp = sub.add_parser("install", help="install <category>/<name>")
    sp.add_argument("path")
    sp = sub.add_parser("uninstall", help="uninstall a previously installed pack")
    sp.add_argument("path")
    sp = sub.add_parser("reset", help="uninstall every pack in <category>")
    sp.add_argument("category")
    sub.add_parser("refresh-vanilla", help="re-extract baseline .col files from FAR3")

    args = p.parse_args()
    repo = Path(args.repo).resolve()
    cfg = load_config(repo)

    if args.cmd is None:
        interactive(cfg, repo)
        return 0
    handlers = {
        "list":            cmd_list,
        "install":         cmd_install,
        "uninstall":       cmd_uninstall,
        "reset":           cmd_reset,
        "refresh-vanilla": cmd_refresh_vanilla,
    }
    return handlers[args.cmd](args, cfg, repo)


if __name__ == "__main__":
    sys.exit(main())
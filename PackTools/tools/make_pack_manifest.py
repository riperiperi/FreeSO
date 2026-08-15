#!/usr/bin/env python3
"""Assemble wwwroot/packs/ for the browser client.

Reads the pack manifests (examples/*.json) for id->guid, pairs each object
with its compiled .iff (one per object id, from `FSO.PackCompiler build`) and
its exported billboard PNG (wwwroot/objects/manifest.json, kept for the
legacy FurnitureLayer fallback), copies the .iffs in, and writes
wwwroot/packs/manifest.json:

    [{"id": "beddouble", "iff": "beddouble.iff", "guid": "0x7F...", "png": "beddouble.png"}]

Usage: make_pack_manifest.py --iff-dir /path/to/packs-out [--packs kenney-tier1 ...]
"""
import argparse
import json
import shutil
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
EXAMPLES = REPO / "PackTools" / "examples"
WWWROOT = REPO / "PackTools" / "FSO.BrowserClient" / "wwwroot"
DEFAULT_PACKS = ["kenney-tier1", "midcentury-collection", "plumbing-pilot", "pet-rock", "gossip-gnome"]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--iff-dir", required=True, type=Path)
    ap.add_argument("--packs", nargs="*", default=DEFAULT_PACKS)
    args = ap.parse_args()

    png_by_id = {}
    legacy = WWWROOT / "objects" / "manifest.json"
    if legacy.exists():
        for entry in json.loads(legacy.read_text()):
            png_by_id[entry["id"]] = entry["png"]

    out_dir = WWWROOT / "packs"
    out_dir.mkdir(exist_ok=True)

    manifest = []
    missing = []
    for pack in args.packs:
        doc = json.loads((EXAMPLES / f"{pack}.json").read_text())
        for obj in doc.get("objects", []):
            oid, guid = obj["id"], obj["guid"]
            iff = args.iff_dir / f"{oid}.iff"
            if not iff.exists():
                missing.append(oid)
                continue
            shutil.copy2(iff, out_dir / iff.name)
            manifest.append({
                "id": oid,
                "iff": iff.name,
                "guid": guid,
                "png": png_by_id.get(oid),
            })

    (out_dir / "manifest.json").write_text(json.dumps(manifest, indent=1) + "\n")
    print(f"wrote {out_dir / 'manifest.json'}: {len(manifest)} objects")
    if missing:
        print(f"WARNING no .iff for: {', '.join(missing)}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())

#!/usr/bin/env python3
"""
Generate convention declaration skeletons for freeso-embodiment.

Reads the verb table from docs/design/verb-catalog.md and emits one JSON per op
with only the fields that the convention framework requires to register. Argument
shapes are deliberately empty — d87-d-* children will populate real schemas when
they land handlers.

Run from the sidecar directory:
    python3 conventions/_gen.py
"""
import json
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
CATALOG = HERE.parents[3] / "docs" / "design" / "verb-catalog.md"

# Ops explicitly subsumed by others per the catalog text (excluded from generation).
EXCLUDED = {"find-lot"}


def parse_catalog(text: str):
    """Extract (op_name, description, family) rows from all markdown tables."""
    ops = []
    seen = set()
    for line in text.splitlines():
        if not line.startswith("| "):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) < 3:
            continue
        op = cells[0]
        if op in ("op_name", ":---", "---"):
            continue
        if not re.match(r"^[a-z][a-z0-9-]+$", op):
            continue
        if op in EXCLUDED or op in seen:
            continue
        description = cells[1]
        family = cells[2]
        ops.append((op, description, family))
        seen.add(op)
    return ops


def write_decl(op: str, description: str, family: str):
    decl = {
        "convention": "freeso-embodiment",
        "version": "1.0",
        "operation": op,
        "family": family,
        "description": description,
        "args": [],
        "produces_tags": [
            {"tag": f"freeso:{op}", "cardinality": "exactly_one"}
        ],
        "antecedents": "none",
        "signing": "member_key",
        "rate_limit": {"max": 30, "per": "sender", "window": "1m"},
        "response": "sync",
        "status": "scaffold",
    }
    out = HERE / f"{op}.json"
    out.write_text(json.dumps(decl, indent=2) + "\n")


def main():
    text = CATALOG.read_text()
    ops = parse_catalog(text)
    # Preserve curated declarations that already have richer arg schemas.
    curated = {"walk-to", "speak"}
    generated = 0
    for op, description, family in ops:
        if op in curated:
            continue
        write_decl(op, description, family)
        generated += 1
    print(f"generated {generated} skeletons ({len(ops)} ops total, {len(curated)} curated)")
    return 0


if __name__ == "__main__":
    sys.exit(main())

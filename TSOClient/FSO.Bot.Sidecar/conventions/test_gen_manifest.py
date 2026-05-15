"""
Tests for _gen.py --manifest

(a) every soul-family JSON's operation appears in the manifest
(b) every non-soul-family operation does NOT appear in the manifest
(c) output parses as the documented format (each verb line matches pattern)

Run: pytest FreeSO/TSOClient/FSO.Bot.Sidecar/conventions/test_gen_manifest.py
"""
import json
import re
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO_ROOT = HERE.parents[3]
MANIFEST_PATH = REPO_ROOT / "docs" / "ops" / "verb-manifest.md"

SOUL_FAMILIES = {
    "movement",
    "queries",
    "interaction",
    "social",
    "navigation",
    "naming",
    "memory",
    "avatar",
    "build-buy-catalog",
    "property",
}


def _generate_manifest() -> str:
    """Run _gen.py --manifest and return the manifest text."""
    result = subprocess.run(
        [sys.executable, str(HERE / "_gen.py"), "--manifest"],
        capture_output=True,
        text=True,
        check=True,
    )
    assert result.returncode == 0, f"_gen.py --manifest failed: {result.stderr}"
    return MANIFEST_PATH.read_text()


def _load_convention_ops():
    """Load all convention JSONs. Return (soul_ops, non_soul_ops) as sets of operation names."""
    soul_ops = set()
    non_soul_ops = set()
    for json_file in sorted(HERE.glob("*.json")):
        try:
            d = json.loads(json_file.read_text())
        except Exception:
            continue
        family = d.get("family", "")
        op = d.get("operation", json_file.stem)
        if family in SOUL_FAMILIES:
            soul_ops.add(op)
        else:
            non_soul_ops.add(op)
    return soul_ops, non_soul_ops


def _extract_verb_names(manifest_text: str) -> set:
    """Extract all verb names from backtick-prefixed manifest lines."""
    # Verb lines start with `verb-name` — ...
    verb_re = re.compile(r"^`([a-z][a-z0-9-]+)`\s+—")
    names = set()
    for line in manifest_text.splitlines():
        m = verb_re.match(line.strip())
        if m:
            names.add(m.group(1))
    return names


class TestManifestGeneration:
    """Tests around _gen.py --manifest output."""

    def setup_method(self):
        self.manifest_text = _generate_manifest()
        self.soul_ops, self.non_soul_ops = _load_convention_ops()
        self.manifest_verbs = _extract_verb_names(self.manifest_text)

    # --- (a) soul-family ops appear in the manifest ---

    def test_all_soul_ops_present(self):
        missing = self.soul_ops - self.manifest_verbs
        assert not missing, (
            f"{len(missing)} soul-family op(s) missing from manifest: {sorted(missing)}"
        )

    def test_verb_count_at_least_40(self):
        assert len(self.manifest_verbs) >= 40, (
            f"Expected ≥40 verbs, got {len(self.manifest_verbs)}"
        )

    # --- (b) non-soul-family ops do NOT appear in the manifest ---

    def test_no_non_soul_ops_in_manifest(self):
        intruders = self.non_soul_ops & self.manifest_verbs
        assert not intruders, (
            f"Non-soul-family op(s) should not appear in manifest: {sorted(intruders)}"
        )

    # --- (c) output parses as documented format ---

    def test_manifest_has_header(self):
        assert self.manifest_text.startswith("# FreeSO Embodiment — Verb Manifest"), (
            "Manifest must start with the expected H1 header"
        )

    def test_verb_line_format(self):
        """Every verb line must match: `verb-name` — <sentence>. [EXAMPLE: `<sig>`]"""
        # The documented format:
        #   `verb-name` — <description first sentence>. EXAMPLE: `<call_sig>`
        # OR (when no call_signatures):
        #   `verb-name` — <description first sentence>.
        verb_line_re = re.compile(
            r"^`[a-z][a-z0-9-]+`\s+—\s+.+$"
        )
        verb_lines = [
            line for line in self.manifest_text.splitlines()
            if re.match(r"^`[a-z]", line.strip())
        ]
        assert verb_lines, "No verb lines found in manifest"
        malformed = [ln for ln in verb_lines if not verb_line_re.match(ln.strip())]
        assert not malformed, (
            f"Malformed verb line(s):\n" + "\n".join(malformed)
        )

    def test_manifest_line_count_under_200(self):
        line_count = len(self.manifest_text.splitlines())
        assert line_count <= 200, (
            f"Manifest is {line_count} lines; must be ≤200"
        )

    def test_manifest_family_sections_present(self):
        """Manifest must contain section headers for all populated soul families."""
        # At minimum these families have ops and should appear as headers.
        expected_families = ["Movement", "Navigation", "Interaction", "Social",
                             "Queries", "Memory", "Avatar", "Property", "Build Buy Catalog"]
        for fam in expected_families:
            assert f"## {fam}" in self.manifest_text, (
                f"Expected section '## {fam}' not found in manifest"
            )

    def test_manifest_file_written(self):
        assert MANIFEST_PATH.exists(), f"Manifest file not written at {MANIFEST_PATH}"

    def test_soul_vs_non_soul_disjoint(self):
        """Sanity: soul and non-soul op sets must be disjoint."""
        overlap = self.soul_ops & self.non_soul_ops
        assert not overlap, f"Op(s) appear in both soul and non-soul sets: {overlap}"

    def test_every_soul_family_with_ops_appears_in_manifest(self):
        """Every soul family that has ops must produce a manifest section.

        This prevents silent-vanish: if someone mis-tags an op as family:'naming'
        (which is in SOUL_FAMILIES but not FAMILY_ORDER), the manifest must still
        emit a section for that family. Failing this test means a family with ops
        was dropped from the manifest output.
        """
        # Compute which families have ops in the convention JSONs
        families_in_ops = set()
        for json_file in sorted(HERE.glob("*.json")):
            try:
                d = json.loads(json_file.read_text())
            except Exception:
                continue
            family = d.get("family", "")
            if family in SOUL_FAMILIES:
                families_in_ops.add(family)

        # Extract family headers from manifest (e.g., "## Movement" → "movement")
        manifest_families = set()
        for line in self.manifest_text.splitlines():
            if line.startswith("## "):
                # Convert "## Movement" → "movement" (and handle multi-word families like "Build Buy Catalog")
                header_text = line[3:].strip()
                # Reverse the title-case to lowercase with hyphens
                # "Build Buy Catalog" → "build-buy-catalog"
                # "Movement" → "movement"
                family_key = header_text.lower().replace(" ", "-")
                manifest_families.add(family_key)

        missing_families = families_in_ops - manifest_families
        assert not missing_families, (
            f"Soul family(s) with ops missing from manifest sections: {sorted(missing_families)}. "
            f"Families with ops: {sorted(families_in_ops)}. "
            f"Manifest sections: {sorted(manifest_families)}"
        )

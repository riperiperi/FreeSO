# Content Ownership Strategy (private — not committed)

Kat, 2026-08-08.

The layer cake:
- Engine: FreeSO (MPL 2.0, open) — ours via fork.
- Authoring layer (schema, compiler, MCP, product surface): new IP, fully ours.
- Content: currently EA's TSO assets (sprites, sounds, animations). Players self-download from the donated archive; we never distribute. This is the only layer not ours.

The play: **progressively replace EA content with AI-generated original content.**
- Formats are fully reverse-engineered; we own write code for SPR2/DGRP (isometric sprites + z-buffers), sounds, animations.
- Pipeline: AI generates original object art in a consistent isometric style → same pack compiler → objects that owe nothing to EA.
- Order: object art first (feeds vibe-coded mods directly — new creations get original art from day one), then sounds, then animations (hardest).
- Endgame: a world that is 100% owned. Unlocks: browser distribution with zero legal exposure (today's blocker: a web server would be distributing EA's content), true open-source content base, no EA takedown surface at all.

Near-term posture: MVP runs on TSO assets (legal, proven, self-downloaded). Don't talk publicly about "replacing EA content" until the generation pipeline actually works — no need to paint a target.

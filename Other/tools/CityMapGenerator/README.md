# CityMapGenerator

Procedural Thousand-Islands-style heightmap + biome generator for FreeSO.
Outputs the seven PNG layers FSO loads from
`FSO.Content.TSO/Content/Cities/city_<id>/`.

## Quick start

```bash
python3 -m pip install --user numpy pillow
python3 generate.py --seed 42 --out city_0200
ls city_0200/
# elevation.png  terraintype.png  roadmap.png
# forestdensity.png  foresttype.png  vertexcolor.png  thumbnail.png
```

Drop the resulting directory into `TSOClient/FSO.Content.TSO/Content/Cities/`
named `city_<NNNN>/`, restart the city server, and pick the new shard from
the city selector.

## Tuning from the command line

```bash
# Many tiny islands, scattered loosely
python3 generate.py --islands 35 --island-size 10 22 --cluster-frac 0.4

# Fewer, larger islands tightly clustered
python3 generate.py --islands 12 --island-size 30 60 --cluster-frac 0.8

# Re-roll the same parameters with a different layout
python3 generate.py --seed 17
```

| Flag | Default | Effect |
|---|---|---|
| `--seed` | 42 | Layout seed; change for different arrangements |
| `--islands` | 8 | Total island count |
| `--cluster-frac` | 0.7 | Fraction of islands in the central tight cluster |
| `--island-size MIN MAX` | 40 80 | Plateau radius range in pixels |
| `--no-roads` | off | Skip road generation; emit a blank roadmap so you can paint roads in-engine afterwards |
| `--out` | ./city_out | Output directory |

Defaults target ~25-30% land coverage of the canvas, matching Alphaville's
scale. Earlier defaults (22 small islands) produced ~12% — visibly sparse.

For finer-grained knobs (peak heights, shore width, foliage density, road
width, color palette) edit the constants near the top of `generate.py`.

## Output format

| File | Size | Mode | Notes |
|---|---|---|---|
| `elevation.png` | 512×512 | RGBA | R-channel is height; 0 = water, 80 = plateau, 180+ = peaks |
| `terraintype.png` | 512×512 | RGBA | Color codes from `CityMapData.cs:19` |
| `roadmap.png` | 512×512 | RGBA | R-channel road density |
| `forestdensity.png` | 512×512 | RGBA | R-channel foliage density |
| `foresttype.png` | 512×512 | RGBA | Tree species color (single species by default) |
| `vertexcolor.png` | 512×512 | RGBA | Pre-baked hillshade tint — required, not optional |
| `thumbnail.png` | 180×135 | RGBA | City-selector preview |

The renderable area is a diamond inside the 512² square (`CityMapData.cs:88-108`);
corners are masked to the `(0,0,0)` "nothing" terrain code so the renderer
ignores them.

## Lot placement

This tool generates **terrain only**. Lot positions are a database concern,
managed in-game by admins or via the `import-nhood` JSON tool
(`./FSO.Server.Core import-nhood <shard-id> <nhood.json>`). Enforce
"flat lots only" by listing only coordinates that fall on plateau pixels —
the in-game lot-placement validator should refuse rocky/water terrain
based on `terraintype.png`.
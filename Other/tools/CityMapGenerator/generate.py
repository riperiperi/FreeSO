#!/usr/bin/env python3
"""
Generate a Thousand-Islands-style FreeSO city map.

Outputs the seven layers FSO.Content.TSO/Content/Cities/city_<id>/ expects:

    elevation.png        512x512 RGBA   grayscale heightmap (R = 0..255)
    terraintype.png      512x512 RGBA   color-coded biome
    roadmap.png          512x512 RGBA   grayscale road density
    forestdensity.png    512x512 RGBA   grayscale foliage density
    foresttype.png       512x512 RGBA   color-coded tree species
    vertexcolor.png      512x512 RGBA   pre-baked hillshade tint
    thumbnail.png         180x135 RGBA  preview for the city selector

Dependencies:
    python3 -m pip install --user numpy pillow

Usage:
    python3 generate.py                                 # defaults
    python3 generate.py --seed 7 --out city_0200
    python3 generate.py --islands 30 --island-size 12 24
"""

import argparse
import math
import os
import numpy as np
from PIL import Image

SIZE = 512
THUMB = (180, 135)

# Heights (0 = sea level, 255 = max).
PLATEAU_HEIGHT     = 80
PEAK_HEIGHT        = (180, 220)

# Layout — defaults tuned to ~30% land coverage (Alphaville is ~30%).
# Earlier defaults (22 islands at 15-30px) produced ~12% — felt too sparse.
DEFAULT_ISLANDS    = 8
DEFAULT_CLUSTER_FR = 0.70                # 70% in the central cluster
CENTRAL_RADIUS     = 170
OUTLIER_BAND       = (180, 230)
DEFAULT_RADIUS     = (40, 80)            # min, max plateau radius (px)
SHORE_WIDTH        = 8
ISLAND_NOISE_AMP   = 0.25                # how much noise warps the boundary

NUM_PEAKS          = 4
PEAK_RADIUS        = (8, 15)
PEAK_NOISE_AMP     = 0.30

ROAD_WIDTH         = 2
FOLIAGE_FLAT       = 30
FOLIAGE_PEAK       = 180

# Terrain colors — match CityMapData.cs:19 exactly. Don't change.
COLOR_GRASS        = (  0, 255,   0)
COLOR_SAND         = (255, 255,   0)
COLOR_ROCK         = (255,   0,   0)
COLOR_SNOW         = (255, 255, 255)
COLOR_WATER        = ( 12,   0, 255)
COLOR_NOTHING      = (  0,   0,   0)

# Tropical-ish single-species default. Sample from city_0100/foresttype.png
# if you want multi-species variety.
COLOR_TREE         = ( 32, 144,  64)

SAND_MAX           = 30
GRASS_MAX          = 150


def diamond_mask(size=SIZE):
    """Renderable diamond inside the 512² square (CityMapData.cs:88-108)."""
    yy = np.arange(size).reshape(-1, 1)
    xx = np.arange(size).reshape(1, -1)
    xs_min = np.where(yy < 306, 306 - yy, yy - 306)
    xs_max = np.where(yy < 205, 307 + yy, 512 - (yy - 205))
    return (xx >= xs_min) & (xx < xs_max)


def in_diamond_pt(x, y):
    xs = (306 - y) if y < 306 else (y - 306)
    xe = (307 + y) if y < 205 else (512 - (y - 205))
    return xs <= x < xe


def value_noise(shape, scale, rng, octaves=4, persistence=0.5):
    h, w = shape
    out = np.zeros(shape, dtype=np.float64)
    amp, total, s = 1.0, 0.0, scale
    for _ in range(octaves):
        nh = max(2, int(h / s)); nw = max(2, int(w / s))
        grid = (rng.random((nh, nw)) * 255).astype(np.uint8)
        up = np.asarray(Image.fromarray(grid).resize((w, h), Image.BICUBIC),
                        dtype=np.float64) / 255.0
        out += up * amp
        total += amp
        amp *= persistence
        s = max(2, s / 2)
    return out / total


def smoothstep(edge0, edge1, x):
    t = np.clip((x - edge0) / (edge1 - edge0 + 1e-9), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def line_pixels(x0, y0, x1, y1):
    pts = []
    dx = abs(x1 - x0); dy = abs(y1 - y0)
    sx = 1 if x0 < x1 else -1
    sy = 1 if y0 < y1 else -1
    err = dx - dy
    while True:
        pts.append((x0, y0))
        if x0 == x1 and y0 == y1:
            break
        e2 = 2 * err
        if e2 > -dy:
            err -= dy; x0 += sx
        if e2 < dx:
            err += dx; y0 += sy
    return pts


def place_islands(rng, total, cluster_frac, radius_min, radius_max):
    n_central = int(round(total * cluster_frac))
    n_outliers = total - n_central
    islands = []
    cx_map, cy_map = SIZE / 2, SIZE / 2

    def try_place(dist_min, dist_max, attempts):
        for _ in range(attempts):
            angle = rng.uniform(0, 2 * math.pi)
            dist = rng.uniform(dist_min, dist_max)
            r = int(rng.integers(radius_min, radius_max + 1))
            cx = int(cx_map + dist * math.cos(angle))
            cy = int(cy_map + dist * math.sin(angle))
            if not in_diamond_pt(cx, cy):
                continue
            if all((cx - x)**2 + (cy - y)**2 > (r + r0 + 6)**2
                   for x, y, r0, _ in islands):
                return (cx, cy, r, False)
        return None

    for _ in range(n_central):
        p = try_place(0, CENTRAL_RADIUS, 40)
        if p: islands.append(p)
    for _ in range(n_outliers):
        p = try_place(*OUTLIER_BAND, 80)
        if p: islands.append(p)

    sorted_idx = sorted(range(len(islands)), key=lambda i: -islands[i][2])
    for i in sorted_idx[:NUM_PEAKS]:
        cx, cy, r, _ = islands[i]
        islands[i] = (cx, cy, r, True)
    return islands


def build_elevation(islands, rng):
    h, w = SIZE, SIZE
    elev = np.zeros((h, w), dtype=np.float64)
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float64)
    boundary_noise = value_noise((h, w), 32, rng, octaves=3) * 2 - 1

    for cx, cy, r, has_peak in islands:
        dist = np.sqrt((xx - cx)**2 + (yy - cy)**2)
        eff_radius = r + boundary_noise * r * ISLAND_NOISE_AMP
        plateau = smoothstep(eff_radius, eff_radius - SHORE_WIDTH, dist) * PLATEAU_HEIGHT
        elev = np.maximum(elev, plateau)

        if has_peak:
            ox, oy = rng.integers(-r // 4, r // 4 + 1, size=2)
            pcx, pcy = cx + int(ox), cy + int(oy)
            pr = int(rng.integers(*PEAK_RADIUS))
            pdist = np.sqrt((xx - pcx)**2 + (yy - pcy)**2)
            peak_noise = value_noise((h, w), 16, rng, octaves=3) * 2 - 1
            peak_eff = pr + peak_noise * pr * PEAK_NOISE_AMP
            peak_h = float(rng.uniform(*PEAK_HEIGHT))
            peak = smoothstep(peak_eff, peak_eff - 3, pdist) * peak_h
            elev = np.maximum(elev, peak)

    return np.clip(elev, 0, 255).astype(np.uint8)


def build_terraintype(elev, mask):
    out = np.zeros((*elev.shape, 3), dtype=np.uint8)
    out[:] = COLOR_NOTHING
    out[mask & (elev == 0)] = COLOR_WATER
    out[mask & (elev > 0) & (elev <= SAND_MAX)] = COLOR_SAND
    out[mask & (elev > SAND_MAX) & (elev <= GRASS_MAX)] = COLOR_GRASS
    out[mask & (elev > GRASS_MAX)] = COLOR_ROCK
    return out


def build_roadmap(islands):
    out = np.zeros((SIZE, SIZE), dtype=np.uint8)
    central = [i for i in islands
               if (i[0] - SIZE/2)**2 + (i[1] - SIZE/2)**2 < CENTRAL_RADIUS**2]
    if len(central) < 2:
        return out
    central.sort(key=lambda i: (i[0] - SIZE/2)**2 + (i[1] - SIZE/2)**2)
    connected, remaining = [central[0]], list(central[1:])
    while remaining:
        best = (1e18, None, None)
        for c in connected:
            for r in remaining:
                d = (c[0] - r[0])**2 + (c[1] - r[1])**2
                if d < best[0]:
                    best = (d, c, r)
        _, a, b = best
        for x, y in line_pixels(a[0], a[1], b[0], b[1]):
            for dy in range(-ROAD_WIDTH, ROAD_WIDTH + 1):
                for dx in range(-ROAD_WIDTH, ROAD_WIDTH + 1):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < SIZE and 0 <= ny < SIZE and dx*dx + dy*dy <= ROAD_WIDTH*ROAD_WIDTH:
                        out[ny, nx] = 255
        connected.append(b)
        remaining.remove(b)
    return out


def build_foliage(elev, rng):
    h, w = elev.shape
    base = np.zeros((h, w), dtype=np.float64)
    base[(elev > SAND_MAX) & (elev <= GRASS_MAX)] = FOLIAGE_FLAT
    base[elev > GRASS_MAX] = FOLIAGE_PEAK
    noise = value_noise((h, w), 12, rng, octaves=4)
    density = (base * noise).astype(np.uint8)
    type_img = np.zeros((h, w, 3), dtype=np.uint8)
    type_img[density > 0] = COLOR_TREE
    return density, type_img


def hillshade(elev):
    """Single-light hillshade in -1..1 from upper-left."""
    e = elev.astype(np.float64)
    gx = np.gradient(e, axis=1)
    gy = np.gradient(e, axis=0)
    return np.clip(-(gx + gy), -25, 25) / 25.0


def build_vertexcolor(elev, terraintype, mask):
    """The pre-baked terrain tint the renderer multiplies into the mesh."""
    out = terraintype.astype(np.float64)
    shade = hillshade(elev)
    for c in range(3):
        out[..., c] = np.clip(out[..., c] + shade * 35, 0, 255)
    # Outside the diamond stays at COLOR_NOTHING (0,0,0).
    out[~mask] = 0
    return out.astype(np.uint8)


def build_thumbnail(vertexcolor):
    """Downscale the shaded RGB image to 180x135 for the city selector."""
    return np.asarray(
        Image.fromarray(vertexcolor, mode='RGB').resize(THUMB, Image.LANCZOS),
        dtype=np.uint8,
    )


def to_rgba(arr):
    """Promote a grayscale or RGB array to RGBA with alpha=255."""
    if arr.ndim == 2:
        a = np.stack([arr, arr, arr, np.full_like(arr, 255)], axis=-1)
    else:
        a = np.dstack([arr, np.full(arr.shape[:2], 255, dtype=np.uint8)])
    return a


def save_rgba(arr, path):
    Image.fromarray(to_rgba(arr), mode='RGBA').save(path)


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--out', default='./city_out')
    ap.add_argument('--seed', type=int, default=42)
    ap.add_argument('--islands', type=int, default=DEFAULT_ISLANDS,
                    help=f'total island count (default {DEFAULT_ISLANDS})')
    ap.add_argument('--cluster-frac', type=float, default=DEFAULT_CLUSTER_FR,
                    help='fraction of islands in the central cluster (default 0.6)')
    ap.add_argument('--island-size', nargs=2, type=int, metavar=('MIN', 'MAX'),
                    default=DEFAULT_RADIUS,
                    help=f'plateau radius range (default {DEFAULT_RADIUS[0]} {DEFAULT_RADIUS[1]})')
    ap.add_argument('--no-roads', action='store_true',
                    help='skip road generation; saves an empty roadmap.png so you can paint roads in-engine afterwards')
    args = ap.parse_args()

    os.makedirs(args.out, exist_ok=True)
    rng = np.random.default_rng(args.seed)
    mask = diamond_mask()

    print(f"seed={args.seed}  islands={args.islands}  size={tuple(args.island_size)}  out={args.out}")

    print("placing islands...")
    islands = place_islands(rng, args.islands, args.cluster_frac,
                            args.island_size[0], args.island_size[1])
    peaks = sum(1 for *_, p in islands if p)
    print(f"  {len(islands)} placed ({peaks} with relief peaks)")

    print("building elevation...")
    elev = build_elevation(islands, rng)
    elev[~mask] = 0

    print("building terrain types...")
    terraintype = build_terraintype(elev, mask)

    if args.no_roads:
        print("skipping road generation (--no-roads)")
        roadmap = np.zeros((SIZE, SIZE), dtype=np.uint8)
    else:
        print("building roads...")
        roadmap = build_roadmap(islands)
        roadmap[~mask] = 0

    print("building foliage...")
    forestdensity, foresttype = build_foliage(elev, rng)
    forestdensity[~mask] = 0
    foresttype[~mask] = 0

    print("building vertex color...")
    vertexcolor = build_vertexcolor(elev, terraintype, mask)

    print("building thumbnail...")
    thumbnail = build_thumbnail(vertexcolor)

    print("saving...")
    save_rgba(elev,          os.path.join(args.out, 'elevation.png'))
    save_rgba(terraintype,   os.path.join(args.out, 'terraintype.png'))
    save_rgba(roadmap,       os.path.join(args.out, 'roadmap.png'))
    save_rgba(forestdensity, os.path.join(args.out, 'forestdensity.png'))
    save_rgba(foresttype,    os.path.join(args.out, 'foresttype.png'))
    save_rgba(vertexcolor,   os.path.join(args.out, 'vertexcolor.png'))
    save_rgba(thumbnail,     os.path.join(args.out, 'thumbnail.png'))

    print(f"done — drop {args.out}/ into FSO.Content.TSO/Content/Cities/ as city_<id>/")


if __name__ == '__main__':
    main()
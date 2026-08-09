#!/usr/bin/env python3
"""
v0 real-world FreeSO city generator.

Address/place -> Cities/city_XXXX raster set:
  elevation.png / terraintype.png / roadmap.png / foresttype.png /
  forestdensity.png / vertexcolor.png / thumbnail.png / info.cst

Formats per tso.client/Rendering/City/CityMapData.cs (512x512 RGBA PNGs, R channel
for elevation/road/density; terraintype exact palette colors) and
CityGeometry.cs (road byte: low nibble = edge road mask N=1 E=2 S=4 W=8).

Data sources (no API key): Nominatim geocoding, AWS terrarium elevation tiles,
Overpass OSM roads.

Usage: python3 generate_city.py "San Francisco" out_dir [cell_meters]
  cell_meters default 77 (faithful: 1 city cell = 1 lot = 77 tiles ~= 77 m).
"""
import json
import math
import sys
import urllib.request
from io import BytesIO

from PIL import Image

SIZE = 512
UA = {"User-Agent": "fso-citygen-v0 (personal experiment)"}


def fetch(url, data=None):
    req = urllib.request.Request(url, data=data, headers=UA)
    with urllib.request.urlopen(req, timeout=60) as r:
        return r.read()


def geocode(query):
    url = ("https://nominatim.openstreetmap.org/search?format=json&limit=1&q="
           + urllib.parse.quote(query))
    results = json.loads(fetch(url))
    if not results:
        raise SystemExit("geocode failed: " + query)
    r = results[0]
    return float(r["lat"]), float(r["lon"]), r["display_name"]


def terrarium_elevation(lat, lon, zoom):
    """Elevation in meters at lat/lon from a cached terrarium tile."""
    n = 2 ** zoom
    xt = (lon + 180.0) / 360.0 * n
    yt = (1.0 - math.asinh(math.tan(math.radians(lat))) / math.pi) / 2.0 * n
    tx, ty = int(xt), int(yt)
    key = (tx, ty)
    tile = _tiles.get(key)
    if tile is None:
        url = f"https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{zoom}/{tx}/{ty}.png"
        tile = Image.open(BytesIO(fetch(url))).convert("RGB")
        _tiles[key] = tile
    px = min(255, int((xt - tx) * 256))
    py = min(255, int((yt - ty) * 256))
    r, g, b = tile.getpixel((px, py))
    return (r * 256 + g + b / 256.0) - 32768.0


_tiles = {}


def overpass_roads(south, west, north, east):
    query = f"""[out:json][timeout:90];
(way["highway"~"^(motorway|trunk|primary|secondary|tertiary|residential|unclassified)$"]({south},{west},{north},{east}););
out geom;"""
    data = fetch("https://overpass-api.de/api/interpreter",
                 data=("data=" + urllib.parse.quote(query)).encode())
    return json.loads(data)["elements"]


def main():
    place = sys.argv[1] if len(sys.argv) > 1 else "San Francisco"
    out = sys.argv[2] if len(sys.argv) > 2 else "city_out"
    cell_m = float(sys.argv[3]) if len(sys.argv) > 3 else 77.0

    lat, lon, display = geocode(place)
    print(f"center: {display} ({lat:.4f}, {lon:.4f}); cell = {cell_m} m; "
          f"map covers {SIZE * cell_m / 1000:.1f} km square")

    half_m = SIZE * cell_m / 2
    dlat = half_m / 111320.0
    dlon = half_m / (111320.0 * math.cos(math.radians(lat)))
    south, north = lat - dlat, lat + dlat
    west, east = lon - dlon, lon + dlon

    # pick terrarium zoom with ~cell resolution
    zoom = max(1, min(14, int(math.log2(156543.0 * math.cos(math.radians(lat)) / cell_m))))
    print(f"terrarium zoom {zoom}")

    # elevation grid (meters); map row 0 = north
    elev = [[0.0] * SIZE for _ in range(SIZE)]
    for y in range(SIZE):
        glat = north - (north - south) * (y + 0.5) / SIZE
        for x in range(SIZE):
            glon = west + (east - west) * (x + 0.5) / SIZE
            elev[y][x] = terrarium_elevation(glat, glon, zoom)
        if y % 64 == 0:
            print(f"elevation row {y}/{SIZE}")

    flat = [v for row in elev for v in row]
    lo = max(min(flat), -5.0)
    hi = max(flat)
    rng = max(hi - lo, 1.0)
    print(f"elevation range {lo:.1f}..{hi:.1f} m")

    # roads rasterized to cells
    road = [[False] * SIZE for _ in range(SIZE)]

    def to_cell(glat, glon):
        x = (glon - west) / (east - west) * SIZE
        y = (north - glat) / (north - south) * SIZE
        return x, y

    try:
        ways = overpass_roads(south, west, north, east)
        print(f"osm ways: {len(ways)}")
        for way in ways:
            geom = way.get("geometry") or []
            pts = [to_cell(p["lat"], p["lon"]) for p in geom]
            for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
                steps = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
                for i in range(steps + 1):
                    x = x0 + (x1 - x0) * i / steps
                    y = y0 + (y1 - y0) * i / steps
                    if 0 <= int(x) < SIZE and 0 <= int(y) < SIZE:
                        road[int(y)][int(x)] = True
    except Exception as e:
        print("overpass failed, continuing without roads:", e)

    import os
    os.makedirs(out, exist_ok=True)

    elevation = Image.new("RGBA", (SIZE, SIZE))
    terrain = Image.new("RGBA", (SIZE, SIZE))
    roadmap = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 255))
    foresttype = Image.new("RGBA", (SIZE, SIZE), (0, 255, 0, 255))   # tree set: green
    forestdensity = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 255))  # no forest v0
    vertexcolor = Image.new("RGBA", (SIZE, SIZE), (255, 255, 255, 255))

    GRASS, WATER, SAND, ROCK = (0, 255, 0, 255), (12, 0, 255, 255), (255, 255, 0, 255), (255, 0, 0, 255)

    for y in range(SIZE):
        for x in range(SIZE):
            m = elev[y][x]
            v = int((max(m, lo) - lo) / rng * 235) + 1  # engine draws height = R/12
            elevation.putpixel((x, y), (v, v, v, 255))
            if m <= 0.5:
                terrain.putpixel((x, y), WATER)
            elif m <= 2.0:
                terrain.putpixel((x, y), SAND)
            elif m > lo + rng * 0.8:
                terrain.putpixel((x, y), ROCK)
            else:
                terrain.putpixel((x, y), GRASS)
            if road[y][x] and m > 0.5:
                # low nibble: connectivity mask N=1 E=2 S=4 W=8 (CityContent.RoadLayout index)
                mask = 0
                if y > 0 and road[y - 1][x]: mask |= 1
                if x < SIZE - 1 and road[y][x + 1]: mask |= 2
                if y < SIZE - 1 and road[y + 1][x]: mask |= 4
                if x > 0 and road[y][x - 1]: mask |= 8
                if mask == 0:
                    mask = 15
                roadmap.putpixel((x, y), (mask, mask, mask, 255))

    elevation.save(out + "/elevation.png")
    terrain.save(out + "/terraintype.png")
    roadmap.save(out + "/roadmap.png")
    foresttype.save(out + "/foresttype.png")
    forestdensity.save(out + "/forestdensity.png")
    vertexcolor.save(out + "/vertexcolor.png")

    thumb = Image.merge("RGBA", elevation.split()).resize((180, 135))
    thumb.save(out + "/thumbnail.png")

    with open(out + "/info.cst", "w") as f:
        f.write(f"1 ^{place}^\n2 ^Generated from real-world data: {display}. "
                f"{cell_m:.0f} m per city cell, {SIZE * cell_m / 1000:.1f} km square.^")

    print("wrote", out)


if __name__ == "__main__":
    main()

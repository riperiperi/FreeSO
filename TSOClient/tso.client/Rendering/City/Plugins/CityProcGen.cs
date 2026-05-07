using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FSO.Client.Rendering.City.Plugins
{
    /// <summary>
    /// Procedural city map generator. Produces all five engine input
    /// layers (elevation / terraintype / forest type / forest density /
    /// road) in-memory into a <see cref="CityMapData"/>. The road layer
    /// is left empty — the byte format encodes corner/edge segments
    /// that don't synthesize cleanly without manual painting.
    ///
    /// Pipeline:
    ///   1. Fractal Perlin elevation field with type-shaped multiplicative
    ///      mask + diamond falloff so the unrenderable corners are deep
    ///      water.
    ///   2. Optional inland water: lakes (Gaussian depressions at random
    ///      interior tiles) and rivers (gradient-descent traces from peaks
    ///      down to coast or carved water).
    ///   3. Sea level picked by percentile so WaterRatio gives the
    ///      requested land/water ratio.
    ///   4. Terrain classification: water → sand band (2-tile dilation) →
    ///      grass / rock / snow. Rock comes from EITHER high elevation OR
    ///      steep slope, so peaks have rocky shoulders below their snow.
    ///   5. Forests: primary noise field thresholded by ForestDensity,
    ///      modulated by a low-frequency cluster mask so patches form
    ///      instead of speckling uniformly.
    ///
    /// All sample stats targeted off the official TSO cities — Coastal
    /// types ~60% land / ~40% water, Inland ~75% / 25%, Mountains ~80% /
    /// 20% with 25–30% of land tiles becoming rock on slopes.
    /// </summary>
    public static class CityProcGen
    {
        public const int SIZE = 512;
        private const int N = SIZE * SIZE;

        // Sea level in the engine's 0..255 elevation byte range.
        private const byte SEA_LEVEL = 60;

        // Terrain-type IDs (CityMapData.TerrainTypeMap, CityMapData.cs:19).
        private const byte TT_GRASS = 0;
        private const byte TT_SAND  = 1;
        private const byte TT_ROCK  = 2;
        private const byte TT_SNOW  = 3;
        private const byte TT_WATER = 4;

        public enum MapType { Island, Coastal, Inland, Mountains }
        public enum Level   { Low, Medium, High }

        public class Parameters
        {
            public MapType Type = MapType.Island;
            public Level HeightAvg     = Level.Medium;
            public Level WaterRatio    = Level.Medium;
            public Level Roughness     = Level.Medium;
            public Level ForestDensity = Level.Medium;
            // Rivers/Lakes count: Low=None (0), Medium=Few (2), High=Many (4).
            public Level Rivers = Level.Low;
            public Level Lakes  = Level.Low;
            public int Seed = 0;

            /// <summary>
            /// Per-type sensible starting points. User can still tweak any
            /// individual knob after picking a type.
            /// </summary>
            public static Parameters DefaultsFor(MapType t)
            {
                var p = new Parameters { Type = t };
                switch (t)
                {
                    case MapType.Island:
                        p.HeightAvg = Level.Medium;
                        p.WaterRatio = Level.Medium;
                        p.Roughness = Level.High;       // dissected coastlines
                        p.ForestDensity = Level.Medium;
                        p.Rivers = Level.Low;            // ocean is the water
                        p.Lakes = Level.Low;
                        break;
                    case MapType.Coastal:
                        p.HeightAvg = Level.Medium;
                        p.WaterRatio = Level.Medium;
                        p.Roughness = Level.High;
                        p.ForestDensity = Level.High;
                        p.Rivers = Level.Medium;         // a river or two flowing into the ocean
                        p.Lakes = Level.Low;
                        break;
                    case MapType.Inland:
                        p.HeightAvg = Level.Medium;
                        p.WaterRatio = Level.Low;
                        p.Roughness = Level.Medium;
                        p.ForestDensity = Level.High;
                        p.Rivers = Level.Medium;
                        p.Lakes = Level.Medium;
                        break;
                    case MapType.Mountains:
                        p.HeightAvg = Level.High;
                        p.WaterRatio = Level.Low;
                        p.Roughness = Level.High;
                        p.ForestDensity = Level.High;
                        p.Rivers = Level.Medium;
                        p.Lakes = Level.Medium;
                        break;
                }
                return p;
            }
        }

        public static void Generate(CityMapData map, Parameters p)
        {
            var rng = new Random(p.Seed);

            var elev = GenerateElevation(p);
            CarveLakes(elev, p, rng);
            CarveRivers(elev, p, rng);

            var terrain = ClassifyTerrain(elev, p);

            Color[] forestType;
            byte[] forestDensity;
            GenerateForests(elev, terrain, p, out forestType, out forestDensity);

            map.ElevationData = elev;
            map.TerrainType = terrain;
            map.TerrainTypeColorData = TerrainTypeColors(terrain);
            map.ForestTypeData = forestType;
            map.ForestDensityData = forestDensity;
            map.RoadData = new byte[N];
            map.Width = SIZE;
            map.Height = SIZE;
        }

        // ---- Elevation ---------------------------------------------------

        private static byte[] GenerateElevation(Parameters p)
        {
            var noise = new PerlinNoise(p.Seed);
            var shapeNoise = new PerlinNoise(unchecked(p.Seed ^ 0x5BD1E995));

            // Three frequency bands. Coarse = continent shape, mid = ridges
            // and valleys, fine = coastline dissection. Roughness shifts
            // weight toward the finer bands.
            float coarseScale = 1f / 96f;
            float midScale    = 1f / 36f;
            float fineScale   = 1f / 12f;

            int   midOctaves = LevelInt(p.Roughness, 2, 3, 4);
            float midWeight  = LevelFloat(p.Roughness, 0.20f, 0.40f, 0.65f);
            float fineWeight = LevelFloat(p.Roughness, 0.05f, 0.15f, 0.35f);

            float heightBias = LevelFloat(p.HeightAvg, -0.25f, 0.0f, 0.30f);

            // Mountains type amplifies everything so peaks reach the top
            // of the byte range and slopes stay steep.
            float typeAmp = (p.Type == MapType.Mountains) ? 1.4f : 1.0f;

            var raw = new float[N];

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    float coarse = noise.Fractal(x * coarseScale, y * coarseScale,
                        3, 0.5f, 2f);
                    float mid = noise.Fractal(x * midScale + 100f, y * midScale + 100f,
                        midOctaves, 0.5f, 2f);
                    float fine = noise.Fractal(x * fineScale + 300f, y * fineScale + 300f,
                        2, 0.5f, 2f);

                    float v = (coarse + mid * midWeight + fine * fineWeight) * typeAmp;

                    float mask = ShapeMask(p.Type, x, y, shapeNoise);

                    float norm = (v + 1f) * 0.5f;
                    norm *= mask;
                    norm += heightBias;

                    raw[y * SIZE + x] = norm;
                }
            }

            ApplyDiamondMask(raw);

            float waterFrac = LevelFloat(p.WaterRatio, 0.20f, 0.40f, 0.55f);
            float seaThreshold = PercentileInDiamond(raw, waterFrac);
            return MapToBytes(raw, seaThreshold);
        }

        private static float ShapeMask(MapType type, int x, int y, PerlinNoise shapeNoise)
        {
            float cx = SIZE * 0.5f;
            float cy = SIZE * 0.5f;
            switch (type)
            {
                case MapType.Island:
                {
                    // Organic radial: instead of a clean circle, modulate
                    // the effective radius by a low-frequency noise so the
                    // landmass is irregular and may fragment into islands.
                    float dx = (x - cx) / cx;
                    float dy = (y - cy) / cy;
                    float r = (float)Math.Sqrt(dx * dx + dy * dy);
                    float n = shapeNoise.Fractal(x / 80f, y / 80f, 2, 0.5f, 2f);
                    float warpedR = r * (0.85f + 0.30f * (1f - (n + 1f) * 0.5f));
                    float v = 1f - warpedR * 1.05f;
                    if (v < 0f) v = 0f;
                    return v * v;
                }

                case MapType.Coastal:
                {
                    // Diagonal coastline, with a noise-warped shoreline so
                    // the boundary isn't a clean line.
                    float t = (x + y) / (2f * SIZE);
                    float n = shapeNoise.Fractal(x / 64f, y / 64f, 2, 0.5f, 2f);
                    float warped = t + n * 0.10f;
                    if (warped < 0.45f) return 1f;
                    return Math.Max(0f, 1f - (warped - 0.45f) * 4f);
                }

                case MapType.Inland:
                {
                    // Mostly land — slight edge falloff so the diamond
                    // boundary doesn't have a hard cliff.
                    float dx = (x - cx) / cx;
                    float dy = (y - cy) / cy;
                    float r = (float)Math.Sqrt(dx * dx + dy * dy);
                    return Math.Max(0.55f, 1.15f - r * 0.55f);
                }

                case MapType.Mountains:
                {
                    // Centered uplift with noise variation so peaks aren't
                    // a clean dome.
                    float dx = (x - cx) / cx;
                    float dy = (y - cy) / cy;
                    float r = (float)Math.Sqrt(dx * dx + dy * dy);
                    float n = shapeNoise.Fractal(x / 96f, y / 96f, 2, 0.5f, 2f);
                    return Math.Max(0.5f, 1.30f - r * 0.55f + n * 0.08f);
                }
            }
            return 1f;
        }

        // Smooth fade outside the engine's playable diamond.
        private static void ApplyDiamondMask(float[] raw)
        {
            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    int xStart = (y < 306) ? 306 - y : y - 306;
                    int xEnd   = (y < 205) ? 307 + y : 512 - (y - 205);

                    int sD = xStart - x;
                    int eD = x - xEnd;
                    int outside = Math.Max(sD, eD);

                    if (outside > 0)
                    {
                        float fade = 1f - outside / 24f;
                        if (fade < 0f) fade = 0f;
                        raw[y * SIZE + x] *= fade * fade;
                    }
                }
            }
        }

        private static float PercentileInDiamond(float[] raw, float frac)
        {
            var samples = new List<float>(N / 16);
            for (int i = 0; i < N; i += 16) samples.Add(raw[i]);
            samples.Sort();
            int idx = (int)Math.Floor(frac * (samples.Count - 1));
            return samples[Math.Max(0, Math.Min(samples.Count - 1, idx))];
        }

        private static byte[] MapToBytes(float[] raw, float seaThreshold)
        {
            float high = seaThreshold;
            float low = seaThreshold;
            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] > high) high = raw[i];
                if (raw[i] < low) low = raw[i];
            }
            if (high <= seaThreshold + 0.001f) high = seaThreshold + 0.001f;
            if (low >= seaThreshold - 0.001f) low = seaThreshold - 0.001f;

            float landRange = high - seaThreshold;
            float waterRange = seaThreshold - low;

            var bytes = new byte[N];
            for (int i = 0; i < N; i++)
            {
                float v = raw[i];
                int b;
                if (v >= seaThreshold)
                {
                    float t = (v - seaThreshold) / landRange;
                    b = SEA_LEVEL + (int)(t * (255 - SEA_LEVEL));
                }
                else
                {
                    float t = (seaThreshold - v) / waterRange;
                    b = SEA_LEVEL - (int)(t * SEA_LEVEL);
                }
                if (b < 0) b = 0;
                if (b > 255) b = 255;
                bytes[i] = (byte)b;
            }
            return bytes;
        }

        // ---- Lakes -------------------------------------------------------

        private static void CarveLakes(byte[] elev, Parameters p, Random rng)
        {
            int count = LevelInt(p.Lakes, 0, 2, 4);
            for (int i = 0; i < count; i++)
            {
                int cx, cy;
                if (!FindInteriorTile(rng, 120, out cx, out cy)) continue;

                int radius = rng.Next(14, 32);
                // Depth in elevation units below the local terrain. We
                // overshoot SEA_LEVEL so the basin stays water even after
                // the percentile re-mapping in MapToBytes (already done by
                // this point — these byte writes are absolute).
                int depthOffset = rng.Next(35, 70);
                CarveDepression(elev, cx, cy, radius, depthOffset);
            }
        }

        private static void CarveDepression(byte[] elev, int cx, int cy, int radius, int depthOffset)
        {
            int r2 = radius * radius;
            // Falloff parameter — smaller value = sharper basin. 0.5 gives
            // a clean Gaussian-shaped depression.
            float sigma = radius * 0.5f;
            float twoSigma2 = 2f * sigma * sigma;

            for (int y = -radius; y <= radius; y++)
            {
                int yy = cy + y;
                if (yy < 0 || yy >= SIZE) continue;
                for (int x = -radius; x <= radius; x++)
                {
                    int xx = cx + x;
                    if (xx < 0 || xx >= SIZE) continue;
                    int d2 = x * x + y * y;
                    if (d2 > r2) continue;

                    float gaussian = (float)Math.Exp(-d2 / twoSigma2);
                    int idx = yy * SIZE + xx;
                    int newE = elev[idx] - (int)(gaussian * depthOffset);
                    if (newE < 0) newE = 0;
                    elev[idx] = (byte)newE;
                }
            }
        }

        // ---- Rivers ------------------------------------------------------

        private static void CarveRivers(byte[] elev, Parameters p, Random rng)
        {
            int count = LevelInt(p.Rivers, 0, 2, 4);
            var taken = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                int sx, sy;
                if (!FindRiverStart(elev, rng, taken, out sx, out sy)) continue;
                TraceRiver(elev, sx, sy);
            }
        }

        // Picks a high-elevation interior tile to start a river from.
        // Samples K candidates and returns the highest above a minimum
        // threshold; returns false if none qualify.
        private static bool FindRiverStart(byte[] elev, Random rng, HashSet<int> taken, out int sx, out int sy)
        {
            sx = sy = -1;
            int bestE = SEA_LEVEL + 60;
            for (int t = 0; t < 200; t++)
            {
                int x, y;
                if (!FindInteriorTile(rng, 90, out x, out y)) continue;
                int idx = y * SIZE + x;
                if (taken.Contains(idx)) continue;
                int e = elev[idx];
                if (e > bestE) { bestE = e; sx = x; sy = y; }
            }
            if (sx < 0) return false;
            taken.Add(sy * SIZE + sx);
            return true;
        }

        // Greedy gradient descent. Each step carves a 2-tile-wide channel
        // below sea level, finds the lowest non-backtrack neighbor, and
        // moves there. Stops on hitting water (sea or carved-lake tile),
        // exhausting steps, or finding no descent.
        private static void TraceRiver(byte[] elev, int sx, int sy)
        {
            const int CARVE_DEPTH = 12; // depth below SEA_LEVEL
            var visited = new HashSet<int>();
            int x = sx, y = sy;
            int prevX = -1, prevY = -1;

            for (int step = 0; step < 800; step++)
            {
                int idx = y * SIZE + x;
                if (visited.Contains(idx)) break;
                visited.Add(idx);

                bool reachedWater = elev[idx] < SEA_LEVEL;

                // Carve a 2x2 block centered roughly on (x,y) so the
                // channel is visibly thick. Center deepest, edges
                // shallower.
                int center = SEA_LEVEL - CARVE_DEPTH;
                int edge = SEA_LEVEL - (CARVE_DEPTH / 2);
                CarveOneIfHigher(elev, x,     y,     center);
                CarveOneIfHigher(elev, x + 1, y,     edge);
                CarveOneIfHigher(elev, x,     y + 1, edge);
                CarveOneIfHigher(elev, x + 1, y + 1, edge);

                if (reachedWater) break;

                int bestX = x, bestY = y, bestE = int.MaxValue;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int tx = x + dx, ty = y + dy;
                        if (tx < 0 || tx >= SIZE || ty < 0 || ty >= SIZE) continue;
                        if (tx == prevX && ty == prevY) continue; // don't immediately backtrack
                        int e = elev[ty * SIZE + tx];
                        if (e < bestE) { bestE = e; bestX = tx; bestY = ty; }
                    }
                }
                if (bestX == x && bestY == y) break; // no descent possible
                prevX = x; prevY = y;
                x = bestX; y = bestY;
            }
        }

        private static void CarveOneIfHigher(byte[] elev, int x, int y, int newElev)
        {
            if (x < 0 || x >= SIZE || y < 0 || y >= SIZE) return;
            int idx = y * SIZE + x;
            if (elev[idx] > newElev) elev[idx] = (byte)newElev;
        }

        // ---- Helpers for picking interior tiles --------------------------

        private static bool FindInteriorTile(Random rng, int margin, out int x, out int y)
        {
            for (int t = 0; t < 60; t++)
            {
                int tx = rng.Next(margin, SIZE - margin);
                int ty = rng.Next(margin, SIZE - margin);
                if (InDiamond(tx, ty))
                {
                    x = tx; y = ty;
                    return true;
                }
            }
            x = y = 0;
            return false;
        }

        private static bool InDiamond(int x, int y)
        {
            int xStart = (y < 306) ? 306 - y : y - 306;
            int xEnd   = (y < 205) ? 307 + y : 512 - (y - 205);
            return x >= xStart && x < xEnd;
        }

        // ---- Terrain classification --------------------------------------

        private static byte[] ClassifyTerrain(byte[] elev, Parameters p)
        {
            var t = new byte[N];

            // Initial water/land split.
            for (int i = 0; i < N; i++)
                t[i] = (elev[i] < SEA_LEVEL) ? TT_WATER : TT_GRASS;

            // 2-tile sand band around all water (sea, lakes, rivers).
            DilateSand(t);

            // Rock from EITHER high elevation OR steep slope. Snow at the
            // very top. Mountains get lower thresholds for drama.
            int rockFloor  = (p.Type == MapType.Mountains) ? 165 : 200;
            int snowFloor  = (p.Type == MapType.Mountains) ? 215 : 235;
            int slopeFloor = (p.Type == MapType.Mountains) ?  10 :  14;

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    int idx = y * SIZE + x;
                    if (t[idx] != TT_GRASS) continue;
                    int e = elev[idx];
                    if (e >= snowFloor)
                    {
                        t[idx] = TT_SNOW;
                        continue;
                    }
                    if (e >= rockFloor)
                    {
                        t[idx] = TT_ROCK;
                        continue;
                    }
                    int slope = MaxSlope(elev, x, y);
                    if (slope >= slopeFloor && e >= SEA_LEVEL + 30)
                    {
                        t[idx] = TT_ROCK;
                    }
                }
            }

            return t;
        }

        // Largest absolute elevation delta to any 8-neighbor. Used as a
        // cheap slope estimate for rock classification.
        private static int MaxSlope(byte[] elev, int x, int y)
        {
            int center = elev[y * SIZE + x];
            int maxDelta = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                int ny = y + dy;
                if (ny < 0 || ny >= SIZE) continue;
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    if (nx < 0 || nx >= SIZE) continue;
                    int delta = Math.Abs(elev[ny * SIZE + nx] - center);
                    if (delta > maxDelta) maxDelta = delta;
                }
            }
            return maxDelta;
        }

        // Two-pass dilation of TT_WATER into adjacent land. Diagonals
        // counted. Result: 2-tile-wide TT_SAND ring around every body of
        // water (ocean, lake, river).
        private static void DilateSand(byte[] t)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                var next = (byte[])t.Clone();
                for (int y = 0; y < SIZE; y++)
                {
                    for (int x = 0; x < SIZE; x++)
                    {
                        int idx = y * SIZE + x;
                        if (t[idx] != TT_GRASS) continue;
                        if (HasNeighbor(t, x, y, pass == 0 ? TT_WATER : TT_SAND))
                            next[idx] = TT_SAND;
                    }
                }
                Array.Copy(next, t, N);
            }
        }

        private static bool HasNeighbor(byte[] t, int x, int y, byte type)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int ny = y + dy;
                if (ny < 0 || ny >= SIZE) continue;
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    if (nx < 0 || nx >= SIZE) continue;
                    if (t[ny * SIZE + nx] == type) return true;
                }
            }
            return false;
        }

        private static Color[] TerrainTypeColors(byte[] t)
        {
            var c = new Color[N];
            for (int i = 0; i < N; i++) c[i] = TerrainTypeToColor(t[i]);
            return c;
        }

        private static Color TerrainTypeToColor(byte t)
        {
            switch (t)
            {
                case TT_GRASS: return new Color(0, 255, 0);
                case TT_SAND:  return new Color(255, 255, 0);
                case TT_ROCK:  return new Color(255, 0, 0);
                case TT_SNOW:  return new Color(255, 255, 255);
                case TT_WATER: return new Color(12, 0, 255);
                default:       return new Color(0, 0, 0);
            }
        }

        // ---- Forests -----------------------------------------------------

        private static void GenerateForests(byte[] elev, byte[] terrain, Parameters p,
            out Color[] type, out byte[] density)
        {
            var primary = new PerlinNoise(unchecked(p.Seed ^ 0x55AA55AA));
            var cluster = new PerlinNoise(unchecked(p.Seed ^ 0x12345678));
            type = new Color[N];
            density = new byte[N];

            // Threshold subtracted from the noise field's 0..1 range.
            // Lower = more forest. Targets ~15 / ~40 / ~60% of grass tiles.
            float threshold = LevelFloat(p.ForestDensity, 0.55f, 0.30f, 0.10f);
            Color treeColor = ForestColorForType(p.Type);

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    int i = y * SIZE + x;
                    if (terrain[i] != TT_GRASS) continue;

                    // Cluster mask: low-frequency field that scales the
                    // effective threshold and density. Patches form
                    // because regions with low cluster value drop out
                    // entirely while high-cluster regions stay dense.
                    float c = cluster.Fractal(x / 96f, y / 96f, 2, 0.5f, 2f);
                    float clusterStrength = (c + 1f) * 0.5f; // 0..1
                    if (clusterStrength < 0.25f) continue;

                    float n = primary.Fractal(x / 26f, y / 26f, 3, 0.5f, 2f);
                    float v = (n + 1f) * 0.5f - threshold;
                    if (v <= 0f) continue;

                    type[i] = treeColor;
                    int d = (int)(v * 1.6f * 255f * clusterStrength);
                    if (d > 255) d = 255;
                    density[i] = (byte)d;
                }
            }
        }

        private static Color ForestColorForType(MapType t)
        {
            switch (t)
            {
                case MapType.Island:    return new Color(255, 0xFC, 0);   // palm
                case MapType.Coastal:   return new Color(0, 0xEB, 0x42);  // birch
                case MapType.Inland:    return new Color(0, 0xEB, 0x42);  // birch
                case MapType.Mountains: return new Color(0, 0x6A, 0x28);  // fir
                default:                return new Color(0, 0x6A, 0x28);
            }
        }

        // ---- Level → numeric helpers -------------------------------------

        private static int LevelInt(Level l, int low, int med, int high)
        {
            switch (l) { case Level.Low: return low; case Level.High: return high; default: return med; }
        }

        private static float LevelFloat(Level l, float low, float med, float high)
        {
            switch (l) { case Level.Low: return low; case Level.High: return high; default: return med; }
        }
    }
}
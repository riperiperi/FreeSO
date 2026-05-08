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
            CleanupTinyFeatures(elev, terrain);

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
            var warpNoise = new PerlinNoise(unchecked(p.Seed ^ 0x7F4A7C15));

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
            bool useRidges = (p.Type == MapType.Mountains);

            // Domain-warp settings: perturb sample coords by a low-freq
            // noise field so coastlines / ridges look organic instead of
            // smooth Perlin blobs. Warp wavelength ~64, amplitude 25 — a
            // fraction of the coarse band's 96-tile wavelength so warping
            // bends the shape without scrambling it.
            const float WARP_SCALE = 1f / 64f;
            const float WARP_AMP   = 25f;

            var raw = new float[N];

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    float wx = warpNoise.Fractal(x * WARP_SCALE,        y * WARP_SCALE,        2, 0.5f, 2f) * WARP_AMP;
                    float wy = warpNoise.Fractal(x * WARP_SCALE + 100f, y * WARP_SCALE + 100f, 2, 0.5f, 2f) * WARP_AMP;
                    float wxx = x + wx;
                    float wyy = y + wy;

                    float coarse = noise.Fractal(wxx * coarseScale, wyy * coarseScale,
                        3, 0.5f, 2f);

                    // Mountains type swaps the mid octave for ridge noise:
                    // 1 - 2*|n| has its maxima where the underlying noise
                    // crosses zero, producing sharp ridge lines instead of
                    // round hills. Result range matches Perlin's [-1, 1].
                    float midRaw = noise.Fractal(wxx * midScale + 100f, wyy * midScale + 100f,
                        midOctaves, 0.5f, 2f);
                    float mid = useRidges ? (1f - 2f * Math.Abs(midRaw)) : midRaw;

                    float fine = noise.Fractal(wxx * fineScale + 300f, wyy * fineScale + 300f,
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

        // Picks the fraction-th percentile of in-diamond tile values.
        // Critical that this filters to the playable diamond — half the
        // canvas is outside, where ApplyDiamondMask has multiplied raw
        // values to ~0. Including those zeros pulls the threshold down
        // and makes the WaterRatio knob produce almost no water.
        private static float PercentileInDiamond(float[] raw, float frac)
        {
            var samples = new List<float>(N / 16);
            // Stride of 4 in each axis = 1/16 of in-diamond tiles, plenty
            // for a percentile estimate and avoids sorting 250k+ values.
            for (int y = 0; y < SIZE; y += 4)
            {
                for (int x = 0; x < SIZE; x += 4)
                {
                    if (!InDiamond(x, y)) continue;
                    samples.Add(raw[y * SIZE + x]);
                }
            }
            if (samples.Count == 0) return 0f;
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

            // Per-type bias on lake style. Alpine = small + sharp-walled,
            // valley = large + gentle. Mirrors what shows up in the
            // official Mountains and Inland cities respectively.
            float alpineBias;
            switch (p.Type)
            {
                case MapType.Mountains: alpineBias = 0.75f; break;
                case MapType.Inland:    alpineBias = 0.40f; break;
                default:                alpineBias = 0.55f; break;
            }

            for (int i = 0; i < count; i++)
            {
                int cx, cy;
                if (!FindLowInteriorTile(elev, rng, 120, out cx, out cy)) continue;

                bool alpine = rng.NextDouble() < alpineBias;
                int radius      = alpine ? rng.Next(6, 13)  : rng.Next(25, 46);
                int depthOffset = alpine ? rng.Next(25, 46) : rng.Next(50, 81);

                // Sigma controls how steep the basin walls are. Alpine
                // lakes use sigma = 0.35 * radius (sharper basin),
                // valley lakes use 0.55 (gentler shores).
                float sigmaScale = alpine ? 0.35f : 0.55f;
                CarveDepression(elev, cx, cy, radius, depthOffset, sigmaScale);
            }
        }

        // Picks an in-diamond tile biased toward low elevation. Samples
        // K candidates and returns the one with the lowest current value.
        // Lake centers placed this way drop into existing valleys instead
        // of materializing on top of mountains.
        private static bool FindLowInteriorTile(byte[] elev, Random rng, int margin, out int sx, out int sy)
        {
            sx = sy = -1;
            int bestE = int.MaxValue;
            for (int t = 0; t < 200; t++)
            {
                int x, y;
                if (!FindInteriorTile(rng, margin, out x, out y)) continue;
                int e = elev[y * SIZE + x];
                if (e < bestE) { bestE = e; sx = x; sy = y; }
            }
            return sx >= 0;
        }

        private static void CarveDepression(byte[] elev, int cx, int cy, int radius, int depthOffset, float sigmaScale = 0.5f)
        {
            int r2 = radius * radius;
            // Falloff parameter — smaller sigmaScale = sharper basin.
            // 0.35 ~ alpine (cliff-walled tarn), 0.55 ~ valley (broad shore).
            float sigma = radius * sigmaScale;
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
        // below sea level + a wider gentle valley around it (so the river
        // sits in a depression instead of a slot in flat ground), finds
        // the lowest non-backtrack neighbor, and moves there. Stops on
        // hitting water (sea or carved-lake tile), exhausting steps, or
        // finding no descent.
        private static void TraceRiver(byte[] elev, int sx, int sy)
        {
            const int CARVE_DEPTH = 12;       // depth below SEA_LEVEL at channel
            const int VALLEY_RADIUS = 6;       // tiles away from channel that get sloped
            const float VALLEY_DROP_FRAC = 0.35f; // max fraction of height-above-sea to drop

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

                // Carve valley shoulders — Gaussian-falloff drop scaled
                // by each tile's height above sea, so high-elevation
                // segments cut dramatic valleys and near-coast segments
                // stay subtle. Skips the inner 2x2 (already carved) and
                // any tile already at or below sea (don't widen existing
                // water bodies).
                float vr2 = VALLEY_RADIUS * VALLEY_RADIUS;
                float twoSigma2 = vr2 * 0.4f;
                for (int dy = -VALLEY_RADIUS; dy <= VALLEY_RADIUS; dy++)
                {
                    int ny = y + dy;
                    if (ny < 0 || ny >= SIZE) continue;
                    for (int dx = -VALLEY_RADIUS; dx <= VALLEY_RADIUS; dx++)
                    {
                        if (Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1) continue;
                        float d2 = dx * dx + dy * dy;
                        if (d2 > vr2) continue;
                        int nx = x + dx;
                        if (nx < 0 || nx >= SIZE) continue;
                        int nidx = ny * SIZE + nx;
                        int curE = elev[nidx];
                        if (curE <= SEA_LEVEL + 2) continue;
                        float falloff = (float)Math.Exp(-d2 / twoSigma2);
                        int aboveSea = curE - SEA_LEVEL;
                        int drop = (int)(falloff * aboveSea * VALLEY_DROP_FRAC);
                        if (drop <= 0) continue;
                        int newE = curE - drop;
                        if (newE < SEA_LEVEL + 1) newE = SEA_LEVEL + 1;
                        if (newE < curE) elev[nidx] = (byte)newE;
                    }
                }

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

            // Slope-aware sand band around all water. Pass 1 always
            // converts (1-tile beach, even on cliffs). Passes 2 and 3
            // only convert tiles whose local slope is gentle, so flat
            // coast gets a wide 2-3 tile beach and cliff coast stays
            // narrow. Officials show this same pattern.
            DilateSand(t, elev);

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

        // Slope-aware sand dilation. First pass: every land tile with
        // a water neighbor becomes sand (always at least a 1-tile beach
        // — gives waves something to break on). Subsequent passes
        // expand sand only where the local slope is gentle, so a flat
        // shore gets a wide 2-3 tile beach while a cliff coast stays
        // narrow.
        private static void DilateSand(byte[] t, byte[] elev)
        {
            const int GENTLE_SLOPE = 8;

            // Pass 1 — unconditional ring around water.
            var next = (byte[])t.Clone();
            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    int idx = y * SIZE + x;
                    if (t[idx] != TT_GRASS) continue;
                    if (HasNeighbor(t, x, y, TT_WATER))
                        next[idx] = TT_SAND;
                }
            }
            Array.Copy(next, t, N);

            // Passes 2 and 3 — only convert if local slope is gentle.
            for (int pass = 0; pass < 2; pass++)
            {
                next = (byte[])t.Clone();
                for (int y = 0; y < SIZE; y++)
                {
                    for (int x = 0; x < SIZE; x++)
                    {
                        int idx = y * SIZE + x;
                        if (t[idx] != TT_GRASS) continue;
                        if (!HasNeighbor(t, x, y, TT_SAND)) continue;
                        if (MaxSlope(elev, x, y) <= GENTLE_SLOPE)
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

        // Removes single-tile noise crumbs after classification:
        //   - Land tiles surrounded by mostly water (1-tile islands) → water
        //     plus elevation drop so the tile renders as proper sea.
        //   - Water tiles surrounded by mostly land (1-tile pits) → grass
        //     plus elevation raise so the tile sits above sea level.
        // Only inner-512 tiles are checked so we don't have to bounds-check.
        // Run after ClassifyTerrain (so the rock/snow/sand classification
        // is already in place) and before GenerateForests (so cleaned tiles
        // can become forested if they end up as grass).
        private static void CleanupTinyFeatures(byte[] elev, byte[] terrain)
        {
            const int WATER_NEIGHBOR_THRESHOLD = 7; // out of 8
            const int LAND_NEIGHBOR_THRESHOLD  = 7;

            var newTerrain = (byte[])terrain.Clone();
            var newElev = (byte[])elev.Clone();

            for (int y = 1; y < SIZE - 1; y++)
            {
                for (int x = 1; x < SIZE - 1; x++)
                {
                    int idx = y * SIZE + x;
                    byte t = terrain[idx];

                    int waterN = 0, landN = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            byte nt = terrain[(y + dy) * SIZE + (x + dx)];
                            if (nt == TT_WATER) waterN++;
                            else landN++;
                        }
                    }

                    if (t != TT_WATER && waterN >= WATER_NEIGHBOR_THRESHOLD)
                    {
                        // Tiny island — drown it.
                        newTerrain[idx] = TT_WATER;
                        int ne = elev[idx] - 20;
                        if (ne < 0) ne = 0;
                        if (ne >= SEA_LEVEL) ne = SEA_LEVEL - 4;
                        newElev[idx] = (byte)ne;
                    }
                    else if (t == TT_WATER && landN >= LAND_NEIGHBOR_THRESHOLD)
                    {
                        // Tiny pit — fill it.
                        newTerrain[idx] = TT_GRASS;
                        int ne = elev[idx];
                        if (ne <= SEA_LEVEL + 1) ne = SEA_LEVEL + 4;
                        newElev[idx] = (byte)ne;
                    }
                }
            }

            Array.Copy(newTerrain, terrain, N);
            Array.Copy(newElev, elev, N);
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
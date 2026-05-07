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
    /// Tunable via <see cref="Parameters"/>. Shape comes from a fractal
    /// Perlin field combined with a type-specific mask (radial for
    /// Island, directional for Coastal, flat for Inland, amplified for
    /// Mountains). The diamond-mask outer ring is always forced to deep
    /// water so the unrenderable corners stay invisible.
    /// </summary>
    public static class CityProcGen
    {
        public const int SIZE = 512;
        private const int N = SIZE * SIZE;

        // Sea level in the engine's 0..255 elevation byte range. Matches
        // the value the Clear button uses for blank-canvas land.
        private const byte SEA_LEVEL = 60;

        // Terrain-type IDs used by the engine's CityMapData.TerrainTypeMap.
        private const byte TT_GRASS = 0;
        private const byte TT_SAND  = 1;
        private const byte TT_ROCK  = 2;
        private const byte TT_SNOW  = 3;
        private const byte TT_WATER = 4;

        // Inverse map of the engine's terrain-color → terrain-type table
        // (CityMapData.cs:19). Stored as a switch in TerrainTypeToColor.

        public enum MapType { Island, Coastal, Inland, Mountains }
        public enum Level   { Low, Medium, High }

        public class Parameters
        {
            public MapType Type = MapType.Island;
            public Level HeightAvg     = Level.Medium;
            public Level WaterRatio    = Level.Medium;
            public Level Roughness     = Level.Medium;
            public Level ForestDensity = Level.Medium;
            public int Seed = 0;

            /// <summary>
            /// Fills out a sensible starting point for each map type.
            /// User can still tweak any individual knob afterwards.
            /// </summary>
            public static Parameters DefaultsFor(MapType t)
            {
                var p = new Parameters { Type = t };
                switch (t)
                {
                    case MapType.Island:
                        p.HeightAvg = Level.Medium;
                        p.WaterRatio = Level.Medium;
                        p.Roughness = Level.Medium;
                        p.ForestDensity = Level.Medium;
                        break;
                    case MapType.Coastal:
                        p.HeightAvg = Level.Medium;
                        p.WaterRatio = Level.Medium;
                        p.Roughness = Level.Medium;
                        p.ForestDensity = Level.Medium;
                        break;
                    case MapType.Inland:
                        p.HeightAvg = Level.Medium;
                        p.WaterRatio = Level.Low;
                        p.Roughness = Level.Medium;
                        p.ForestDensity = Level.High;
                        break;
                    case MapType.Mountains:
                        p.HeightAvg = Level.High;
                        p.WaterRatio = Level.Low;
                        p.Roughness = Level.High;
                        p.ForestDensity = Level.Medium;
                        break;
                }
                return p;
            }
        }

        /// <summary>
        /// Replaces all five layers in the given map with a freshly
        /// generated city. Caller is responsible for triggering a mesh
        /// regen afterwards (Terrain.GenerateCityMesh).
        /// </summary>
        public static void Generate(CityMapData map, Parameters p)
        {
            var elev = GenerateElevation(p);
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

            // Two scales: a coarse continent-shape field and a finer
            // detail field summed on top. Roughness controls how much
            // the detail dominates.
            float coarseScale  = 1f / 96f;   // ~5-6 features across 512
            float detailScale  = 1f / 24f;   // ~20 features across 512
            int   detailOctaves = LevelInt(p.Roughness, 2, 3, 5);
            float detailWeight  = LevelFloat(p.Roughness, 0.20f, 0.45f, 0.75f);

            float heightBias = LevelFloat(p.HeightAvg, -0.25f, 0.0f, 0.30f);

            var raw = new float[N];
            float min = float.MaxValue, max = float.MinValue;

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    float nx = x * coarseScale;
                    float ny = y * coarseScale;
                    float coarse = noise.Fractal(nx, ny, 3, 0.5f, 2f);

                    float dx = x * detailScale;
                    float dy = y * detailScale;
                    float detail = noise.Fractal(dx + 100f, dy + 100f,
                        detailOctaves, 0.5f, 2f);

                    float v = coarse + detail * detailWeight;

                    // Type-specific shaping mask. Multiplicative on the
                    // (re-biased to 0..1) noise so it can reduce or boost
                    // the land mass without inverting it.
                    float mask = ShapeMask(p.Type, x, y);

                    // Re-center noise into 0..1 range, apply mask, then
                    // re-center to a roughly 0..1 again.
                    float norm = (v + 1f) * 0.5f;
                    norm *= mask;
                    norm += heightBias;

                    raw[y * SIZE + x] = norm;
                    if (norm < min) min = norm;
                    if (norm > max) max = norm;
                }
            }

            // Diamond + edge fade: outside the playable diamond, force
            // depth proportional to distance-to-diamond so the corners
            // smoothly drop into deep water.
            ApplyDiamondMask(raw);

            // WaterRatio sets the sea level: pick a threshold such that
            // exactly that fraction of in-diamond tiles are below sea.
            float waterFrac = LevelFloat(p.WaterRatio, 0.20f, 0.45f, 0.65f);
            float seaThreshold = PercentileInDiamond(raw, waterFrac);

            // Map the raw field into the engine's 0..255 elevation range,
            // with seaThreshold mapping exactly to SEA_LEVEL so the rest
            // of the pipeline (terrain classification, mesh generation)
            // sees a consistent sea-level boundary.
            return MapToBytes(raw, seaThreshold);
        }

        private static float ShapeMask(MapType type, int x, int y)
        {
            float cx = SIZE * 0.5f;
            float cy = SIZE * 0.5f;
            switch (type)
            {
                case MapType.Island:
                {
                    float dx = (x - cx) / cx;
                    float dy = (y - cy) / cy;
                    float r = (float)Math.Sqrt(dx * dx + dy * dy);
                    // Smooth radial falloff: full strength at center,
                    // ~0 at the diamond corners.
                    float v = 1f - r * 1.1f;
                    if (v < 0f) v = 0f;
                    return v * v;
                }

                case MapType.Coastal:
                {
                    // Coastline runs roughly NE↔SW; one half of the map
                    // is land, the other ocean. The diagonal matches the
                    // diamond's natural orientation.
                    float t = (x + y) / (2f * SIZE);
                    return t < 0.5f ? 1f : Math.Max(0f, 1f - (t - 0.5f) * 4f);
                }

                case MapType.Inland:
                {
                    // No directional bias — land everywhere except where
                    // the noise dips below sea. Slight edge falloff to
                    // discourage water touching the outer diamond.
                    float dx = (x - cx) / cx;
                    float dy = (y - cy) / cy;
                    float r = (float)Math.Sqrt(dx * dx + dy * dy);
                    return Math.Max(0.4f, 1.1f - r * 0.6f);
                }

                case MapType.Mountains:
                {
                    // Centered uplift — peaks toward the middle, lower
                    // ground at the edges, but above sea everywhere.
                    float dx = (x - cx) / cx;
                    float dy = (y - cy) / cy;
                    float r = (float)Math.Sqrt(dx * dx + dy * dy);
                    return Math.Max(0.5f, 1.3f - r * 0.6f);
                }
            }
            return 1f;
        }

        // Smooth fade to 0 outside the engine's playable diamond. The
        // engine renders the outside as "non-buildable" — keeping it
        // deep water removes the sharp band you'd otherwise see at the
        // diamond boundary.
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
                        // Inside-the-diamond → fade to 0 over a ~24-tile
                        // border so the transition isn't a hard step.
                        float fade = 1f - outside / 24f;
                        if (fade < 0f) fade = 0f;
                        raw[y * SIZE + x] *= fade * fade;
                    }
                }
            }
        }

        private static float PercentileInDiamond(float[] raw, float frac)
        {
            // Simple sorted-sample percentile. Sampling stride = 4 to
            // keep the sort cheap (16k samples is plenty for a threshold).
            var samples = new List<float>(N / 16);
            for (int i = 0; i < N; i += 16) samples.Add(raw[i]);
            samples.Sort();
            int idx = (int)Math.Floor(frac * (samples.Count - 1));
            return samples[Math.Max(0, Math.Min(samples.Count - 1, idx))];
        }

        private static byte[] MapToBytes(float[] raw, float seaThreshold)
        {
            // Find a high reference so we map [seaThreshold .. high] to
            // [SEA_LEVEL .. 255] linearly. Below seaThreshold we map
            // linearly down to 0 (deep water).
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

        // ---- Terrain classification --------------------------------------

        private static byte[] ClassifyTerrain(byte[] elev, Parameters p)
        {
            var t = new byte[N];

            // Initial pass: water vs land by elevation only. Beach band
            // comes next.
            for (int i = 0; i < N; i++)
            {
                if (elev[i] < SEA_LEVEL) t[i] = TT_WATER;
                else                     t[i] = TT_GRASS;
            }

            // Sand band: any land tile with a water tile within 2 tiles
            // becomes sand. Two-pass dilation so we get a 2-tile-wide
            // beach ring without rescanning the whole map.
            DilateSand(t);

            // High-elevation classification on top of grass: rock above
            // a high threshold, snow above a higher one. Both leave
            // water/sand alone. Mountain maps get a lower rock floor so
            // the peaks come out properly stony.
            int rockFloor = p.Type == MapType.Mountains ? 170 : 200;
            int snowFloor = p.Type == MapType.Mountains ? 220 : 235;
            for (int i = 0; i < N; i++)
            {
                if (t[i] != TT_GRASS) continue;
                if (elev[i] >= snowFloor)      t[i] = TT_SNOW;
                else if (elev[i] >= rockFloor) t[i] = TT_ROCK;
            }

            return t;
        }

        // Two-step dilation of TT_WATER into adjacent land. Tiles within
        // 2 of any water become TT_SAND. Diagonals counted.
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

        // Inverse of CityMapData.TerrainTypeMap (CityMapData.cs:19).
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
            var noise = new PerlinNoise(p.Seed ^ 0x55AA);
            type = new Color[N];
            density = new byte[N];

            float threshold = LevelFloat(p.ForestDensity, 0.40f, 0.20f, 0.05f);
            Color treeColor = ForestColorForType(p.Type);

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    int i = y * SIZE + x;
                    if (terrain[i] != TT_GRASS) continue;

                    float n = noise.Fractal(x / 32f, y / 32f, 3, 0.5f, 2f);
                    float v = (n + 1f) * 0.5f - threshold; // 0..1 minus threshold
                    if (v <= 0f) continue;

                    type[i] = treeColor;
                    int d = (int)(v * 1.8f * 255);
                    if (d > 255) d = 255;
                    density[i] = (byte)d;
                }
            }
        }

        // Forest type from MapPainterPlugin.ForestTypes — match by
        // colour value so the engine and painter agree on the species.
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
using System;

namespace FSO.Client.Rendering.City.Plugins
{
    /// <summary>
    /// Self-contained 2D Perlin noise + fractal-sum helper. Used by
    /// <see cref="CityProcGen"/> for elevation and forest fields. No
    /// dependencies beyond System; safe to instantiate per-generation.
    /// </summary>
    public class PerlinNoise
    {
        private readonly int[] _Perm = new int[512];

        // Standard 2D gradient set — 8 unit-ish directions evenly spaced
        // around the circle. Scaled so the noise output stays in roughly
        // [-1, 1] after the dot product / interpolation.
        private static readonly float[,] _Grad = new float[8, 2]
        {
            {  1f,  0f }, { -1f,  0f }, {  0f,  1f }, {  0f, -1f },
            {  0.7071f,  0.7071f }, { -0.7071f,  0.7071f },
            {  0.7071f, -0.7071f }, { -0.7071f, -0.7071f },
        };

        public PerlinNoise(int seed)
        {
            var rng = new Random(seed);
            var p = new int[256];
            for (int i = 0; i < 256; i++) p[i] = i;
            // Fisher-Yates shuffle.
            for (int i = 255; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = p[i]; p[i] = p[j]; p[j] = tmp;
            }
            for (int i = 0; i < 512; i++) _Perm[i] = p[i & 255];
        }

        // Smoothstep used by classic Perlin: 6t^5 - 15t^4 + 10t^3.
        private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);
        private static float Lerp(float a, float b, float t) => a + t * (b - a);

        private float DotGrad(int hash, float x, float y)
        {
            int g = hash & 7;
            return _Grad[g, 0] * x + _Grad[g, 1] * y;
        }

        /// <summary>
        /// Single-octave Perlin sample. Returns roughly [-1, 1].
        /// </summary>
        public float Noise(float x, float y)
        {
            int xi = (int)Math.Floor(x) & 255;
            int yi = (int)Math.Floor(y) & 255;
            float xf = x - (float)Math.Floor(x);
            float yf = y - (float)Math.Floor(y);
            float u = Fade(xf);
            float v = Fade(yf);

            int aa = _Perm[_Perm[xi    ] + yi    ];
            int ab = _Perm[_Perm[xi    ] + yi + 1];
            int ba = _Perm[_Perm[xi + 1] + yi    ];
            int bb = _Perm[_Perm[xi + 1] + yi + 1];

            float x1 = Lerp(DotGrad(aa, xf,        yf       ), DotGrad(ba, xf - 1f, yf       ), u);
            float x2 = Lerp(DotGrad(ab, xf,        yf - 1f  ), DotGrad(bb, xf - 1f, yf - 1f  ), u);
            return Lerp(x1, x2, v);
        }

        /// <summary>
        /// Fractal-sum (fBM) of multiple octaves. Output normalized so
        /// that for typical settings (persistence 0.5, lacunarity 2.0)
        /// the result is roughly [-1, 1] independent of octave count.
        /// </summary>
        public float Fractal(float x, float y, int octaves, float persistence, float lacunarity)
        {
            float sum = 0f;
            float amp = 1f;
            float freq = 1f;
            float max = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Noise(x * freq, y * freq) * amp;
                max += amp;
                amp *= persistence;
                freq *= lacunarity;
            }
            return max > 0f ? sum / max : 0f;
        }
    }
}
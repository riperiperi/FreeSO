using System;
using System.IO;
using FSO.Common;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.Rendering.City.Plugins
{
    /// <summary>
    /// Bakes the two output PNGs FreeSO requires alongside the five input
    /// layers — vertexcolor.png and thumbnail.png — when the city painter
    /// saves a city directory.
    ///
    /// Current implementation: procedural Lambert-style hillshade modulated
    /// by terraintype color. Output is "good enough to ship" — the city
    /// loads and responds to terrain edits — but is not pixel-identical to
    /// what the live engine renders. A future stage can replace either or
    /// both with a real RenderTarget pass through the live shader.
    /// </summary>
    public static class CityBaker
    {
        private const int MAP_SIZE = 512;
        private const int THUMB_W = 180;
        private const int THUMB_H = 135;

        // Tuned to keep flat plateaus close to base color and put visible
        // shading on the slopes. Sun comes from the northwest (the angle
        // FreeSO's actual renderer uses for city lighting).
        private const float SHADE_AMBIENT = 0.55f;
        private const float SHADE_DIFFUSE = 0.45f;
        private const float SHADE_RANGE = 25f;

        /// <summary>
        /// Save vertexcolor.png and thumbnail.png into <paramref name="dir"/>.
        /// </summary>
        public static void Save(CityMapData map, string dir)
        {
            var shaded = ShadeColors(map);
            SaveTex(Path.Combine(dir, "vertexcolor.png"), MAP_SIZE, MAP_SIZE, shaded);

            var thumb = Decimate(shaded, MAP_SIZE, MAP_SIZE, THUMB_W, THUMB_H);
            SaveTex(Path.Combine(dir, "thumbnail.png"), THUMB_W, THUMB_H, thumb);
        }

        /// <summary>
        /// Lambert-style hillshade modulated by terraintype color. One pass,
        /// 512² pixels, allocates a single Color[] buffer.
        /// </summary>
        private static Color[] ShadeColors(CityMapData map)
        {
            var elev = map.ElevationData;
            var typeC = map.TerrainTypeColorData;
            var result = new Color[MAP_SIZE * MAP_SIZE];

            for (int y = 0; y < MAP_SIZE; y++)
            {
                int yEast = (y + 1 < MAP_SIZE) ? y + 1 : y;
                for (int x = 0; x < MAP_SIZE; x++)
                {
                    int xSouth = (x + 1 < MAP_SIZE) ? x + 1 : x;
                    int idx = y * MAP_SIZE + x;

                    // Slope: east-neighbour minus self, south-neighbour minus self.
                    int dx = elev[y * MAP_SIZE + xSouth] - elev[idx];
                    int dy = elev[yEast * MAP_SIZE + x] - elev[idx];

                    // Sun from NW means surfaces facing NW are lit, SE shadowed.
                    // Negative dx + negative dy → facing NW → bright.
                    float light = (-dx - dy) / SHADE_RANGE;
                    float shade = SHADE_AMBIENT + SHADE_DIFFUSE * light;
                    if (shade < 0f) shade = 0f;
                    if (shade > 1f) shade = 1f;

                    var baseColor = typeC[idx];
                    result[idx] = new Color(
                        (byte)(baseColor.R * shade),
                        (byte)(baseColor.G * shade),
                        (byte)(baseColor.B * shade),
                        (byte)255);
                }
            }
            return result;
        }

        /// <summary>
        /// Naive box-filter downscale. We're shrinking ~8× so even simple
        /// averaging gives an acceptable result for a 180×135 thumbnail.
        /// </summary>
        private static Color[] Decimate(Color[] src, int srcW, int srcH, int dstW, int dstH)
        {
            var dst = new Color[dstW * dstH];
            for (int y = 0; y < dstH; y++)
            {
                int sy0 = (y * srcH) / dstH;
                int sy1 = ((y + 1) * srcH) / dstH;
                if (sy1 <= sy0) sy1 = sy0 + 1;
                for (int x = 0; x < dstW; x++)
                {
                    int sx0 = (x * srcW) / dstW;
                    int sx1 = ((x + 1) * srcW) / dstW;
                    if (sx1 <= sx0) sx1 = sx0 + 1;

                    int r = 0, g = 0, b = 0, count = 0;
                    for (int sy = sy0; sy < sy1; sy++)
                    {
                        for (int sx = sx0; sx < sx1; sx++)
                        {
                            var c = src[sy * srcW + sx];
                            r += c.R; g += c.G; b += c.B;
                            count++;
                        }
                    }
                    dst[y * dstW + x] = new Color(
                        (byte)(r / count), (byte)(g / count), (byte)(b / count), (byte)255);
                }
            }
            return dst;
        }

        /// <summary>
        /// Mirrors CityMapData.SaveTex: GPU upload + deferred PNG write on
        /// the game thread. Avoids cross-thread GraphicsDevice access.
        /// </summary>
        private static void SaveTex(string filename, int width, int height, Color[] data)
        {
            var tex = new Texture2D(GameFacade.GraphicsDevice, width, height);
            tex.SetData(data);
            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            GameThread.NextUpdate(_ =>
            {
                var strm = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None);
                tex.SaveAsPng(strm, width, height);
                GameThread.SetTimeout(() => strm.Close(), 500);
                tex.Dispose();
            });
        }
    }
}
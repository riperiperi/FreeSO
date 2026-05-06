using System;
using System.IO;
using FSO.Common;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.Rendering.City.Plugins
{
    /// <summary>
    /// Bakes vertexcolor.png and thumbnail.png alongside the five input
    /// layers when the city painter saves a city directory.
    ///
    /// The shader treats vertexcolor as a per-pixel multiplier on top of
    /// the terrain texture (PixShader.fx:386: <c>Base *= tex2D(USampler, …);</c>),
    /// with runtime diffuse lighting + shadows applied separately. Vertex-
    /// color is therefore a *tint*, not a rendered snapshot — the official
    /// "Crafting a City" doc instructs operators to paint it manually in
    /// Photoshop. We do the same algorithmically.
    ///
    /// The tint constants are sampled from Alphaville's own vertexcolor.png
    /// (city_0100), so output is faithful to the canonical look. A subtle
    /// elevation hillshade matches the ~3% slope/flat brightness variation
    /// that exists in Alphaville's bake.
    /// </summary>
    public static class CityBaker
    {
        private const int MAP_SIZE = 512;
        private const int THUMB_W = 180;
        private const int THUMB_H = 135;

        // Canonical per-terrain tints sampled from city_0100/vertexcolor.png.
        // See Documentation/Crafting a City.md for the manual editing guidance
        // these constants algorithmically replace.
        private static readonly Color GrassTint = new Color(169, 192, 140);
        private static readonly Color WaterTint = new Color( 99, 168, 207);
        private static readonly Color RockTint  = new Color(187, 189, 152);
        private static readonly Color SnowTint  = new Color(228, 229, 218);
        private static readonly Color SandTint  = new Color(229, 240, 236);

        // Slope/flat brightness variation in Alphaville is ~3%. Keep the
        // hillshade subtle so flat plateaus don't look quilted.
        private const float SHADE_AMBIENT = 0.97f;
        private const float SHADE_DIFFUSE = 0.05f;
        private const float SHADE_RANGE = 25f;

        // Terrain type IDs from CityMapData.TerrainTypeMap (CityMapData.cs:19).
        private const byte TT_GRASS = 0;
        private const byte TT_SAND  = 1;
        private const byte TT_ROCK  = 2;
        private const byte TT_SNOW  = 3;
        private const byte TT_WATER = 4;

        public static void Save(CityMapData map, string dir)
        {
            var shaded = ShadeColors(map);
            SaveTex(Path.Combine(dir, "vertexcolor.png"), MAP_SIZE, MAP_SIZE, shaded);

            var thumb = Decimate(shaded, MAP_SIZE, MAP_SIZE, THUMB_W, THUMB_H);
            SaveTex(Path.Combine(dir, "thumbnail.png"), THUMB_W, THUMB_H, thumb);
        }

        private static Color TintFor(byte terrainType)
        {
            switch (terrainType)
            {
                case TT_WATER: return WaterTint;
                case TT_SAND:  return SandTint;
                case TT_ROCK:  return RockTint;
                case TT_SNOW:  return SnowTint;
                case TT_GRASS:
                default:       return GrassTint;
            }
        }

        private static Color[] ShadeColors(CityMapData map)
        {
            var elev = map.ElevationData;
            var type = map.TerrainType;
            var result = new Color[MAP_SIZE * MAP_SIZE];

            for (int y = 0; y < MAP_SIZE; y++)
            {
                int yEast = (y + 1 < MAP_SIZE) ? y + 1 : y;
                for (int x = 0; x < MAP_SIZE; x++)
                {
                    int xSouth = (x + 1 < MAP_SIZE) ? x + 1 : x;
                    int idx = y * MAP_SIZE + x;

                    int dx = elev[y * MAP_SIZE + xSouth] - elev[idx];
                    int dy = elev[yEast * MAP_SIZE + x] - elev[idx];

                    float light = (-dx - dy) / SHADE_RANGE;
                    float shade = SHADE_AMBIENT + SHADE_DIFFUSE * light;
                    if (shade < 0f) shade = 0f;
                    if (shade > 1f) shade = 1f;

                    var tint = TintFor(type[idx]);
                    result[idx] = new Color(
                        (byte)(tint.R * shade),
                        (byte)(tint.G * shade),
                        (byte)(tint.B * shade),
                        (byte)255);
                }
            }
            return result;
        }

        // Naive box-filter downscale — adequate for 180×135 from 512².
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

        // Mirrors CityMapData.SaveTex: GPU upload + deferred PNG write
        // on the game thread to avoid cross-thread GraphicsDevice access.
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
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
    /// vertexcolor.png is an engine *input*: the shader treats it as a
    /// per-pixel multiplier on top of the terrain texture
    /// (PixShader.fx:386: <c>Base *= tex2D(USampler, …);</c>), with runtime
    /// diffuse lighting + shadows applied separately. The official
    /// "Crafting a City" doc instructs operators to paint vertexcolor.png
    /// manually in Photoshop using canonical per-terrain tints; we do that
    /// algorithmically with constants sampled from city_0100/vertexcolor.png.
    ///
    /// thumbnail.png is rendered live: we point a RenderTarget2D at the
    /// existing Terrain renderer, drive its current camera, and capture a
    /// real isometric frame — exactly what the player will see in-game.
    /// </summary>
    public static class CityBaker
    {
        private const int MAP_SIZE = 512;
        private const int THUMB_W = 180;
        private const int THUMB_H = 135;
        // 4× supersample of THUMB_W×THUMB_H — gives a clean downscale and
        // matches the canonical 4:3 thumbnail aspect.
        private const int RENDER_W = 720;
        private const int RENDER_H = 540;

        // Per-terrain tints sampled as in-diamond medians from the
        // vibrant official cities (city_0008 / city_0010 / city_0030),
        // not Alphaville (city_0100). Alphaville's water in particular
        // is a dull (99, 168, 207) — the vibrant cities use a tropical
        // cyan around (64, 220, 255) which is what makes their renders
        // pop. These constants drive both saved vertexcolor.png and the
        // live in-memory tint texture refreshed after Generate.
        private static readonly Color GrassTint = new Color(130, 175,  95);
        private static readonly Color WaterTint = new Color( 64, 220, 255);
        private static readonly Color RockTint  = new Color(155, 160, 105);
        private static readonly Color SnowTint  = new Color(240, 240, 230);
        private static readonly Color SandTint  = new Color(250, 250, 220);

        // Diffuse weight bumped from 0.05 to 0.12 so terrain relief
        // (slope-driven shading) is visibly readable in the bake. Below
        // ~0.10 the result reads as a flat tint and the eye misses the
        // hillshade entirely.
        private const float SHADE_AMBIENT = 0.98f;
        private const float SHADE_DIFFUSE = 0.12f;
        private const float SHADE_RANGE = 25f;

        private const byte TT_GRASS = 0;
        private const byte TT_SAND  = 1;
        private const byte TT_ROCK  = 2;
        private const byte TT_SNOW  = 3;
        private const byte TT_WATER = 4;

        public static void Save(Terrain terrain, string dir)
        {
            var shaded = ShadeColors(terrain.MapData);
            SaveTex(Path.Combine(dir, "vertexcolor.png"), MAP_SIZE, MAP_SIZE, shaded);
            CaptureThumbnail(terrain, Path.Combine(dir, "thumbnail.png"));
        }

        /// <summary>
        /// Re-bakes the in-memory vertex color texture from the current
        /// CityMapData and uploads to the live VertexColor GPU texture.
        /// Call after CityProcGen.Generate so the elevation-based color
        /// gradient (lush low / dry high) shows immediately, instead of
        /// only after the user saves+reloads.
        /// </summary>
        public static void UpdateLiveVertexColor(Terrain terrain)
        {
            if (terrain == null || terrain.MapData == null
                || terrain.Content == null || terrain.Content.VertexColor == null)
                return;
            var shaded = ShadeColors(terrain.MapData);
            // Texture must be 512x512 to match the existing VertexColor
            // — both the loaded and synthesized layers are this size.
            terrain.Content.VertexColor.SetData(shaded);
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

            // Sea level matches the procgen baseline. Used for the
            // height-above-sea normalisation that drives the
            // lush/dry color gradient on grass tiles.
            const byte SEA_LEVEL_BAKE = 60;

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

                    // Elevation-based color variation for grass: at sea
                    // level pull toward a more lush green; at high
                    // elevation pull toward a drier, browner tone. Other
                    // terrain types keep their canonical tint so sand
                    // stays sand and snow stays snow regardless of
                    // height. Variation amplitude is small so the
                    // overall map still reads as Alphaville-style.
                    if (type[idx] == TT_GRASS)
                    {
                        int height = elev[idx] - SEA_LEVEL_BAKE;
                        if (height < 0) height = 0;
                        float heightT = height / 130f;
                        if (heightT > 1f) heightT = 1f;

                        // dry shifts: +R, -G, -B; lush shifts: -R, +G, +B
                        // Combined coefficients picked so transition is
                        // visible but not garish (max 12 R, 8 G, 8 B).
                        float dryAmt = heightT;
                        float lushAmt = 1f - heightT;
                        int r = tint.R + (int)(dryAmt * 12) - (int)(lushAmt * 4);
                        int g = tint.G + (int)(lushAmt * 8) - (int)(dryAmt * 6);
                        int b = tint.B + (int)(lushAmt * 4) - (int)(dryAmt * 8);
                        if (r < 0) r = 0; if (r > 255) r = 255;
                        if (g < 0) g = 0; if (g > 255) g = 255;
                        if (b < 0) b = 0; if (b > 255) b = 255;
                        tint = new Color((byte)r, (byte)g, (byte)b);
                    }

                    result[idx] = new Color(
                        (byte)(tint.R * shade),
                        (byte)(tint.G * shade),
                        (byte)(tint.B * shade),
                        (byte)255);
                }
            }
            return result;
        }

        // Renders the live Terrain to an offscreen RenderTarget. Forces
        // Far-zoom for the Draw call so the thumbnail always frames the
        // whole city — independent of where the user has the camera —
        // then restores the user's camera state. Detaches the painter
        // Plugin during the capture so the brush preview doesn't bleed in.
        private static void CaptureThumbnail(Terrain terrain, string filename)
        {
            // Run on the game thread so we don't fight the live Draw cycle.
            GameThread.NextUpdate(_ =>
            {
                var gd = GameFacade.GraphicsDevice;
                if (gd == null || terrain == null || terrain.MapData == null) return;

                RenderTarget2D rt = null;
                var savedPlugin = terrain.Plugin;
                var savedTargets = gd.GetRenderTargets();
                var savedZoom = terrain.m_Zoomed;
                var savedZoomProgress = terrain.m_ZoomProgress;
                float savedWheelZoom = 0f, savedWheelZoomTarg = 0f;
                var cam2d = terrain.Camera as CityCamera2D;
                if (cam2d != null)
                {
                    savedWheelZoom = cam2d.m_WheelZoom;
                    savedWheelZoomTarg = cam2d.m_WheelZoomTarg;
                }

                try
                {
                    rt = new RenderTarget2D(
                        gd, RENDER_W, RENDER_H, false,
                        SurfaceFormat.Color, DepthFormat.Depth24,
                        0, RenderTargetUsage.DiscardContents);

                    // Hide the brush preview during capture.
                    terrain.Plugin = null;

                    // Snap the camera to Far view (whole-city overview).
                    // m_ZoomProgress = 0 means fully Far in CityCamera2D's
                    // Near/Far interpolation. WheelZoom doesn't drive Far
                    // projection but we set a sensible value just in case.
                    terrain.m_Zoomed = TerrainZoomMode.Far;
                    terrain.m_ZoomProgress = 0f;
                    if (cam2d != null)
                    {
                        cam2d.m_WheelZoom = 0.55f;
                        cam2d.m_WheelZoomTarg = 0.55f;
                    }

                    // Camera projection caches viewport size; the RT swap
                    // changes the viewport, so invalidate before & after.
                    terrain.Camera.ProjectionDirty();

                    gd.SetRenderTarget(rt);
                    gd.Clear(Color.Black);
                    terrain.Draw(gd);
                }
                catch (Exception)
                {
                    // Capture failures should not break the save flow —
                    // the five input layers + vertexcolor.png are already
                    // committed by the time we get here.
                }
                finally
                {
                    gd.SetRenderTargets(savedTargets);
                    terrain.Plugin = savedPlugin;
                    terrain.m_Zoomed = savedZoom;
                    terrain.m_ZoomProgress = savedZoomProgress;
                    if (cam2d != null)
                    {
                        cam2d.m_WheelZoom = savedWheelZoom;
                        cam2d.m_WheelZoomTarg = savedWheelZoomTarg;
                    }
                    terrain.Camera.ProjectionDirty();
                }

                if (rt == null) return;

                var rendered = new Color[RENDER_W * RENDER_H];
                rt.GetData(rendered);

                // Force alpha to 255 — the RT's depth-cleared regions can
                // come back transparent and PNG viewers handle that
                // inconsistently.
                for (int i = 0; i < rendered.Length; i++) rendered[i].A = 255;

                var thumb = Decimate(rendered, RENDER_W, RENDER_H, THUMB_W, THUMB_H);
                rt.Dispose();

                Directory.CreateDirectory(Path.GetDirectoryName(filename));
                var tex = new Texture2D(gd, THUMB_W, THUMB_H);
                tex.SetData(thumb);
                var strm = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None);
                tex.SaveAsPng(strm, THUMB_W, THUMB_H);
                GameThread.SetTimeout(() => { strm.Close(); tex.Dispose(); }, 500);
            });
        }

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
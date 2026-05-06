using System;
using System.IO;
using FSO.Client.Debug;
using FSO.Client.Rendering.City;
using FSO.Client.Rendering.City.Plugins;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Screens
{
    /// <summary>
    /// Bare-bones screen used by FSO.CityEditor.exe. Stage 3.6 — heavy
    /// instrumentation in this build to track down a persistent
    /// no-rendering issue. Logging lands at <see cref="LogPath"/>; the
    /// red diagnostic rectangle is a visual sanity-check that this
    /// screen is even reaching its Draw override.
    /// </summary>
    public class CityEditorScreen : GameScreen, IDisposable
    {
        public Terrain CityRenderer;

        private const int InitialCityId = 100;

        // Same directory as FSO.CityEditor.exe — reliable on every platform,
        // unlike FSOEnvironment.UserDir which is "Content/" on Linux desktop
        // and resolves relative to whatever the cwd was at log time.
        private static readonly string LogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cityeditor.log");

        private int _drawCount;
        private int _updateCount;

        public CityEditorScreen() : base()
        {
            Log("=== ctor ===");
            Log($"  UIScreen.Current = {UIScreen.Current?.GetType().Name ?? "null"}");
        }

        public void Initialize()
        {
            Log("--- Initialize: begin ---");
            Log($"  UIScreen.Current = {UIScreen.Current?.GetType().Name ?? "null"}");
            Log($"  ScreenWidth/Height = {ScreenWidth} x {ScreenHeight}");
            Log($"  GraphicsDevice = {(GameFacade.GraphicsDevice != null ? "ok" : "NULL")}");

            try
            {
                CalculateMatrix();
                Log("  CalculateMatrix done");

                InitializeMap(InitialCityId);
                Log($"  InitializeMap done — Camera={CityRenderer?.Camera?.GetType().Name}");

                CityRenderer.Visible = true;
                CityRenderer.m_Zoomed = TerrainZoomMode.Far;
                CityRenderer.m_ZoomProgress = 0;

                CityRenderer.Plugin = new MapPainterPlugin(CityRenderer);
                Log("  Plugin = MapPainterPlugin");

                CityEditorHook.Editor?.OnCityReady();
                Log("--- Initialize: end (success) ---");
            }
            catch (Exception ex)
            {
                Log("INITIALIZE THREW: " + ex);
            }
        }

        private void InitializeMap(int cityMap)
        {
            Log("    InitializeMap: new Terrain");
            CityRenderer = new Terrain(GameFacade.GraphicsDevice);
            CityRenderer.m_GraphicsDevice = GameFacade.GraphicsDevice;

            Log("    InitializeMap: Terrain.Initialize");
            CityRenderer.Initialize(cityMap);

            Log("    InitializeMap: LoadContent");
            Log($"    InitializeMap: cwd = {System.Environment.CurrentDirectory}");
            Log($"    InitializeMap: ContentDir = {FSOEnvironment.ContentDir}");
            var expectedDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, FSOEnvironment.ContentDir, "Cities", "city_0100");
            Log($"    InitializeMap: expected city dir = {expectedDir}");
            Log($"    InitializeMap: city dir exists = {System.IO.Directory.Exists(expectedDir)}");
            if (System.IO.Directory.Exists(expectedDir))
            {
                foreach (var f in System.IO.Directory.GetFiles(expectedDir))
                    Log($"      file: {System.IO.Path.GetFileName(f)} ({new System.IO.FileInfo(f).Length} bytes)");
            }

            CityRenderer.LoadContent(GameFacade.GraphicsDevice);

            Log($"    InitializeMap: post-LoadContent — MapData={CityRenderer.MapData != null} Geometry={CityRenderer.Geometry != null} SubdivGeometry={CityRenderer.SubdivGeometry != null}");
            if (CityRenderer.MapData != null)
            {
                var md = CityRenderer.MapData;
                Log($"      MapData.Width = {md.Width}, Height = {md.Height}");
                Log($"      ElevationData.Length = {md.ElevationData?.Length ?? -1}");
                if (md.ElevationData != null && md.ElevationData.Length > 0)
                {
                    int min = 255, max = 0, sum = 0;
                    foreach (var v in md.ElevationData) { if (v < min) min = v; if (v > max) max = v; sum += v; }
                    Log($"      Elevation min/max/avg = {min}/{max}/{sum / md.ElevationData.Length}");
                }
                Log($"      TerrainTypeColorData.Length = {md.TerrainTypeColorData?.Length ?? -1}");
            }

            CityRenderer.RegenData = true;
            CityRenderer.SetTimeOfDay(0.5);

            Log("    InitializeMap: GameFacade.Scenes.Add");
            GameFacade.Scenes.Add(CityRenderer);
        }

        public override void GameResized()
        {
            base.GameResized();
            CalculateMatrix();
            CityRenderer?.Camera.ProjectionDirty();
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            _updateCount++;
            if (_updateCount == 1 || _updateCount == 60 || _updateCount == 300)
            {
                Log($"Update tick {_updateCount}: " +
                    $"sceneCount={GameFacade.Scenes?.Scenes?.Count} " +
                    $"renderer.Visible={CityRenderer?.Visible} " +
                    $"renderer.RegenData={CityRenderer?.RegenData} " +
                    $"subdivReady={CityRenderer?.SubdivGeometry?.Ready} " +
                    $"plugin={CityRenderer?.Plugin?.GetType().Name ?? "null"}");
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);
            _drawCount++;

            if (_drawCount == 1 || _drawCount == 60)
            {
                Log($"Draw tick {_drawCount} reached");
            }

            // Visible canary: a 200×80 red rectangle in the top-left.
            // If you see this in the running editor, this screen IS being
            // drawn — the issue is purely in the 3D scene below us.
            // If you do NOT see this, the screen itself isn't drawing.
            try
            {
                DrawLocalTexture(batch, TextureGenerator.GetPxWhite(batch.GraphicsDevice),
                    null, new Vector2(20, 20), new Vector2(200, 80), Color.Red, 0f);
            }
            catch (Exception ex)
            {
                if (_drawCount == 1) Log("Diag rect draw threw: " + ex.Message);
            }
        }

        public void Dispose()
        {
            if (CityRenderer != null)
            {
                GameFacade.Scenes.Remove(CityRenderer);
                CityRenderer.Dispose();
                CityRenderer = null;
            }
        }

        private static void Log(string msg)
        {
            try
            {
                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}");
            }
            catch
            {
                // Logging must never crash the editor.
            }
        }
    }
}
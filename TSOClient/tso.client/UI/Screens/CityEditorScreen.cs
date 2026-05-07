using System;
using System.IO;
using System.Linq;
using FSO.Client.Debug;
using FSO.Client.Rendering.City;
using FSO.Client.Rendering.City.Plugins;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
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

                // Honour the path override the launcher may have set from
                // a CLI arg. Falls back to the requested city ID (default
                // Alphaville).
                int cityId = CityEditorHook.RequestedCityId;
                if (!string.IsNullOrEmpty(CityEditorHook.RequestedCityPath))
                {
                    CityContent.PathOverride = CityEditorHook.RequestedCityPath;
                    Log($"  Loading city from path: {CityEditorHook.RequestedCityPath}");
                }
                else
                {
                    Log($"  Loading city ID: {cityId}");
                }

                InitializeMap(cityId);
                Log($"  InitializeMap done — Camera={CityRenderer?.Camera?.GetType().Name}");

                CityRenderer.Visible = true;
                // Start in Near zoom: the painter's mouse handling
                // (Terrain.Update line ~1185) is gated on
                // Camera.Zoomed == TerrainZoomMode.Near, so Far mode
                // has no tile-hover / click / TileMouseDown forwarded to
                // the plugin. Near view is what the in-game City Painter
                // uses too — MapPainterPlugin sets ForceNear = true in
                // its ctor.
                CityRenderer.m_Zoomed = TerrainZoomMode.Near;
                CityRenderer.m_ZoomProgress = 1;

                // CoreGameScreen sets HandleMouse=true via a full-screen
                // UICustomTooltipContainer that captures MouseOver events
                // and forwards them to CityRenderer.UIMouseEvent. The
                // editor screen IS the city — no "off-city" area for the
                // mouse to be over — so we just turn on HandleMouse
                // directly. Without this, the mouse-handling block in
                // Terrain.Update bails out: no hover-tile, no click, no
                // brush input, no edge-scroll cursor changes.
                CityRenderer.HandleMouse = true;

                // Pre-warm m_WheelZoom away from 0. CityCamera2D.GetIsoScale
                // computes ZisoScale = sqrt(0.5)/(288*m_WheelZoom). With
                // m_WheelZoom == 0 (struct default) that's infinity, and
                // even though far-view interpolation multiplies it by 0
                // the IEEE result is NaN. Camera.Update normally ramps
                // m_WheelZoom toward m_WheelZoomTarg each frame; jumping
                // straight to the target avoids any first-frame NaN.
                if (CityRenderer.Camera is CityCamera2D cam2d)
                {
                    cam2d.m_WheelZoom = cam2d.m_WheelZoomTarg;
                }

                var painter = new MapPainterPlugin(CityRenderer);
                CityRenderer.Plugin = painter;
                Log("  Plugin = MapPainterPlugin");

                // Top-of-screen tool palette: mode + modifier buttons,
                // brush size, save. All keyboard shortcuts still work.
                Add(new CityEditorToolbar(painter, CityRenderer));
                Log("  Toolbar added");

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
            UpdateMouseHandling(state);
            HandleZoomKeys(state);
            HandleZoomToCursor(state);
            _updateCount++;
            if (_updateCount == 1 || _updateCount == 60 || _updateCount == 300)
            {
                Log($"Update tick {_updateCount}: " +
                    $"sceneCount={GameFacade.Scenes?.Scenes?.Count} " +
                    $"renderer.Visible={CityRenderer?.Visible} " +
                    $"renderer.RegenData={CityRenderer?.RegenData} " +
                    $"subdivReady={CityRenderer?.SubdivGeometry?.Ready} " +
                    $"plugin={CityRenderer?.Plugin?.GetType().Name ?? "null"}");

                if (CityRenderer?.Geometry != null)
                {
                    var g = CityRenderer.Geometry;
                    Log($"  Geometry: LayerPrims=[{string.Join(",", g.LayerPrims)}] RoadPrims={g.RoadPrims}");
                    Log($"  Geometry.LayerVertices null counts: " +
                        $"[{string.Join(",", Enumerable.Range(0, 5).Select(i => g.LayerVertices[i] == null ? "null" : g.LayerVertices[i].VertexCount.ToString()))}]");
                }
                if (CityRenderer?.Camera != null)
                {
                    var c = CityRenderer.Camera;
                    Log($"  Camera: Zoomed={c.Zoomed} ZoomProgress={c.ZoomProgress:F3} LotZoomProgress={c.LotZoomProgress:F3}");
                }
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

        /// <summary>
        /// Three discrete zoom levels:
        ///   0 = Far          (whole-map overview, broad-stroke painting)
        ///   1 = Near medium  (zoom 0.55 — typical detail work)
        ///   2 = Near close   (zoom 1.0  — fine pixel-level edits)
        /// Tab cycles forward, Shift+Tab cycles backward, both wrap.
        /// </summary>
        private struct ZoomPreset
        {
            public TerrainZoomMode Mode;
            public float ZoomProg;
            public float WheelTarg;
            public ZoomPreset(TerrainZoomMode mode, float zoomProg, float wheelTarg)
            {
                Mode = mode; ZoomProg = zoomProg; WheelTarg = wheelTarg;
            }
        }

        private int _zoomLevel = 1;
        private static readonly ZoomPreset[] _zoomLevels =
        {
            new ZoomPreset(TerrainZoomMode.Far,  0f, 0.55f),
            new ZoomPreset(TerrainZoomMode.Near, 1f, 0.55f),
            new ZoomPreset(TerrainZoomMode.Near, 1f, 1.00f),
        };

        // Toolbar lives at the top of the screen (rows at Y=10/44/78). The
        // bottom of the brush-row buttons sits around Y≈100. Disable the
        // city's mouse handling whenever the cursor is in this strip so
        // clicks on toolbar buttons don't fall through and paint the map.
        private const int TOOLBAR_BOTTOM_Y = 110;

        // Tracks an active "center this tile" target while zoom is
        // animating after a wheel scroll. Cleared once the zoom settles
        // or the user wheels onto a new tile.
        private Vector2? _CenterTile;
        private float? _PrevWheelZoomTarg;

        private void UpdateMouseHandling(UpdateState state)
        {
            if (CityRenderer == null) return;
            float my = state.MouseState.Y / FSOEnvironment.DPIScaleFactor;
            CityRenderer.HandleMouse = my >= TOOLBAR_BOTTOM_Y;
        }

        /// <summary>
        /// Zoom controls — keyboard, since the editor doesn't have the
        /// UCP that the regular game uses for camera transitions.
        ///   Tab           : cycle zoom level forward
        ///   Shift + Tab   : cycle zoom level backward
        ///   + / =         : nudge wheel zoom in (Near only)
        ///   -             : nudge wheel zoom out (Near only)
        /// Mouse wheel is handled by CityCamera2D.Update directly; we
        /// post-correct the offset in HandleZoomToCursor below so the
        /// world point under the cursor stays put.
        /// </summary>
        private void HandleZoomKeys(UpdateState state)
        {
            if (CityRenderer == null) return;
            if (!(CityRenderer.Camera is CityCamera2D cam)) return;

            if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.Tab))
            {
                // Capture the world tile under cursor in the CURRENT view
                // before we change zoom state — that's the "anchor" we'll
                // pin under the cursor in the destination view.
                Vector2 anchor = CityRenderer.EstTileAtPosWithScroll(
                    state.MouseState.Position.ToVector2() / FSOEnvironment.DPIScaleFactor,
                    null);

                _zoomLevel = state.ShiftDown
                    ? (_zoomLevel - 1 + _zoomLevels.Length) % _zoomLevels.Length
                    : (_zoomLevel + 1) % _zoomLevels.Length;
                var lvl = _zoomLevels[_zoomLevel];

                // Snap the zoom state instantly (no animation). The anchor
                // formula uses the new View/Projection, so a mid-interpolation
                // snapshot would put the offset in the wrong place.
                CityRenderer.m_Zoomed = lvl.Mode;
                CityRenderer.m_ZoomProgress = lvl.ZoomProg;
                cam.m_WheelZoomTarg = lvl.WheelTarg;
                cam.m_WheelZoom = lvl.WheelTarg;
                cam.ProjectionDirty();

                // Far view is fixed-position (m_ViewOff is multiplied by
                // ZoomProgress=0), so centering only matters for Near levels.
                // Also skip when cursor is over the toolbar — the tile we'd
                // compute under it is meaningless.
                float toolbarY = state.MouseState.Y / FSOEnvironment.DPIScaleFactor;
                if (lvl.Mode == TerrainZoomMode.Near && toolbarY >= TOOLBAR_BOTTOM_Y &&
                    anchor.X >= 0 && anchor.X < 512 && anchor.Y >= 0 && anchor.Y < 512)
                {
                    CenterTileOnScreen(cam, anchor);
                }

                // Tab is one-shot — clear any active wheel-zoom centering.
                _CenterTile = null;
                _PrevWheelZoomTarg = cam.m_WheelZoomTarg;
            }

            // OemPlus is `=` on US keyboards; players also expect `+` to zoom in.
            // Camera only honours wheel-zoom in Near mode, so these no-op in Far.
            if (state.KeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.OemPlus) && !state.CtrlDown)
                cam.m_WheelZoomTarg = System.Math.Min(1.0f, cam.m_WheelZoomTarg + 0.01f);
            if (state.KeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.OemMinus) && !state.CtrlDown)
                cam.m_WheelZoomTarg = System.Math.Max(0.33f, cam.m_WheelZoomTarg - 0.01f);
        }

        /// <summary>
        /// Sets the camera's view offset so that the given world tile
        /// projects to the screen center in the camera's current
        /// View/Projection state. Must be called *after* the zoom state
        /// has been snapped to its new value.
        /// </summary>
        private void CenterTileOnScreen(CityCamera2D cam, Vector2 tile)
        {
            if (CityRenderer.MapData == null) return;

            int tx = System.Math.Max(0, System.Math.Min(511, (int)tile.X));
            int ty = System.Math.Max(0, System.Math.Min(511, (int)tile.Y));
            float elev = CityRenderer.GetElevationAt(tx, ty) / 12f;

            // Transform world point through current View matrix.
            var world = new Vector3(tile.X, elev, tile.Y);
            var vp = Vector3.Transform(world, cam.View);

            float zp = CityRenderer.m_ZoomProgress;
            if (zp < 0.001f) return;

            // Center: the projection's CreateOrthographicOffCenter has
            // screen_x = scrW/2 when proj_x = m_ViewOffX (likewise Y).
            // So setting m_ViewOff = vp puts the tile at screen center.
            // m_ViewOff = m_TargVOff * ZoomProgress (CityCamera2D.cs:426).
            cam.m_TargVOffX = vp.X / zp;
            cam.m_TargVOffY = vp.Y / zp;
        }

        /// <summary>
        /// Wheel-zoom centering. When the user scrolls the wheel, capture
        /// the world tile under the cursor as a center target. While the
        /// resulting zoom animation is still running, re-center on that
        /// tile every frame so the camera glides toward putting the
        /// captured tile at screen center as it zooms — same shape as the
        /// City Painter's "click in Far view to zoom and center on that
        /// tile" behavior, but driven by the wheel.
        /// </summary>
        private void HandleZoomToCursor(UpdateState state)
        {
            if (CityRenderer == null || !(CityRenderer.Camera is CityCamera2D cam)) return;

            float curWheelTarg = cam.m_WheelZoomTarg;
            if (_PrevWheelZoomTarg == null)
            {
                _PrevWheelZoomTarg = curWheelTarg;
                return;
            }

            // Detect a fresh wheel scroll: m_WheelZoomTarg jumped this
            // frame (Camera.Update set it from the new ScrollWheelValue).
            // The +/- nudge keys also tweak it — we treat them the same.
            bool wheelEvent = System.Math.Abs(curWheelTarg - _PrevWheelZoomTarg.Value) > 0.001f;
            _PrevWheelZoomTarg = curWheelTarg;

            if (wheelEvent && CityRenderer.m_Zoomed == TerrainZoomMode.Near)
            {
                float toolbarY = state.MouseState.Y / FSOEnvironment.DPIScaleFactor;
                if (toolbarY >= TOOLBAR_BOTTOM_Y)
                {
                    var t = CityRenderer.EstTileAtPosWithScroll(
                        state.MouseState.Position.ToVector2() / FSOEnvironment.DPIScaleFactor,
                        null);
                    if (t.X >= 0 && t.X < 512 && t.Y >= 0 && t.Y < 512)
                        _CenterTile = t;
                }
            }

            // Re-center every frame until the zoom animation settles —
            // m_WheelZoom catches up to m_WheelZoomTarg (CityCamera2D.cs:424).
            // Without per-frame re-centering, isoScale changes during the
            // animation drag the centered tile away from screen center.
            if (_CenterTile.HasValue &&
                CityRenderer.m_Zoomed == TerrainZoomMode.Near &&
                CityRenderer.m_ZoomProgress > 0.5f)
            {
                CenterTileOnScreen(cam, _CenterTile.Value);
                if (System.Math.Abs(cam.m_WheelZoom - cam.m_WheelZoomTarg) < 0.005f)
                    _CenterTile = null;
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
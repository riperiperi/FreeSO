using System;
using FSO.Client.Debug;
using FSO.Client.Rendering.City;
using FSO.Client.Rendering.City.Plugins;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.Model;

namespace FSO.Client.UI.Screens
{
    /// <summary>
    /// Bare-bones screen used by FSO.CityEditor.exe.
    ///
    /// Loads a city via the live renderer, auto-enables MapPainterPlugin,
    /// and skips every UI element CoreGameScreen wires up for actual
    /// gameplay (UCP, Gizmo, message tray, Discord RPC, controllers, etc).
    ///
    /// Ctor is empty by design: heavy renderer setup happens in Initialize,
    /// which the controller calls *after* the screen is added to the screen
    /// stack. The Camera projection reads UIScreen.Current's dimensions, so
    /// initializing before the screen is current produces a degenerate
    /// projection and an invisible city. Mirrors EnterSandboxMode.
    /// </summary>
    public class CityEditorScreen : GameScreen, IDisposable
    {
        public Terrain CityRenderer;

        private const int InitialCityId = 100;

        public CityEditorScreen() : base() { }

        public void Initialize()
        {
            CalculateMatrix();
            InitializeMap(InitialCityId);

            // Mirror the visible-camera state CoreGameScreen establishes
            // via its ZoomLevel=5 setter. Without these explicit values
            // first-frame defaults aren't enough — geometry never draws.
            CityRenderer.Visible = true;
            CityRenderer.m_Zoomed = TerrainZoomMode.Far;
            CityRenderer.m_ZoomProgress = 0;

            CityRenderer.Plugin = new MapPainterPlugin(CityRenderer);
            CityEditorHook.Editor?.OnCityReady();
        }

        private void InitializeMap(int cityMap)
        {
            CityRenderer = new Terrain(GameFacade.GraphicsDevice);
            CityRenderer.m_GraphicsDevice = GameFacade.GraphicsDevice;
            CityRenderer.Initialize(cityMap);
            CityRenderer.LoadContent(GameFacade.GraphicsDevice);
            CityRenderer.RegenData = true;
            CityRenderer.SetTimeOfDay(0.5);
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
    }
}
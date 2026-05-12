using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FSO.LotRenderer
{
    /// <summary>
    /// Headless IGraphicsDeviceService for Linux (no Windows.Forms dependency).
    ///
    /// Creates a MonoGame DesktopGL GraphicsDevice on the calling thread (critical: OpenGL
    /// contexts are thread-affine — all GL calls must happen on the thread that created the
    /// context).
    ///
    /// For display-free rendering:
    ///   SDL_VIDEODRIVER=offscreen freeso-renderer ...   (SDL offscreen backend)
    /// Or:
    ///   xvfb-run -a freeso-renderer ...                (virtual X display)
    ///
    /// Usage:
    ///   Call Create() on the thread that will do all rendering, then use GraphicsDevice
    ///   from that same thread only.
    /// </summary>
    public class HeadlessGraphicsDeviceService : IGraphicsDeviceService, IDisposable
    {
        private HeadlessGame _game;
        private GraphicsDeviceManager _gdm;

        public GraphicsDevice GraphicsDevice { get; private set; }

        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;

        /// <summary>
        /// Initialise the graphics device on the calling thread.
        /// Must be called from the thread that will perform all GL operations.
        /// </summary>
        public HeadlessGraphicsDeviceService()
        {
            _game = new HeadlessGame();
            _gdm = new GraphicsDeviceManager(_game)
            {
                PreferredBackBufferWidth  = 1280,
                PreferredBackBufferHeight = 720,
                GraphicsProfile           = GraphicsProfile.HiDef,
                SynchronizeWithVerticalRetrace = false,
            };

            // RunOneFrame triggers DoInitialize → platform BeforeRun → CreateDevice,
            // then returns immediately (does not enter the run-loop).
            _game.RunOneFrame();

            GraphicsDevice = _game.GraphicsDevice;
            if (GraphicsDevice == null)
                throw new Exception("HeadlessGraphicsDeviceService: GraphicsDevice is null after RunOneFrame.");
        }

        public void Release() => Dispose();

        public void Dispose()
        {
            _gdm?.Dispose();
            _game?.Dispose();
        }

        /// <summary>
        /// Minimal Game subclass — just gets us a fully-initialised GraphicsDevice.
        /// </summary>
        private class HeadlessGame : Game
        {
            public HeadlessGame()
            {
                Window.Title = "freeso-renderer";
            }

            protected override void Initialize()
            {
                base.Initialize();
                // Immediately request exit so RunOneFrame/Run returns.
                Exit();
            }
        }
    }
}

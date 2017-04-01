/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using LogThis;
using FSO.Common.Rendering.Framework;
using FSO.LotView;
using FSO.HIT;
using FSO.Client.Network;
using FSO.Client.UI;
using FSO.Client.GameContent;
using Ninject;
using FSO.Client.Regulators;
using FSO.Server.Protocol.Voltron.DataService;
using FSO.Common.DataService;
using FSO.Server.DataService.Providers.Client;
using FSO.Common.Domain;
using FSO.Common.Utils;
using FSO.Common;
using Microsoft.Xna.Framework.Audio;
using FSO.HIT.Model;
//using System.Windows.Forms;

namespace FSO.Client
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class TSOGame : FSO.Common.Rendering.Framework.Game
    {
        public UILayer uiLayer;
        public _3DLayer SceneMgr;

		public TSOGame() : base()
        {
            GameFacade.Game = this;
            if (GameFacade.DirectX) TimedReferenceController.SetMode(CacheType.PERMANENT);
            Content.RootDirectory = FSOEnvironment.GFXContentDir;
            Graphics.SynchronizeWithVerticalRetrace = true;
            
            Graphics.PreferredBackBufferWidth = GlobalSettings.Default.GraphicsWidth;
            Graphics.PreferredBackBufferHeight = GlobalSettings.Default.GraphicsHeight;
            TargetElapsedTime = new TimeSpan(10000000 / GlobalSettings.Default.TargetRefreshRate);
            FSOEnvironment.RefreshRate = GlobalSettings.Default.TargetRefreshRate;

            Graphics.HardwareModeSwitch = false;
            Graphics.ApplyChanges();

            this.Window.AllowUserResizing = true;
            this.Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);

            //might want to disable for linux
                        Log.UseSensibleDefaults();

            Thread.CurrentThread.Name = "Game";
        }

        bool newChange = false;
        void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            if (newChange || !GlobalSettings.Default.Windowed) return;
            if (Window.ClientBounds.Width == 0 || Window.ClientBounds.Height == 0) return;
            newChange = true;
            var width = Math.Max(1, Window.ClientBounds.Width);
            var height = Math.Max(1, Window.ClientBounds.Height);
            Graphics.PreferredBackBufferWidth = width;
            Graphics.PreferredBackBufferHeight = height;
            Graphics.ApplyChanges();

            GlobalSettings.Default.GraphicsWidth = width;
            GlobalSettings.Default.GraphicsHeight = height;

            newChange = false;
            if (uiLayer?.CurrentUIScreen == null) return;

            uiLayer.SpriteBatch.ResizeBuffer(GlobalSettings.Default.GraphicsWidth, GlobalSettings.Default.GraphicsHeight);
            uiLayer.CurrentUIScreen.GameResized();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            var kernel = new StandardKernel(
                new RegulatorsModule(),
                new NetworkModule(),
                new CacheModule()
            );
            GameFacade.Kernel = kernel;

            if (FSOEnvironment.DPIScaleFactor != 1 || FSOEnvironment.SoftwareDepth)
            {
                GlobalSettings.Default.GraphicsWidth = GraphicsDevice.Viewport.Width / FSOEnvironment.DPIScaleFactor;
                GlobalSettings.Default.GraphicsHeight = GraphicsDevice.Viewport.Height / FSOEnvironment.DPIScaleFactor;
            }

            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;
            GameFacade.Linux = (pid == PlatformID.MacOSX || pid == PlatformID.Unix);

            FSO.Content.Content.Init(GlobalSettings.Default.StartupPath, GraphicsDevice);
            base.Initialize();

            GameFacade.GameThread = Thread.CurrentThread;

            SceneMgr = new _3DLayer();
            SceneMgr.Initialize(GraphicsDevice);

            GameFacade.Controller = kernel.Get<GameController>();
            GameFacade.Screens = uiLayer;
            GameFacade.Scenes = SceneMgr;
            GameFacade.GraphicsDevice = GraphicsDevice;
            GameFacade.GraphicsDeviceManager = Graphics;
            GameFacade.Cursor = new CursorManager(GraphicsDevice);
            if (!GameFacade.Linux) GameFacade.Cursor.Init(FSO.Content.Content.Get().GetPath(""));

            /** Init any computed values **/
            GameFacade.Init();

            //init audio now
            HITVM.Init();
            var hit = HITVM.Get();
            hit.SetMasterVolume(HITVolumeGroup.FX, GlobalSettings.Default.FXVolume / 10f);
            hit.SetMasterVolume(HITVolumeGroup.MUSIC, GlobalSettings.Default.MusicVolume / 10f);
            hit.SetMasterVolume(HITVolumeGroup.VOX, GlobalSettings.Default.VoxVolume / 10f);
            hit.SetMasterVolume(HITVolumeGroup.AMBIENCE, GlobalSettings.Default.AmbienceVolume / 10f);

            GameFacade.Strings = new ContentStrings();
            GameFacade.Controller.StartLoading();

            GraphicsDevice.RasterizerState = new RasterizerState() { CullMode = CullMode.None };

            try {
                var audioTest = new SoundEffect(new byte[2], 44100, AudioChannels.Mono); //initialises XAudio.
                audioTest.CreateInstance().Play();
            } catch (Exception e)
            {
                //MessageBox.Show("Failed to initialize audio: \r\n\r\n" + e.StackTrace);
            }

            this.IsMouseVisible = true;
            this.IsFixedTimeStep = true;

            WorldContent.Init(this.Services, Content.RootDirectory);
            if (!FSOEnvironment.SoftwareKeyboard) AddTextInput();
            base.Screen.Layers.Add(SceneMgr);
            base.Screen.Layers.Add(uiLayer);
            GameFacade.LastUpdateState = base.Screen.State;
            //Bind ninject objects
            kernel.Bind<FSO.Content.Content>().ToConstant(FSO.Content.Content.Get());
            kernel.Load(new ClientDomainModule());

            //Have to be eager with this, it sets a singleton instance on itself to avoid packets having
            //to be created using Ninject for performance reasons
            kernel.Get<cTSOSerializer>();
            var ds = kernel.Get<DataService>();
            ds.AddProvider(new ClientAvatarProvider());

            this.Window.Title = "FreeSO";

            if (!GlobalSettings.Default.Windowed && !GameFacade.GraphicsDeviceManager.IsFullScreen)
            {
                GameFacade.GraphicsDeviceManager.ToggleFullScreen();
            }
        }

        /// <summary>
        /// Only used on desktop targets. Use extensive reflection to AVOID linking on iOS!
        /// </summary>
        void AddTextInput()
        {
            this.Window.GetType().GetEvent("TextInput").AddEventHandler(this.Window, (EventHandler<TextInputEventArgs>)GameScreen.TextInput);
        }

        void RegainFocus(object sender, EventArgs e)
        {
            GameFacade.Focus = true;
        }

        void LostFocus(object sender, EventArgs e)
        {
            GameFacade.Focus = false;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {

            Effect vitaboyEffect = null;
            try
            {
                GameFacade.MainFont = new FSO.Client.UI.Framework.Font();
                GameFacade.MainFont.AddSize(10, Content.Load<SpriteFont>("Fonts/FreeSO_10px"));
                GameFacade.MainFont.AddSize(12, Content.Load<SpriteFont>("Fonts/FreeSO_12px"));
                GameFacade.MainFont.AddSize(14, Content.Load<SpriteFont>("Fonts/FreeSO_14px"));
                GameFacade.MainFont.AddSize(16, Content.Load<SpriteFont>("Fonts/FreeSO_16px"));

                GameFacade.EdithFont = new FSO.Client.UI.Framework.Font();
                GameFacade.EdithFont.AddSize(12, Content.Load<SpriteFont>("Fonts/Trebuchet_12px"));
                GameFacade.EdithFont.AddSize(14, Content.Load<SpriteFont>("Fonts/Trebuchet_14px"));

                vitaboyEffect = Content.Load<Effect>("Effects/Vitaboy");
                uiLayer = new UILayer(this, Content.Load<SpriteFont>("Fonts/FreeSO_12px"), Content.Load<SpriteFont>("Fonts/FreeSO_16px"));
            }
            catch (Exception e)
            {
                //MessageBox.Windows.Forms.MessageBox.Show("Content could not be loaded. Make sure that the FreeSO content has been compiled! (ContentSrc/TSOClientContent.mgcb)");
                Exit();
            }

            FSO.Vitaboy.Avatar.setVitaboyEffect(vitaboyEffect);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }
       
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            GameThread.UpdateExecuting = true;

            if (HITVM.Get() != null) HITVM.Get().Tick();

            base.Update(gameTime);
            GameThread.UpdateExecuting = false;
        }
    }
}

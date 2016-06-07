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
using FSO.Common;
using Microsoft.Xna.Framework.Audio;
using System.Windows.Forms;

namespace FSO.Client
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class TSOGame : FSO.Common.Rendering.Framework.Game
    {
        public UILayer uiLayer;
        public _3DLayer SceneMgr;

        public TSOGame()
        {
            GameFacade.Game = this;
            Content.RootDirectory = FSOEnvironment.GFXContentDir;
            Graphics.SynchronizeWithVerticalRetrace = true; //GameFacade.DirectX || GlobalSettings.Default.Windowed; //why was this disabled
            
            Graphics.PreferredBackBufferWidth = GlobalSettings.Default.GraphicsWidth;
            Graphics.PreferredBackBufferHeight = GlobalSettings.Default.GraphicsHeight;

            Graphics.HardwareModeSwitch = false;
            Graphics.ApplyChanges();

            //disabled for now. It's a hilarious mess and is causing linux to freak out.
            //Log.UseSensibleDefaults();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;
            GameFacade.Linux = (pid == PlatformID.MacOSX || pid == PlatformID.Unix);

            FSO.Content.Content.Init(GlobalSettings.Default.StartupPath, GraphicsDevice);
            base.Initialize();

            GameFacade.GameThread = Thread.CurrentThread;

            SceneMgr = new _3DLayer();
            SceneMgr.Initialize(GraphicsDevice);

            GameFacade.Controller = new GameController();
            GameFacade.Screens = uiLayer;
            GameFacade.Scenes = SceneMgr;
            GameFacade.GraphicsDevice = GraphicsDevice;
            GameFacade.GraphicsDeviceManager = Graphics;
            GameFacade.Cursor = new CursorManager(this.Window);
            if (!GameFacade.Linux) GameFacade.Cursor.Init(FSO.Content.Content.Get().GetPath(""));

            /** Init any computed values **/
            GameFacade.Init();

            //init audio now
            HITVM.Init();

            GameFacade.Strings = new ContentStrings();
            GameFacade.Controller.StartLoading();

            GraphicsDevice.RasterizerState = new RasterizerState() { CullMode = CullMode.None };

            try {
                var audioTest = new SoundEffect(new byte[2], 44100, AudioChannels.Mono); //initialises XAudio.
                audioTest.CreateInstance().Play();
            } catch (Exception e)
            {
                MessageBox.Show("Failed to initialize audio: \r\n\r\n" + e.StackTrace);
            }

            this.IsMouseVisible = true;
            this.IsFixedTimeStep = true;

            WorldContent.Init(this.Services, Content.RootDirectory);

            base.Screen.Layers.Add(SceneMgr);
            base.Screen.Layers.Add(uiLayer);
            GameFacade.LastUpdateState = base.Screen.State;
            this.Window.TextInput += GameScreen.TextInput;
            this.Window.Title = "FreeSO";

            if (!GlobalSettings.Default.Windowed)
            {
                GameFacade.GraphicsDeviceManager.ToggleFullScreen();
            }
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
                System.Windows.Forms.MessageBox.Show("Content could not be loaded. Make sure that the FreeSO content has been compiled! (ContentSrc/TSOClientContent.mgcb)");
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
            NetworkFacade.Client.ProcessPackets();
            if (HITVM.Get() != null) HITVM.Get().Tick();

            base.Update(gameTime);
        }
    }
}

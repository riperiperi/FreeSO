/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Un4seen.Bass;
using System.Threading;
using LogThis;
using FSO.Common.Rendering.Framework;
using FSO.LotView;
using FSO.HIT;
using FSO.Client.Network;
using FSO.Client.UI;
using FSO.Client.GameContent;

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
            Content.RootDirectory = "Content";
            Graphics.SynchronizeWithVerticalRetrace = true; //why was this disabled

            Graphics.PreferredBackBufferWidth = GlobalSettings.Default.GraphicsWidth;
            Graphics.PreferredBackBufferHeight = GlobalSettings.Default.GraphicsHeight;

            Graphics.ApplyChanges();

            Log.UseSensibleDefaults();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            FSO.Content.Content.Init(GlobalSettings.Default.StartupPath, GraphicsDevice);
            base.Initialize();

            GameFacade.SoundManager = new FSO.Client.Sound.SoundManager();
            GameFacade.GameThread = Thread.CurrentThread;

            SceneMgr = new _3DLayer();
            SceneMgr.Initialize(GraphicsDevice);

            GameFacade.Controller = new GameController();
            GameFacade.Screens = uiLayer;
            GameFacade.Scenes = SceneMgr;
            GameFacade.GraphicsDevice = GraphicsDevice;
            GameFacade.Cursor = new CursorManager(this.Window);
            GameFacade.Cursor.Init(FSO.Content.Content.Get().GetPath(""));

            /** Init any computed values **/
            GameFacade.Init();

            GameFacade.Strings = new ContentStrings();
            GameFacade.Controller.StartLoading();

            GraphicsDevice.RasterizerState = new RasterizerState() { CullMode = CullMode.None }; //no culling until i find a good way to do this in xna4 (apparently recreating state obj is bad?)

            BassNet.Registration("afr088@hotmail.com", "2X3163018312422");
                Bass.BASS_Init(-1, 8000, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero, System.Guid.Empty);

            this.IsMouseVisible = true;

            this.IsFixedTimeStep = true;

            WorldContent.Init(this.Services, Content.RootDirectory);

            base.Screen.Layers.Add(SceneMgr);
            base.Screen.Layers.Add(uiLayer);
            GameFacade.LastUpdateState = base.Screen.State;
            if (!GlobalSettings.Default.Windowed) Graphics.ToggleFullScreen();

            //var test = new IDE.TestEditor();
            //test.Show();

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
                GameFacade.MainFont.AddSize(10, Content.Load<SpriteFont>("Fonts/ProjectDollhouse_10px"));
                GameFacade.MainFont.AddSize(12, Content.Load<SpriteFont>("Fonts/ProjectDollhouse_12px"));
                GameFacade.MainFont.AddSize(14, Content.Load<SpriteFont>("Fonts/ProjectDollhouse_14px"));
                GameFacade.MainFont.AddSize(16, Content.Load<SpriteFont>("Fonts/ProjectDollhouse_16px"));

                GameFacade.EdithFont = new FSO.Client.UI.Framework.Font();
                GameFacade.EdithFont.AddSize(12, Content.Load<SpriteFont>("Fonts/Trebuchet_12px"));
                GameFacade.EdithFont.AddSize(14, Content.Load<SpriteFont>("Fonts/Trebuchet_14px"));

                vitaboyEffect = GameFacade.Game.Content.Load<Effect>("Effects\\Vitaboy");
                uiLayer = new UILayer(this, Content.Load<SpriteFont>("Fonts/ProjectDollhouse_12px"), Content.Load<SpriteFont>("Fonts/ProjectDollhouse_16px"));
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("Content could not be loaded. Make sure that the Project Dollhouse content has been compiled! (ContentSrc/TSOClientContent.mgcb)");
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
            GameFacade.SoundManager.MusicUpdate();
            if (HITVM.Get() != null) HITVM.Get().Tick();

            base.Update(gameTime);
        }
    }
}

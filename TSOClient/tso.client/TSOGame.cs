/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TSOClient;
using Un4seen.Bass;
using Microsoft.Win32;
using TSOClient.Code.UI.Model;
using TSOClient.LUI;
using TSOClient.Code;
using System.Threading;
using TSOClient.Code.UI.Framework;
using LogThis;
using TSO.Common.rendering.framework.model;
using TSO.Common.rendering.framework;
using tso.world;
using TSO.HIT;
using TSO.Files;

namespace TSOClient
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class TSOGame : TSO.Common.rendering.framework.Game
    {
        public UILayer uiLayer;
        public _3DLayer SceneMgr;

        public TSOGame()
        {
            GameFacade.Game = this;
            Content.RootDirectory = "Content";

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
            TSO.Content.Content.Init(GlobalSettings.Default.StartupPath, GraphicsDevice);

            // TODO: Add your initialization logic here
            if (GlobalSettings.Default.Windowed)
                Graphics.IsFullScreen = false;
            else
                Graphics.IsFullScreen = false;

            GraphicsDevice.RasterizerState = new RasterizerState() { CullMode = CullMode.None }; //no culling until i find a good way to do this in xna4 (apparently recreating state obj is bad?)

            BassNet.Registration("afr088@hotmail.com", "2X3163018312422");
            Bass.BASS_Init(-1, 8000, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero, System.Guid.Empty);

            this.IsMouseVisible = true;

            this.IsFixedTimeStep = true;
            Graphics.SynchronizeWithVerticalRetrace = true; //why was this disabled

            Graphics.PreferredBackBufferWidth = GlobalSettings.Default.GraphicsWidth;
            Graphics.PreferredBackBufferHeight = GlobalSettings.Default.GraphicsHeight;

            WorldContent.Init(this.Services, Content.RootDirectory);
            Graphics.ApplyChanges();

            base.Initialize();
            base.Screen.Layers.Add(SceneMgr);
            base.Screen.Layers.Add(uiLayer);
            GameFacade.LastUpdateState = base.Screen.State;
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
            GameFacade.MainFont = new TSOClient.Code.UI.Framework.Font();
            GameFacade.MainFont.AddSize(10, Content.Load<SpriteFont>("Fonts/ProjectDollhouse_10px"));
            GameFacade.MainFont.AddSize(12, Content.Load<SpriteFont>("Fonts/ProjectDollhouse_12px"));
            GameFacade.MainFont.AddSize(14, Content.Load<SpriteFont>("Fonts/ProjectDollhouse_14px"));
            GameFacade.MainFont.AddSize(16, Content.Load<SpriteFont>("Fonts/ProjectDollhouse_16px"));

            GameFacade.SoundManager = new TSOClient.Code.Sound.SoundManager();
            GameFacade.GameThread = Thread.CurrentThread;

            //uiLayer = new UILayer(this, Content.Load<SpriteFont>("ComicSans"), Content.Load<SpriteFont>("ComicSansSmall"));
            uiLayer = new UILayer(this, Content.Load<SpriteFont>("Fonts/ProjectDollhouse_12px"), Content.Load<SpriteFont>("Fonts/ProjectDollhouse_16px"));
            SceneMgr = new _3DLayer();
            SceneMgr.Initialize(GraphicsDevice);

            GameFacade.Controller = new GameController();
            GameFacade.Screens = uiLayer;
            GameFacade.Scenes = SceneMgr;
            GameFacade.GraphicsDevice = GraphicsDevice;
            GameFacade.Cursor = new CursorManager(this.Window);
            GameFacade.Cursor.Init(TSO.Content.Content.Get().GetPath(""));

            /** Init any computed values **/
            GameFacade.Init();

            GameFacade.Strings = new ContentStrings();
            GameFacade.Controller.StartLoading();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        private float m_FPS = 0;

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            m_FPS = (float)(1 / gameTime.ElapsedGameTime.TotalSeconds);

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                this.Exit();

            GameFacade.SoundManager.MusicUpdate();
            if (HITVM.Get() != null) HITVM.Get().Tick();

            base.Update(gameTime);
        }
    }
}

/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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
using LuaInterface;
using Microsoft.Win32;
using TSOClient.Code.UI.Model;
using TSOClient.LUI;
using TSOClient.Code;
using System.Threading;
using TSOClient.Code.UI.Framework;
using LogThis;
using tso.common.rendering.framework.model;
using tso.common.rendering.framework;
using tso.world;

namespace TSOClient
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : tso.common.rendering.framework.Game
    {
        UISpriteBatch spriteBatch;

        public UILayer uiLayer;
        public _3DLayer SceneMgr;

        public Game1()
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
            GlobalSettings.Default.StartupPath = @"C:\Program Files\Maxis\The Sims Online\TSOClient\";
            tso.content.Content.Init(GlobalSettings.Default.StartupPath, GraphicsDevice);

            // TODO: Add your initialization logic here
            if (GlobalSettings.Default.Windowed)
                Graphics.IsFullScreen = false;
            else
                Graphics.IsFullScreen = false;

            GraphicsDevice.RenderState.CullMode = CullMode.CullClockwiseFace;

            BassNet.Registration("afr088@hotmail.com", "2X3163018312422");
            Bass.BASS_Init(-1, 8000, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero, System.Guid.Empty);

            this.IsMouseVisible = true;

            this.IsFixedTimeStep = true;
            Graphics.SynchronizeWithVerticalRetrace = true; //why was this disabled

            Graphics.PreferredBackBufferWidth = GlobalSettings.Default.GraphicsWidth;
            Graphics.PreferredBackBufferHeight = GlobalSettings.Default.GraphicsHeight;

            //800 * 600 is the default resolution. Since all resolutions are powers of 2, just scale using
            //the width (because the height would end up with the same scalefactor).
            GlobalSettings.Default.ScaleFactor = GlobalSettings.Default.GraphicsWidth / 800;
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
            // TODO: use this.Content to load your game content here
            int Channel = Bass.BASS_StreamCreateFile("Sounds\\BUTTON.WAV", 0, 0, BASSFlag.BASS_DEFAULT);
            UISounds.AddSound(new UISound(0x01, Channel));

            GameFacade.MainFont = new TSOClient.Code.UI.Framework.Font();
            GameFacade.MainFont.AddSize(10, Content.Load<SpriteFont>("Fonts/ProjectDollhouse_10px"));
            GameFacade.MainFont.AddSize(12, Content.Load<SpriteFont>("Fonts/ProjectDollhouse_12px"));
            GameFacade.MainFont.AddSize(14, Content.Load<SpriteFont>("Fonts/ProjectDollhouse_14px"));
            GameFacade.MainFont.AddSize(16, Content.Load<SpriteFont>("Fonts/ProjectDollhouse_16px"));

            GameFacade.SoundManager = new TSOClient.Code.Sound.SoundManager();
            GameFacade.GameThread = Thread.CurrentThread;

            uiLayer = new UILayer(this, Content.Load<SpriteFont>("ComicSans"), Content.Load<SpriteFont>("ComicSansSmall"));
            SceneMgr = new _3DLayer();
            SceneMgr.Initialize(GraphicsDevice);

            GameFacade.Controller = new GameController();
            GameFacade.Screens = uiLayer;
            GameFacade.Scenes = SceneMgr;
            GameFacade.GraphicsDevice = GraphicsDevice;
            GameFacade.Cursor = new CursorManager(this.Window);
            GameFacade.Cursor.Init(tso.content.Content.Get().GetPath(""));

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

            base.Update(gameTime);
        }
    }
}

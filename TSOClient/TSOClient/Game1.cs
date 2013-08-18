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
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using TSOClient;
using TSOClient.Network;
using TSOClient.ThreeD;
using SimsLib.FAR3;
using LogThis;
using Un4seen.Bass;
using LuaInterface;
using Microsoft.Win32;
using TSOClient.Code.UI.Model;
using TSOClient.LUI;
using TSOClient.Code;
using System.Threading;
using TSOClient.Code.UI.Framework;

namespace TSOClient
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        UISpriteBatch spriteBatch;

        public ScreenManager ScreenMgr;
        public SceneManager SceneMgr;

        public Game1()
        {
            GameFacade.Game = this;

            graphics = new GraphicsDeviceManager(this);
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
            // TODO: Add your initialization logic here
            if (GlobalSettings.Default.Windowed)
                graphics.IsFullScreen = false;
            else
                graphics.IsFullScreen = false;

            GraphicsDevice.RenderState.CullMode = CullMode.None;

            BassNet.Registration("afr088@hotmail.com", "2X3163018312422");
            Bass.BASS_Init(-1, 8000, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero, System.Guid.Empty);

            this.IsMouseVisible = true;

            //Might want to reconsider this...
            this.IsFixedTimeStep = false;
            graphics.SynchronizeWithVerticalRetrace = false;

            StreamReader SReader = new StreamReader(File.OpenRead(GlobalSettings.Default.StartupPath + "version"));
            GlobalSettings.Default.ClientVersion = SReader.ReadLine().Trim();
            SReader.Close();

            graphics.PreferredBackBufferWidth = GlobalSettings.Default.GraphicsWidth;
            graphics.PreferredBackBufferHeight = GlobalSettings.Default.GraphicsHeight;

            //800 * 600 is the default resolution. Since all resolutions are powers of 2, just scale using
            //the width (because the height would end up with the same scalefactor).
            GlobalSettings.Default.ScaleFactor = GlobalSettings.Default.GraphicsWidth / 800;

            graphics.ApplyChanges();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new UISpriteBatch(GraphicsDevice, 3);

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

            ScreenMgr = new ScreenManager(this, Content.Load<SpriteFont>("ComicSans"),
                Content.Load<SpriteFont>("ComicSansSmall"));
            SceneMgr = new SceneManager(this);

            GameFacade.Controller = new GameController();
            GameFacade.Screens = ScreenMgr;
            GameFacade.Scenes = SceneMgr;
            GameFacade.GraphicsDevice = GraphicsDevice;

            /** Init any computed values **/
            GameFacade.Init();

            GameFacade.LastUpdateState = m_UpdateState;
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
        /// Object used to store info used in the update loop, no reason to make
        /// a new one each loop.
        /// </summary>
        private UpdateState m_UpdateState = new UpdateState();

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            m_FPS = (float)(1 / gameTime.ElapsedGameTime.TotalSeconds);

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                this.Exit();

            m_UpdateState.Time = gameTime;
            m_UpdateState.MouseState = Mouse.GetState();
            m_UpdateState.PreviousKeyboardState = m_UpdateState.KeyboardState;
            m_UpdateState.KeyboardState = Keyboard.GetState();
            m_UpdateState.SharedData.Clear();
            m_UpdateState.Update();
            
            ScreenMgr.Update(m_UpdateState);
            SceneMgr.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            
            /** Any pre-draw work **/
            lock (GraphicsDevice)
            {
                spriteBatch.UIBegin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);
                ScreenMgr.PreDraw(spriteBatch);
                spriteBatch.End();
            }

            GraphicsDevice.Clear(new Color(23, 23, 23));
            GraphicsDevice.RenderState.AlphaBlendEnable = true;
            GraphicsDevice.RenderState.DepthBufferEnable = true;
            
            //Deferred sorting seems to just work...
            //NOTE: Using SaveStateMode.SaveState is IMPORTANT to make 3D rendering work properly!
            lock (GraphicsDevice)
            {
                spriteBatch.UIBegin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);
                ScreenMgr.Draw(spriteBatch, m_FPS);
                spriteBatch.End();
                SceneMgr.Draw();
            }
        }
    }
}

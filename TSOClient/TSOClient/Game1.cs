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
using TSOClient.LUI;
using TSOClient.Network;
using TSOClient.ThreeD;
using SimsLib.FAR3;
using LogThis;
using Un4seen.Bass;
using LuaInterface;
using Microsoft.Win32;

namespace TSOClient
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        public ScreenManager ScreenMgr;
        public SceneManager SceneMgr;

        private Dictionary<int, string> m_TextDict = new Dictionary<int, string>();

        public Game1()
        {
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
                graphics.IsFullScreen = true;

            GraphicsDevice.VertexDeclaration = new VertexDeclaration(GraphicsDevice, 
                VertexPositionNormalTexture.VertexElements);
            GraphicsDevice.RenderState.CullMode = CullMode.None;

            BassNet.Registration("afr088@hotmail.com", "2X3163018312422");
            Bass.BASS_Init(-1, 8000, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero, System.Guid.Empty);

            this.IsMouseVisible = true;

            //Might want to reconsider this...
            this.IsFixedTimeStep = false;
            graphics.SynchronizeWithVerticalRetrace = false;

            //InitLoginNotify - 2 bytes
            NetworkClient.RegisterLoginPacketID(0x01, 2);
            //LoginFailResponse - 2 bytes
            NetworkClient.RegisterLoginPacketID(0x02, 2);
            /*LoginSuccessResponse - 33 bytes
            NetworkClient.RegisterLoginPacketID(0x04, 33);*/
            //CharacterInfoResponse - Variable size
            NetworkClient.RegisterLoginPacketID(0x05, 0);

            //Read settings...
            LuaFunctions.ReadSettings("gamedata\\settings\\settings.lua");

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
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            int Channel = Bass.BASS_StreamCreateFile("Sounds\\BUTTON.WAV", 0, 0, BASSFlag.BASS_DEFAULT);
            UISounds.AddSound(new UISound(0x01, Channel));

            ScreenMgr = new ScreenManager(this, Content.Load<SpriteFont>("ComicSans"),
                Content.Load<SpriteFont>("ComicSansSmall"));
            SceneMgr = new SceneManager(this);

            //Make the screenmanager, scenemanager and the startup path globally available to all Lua scripts.
            LuaInterfaceManager.ExportObject("ScreenManager", ScreenMgr);
            LuaInterfaceManager.ExportObject("ThreeDManager", SceneMgr);
            LuaInterfaceManager.ExportObject("StartupPath", GlobalSettings.Default.StartupPath);
            LuaInterfaceManager.ExportObject("GraphicsWidth", GlobalSettings.Default.GraphicsWidth);
            LuaInterfaceManager.ExportObject("GraphicsHeight", GlobalSettings.Default.GraphicsHeight);

            LoadStrings();
            ScreenMgr.TextDict = m_TextDict;

            if (GlobalSettings.Default.GraphicsWidth == 800)
                ScreenMgr.LoadInitialScreen("gamedata\\luascripts\\loading.lua");
            else
                ScreenMgr.LoadInitialScreen("gamedata\\luascripts\\loading_1024.lua");
            ContentManager.InitLoading();
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
            base.Update(gameTime);

            m_FPS = (float)(1 / gameTime.ElapsedGameTime.TotalSeconds);

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here
            ScreenMgr.Update(gameTime);
            SceneMgr.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            
            GraphicsDevice.Clear(new Color(23, 23, 23));

            //Deferred sorting seems to just work...
            //NOTE: Using SaveStateMode.SaveState is IMPORTANT to make 3D rendering work properly!
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);

            ScreenMgr.Draw(spriteBatch, m_FPS);

            spriteBatch.End();

            SceneMgr.Draw();
        }

        /// <summary>
        /// Loads the correct set of strings based on the current language.
        /// This method is a bit of a hack, but it works.
        /// </summary>
        private void LoadStrings()
        {
            string CurrentLang = GlobalSettings.Default.CurrentLang.ToLower();

            LuaInterfaceManager.RunFileInThread("gamedata\\uitext\\luatext\\" +
                CurrentLang + "\\" + CurrentLang + ".lua");

            m_TextDict.Add(1, (string)LuaInterfaceManager.LuaVM["LoginName"]);
            m_TextDict.Add(2, (string)LuaInterfaceManager.LuaVM["LoginPass"]);
            m_TextDict.Add(3, (string)LuaInterfaceManager.LuaVM["Login"]);
            m_TextDict.Add(4, (string)LuaInterfaceManager.LuaVM["Exit"]);
            m_TextDict.Add(5, (string)LuaInterfaceManager.LuaVM["OverallProgress"]);
            m_TextDict.Add(6, (string)LuaInterfaceManager.LuaVM["CurrentTask"]);
            m_TextDict.Add(7, (string)LuaInterfaceManager.LuaVM["InfoPopup1"]);
            m_TextDict.Add(8, (string)LuaInterfaceManager.LuaVM["PersonSelectionCaption"]);
            m_TextDict.Add(9, (string)LuaInterfaceManager.LuaVM["TimeStart"]);
            m_TextDict.Add(10, (string)LuaInterfaceManager.LuaVM["PersonSelectionEditCaption"]);
            m_TextDict.Add(11, (string)LuaInterfaceManager.LuaVM["CreateASim"]);
            m_TextDict.Add(12, (string)LuaInterfaceManager.LuaVM["RetireASim"]);

            //Loading strings
            m_TextDict.Add(13, (string)LuaInterfaceManager.LuaVM["LoadText1"]);
            m_TextDict.Add(14, (string)LuaInterfaceManager.LuaVM["LoadText2"]);
            m_TextDict.Add(15, (string)LuaInterfaceManager.LuaVM["LoadText3"]);
            m_TextDict.Add(16, (string)LuaInterfaceManager.LuaVM["LoadText4"]);
            m_TextDict.Add(17, (string)LuaInterfaceManager.LuaVM["LoadText5"]);
            m_TextDict.Add(18, (string)LuaInterfaceManager.LuaVM["LoadText6"]);
            m_TextDict.Add(19, (string)LuaInterfaceManager.LuaVM["LoadText7"]);
            m_TextDict.Add(20, (string)LuaInterfaceManager.LuaVM["LoadText8"]);
            m_TextDict.Add(21, (string)LuaInterfaceManager.LuaVM["LoadText9"]);
            m_TextDict.Add(22, (string)LuaInterfaceManager.LuaVM["LoadText10"]);
            m_TextDict.Add(23, (string)LuaInterfaceManager.LuaVM["LoadText11"]);
        }
    }
}

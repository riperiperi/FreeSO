using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace CityRenderer
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        //Which city are we loading?
        public const int CITY_NUMBER = 3;

        private Terrain m_Terrain;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 768;
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            this.IsMouseVisible = true;
            graphics.DeviceResetting += new EventHandler(GraphicsDevice_DeviceResetting);

            CityDataRetriever cityData = new CityDataRetriever();

            m_Terrain = new Terrain(GraphicsDevice, CITY_NUMBER, cityData, Content);
            m_Terrain.Initialize();
            m_Terrain.RegenData = true;


            //Shadow configuration. Very Low quality res: 512, Low quality: 1024, High quality: 2048
            m_Terrain.ShadowsEnabled = true;
            m_Terrain.ShadowRes = 2048;

            base.Initialize();
        }

        private void GraphicsDevice_DeviceResetting(object sender, EventArgs e)
        {
            UnloadContent();
            LoadContent();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            m_Terrain.m_GraphicsDevice = GraphicsDevice;
            m_Terrain.LoadContent(GraphicsDevice, Content);
            m_Terrain.RegenData = true;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            m_Terrain.UnloadEverything(); //call this when you're removing the cityview.
            Content.Unload();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            m_Terrain.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.RenderState.DepthBufferEnable = true;
            GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
            GraphicsDevice.RenderState.AlphaBlendEnable = true;
            m_Terrain.Draw(GraphicsDevice);

            base.Draw(gameTime);
        }
    }
}

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
        SpriteBatch spriteBatch;

        //Which city are we loading?
        public const int CITY_NUMBER = 30;

        private Matrix m_ProjectionViewMatrix, m_ViewMatrix, m_WorldMatrix;

        private Terrain m_Terrain;
        private Effect m_VertexShader, m_PixelShader;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
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
            m_ProjectionViewMatrix = m_ViewMatrix = m_WorldMatrix = Matrix.Identity;
            GraphicsDevice.VertexDeclaration = new VertexDeclaration(GraphicsDevice, MeshVertex.VertexElements);
            GraphicsDevice.RenderState.CullMode = CullMode.None;

            GraphicsDevice.DeviceResetting += new EventHandler(GraphicsDevice_DeviceResetting);

            base.Initialize();
        }

        private void GraphicsDevice_DeviceResetting(object sender, EventArgs e)
        {
            m_ProjectionViewMatrix = m_ViewMatrix = m_WorldMatrix = Matrix.Identity;
            GraphicsDevice.VertexDeclaration = new VertexDeclaration(GraphicsDevice, MeshVertex.VertexElements);
            GraphicsDevice.RenderState.CullMode = CullMode.None;
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
            m_VertexShader = Content.Load<Effect>("VerShader");
            m_PixelShader = Content.Load<Effect>("PixShader");

            m_Terrain = new Terrain(GraphicsDevice, CITY_NUMBER);
            m_Terrain.Initialize();
            m_Terrain.GenerateCityMesh(GraphicsDevice);
            m_Terrain.CreateTextureAtlas(spriteBatch);
            m_Terrain.CreateTransparencyAtlas(spriteBatch);
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
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            GraphicsDevice.RenderState.DepthBufferEnable = true;
            GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
            GraphicsDevice.RenderState.AlphaBlendEnable = false;

            // TODO: Add your drawing code here
            spriteBatch.Begin();

            /*spriteBatch.Draw(m_Terrain.TransAtlas, new Rectangle(0, 0, m_Terrain.TransAtlas.Width, 
                m_Terrain.TransAtlas.Height), Color.White);*/
            m_Terrain.Draw(m_VertexShader, m_PixelShader, m_ProjectionViewMatrix, m_ViewMatrix, m_WorldMatrix);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}

using System;
using System.IO;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace TSOClientMono
{
	public class Game1 : Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;
		
		//public ScreenManager ScreenMgr;
		
		private Dictionary<int, string> m_TextDict = new Dictionary<int, string>();
		
		string tsoDir;
		
		public Game1(string tsoDir)
		{
			if (Directory.Exists(tsoDir))
			{
				this.tsoDir = tsoDir;
			}
			else
			{
				Console.WriteLine("The TSO directory you have provided does not exist.");
				Environment.Exit(1);
			}
			
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
		}
		
		protected override void Initialize()
		{
			graphics.PreferredBackBufferWidth = 800;
			graphics.PreferredBackBufferHeight = 800;
			
			this.IsMouseVisible = false;
			
			this.IsFixedTimeStep = false;
			graphics.SynchronizeWithVerticalRetrace = false;
			
			base.Initialize();
		}
		
		protected override void LoadContent()
		{
			spriteBatch = new SpriteBatch(GraphicsDevice);
		}
		
		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(new Color(23, 23, 23));
			spriteBatch.Begin(SpriteBlendMode.AlphaBlend);
			//ScreenMgr.Draw(spriteBatch, m_FPS);
			spriteBatch.End();
			base.Draw(gameTime);
		}
	}
}

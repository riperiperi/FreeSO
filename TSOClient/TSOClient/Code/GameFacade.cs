using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code
{
    /// <summary>
    /// Central point for accessing game objects
    /// </summary>
    public class GameFacade
    {
        public static GameController Controller;
        public static ScreenManager Screens;
        public static GraphicsDevice GraphicsDevice;
    }
}

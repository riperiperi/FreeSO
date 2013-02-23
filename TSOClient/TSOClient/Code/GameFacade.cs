using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Debug;
using TSOClient.Code.UI.Framework;

namespace TSOClient.Code
{
    /// <summary>
    /// Central point for accessing game objects
    /// </summary>
    public class GameFacade
    {
        public static ContentStrings Strings;
        public static GameController Controller;
        public static ScreenManager Screens;
        public static GraphicsDevice GraphicsDevice;
        public static Game1 Game;
        public static TSOClientTools DebugWindow;
        public static Font MainFont;


        /**
         * Important top level events
         */
        public static event BasicEventHandler OnContentLoaderReady;



        public static void TriggerContentLoaderReady()
        {
            if (OnContentLoaderReady != null)
            {
                OnContentLoaderReady();
            }
        }
    }


    public delegate void BasicEventHandler();

}

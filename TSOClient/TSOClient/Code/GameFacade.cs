using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Debug;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Model;
using TSOClient.Code.Sound;
using System.IO;
using System.Threading;

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
        public static SoundManager SoundManager;
        public static UpdateState LastUpdateState;
        public static Thread GameThread;


        public static BlobCache Cache;

        /// <summary>
        /// Place where the game can store cached values, e.g. pre modified textures to improve
        /// 2nd load speed, etc.
        /// </summary>
        public static string CacheDirectory;
        public static string CacheRoot = @"E:\Games\TSOCache\";


        public static void Init()
        {
            CacheDirectory = Path.Combine(CacheRoot, "_pdcache");
            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
            }
        }



        /**
         * Important top level events
         */
        public static event BasicEventHandler OnContentLoaderReady;

        public static string GameFilePath(string relativePath)
        {
            return Path.Combine(GlobalSettings.Default.StartupPath, relativePath);
        }


        public static void TriggerContentLoaderReady()
        {
            if (OnContentLoaderReady != null)
            {
                OnContentLoaderReady();
            }
        }



        public static TimeSpan GameRunTime
        {
            get
            {
                if (LastUpdateState.Time != null)
                {
                    return LastUpdateState.Time.TotalRealTime;
                }
                else
                {
                    return new TimeSpan(0);
                }
            }
        }
    }


    public delegate void BasicEventHandler();

}

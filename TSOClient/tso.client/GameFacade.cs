/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.Debug;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Client.Sound;
using System.IO;
using System.Threading;
using FSO.Client.Utils;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework;
using FSO.Client.UI.Panels;
using FSO.Client.Rendering.City;
using FSO.Client.GameContent;
using FSO.Client.UI;
using Microsoft.Xna.Framework;

namespace FSO.Client
{
    /// <summary>
    /// Central point for accessing game objects
    /// </summary>
    public class GameFacade
    {
        public static ContentStrings Strings;
        public static GameController Controller;
        public static UILayer Screens;
        public static _3DLayer Scenes;
        public static GraphicsDevice GraphicsDevice;
        public static GraphicsDeviceManager GraphicsDeviceManager;
        public static TSOGame Game;
        public static TSOClientTools DebugWindow;
        public static Font MainFont;
        public static Font EdithFont;
        public static SoundManager SoundManager;
        public static UpdateState LastUpdateState;
        public static Thread GameThread;
        public static bool Focus = true;

        public static CursorManager Cursor;
        public static UIMessageController MessageController = new UIMessageController();

        //Entries received from city server, see UIPacketHandlers.OnCityTokenResponse()
        public static CityDataRetriever CDataRetriever = new CityDataRetriever();

        /// <summary>
        /// Place where the game can store cached values, e.g. pre modified textures to improve
        /// 2nd load speed, etc.
        /// </summary>
        public static string CacheDirectory;
        public static string CacheRoot = @"TSOCache\";

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

        /// <summary>
        /// This gets the number of a city when provided with a name.
        /// </summary>
        /// <param name="CityName">Name of the city.</param>
        /// <returns>Number of the city.</returns>
        public static int GetCityNumber(string CityName)
        {
            switch (CityName)
            {
                case "Blazing Falls":
                    return 1;
                case "Alphaville":
                    return 2;
                case "Test Center":
                    return 3;
                case "Interhogan":
                    return 4;
                case "Ocean's Edge":
                    return 5;
                case "East Jerome":
                    return 6;
                case "Fancy Fields":
                    return 7;
                case "Betaville":
                    return 8;
                case "Charvatia":
                    return 9;
                case "Dragon's Cove":
                    return 10;
                case "Rancho Rizzo":
                    return 11;
                case "Zavadaville":
                    return 12;
                case "Queen Margaret's":
                    return 13;
                case "Shannopolis":
                    return 14;
                case "Grantley Grove":
                    return 15;
                case "Calvin's Creek":
                    return 16;
                case "The Billabong":
                    return 17;
                case "Mount Fuji":
                    return 18;
                case "Dan's Grove":
                    return 19;
                case "Jolly Pines":
                    return 20;
                case "Yatesport":
                    return 21;
                case "Landry Lakes":
                    return 22;
                case "Nichol's Notch":
                    return 23;
                case "King Canyons":
                    return 24;
                case "Virginia Islands":
                    return 25;
                case "Pixie Point":
                    return 26;
                case "West Darrington":
                    return 27;
                case "Upper Shankelston":
                    return 28;
                case "Albertstown":
                    return 29;
                case "Terra Tablante":
                    return 30;
            }

            return 1;
        }


        /// <summary>
        /// Kills the application.
        /// </summary>
        public static void Kill()
        {
            //TODO: Add any needed deconstruction here.
            Game.Exit();
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
                if (LastUpdateState != null && LastUpdateState.Time != null)
                {
                    return LastUpdateState.Time.TotalGameTime;
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

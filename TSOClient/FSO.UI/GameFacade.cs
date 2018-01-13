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
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using System.IO;
using System.Threading;
using FSO.Client.Utils;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework;
using FSO.Client.GameContent;
using FSO.Client.UI;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace FSO.Client
{
    /// <summary>
    /// Central point for accessing game objects
    /// </summary>
    public class GameFacade
    {
        public static ContentStrings Strings; //todo: cross tso/ts1
        public static UILayer Screens;
        public static _3DLayer Scenes;
        public static GraphicsDevice GraphicsDevice;
        public static GraphicsDeviceManager GraphicsDeviceManager;
        public static Common.Rendering.Framework.Game Game;
        //public static TSOClientTools DebugWindow;
        public static Font MainFont;
        public static Font EdithFont;
        public static UpdateState LastUpdateState;
        public static Thread GameThread;
        public static bool Focus = true;
        public static string CurrentCityName = "Sandbox";

        public static bool Linux;
        public static bool DirectX;
        public static bool EnableMod;

        public static CursorManager Cursor;

        //Entries received from city server, see UIPacketHandlers.OnCityTokenResponse()

        public static void Init()
        {
        }

        public static string GameFilePath(string relativePath)
        {
            return Path.Combine(GlobalSettings.Default.StartupPath, relativePath);
        }

        /// <summary>
        /// Kills the application.
        /// </summary>
        public static void Kill()
        {
            //TODO: Add any needed deconstruction here.
            Game.Exit();
            Process.GetCurrentProcess().Kill();
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

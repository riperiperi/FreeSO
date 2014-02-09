/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

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
using TSOClient.Code.Utils;
using tso.common.rendering.framework.model;
using tso.common.rendering.framework;

namespace TSOClient.Code
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
        public static Game1 Game;
        public static TSOClientTools DebugWindow;
        public static Font MainFont;
        public static SoundManager SoundManager;
        public static UpdateState LastUpdateState;
        public static Thread GameThread;
        public static bool Focus = true;

        public static CursorManager Cursor;

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

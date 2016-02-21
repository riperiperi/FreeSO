/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using FSO.Client.Utils.GameLocator;
using FSO.Client.Utils;

namespace FSO.Client
{
    public static class Program
    {

        public static bool UseDX;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        [STAThread]
        public static void Main(string[] args)
        {
            if (InitWithArguments(args))
                (new GameStartProxy()).Start(UseDX);
        }

        public static bool InitWithArguments(string[] args)
        {
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;

            ILocator gameLocator;
            if (pid == PlatformID.MacOSX || pid == PlatformID.Unix) gameLocator = new LinuxLocator();
            else gameLocator = new WindowsLocator();

            bool useDX = false;

            #region User resolution parmeters

            foreach (var arg in args)
            {
                if (char.IsDigit(arg[0]))
                {
                    //attempt parsing resoulution
                    try
                    {
                        var split = arg.Split("x".ToCharArray());
                        int ScreenWidth = int.Parse(split[0]);
                        int ScreenHeight = int.Parse(split[1]);

                        GlobalSettings.Default.GraphicsWidth = ScreenWidth;
                        GlobalSettings.Default.GraphicsHeight = ScreenHeight;
                    }
                    catch (Exception) { }
                }
                else if (arg[0] == '-')
                {
                    //normal style param
                    switch (arg.Substring(1))
                    {
                        case "dx11":
                        case "dx":
                            useDX = true;
                            break;
                    }
                }
                else
                {
                    if (arg.Equals("w", StringComparison.InvariantCultureIgnoreCase))
                        GlobalSettings.Default.Windowed = true;
                    else if (arg.Equals("f", StringComparison.InvariantCultureIgnoreCase))
                        GlobalSettings.Default.Windowed = false;
                }
            }

            #endregion

            UseDX = MonogameLinker.Link(useDX);

            var path = gameLocator.FindTheSimsOnline();

            if (UseDX) GlobalSettings.Default.AntiAlias = false;

            if (path != null)
            {
                GlobalSettings.Default.StartupPath = path;
                GlobalSettings.Default.ClientVersion = GetClientVersion();
                return true;
            }
            else
            {
                MessageBox.Show("The Sims Online was not found on your system. FreeSO will not be able to run without access to the original game files.");
                return false;
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Exception: \r\n" + e.ExceptionObject.ToString());
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            LogThis.Log.LogThis("Exception: " + e.Exception.ToString(), LogThis.eloglevel.error);
            MessageBox.Show("Exception: \r\n" + e.Exception.ToString());
        }

        /// <summary>
        /// Loads the client's version from "Client.manifest".
        /// This is here because it should be one of the first
        /// things the client does when it starts.
        /// </summary>
        /// <returns>The version.</returns>
        private static string GetClientVersion()
        {
            string ExeDir = GlobalSettings.Default.StartupPath;

            //Never make an assumption that a file exists.
            if (File.Exists(ExeDir + "\\Client.manifest"))
            {
                using (BinaryReader Reader = new BinaryReader(File.Open(ExeDir + "\\Client.manifest", FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    return Reader.ReadString() + ".0"; //Last version number is unused.
                }
            }
            else
            {
                //Version as of writing this method.
                return "0.1.26.0";
            }
        }
    }
}

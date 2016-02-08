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

namespace FSO.Client
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        [STAThread]
        public static void Main(string[] args)
        {

            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException +=new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;

            ILocator gameLocator;
            if (pid == PlatformID.MacOSX || pid == PlatformID.Unix) gameLocator = new LinuxLocator();
            else gameLocator = new WindowsLocator();


#region User resolution parmeters
            if (args.Length > 0)
            {
                int ScreenWidth = int.Parse(args[0].Split("x".ToCharArray())[0]);
                int ScreenHeight = int.Parse(args[0].Split("x".ToCharArray())[1]);

                GlobalSettings.Default.GraphicsWidth = ScreenWidth;
                GlobalSettings.Default.GraphicsHeight = ScreenHeight;

                if (args.Length >= 1)
                {
                    if (args[1].Equals("w", StringComparison.InvariantCultureIgnoreCase))
                        GlobalSettings.Default.Windowed = true;
                    else if (args[1].Equals("f", StringComparison.InvariantCultureIgnoreCase))
                        GlobalSettings.Default.Windowed = false;
                }
            }
            #endregion

            var path = gameLocator.FindTheSimsOnline();

            if (path != null)
            {
                GlobalSettings.Default.StartupPath = path;
                using (TSOGame game = new TSOGame())
                {
                    GlobalSettings.Default.ClientVersion = GetClientVersion();
                    game.Run();
                }
            }
            else
            {
                MessageBox.Show("The Sims Online was not found on your system. FreeSO will not be able to run without access to the original game files.");
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

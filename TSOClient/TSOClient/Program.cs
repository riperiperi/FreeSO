/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using TSOClient.LUI;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Security.Permissions;
using System.Security;
using System.Security.Principal;

namespace TSOClient
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            RegistryKey softwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE");
            if (Array.Exists(softwareKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("Microsoft") == 0; }))
            {
                RegistryKey msKey = softwareKey.OpenSubKey("Microsoft");
                if (Array.Exists(msKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("XNA") == 0; }))
                {
                    RegistryKey xnaKey = msKey.OpenSubKey("XNA");
                    if (Array.Exists(xnaKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("Framework") == 0; }))
                    {
                        RegistryKey asmKey = xnaKey.OpenSubKey("Framework");
                        if (Array.Exists(asmKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("v3.1") == 0; }))
                        {
                            // OK
                        }
                        else
                        {
                            MessageBox.Show("XNA was found to be installed on your system, but you do not have version 3.1. Please download and install XNA version 3.1.");
                        }
                    }
                    else
                        MessageBox.Show("XNA was found to be installed on your system, but certain components are missing. Please (re)download and (re)install XNA version 3.1.");
                }
                else
                    MessageBox.Show("XNA was not found to be installed on your system. Please download and install XNA version 3.1.");
            }
            else
                MessageBox.Show("Error: No Microsoft products were found on your system.");

            if (args.Length > 0)
            {
                int ScreenWidth = int.Parse(args[0].Split("x".ToCharArray())[0]);
                int ScreenHeight = int.Parse(args[0].Split("x".ToCharArray())[1]);

                if (args.Length >= 1)
                {
                    if (args[1] == "windowed" || args[1] == "Windowed")
                        GlobalSettings.Default.Windowed = true;
                }
            }

            //NICHOLAS: There is no need for this now. I'm not running the game as an admin and it works fine for me.
            //          We can enable this in a Release build.
            //if (System.Environment.OSVersion.Platform == PlatformID.Win32Windows || (System.Environment.OSVersion.Platform == PlatformID.Win32NT && System.Environment.OSVersion.Version.Major < 6 ) || IsAdministrator)
            //{
                using (Game1 game = new Game1())
                {
                    //LuaFunctionAttribute.RegisterAllLuaFunctions(game, LuaInterfaceManager.LuaVM);
                    game.Run();
                }
            //}
            //else
                //MessageBox.Show("Please close this message box and run TSOClient.exe as an administrator.");
        }
        
        /// <summary>
        /// Determines whether or not the program is being run as an administrator.
        /// </summary>
        private static bool IsAdministrator
        {
            get
            {
                WindowsIdentity wi = WindowsIdentity.GetCurrent();
                WindowsPrincipal wp = new WindowsPrincipal(wi);

                return wp.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}


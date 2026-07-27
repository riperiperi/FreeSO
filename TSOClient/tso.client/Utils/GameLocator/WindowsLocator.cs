using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace FSO.Client.Utils.GameLocator
{
    public class WindowsLocator : ILocator
    {
        public string FindTheSimsOnline()
        {
            string Software = "";

            // Search relative directory similar to how macOS and Linux works; allows portability
            string localDir = @"../The Sims Online/TSOClient/";
            if (ILocator.ValidPath(localDir)) return localDir;

            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                //Find the path to TSO on the user's system.
                RegistryKey softwareKey = hklm.OpenSubKey("SOFTWARE");

                if (Array.Exists(softwareKey.GetSubKeyNames(), delegate (string s) { return s.Equals("Maxis", StringComparison.InvariantCultureIgnoreCase); }))
                {
                    RegistryKey maxisKey = softwareKey.OpenSubKey("Maxis");
                    if (Array.Exists(maxisKey.GetSubKeyNames(), delegate (string s) { return s.Equals("The Sims Online", StringComparison.InvariantCultureIgnoreCase); }))
                    {
                        RegistryKey tsoKey = maxisKey.OpenSubKey("The Sims Online");
                        string installDir = (string)tsoKey.GetValue("InstallDir");
                        installDir += @"\TSOClient\";
                        installDir = installDir.Replace('\\', '/');

                        if (ILocator.ValidPath(installDir))
                        {
                            return installDir;
                        }
                    }
                }
            }

            string defaultPath = "C:/Program Files/Maxis/The Sims Online/TSOClient/";

            if (ILocator.ValidPath(defaultPath)) return defaultPath;

            // If nothing was found, try appdata (the user will be asked to install here if it's not already there)
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "The Sims Online", "TSOClient");
        }

        private static bool is64BitProcess = (IntPtr.Size == 8);
        private static bool is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );

        /// <summary>
        /// Determines if this process is run on a 64bit OS.
        /// </summary>
        /// <returns>True if it is, false otherwise.</returns>
        public static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
                Environment.OSVersion.Version.Major >= 6)
            {
                using (Process p = Process.GetCurrentProcess())
                {
                    bool retVal;
                    if (!IsWow64Process(p.Handle, out retVal))
                    {
                        return false;
                    }
                    return retVal;
                }
            }
            else
            {
                return false;
            }
        }
    }
}

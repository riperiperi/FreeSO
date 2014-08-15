using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
using LogThis;

namespace PDPatcher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Log.UseSensibleDefaults("PDPatcher.txt", "C:\\", eloglevel.error);

            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.Automatic);

            RegistryKey softwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE");

            bool MaxisExists = false;

            if (Array.Exists(softwareKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("Maxis") == 0; }))
            {
                MaxisExists = true;

                RegistryKey maxisKey = softwareKey.OpenSubKey("Maxis");
                if (Array.Exists(maxisKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("The Sims Online") == 0; }))
                {
                    RegistryKey tsoKey = maxisKey.OpenSubKey("The Sims Online");
                    string installDir = (string)tsoKey.GetValue("InstallDir");
                    installDir += "\\TSOClient\\";
                    GlobalSettings.Default.ClientPath = installDir;
                }
                else
                    MessageBox.Show("Error TSO was not found on your system.");
            }

            if (!MaxisExists)
            {
                RegistryKey NodeKey = softwareKey.OpenSubKey("Wow6432Node");

                if (Array.Exists(NodeKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("Maxis") == 0; }))
                {
                    MaxisExists = true;

                    RegistryKey maxisKey = NodeKey.OpenSubKey("Maxis");
                    if (Array.Exists(maxisKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("The Sims Online") == 0; }))
                    {
                        RegistryKey tsoKey = maxisKey.OpenSubKey("The Sims Online");
                        string installDir = (string)tsoKey.GetValue("InstallDir");
                        installDir += "\\TSOClient\\";
                        GlobalSettings.Default.ClientPath = installDir;
                    }
                    else
                        MessageBox.Show("Error TSO was not found on your system.");
                }
                else
                    MessageBox.Show("No Maxis products were found on your system!");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        public static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Log.LogThis("Unhandled exception: \n" + e.Exception.ToString(), eloglevel.error);
        }
    }
}

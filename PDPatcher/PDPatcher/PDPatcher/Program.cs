using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
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

            //If a new version of the patcher's been downloaded, it will have been renamed to "...NEW.exe".
            if (Application.ExecutablePath.Contains(" NEW.exe"))
            {
                //TODO: Modify this to support any number of new files?
                File.Move(Application.ExecutablePath, Application.ExecutablePath.Replace(" NEW.exe", ".exe"));
                File.Move(Application.StartupPath + "KISS.net NEW.dll", 
                    Application.StartupPath + "KISS.net.dll");
                File.Move(Application.ExecutablePath + ".config", 
                    Application.ExecutablePath.Replace(" NEW.exe.config", ".exe.config"));
                File.Move(Application.StartupPath + "Interop.Shell32 NEW.dll",
                    Application.StartupPath + "Interop.Shell32.dll");

                //Modify desktop shortcut to point to this executable.
                Program.ModifyShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + 
                    "Project Dollhouse.lnk", Application.StartupPath + "PDPatcher.exe");
            }

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
                {
                    MessageBox.Show("Error TSO was not found on your system.");
                    return;
                }
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
                    {
                        MessageBox.Show("Error TSO was not found on your system.");
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("No Maxis products were found on your system!");
                    return;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        public static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show("Exception: \r\n" + e.ToString());
            Log.LogThis("Unhandled exception: \n" + e.Exception.ToString(), eloglevel.error);

            //May want to change this...
            Application.Exit();
        }

        /// <summary>
        /// Modifies a shortcut to point to a new path.
        /// </summary>
        /// <param name="ShortcutPath">Full path to shortcut.</param>
        /// <param name="NewPath">New path of shortcut.</param>
        public static void ModifyShortcut(string ShortcutPath, string NewPath)
        {
            Shell32.Shell Shl = new Shell32.ShellClass();
            Shell32.Folder Folder = Shl.NameSpace(Path.GetFullPath(ShortcutPath));
            Shell32.FolderItem Item = Folder.Items().Item(Path.GetFileName(ShortcutPath));
            Shell32.ShellLinkObject Link = (Shell32.ShellLinkObject)Item.GetLink;

            Link.Path = NewPath;
        }
    }
}

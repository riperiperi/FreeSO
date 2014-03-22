using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Security.Principal;

namespace PDBootstrap
{
    class Program
    {
        /// <summary>
        /// The function checks whether the current process is run as administrator.
        /// In other words, it dictates whether the primary access token of the 
        /// process belongs to user account that is a member of the local 
        /// Administrators group and it is elevated.
        /// </summary>
        /// <returns>
        /// Returns true if the primary access token of the process belongs to user 
        /// account that is a member of the local Administrators group and it is 
        /// elevated. Returns false if the token does not.
        /// </returns>
        internal static bool IsRunAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void Main(string[] args)
        {
            string WorkingDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string TSOPatchDir = WorkingDir.Replace("TSOClient", "TSOPatch");

            //If a new version exists, it will reside in "TSOClient" along
            //with this bootstrapper, so move it over.
            if (File.Exists(WorkingDir + "\\PDPatcher.exe"))
            {
                File.Delete(TSOPatchDir + "\\PDPatcher.exe");
                File.Move(WorkingDir + "\\PDPatcher.exe", TSOPatchDir + "\\PDPatcher.exe");
            }
            if (File.Exists(WorkingDir + "\\KISS.net.dll"))
            {
                File.Delete(TSOPatchDir + "\\KISS.net.dll");
                File.Move(WorkingDir + "\\KISS.net.dll", TSOPatchDir + "\\KISS.net.dll");
            }
            if (File.Exists(WorkingDir + "\\PDPatcher.exe.config"))
            {
                File.Delete(TSOPatchDir + "\\PDPatcher.exe.config");
                File.Move(WorkingDir + "\\PDPatcher.exe.config", TSOPatchDir + "\\PDPatcher.exe.config");
            }

            ProcessStartInfo Proc = new ProcessStartInfo();

            if (Environment.OSVersion.Version.Major >= 6)
            {
                if (!IsRunAsAdmin())
                {
                    // Launch itself as administrator
                    Proc.UseShellExecute = true;
                    Proc.WorkingDirectory = TSOPatchDir;
                    Proc.FileName = TSOPatchDir + "\\PDPatcher.exe";
                    Proc.Verb = "runas";
                    Proc.WorkingDirectory = TSOPatchDir;

                    try
                    {
                        Process.Start(Proc);
                    }
                    catch
                    {
                        Console.WriteLine("You need to run PDPatcher as admin!");
                        Console.ReadKey();

                        return;
                    }

                    Environment.Exit(0);  //Quit
                }
            }

            //Bootstrap...
            Proc.UseShellExecute = true;
            Proc.WorkingDirectory = TSOPatchDir;
            Proc.FileName = TSOPatchDir + "\\PDPatcher.exe";
            Proc.WorkingDirectory = TSOPatchDir;

            try
            {
                Process.Start(Proc);
            }
            catch
            {
                Console.WriteLine("You need to run PDPatcher as admin!");
                Console.ReadKey();

                return;
            }

            Environment.Exit(0);  //Quit

        }
    }
}

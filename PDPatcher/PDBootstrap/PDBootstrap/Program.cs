using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace PDBootstrap
{
    class Program
    {
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

            //Bootstrap...
            Process.Start(TSOPatchDir + "\\PDPatcher.exe");
        }
    }
}

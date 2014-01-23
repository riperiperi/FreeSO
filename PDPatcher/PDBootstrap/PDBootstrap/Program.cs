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
            //If a new version exists, it will reside in "TSOClient" along
            //with this bootstrapper, so move it over.
            if (File.Exists("PDPatcher.exe"))
            {
                File.Move("PDPatcher.exe", "..\\TSOPatch\\PDPatcher.exe");
                File.Move("KISS.net.dll", "..\\TSOPatch\\KISS.net.dll");
            }

            //Bootstrap...
            Process.Start("..\\TSOPatch\\PDPatcher.exe");
        }
    }
}

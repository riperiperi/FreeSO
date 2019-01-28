using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FSO.Client.Utils.GameLocator
{
    public class LinuxLocator : ILocator
    {
        public string FindTheSimsOnline()
        {
            string localDir = @"../The Sims Online/TSOClient/";
            if (File.Exists(Path.Combine(localDir, "tuning.dat"))) return localDir;

            return "game/TSOClient/";
        }
    }
}

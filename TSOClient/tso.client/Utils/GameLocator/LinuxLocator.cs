using System;
using System.IO;

namespace FSO.Client.Utils.GameLocator
{
    public class LinuxLocator : ILocator
    {
        public string FindTheSimsOnline()
        {
            string localDir = @"../The Sims Online/TSOClient/";
            if (File.Exists(Path.Combine(localDir, "tuning.dat"))) return localDir;

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string homeDir = Path.Combine(home, "Documents", "The Sims Online", "TSOClient") + "/";
            if (File.Exists(Path.Combine(homeDir, "tuning.dat"))) return homeDir;

            return "game/TSOClient/";
        }
    }
}

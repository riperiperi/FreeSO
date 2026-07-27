using System;
using System.IO;

namespace FSO.Client.Utils.GameLocator
{
    public class LinuxLocator : ILocator
    {
        public string FindTheSimsOnline()
        {
            string localDir = @"../The Sims Online/TSOClient/";
            if (ILocator.ValidPath(localDir)) return localDir;

            string localDir2 = "game/TSOClient/";
            if (ILocator.ValidPath(localDir2)) return localDir2;

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string homeDir = Path.Combine(home, "Documents", "The Sims Online", "TSOClient") + "/";
            if (ILocator.ValidPath(homeDir)) return homeDir;

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "The Sims Online", "TSOClient");
        }
    }
}

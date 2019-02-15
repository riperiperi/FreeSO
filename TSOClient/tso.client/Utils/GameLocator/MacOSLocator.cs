using System;
using System.IO;

namespace FSO.Client.Utils.GameLocator
{
    public class MacOSLocator : ILocator
    {
        public string FindTheSimsOnline()
        {
            string localDir = @"../The Sims Online/TSOClient/";
            if (File.Exists(Path.Combine(localDir, "tuning.dat"))) return localDir;
            
            return string.Format("{0}/Documents/The Sims Online/TSOClient/", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }
    }
}

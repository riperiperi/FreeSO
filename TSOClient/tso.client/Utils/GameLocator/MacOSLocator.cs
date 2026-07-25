using System;
using System.IO;

namespace FSO.Client.Utils.GameLocator
{
    public class MacOSLocator : ILocator
    {
        public string FindTheSimsOnline()
        {
            string localDir = @"../The Sims Online/TSOClient/";
            if (ILocator.ValidPath(localDir)) return localDir;

            string docsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online", "TSOClient");
            if (ILocator.ValidPath(docsPath)) return docsPath;

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "The Sims Online", "TSOClient");
        }
    }
}

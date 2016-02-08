using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Client.Utils.GameLocator
{
    public class LinuxLocator : ILocator
    {
        public string FindTheSimsOnline()
        {
            return "game/TSOClient/";
        }
    }
}

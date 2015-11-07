using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot
{
    public class LotServerConfiguration
    {
        public string Certificate;
        public string Binding;

        public string InternalHost;
        public string PublicHost;

        //Which cities to provide lot hosting for
        public string[] Cities;
    }
}

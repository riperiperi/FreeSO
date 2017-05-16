using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Framework.Aries
{
    public abstract class AbstractAriesServerConfig
    {
        public string Call_Sign;
        public string Certificate;
        public string Binding;
        public string Internal_Host;
        public string Public_Host;
    }
}

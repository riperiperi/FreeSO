using FSO.Server.Framework.Aries;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City
{
    public class CityServer : AbstractAriesServer
    {
        public CityServer(CityServerConfiguration config, IKernel kernel) : base(config, kernel)
        {
        }
    }
}

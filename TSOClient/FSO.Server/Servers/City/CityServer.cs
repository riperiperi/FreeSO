using FSO.Server.Framework.Voltron;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City
{
    public class CityServer : AbstractVoltronServer
    {
        public CityServer(CityServerConfiguration config, IKernel kernel) : base(config, kernel)
        {
        }
    }
}

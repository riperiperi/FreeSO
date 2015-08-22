using FSO.Server.Framework.Aries;
using FSO.Server.Servers.City.Handlers;
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

        public override Type[] GetHandlers()
        {
            return new Type[]{
                typeof(SetPreferencesHandler),
                typeof(RegistrationHandler),
                typeof(DataServicWrapperHandler)
            };
        }
    }
}

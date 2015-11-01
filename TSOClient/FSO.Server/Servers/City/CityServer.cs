using FSO.Server.Framework;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Voltron.Packets;
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
        private ISessionGroup VoltronSessions;

        public CityServer(CityServerConfiguration config, IKernel kernel) : base(config, kernel)
        {
            VoltronSessions = Sessions.GetOrCreateGroup(Groups.VOLTRON);
        }

        public override Type[] GetHandlers()
        {
            return new Type[]{
                typeof(SetPreferencesHandler),
                typeof(RegistrationHandler),
                typeof(DataServiceWrapperHandler),
                typeof(DBRequestWrapperHandler),
                typeof(VoltronConnectionLifecycleHandler),
                typeof(FindPlayerHandler),
                typeof(PurchaseLotHandler)
            };
        }
    }
}

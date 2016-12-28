using FSO.Common.Domain.Top100;
using FSO.Server.Domain;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City
{
    public class CityServerModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ServerTop100Domain>().ToSelf().InSingletonScope();
            Bind<ITop100Domain>().To<ServerTop100Domain>();
        }
    }
}

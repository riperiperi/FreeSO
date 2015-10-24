using FSO.Common.DataService;
using FSO.Server.Servers.City;
using Ninject.Activation;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.DataService
{
    public class ShardDataServiceModule : NinjectModule
    {

        public ShardDataServiceModule()
        {
        }

        public override void Load()
        {
            this.Bind<IDataService>().To<ServerDataService>().InSingletonScope();
        }
    }
}

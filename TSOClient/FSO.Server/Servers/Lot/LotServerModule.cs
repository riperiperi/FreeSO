using FSO.Common.DataService;
using FSO.Server.DataService;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot
{
    public class LotServerModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IDataService>().To<NullDataService>().InSingletonScope();
            Bind<IDataServiceSyncFactory>().To<DataServiceSyncFactory>().InSingletonScope();
        }
    }
}

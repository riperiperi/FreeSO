using FSO.Common.DataService;
using FSO.Server.DataService;
using Ninject.Modules;

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

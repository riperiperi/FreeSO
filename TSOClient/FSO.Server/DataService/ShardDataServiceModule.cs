using FSO.Common.DataService;
using FSO.Common.Domain;
using Ninject.Modules;

namespace FSO.Server.DataService
{
    public class ShardDataServiceModule : NinjectModule
    {
        private ServerNFSProvider NFSProvider;
        public ShardDataServiceModule(string simNFS)
        {
            NFSProvider = new ServerNFSProvider(simNFS);
        }

        public override void Load()
        {
            this.Bind<IServerNFSProvider>().ToConstant(NFSProvider);
            this.Bind<IDataService>().To<ServerDataService>().InSingletonScope();
        }
    }
}

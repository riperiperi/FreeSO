using FSO.Common.DataService;
using FSO.Common.DataService.Framework;
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

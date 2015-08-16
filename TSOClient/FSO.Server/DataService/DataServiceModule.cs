using FSO.Server.DataService.Shards;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.DataService
{
    public class DataServiceModule : NinjectModule
    {
        public override void Load()
        {
            this.Bind<ShardsDataService>().To<ShardsDataService>().InSingletonScope();
        }
    }
}

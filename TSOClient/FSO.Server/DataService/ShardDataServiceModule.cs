using FSO.Server.DataService.Avatars;
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
        public override void Load()
        {
            this.Bind<AvatarsDataService>().To<AvatarsDataService>().InSingletonScope();
        }
    }
}

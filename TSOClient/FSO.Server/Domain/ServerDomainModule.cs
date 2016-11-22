using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.Shards;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Domain
{
    public class ServerDomainModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IShardsDomain>().To<Shards>().InSingletonScope();
            Bind<IRealestateDomain>().To<FSO.Common.Domain.Realestate.RealestateDomain>().InSingletonScope();
        }
    }
}

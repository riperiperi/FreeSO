using FSO.Server.Domain.Shards;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Domain
{
    public class DomainModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IDomain>().To<Domain>();
            Bind<IShards>().To<Shards.Shards>();
        }
    }
}

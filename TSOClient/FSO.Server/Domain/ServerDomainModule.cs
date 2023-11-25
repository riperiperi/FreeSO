using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.Shards;
using Ninject.Modules;

namespace FSO.Server.Domain
{
    public class ServerDomainModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IShardsDomain>().To<Shards>().InSingletonScope();
            Bind<IRealestateDomain>().To<RealestateDomain>().InSingletonScope();
        }
    }
}

using FSO.Common.Domain.Top100;
using FSO.Server.Domain;
using Ninject.Modules;

namespace FSO.Server.Servers.City
{
    public class CityServerModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ServerTop100Domain>().ToSelf().InSingletonScope();
            Bind<ITop100Domain>().To<ServerTop100Domain>();
        }
    }
}

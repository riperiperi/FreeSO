using FSO.Server.Database.DA;
using Ninject.Modules;

namespace FSO.Server.Database
{
    public class DatabaseModule : NinjectModule
    {
        public override void Load()
        {
            //TODO: If we add more drivers make this a provider
            this.Bind<IDAFactory>().To<MySqlDAFactory>().InSingletonScope();
        }
    }
}

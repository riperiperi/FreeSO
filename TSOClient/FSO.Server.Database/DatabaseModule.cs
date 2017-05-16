using FSO.Server.Database.DA;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

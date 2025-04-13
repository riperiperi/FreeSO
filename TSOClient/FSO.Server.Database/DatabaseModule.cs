using FSO.Common.Serialization;
using FSO.Server.Database.DA;
using Ninject.Activation;
using Ninject.Modules;
using System;

namespace FSO.Server.Database
{
    public class DatabaseModule : NinjectModule
    {
        public override void Load()
        {
            this.Bind<IDAFactory>().ToProvider<DAFactoryProvider>().InSingletonScope();
        }

        class DAFactoryProvider : IProvider<IDAFactory>
        {
            private DatabaseConfiguration Config;

            public DAFactoryProvider(DatabaseConfiguration config)
            {
                this.Config = config;
            }

            public Type Type
            {
                get
                {
                    return typeof(IDAFactory);
                }
            }

            public object Create(IContext context)
            {
                switch (Config.Engine)
                {
                    case "mysql":
                        return new MySqlDAFactory(Config);
                    case "sqlite":
                        return new SqliteDAFactory(Config);
                }

                throw new NotSupportedException($"Unsupported database engine {Config.Engine}");
            }
        }
    }
}

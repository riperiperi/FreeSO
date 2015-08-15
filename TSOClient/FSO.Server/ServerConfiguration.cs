using FSO.Server.Database;
using FSO.Server.Servers.Api;
using Ninject.Activation;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server
{
    public class ServerConfiguration
    {
        public DatabaseConfiguration Database;
        public ServerConfigurationservices Services;
    }


    public class ServerConfigurationservices
    {
        public ApiServerConfiguration Api;
    }





    public class ServerConfigurationModule : NinjectModule
    {
        private ServerConfiguration GetConfiguration(IContext context)
        {
            //TODO: Allow config path to be overriden in a switch
            var configPath = "config.json";
            if (!File.Exists(configPath))
            {
                throw new Exception("Configuration file, config.json, missing");
            }

            var data = File.ReadAllText(configPath);

            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<ServerConfiguration>(data);
            }catch(Exception ex)
            {
                throw new Exception("Could not deserialize config.json", ex);
            }
        }

        private class DatabaseConfigurationProvider : IProvider<DatabaseConfiguration>
        {
            private ServerConfiguration Config;

            public DatabaseConfigurationProvider(ServerConfiguration config)
            {
                this.Config = config;    
            }


            public Type Type
            {
                get
                {
                    return typeof(DatabaseConfiguration);
                }
            }

            public object Create(IContext context)
            {
                return this.Config.Database;
            }
        }

        public override void Load()
        {
            this.Bind<ServerConfiguration>().ToMethod(new Func<Ninject.Activation.IContext, ServerConfiguration>(GetConfiguration)).InSingletonScope();
            this.Bind<DatabaseConfiguration>().ToProvider<DatabaseConfigurationProvider>().InSingletonScope();
        }
    }
}

using FSO.Server.Database;
using FSO.Server.Servers.Api;
using FSO.Server.Servers.Api.JsonWebToken;
using FSO.Server.Servers.City;
using FSO.Server.Servers.Lot;
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
        public string GameLocation;
        public string SimNFS;

        public DatabaseConfiguration Database;
        public ServerConfigurationservices Services;


        /// <summary>
        /// Secret string used as a key for signing JWT tokens for the admin system
        /// </summary>
        public string Secret;
    }


    public class ServerConfigurationservices
    {
        public ApiServerConfiguration Api;
        public List<CityServerConfiguration> Cities;
        public List<LotServerConfiguration> Lots;
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


        private class JWTConfigurationProvider : IProvider<JWTConfiguration>
        {
            private ServerConfiguration Config;

            public JWTConfigurationProvider(ServerConfiguration config)
            {
                this.Config = config;
            }


            public Type Type
            {
                get
                {
                    return typeof(JWTConfiguration);
                }
            }

            public object Create(IContext context)
            {
                return new JWTConfiguration() {
                    Key = System.Text.UTF8Encoding.UTF8.GetBytes(Config.Secret)
                };
            }
        }

        public override void Load()
        {
            this.Bind<ServerConfiguration>().ToMethod(new Func<Ninject.Activation.IContext, ServerConfiguration>(GetConfiguration)).InSingletonScope();
            this.Bind<DatabaseConfiguration>().ToProvider<DatabaseConfigurationProvider>().InSingletonScope();
            this.Bind<JWTConfiguration>().ToProvider<JWTConfigurationProvider>().InSingletonScope();
        }
    }
}

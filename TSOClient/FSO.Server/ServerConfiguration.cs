using FSO.Common;
using FSO.Server.Database;
using FSO.Server.Discord;
using FSO.Server.Servers.Api.JsonWebToken;
using FSO.Server.Servers.City;
using FSO.Server.Servers.Lot;
using FSO.Server.Servers.Tasks;
using FSO.Server.Servers.UserApi;
using Newtonsoft.Json;
using Ninject.Activation;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.IO;

namespace FSO.Server
{
    public class ServerConfiguration
    {
        [JsonProperty("gameLocation")]
        public string GameLocation;
        [JsonProperty("simNFS")]
        public string SimNFS;
        [JsonProperty("updateBranch")]
        public string UpdateBranch;

        [JsonProperty("allOpenable")]
        public bool AllOpenable;

        [JsonProperty("archive")]
        public ArchiveConfiguration Archive; // If this is present, the server is running in archive mode

        [JsonProperty("database")]
        public DatabaseConfiguration Database;
        [JsonProperty("services")]
        public ServerConfigurationservices Services;
        [JsonProperty("discord")]
        public DiscordConfiguration Discord;

        /// <summary>
        /// Secret string used as a key for signing JWT tokens for the admin system
        /// </summary>
        [JsonProperty("secret")]
        public string Secret;

        /// <summary>
        /// Update ID this server is running on. All shards that we host will report needing this version, and this is reported with our host information.
        /// Loaded from updateID.txt if present.
        /// </summary>
        [JsonProperty("updateID")]
        public int? UpdateID;

        [JsonProperty("events")]
        public EventConfig? Events; // If this is present, the server automatically schedules events on start.
    }


    public class ServerConfigurationservices
    {
        [JsonProperty("userApi")]
        public ApiServerConfiguration UserApi;
        [JsonProperty("tasks")]
        public TaskServerConfiguration Tasks;
        [JsonProperty("cities")]
        public List<CityServerConfiguration> Cities;
        [JsonProperty("lots")]
        public List<LotServerConfiguration> Lots;
    }

    

    public class ServerConfigurationModule : NinjectModule
    {
        private ServerConfiguration ExplicitConfig;

        public ServerConfigurationModule()
        {

        }

        public ServerConfigurationModule(ServerConfiguration config)
        {
            ExplicitConfig = config;
        }

        private ServerConfiguration GetConfiguration(IContext context)
        {
            if (ExplicitConfig != null)
            {
                return ExplicitConfig;
            }

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

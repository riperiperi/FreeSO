using FSO.Client.Network.DB;
using FSO.Client.Regulators;
using FSO.Common.DatabaseService.Framework;
using FSO.Common.DataService;
using FSO.Common.DataService.Framework;
using FSO.Common.Serialization;
using FSO.Server.Clients;
using FSO.Server.Protocol.Voltron.DataService;
using Ninject.Activation;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Client.Network
{
    public class NetworkModule : NinjectModule
    {
        public override void Load(){
            Bind<AuthClient>().ToProvider<AuthClientProvider>().InSingletonScope();
            Bind<CityClient>().ToProvider<CityClientProvider>().InSingletonScope();
            Bind<AriesClient>().To<AriesClient>().InSingletonScope().Named("City");
            Bind<cTSOSerializer>().ToProvider<cTSOSerializerProvider>().InSingletonScope();

            Bind<IModelSerializer>().ToProvider<ModelSerializerProvider>().InSingletonScope();
            Bind<ISerializationContext>().To<SerializationContext>();

            Bind<DBService>().To<DBService>().InSingletonScope();
            Bind<IClientDataService>().To<ClientDataService>().InSingletonScope();
            Bind<Network>().To<Network>().InSingletonScope();
        }
    }

    class ModelSerializerProvider : IProvider<IModelSerializer>
    {
        private Content.Content Content;

        public ModelSerializerProvider(Content.Content content)
        {
            this.Content = content;
        }

        public Type Type
        {
            get
            {
                return typeof(IModelSerializer);
            }
        }

        public object Create(IContext context)
        {
            var serializer = new ModelSerializer();
            serializer.AddTypeSerializer(new DatabaseTypeSerializer());
            serializer.AddTypeSerializer(new DataServiceModelTypeSerializer(Content.DataDefinition));
            serializer.AddTypeSerializer(new DataServiceModelVectorTypeSerializer(Content.DataDefinition));
            return serializer;
        }
    }

    class cTSOSerializerProvider : IProvider<cTSOSerializer>
    {
        private Content.Content Content;

        public cTSOSerializerProvider(Content.Content content)
        {
            this.Content = content;
        }

        public Type Type
        {
            get
            {
                return typeof(cTSOSerializer);
            }
        }

        public object Create(IContext context)
        {
            return new cTSOSerializer(this.Content.DataDefinition);
        }
    }

    public class AuthClientProvider : IProvider<AuthClient>
    {
        private Content.Content Content;

        public AuthClientProvider(Content.Content content){
            this.Content = content;
        }

        public Type Type
        {
            get
            {
                return typeof(AuthClient);
            }
        }

        public object Create(IContext context)
        {
            var authClientConfig = Content.Ini.Get("gameentry.ini");
            var serverAddress = authClientConfig["Auth"]["Server"];
            if(serverAddress.IndexOf(",") != -1){
                //Choose the first
                serverAddress = serverAddress.Substring(0, serverAddress.IndexOf(","));
            }

            if(serverAddress.IndexOf("://") == -1)
            {
                //Default to https
                serverAddress = "https://" + serverAddress;
            }

            return new AuthClient(serverAddress);
        }
    }


    public class CityClientProvider : IProvider<CityClient>
    {
        private Content.Content Content;

        public CityClientProvider(Content.Content content)
        {
            this.Content = content;
        }

        public Type Type
        {
            get
            {
                return typeof(CityClient);
            }
        }

        public object Create(IContext context)
        {
            var cityClientConfig = Content.Ini.Get("cityselector.ini");
            var serverAddress = cityClientConfig["CitySelector"]["ServerName"];
            if (serverAddress.IndexOf(",") != -1)
            {
                //Choose the first
                serverAddress = serverAddress.Substring(0, serverAddress.IndexOf(","));
            }

            if (serverAddress.IndexOf("://") == -1)
            {
                //Default to https
                serverAddress = "https://" + serverAddress;
            }

            return new CityClient(serverAddress);
        }
    }
}

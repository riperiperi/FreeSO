using FSO.Common.DatabaseService.Framework;
using FSO.Common.DataService.Framework;
using FSO.Common.Serialization;
using FSO.Server.DataService.Shards;
using FSO.Server.Protocol.Voltron.DataService;
using Ninject.Activation;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.DataService
{
    /// <summary>
    /// Data service classes that can be shared between multiple shards when multi-tenanting
    /// </summary>
    public class GlobalDataServiceModule : NinjectModule
    {
        public override void Load()
        {
            this.Bind<ShardsDataService>().To<ShardsDataService>().InSingletonScope();
            this.Bind<cTSOSerializer>().ToProvider<cTSOSerializerProvider>().InSingletonScope();
            this.Bind<IModelSerializer>().ToProvider<ModelSerializerProvider>().InSingletonScope();
            this.Bind<ISerializationContext>().To<SerializationContext>();
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

        public object Create(IContext context){
            return new cTSOSerializer(this.Content.DataDefinition);
        }
    }
}

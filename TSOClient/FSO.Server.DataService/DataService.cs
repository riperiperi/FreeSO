using FSO.Common.DataService.Framework;
using FSO.Common.Serialization;
using FSO.Files.Formats.tsodata;
using FSO.Server.DataService.Model;
using FSO.Server.Protocol.Voltron.DataService;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FSO.Common.DataService
{
    public class DataService : IDataService
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private Dictionary<uint, IDataServiceProvider> ProviderByTypeId = new Dictionary<uint, IDataServiceProvider>();
        private Dictionary<Type, IDataServiceProvider> ProviderByType = new Dictionary<Type, IDataServiceProvider>();
        private Dictionary<MaskedStruct, IDataServiceProvider> ProviderByDerivedStruct = new Dictionary<MaskedStruct, IDataServiceProvider>();
        private Dictionary<MaskedStruct, StructField[]> MaskedStructToActualFields = new Dictionary<MaskedStruct, StructField[]>();

        private IModelSerializer Serializer;
        private TSODataDefinition DataDefinition;

        public DataService(IModelSerializer serializer, FSO.Content.Content content){
            this.Serializer = serializer;
            this.DataDefinition = content.DataDefinition;

            //Build Struct => Field[] maps for quicker serialization
            foreach (var derived in DataDefinition.DerivedStructs)
            {
                var type = MaskedStructUtils.FromID(derived.ID);
                List<StructField> fields = new List<StructField>();
                var parent = DataDefinition.Structs.First(x => x.ID == derived.Parent);

                foreach (var field in parent.Fields)
                {
                    var mask = derived.FieldMasks.FirstOrDefault(x => x.ID == field.ID);
                    var action = DerivedStructFieldMaskType.KEEP;
                    if (mask != null){
                        action = mask.Type;
                    }

                    if (action == DerivedStructFieldMaskType.REMOVE){
                        continue;
                    }

                    fields.Add(field);
                }
                MaskedStructToActualFields.Add(type, fields.ToArray());
            }
        }

        public Task<T> Get<T>(object key){
            return Get(typeof(T), key).ContinueWith<T>(x => (T)x.Result);
        }

        public Task<object> Get(Type type, object key){
            var provider = ProviderByType[type];
            return Get(provider, key);
        }

        public Task<object> Get(uint type, object key){
            var provider = ProviderByTypeId[type];
            return Get(provider, key);
        }

        public Task<object> Get(MaskedStruct type, object key)
        {
            if (!ProviderByDerivedStruct.ContainsKey(type))
            {
                return null;
            }
            var provider = ProviderByDerivedStruct[type];
            return Get(provider, key);
        }

        private Task<object> Get(IDataServiceProvider provider, object key){
            return provider.Get(key);
        }

        public List<cTSOTopicUpdateMessage> SerializeUpdate(MaskedStruct mask, object value, uint id){
            return SerializeUpdateFields(MaskedStructToActualFields[mask], value, id);
        }

        private List<cTSOTopicUpdateMessage> SerializeUpdateFields(StructField[] fields, object instance, uint id)
        {
            var result = new List<cTSOTopicUpdateMessage>();
            foreach (var field in fields)
            {
                object value = GetFieldValue(instance, field.Name);
                if (value == null) { continue; }

                if (field.Name == "Avatar_Name")
                {
                    System.Diagnostics.Debug.WriteLine("Name is " + value);
                }

                try
                {
                    var serialized = Serializer.Serialize(value);
                    if (serialized == null) { continue; }

                    var update = new cTSOTopicUpdateMessage();
                    update.StructType = field.TypeID;
                    update.StructField = field.ID;
                    update.StructId = id;
                    update.Value = serialized;
                    result.Add(update);
                }
                catch (Exception ex)
                {
                    LOG.Error(ex);
                }
            }
            return result;
        }

        private object GetFieldValue(object obj, string fieldName)
        {
            var objectField = obj.GetType().GetProperty(fieldName);
            if (objectField == null) { return null; }

            var value = objectField.GetValue(obj);

            return value;
        }

        public void ApplyUpdates(){
            /**
             * 1) Get the entity
             * 2) Perform validation
             * 3) Set the values
             * 4) Notify anything watching this object
             */
        }

        public void AddProvider(IDataServiceProvider provider){
            var type = provider.GetValueType();
            var structDef = DataDefinition.Structs.First(x => x.Name == type.Name);

            ProviderByTypeId.Add(structDef.ID, provider);
            ProviderByType.Add(type, provider);

            var derived = DataDefinition.DerivedStructs.Where(x => x.Parent == structDef.ID);
            foreach(var item in derived){
                ProviderByDerivedStruct.Add(MaskedStructUtils.FromID(item.ID), provider);
            }
        }
    }
}

using FSO.Common.DataService.Framework;
using FSO.Common.Serialization;
using FSO.Common.Serialization.Primitives;
using FSO.Files.Formats.tsodata;
using FSO.Server.DataService.Model;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private Dictionary<uint, Type> ModelTypeById = new Dictionary<uint, Type>();

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

            var assembly = Assembly.GetAssembly(typeof(DataService));

            foreach (Type type in assembly.GetTypes()){
                System.Attribute[] attributes = System.Attribute.GetCustomAttributes(type);

                foreach (Attribute attribute in attributes)
                {
                    if (attribute is DataServiceModel)
                    {
                        var _struct = DataDefinition.GetStruct(type.Name);
                        if(_struct != null){
                            ModelTypeById.Add(_struct.ID, type);
                        }
                    }
                }
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

        public async Task<cTSOTopicUpdateMessage> ApplyUpdate(cTSOTopicUpdateMessage update){
            Queue<uint> path = new Queue<uint>(update.DotPath);
            Stack<object> objects = new Stack<object>();

            var obj = await Get(path.Dequeue(), path.Dequeue());
            if (obj == null) { throw new Exception("Unknown object in dot path"); }

            
            var _struct = DataDefinition.GetStructFromValue(obj);
            if (_struct == null) { throw new Exception("Unknown struct in dot path"); }

            while(path.Count > 1){
                var nextField = path.Dequeue();
                var field = _struct.Fields.FirstOrDefault(x => x.ID == nextField);
                if (field == null) { throw new Exception("Unknown field in dot path"); }

                obj = GetFieldValue(obj, field.Name);
                if (obj == null) { throw new Exception("Member not found, unable to apply update"); }
                _struct = DataDefinition.GetStructFromValue(obj);

                if (field.Classification == StructFieldClassification.List){
                    //Array index comes next
                    if (path.Count > 1)
                    {
                        var arr = (IList)obj;
                        var arrIndex = path.Dequeue();

                        if (arrIndex < arr.Count)
                        {
                            obj = arr[(int)arrIndex];
                            if (obj == null) { throw new Exception("Item at index not found, unable to apply update"); }
                            _struct = DataDefinition.GetStructFromValue(obj);
                        }
                        else
                        {
                            throw new Exception("Item at index not found, unable to apply update");
                        }
                    }
                }
            }

            //Apply the change!
            var objType = obj.GetType();
            var finalPath = path.Dequeue();
            var value = GetUpdateValue(update.Value);

            if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(List<>))
            {
                //Array, we expect the final path component to be an array index
                var arr = (IList)obj;
                if(finalPath < arr.Count)
                {
                    //Update existing
                    arr[(int)finalPath] = value;
                }else if(finalPath == arr.Count)
                {
                    //Insert
                    arr.Add(value);
                }
            }
            else
            {
                //We expect a field value
                var field = _struct.Fields.FirstOrDefault(x => x.ID == finalPath);
                if (field == null) { throw new Exception("Unknown field in dot path"); }
                SetFieldValue(obj, field.Name, value);
            }

            return update;
        }

        private object GetUpdateValue(object value)
        {
            if(value is cTSOProperty){
                //Convert to model
                return ConvertProperty(value as cTSOProperty);
            }
            return value;
        }

        public object ConvertProperty(cTSOProperty property)
        {
            var _struct = DataDefinition.GetStruct(property.StructType);
            if (_struct == null) { return null; }

            if (!ModelTypeById.ContainsKey(_struct.ID)){
                return null;
            }

            var type = ModelTypeById[_struct.ID];
            var instance = ModelActivator.NewInstance(type);
            
            foreach(var field in property.StructFields){
                var _field = _struct.Fields.FirstOrDefault(x => x.ID == field.StructFieldID);
                if (_field == null) { continue; }
                
                SetFieldValue(instance, _field.Name, GetUpdateValue(field.Value));
            }

            return instance;
        }

        private List<cTSOTopicUpdateMessage> SerializeUpdateFields(StructField[] fields, object instance, uint id)
        {
            var result = new List<cTSOTopicUpdateMessage>();
            foreach (var field in fields)
            {
                object value = GetFieldValue(instance, field.Name);
                if (value == null) { continue; }

                //Might be a struct

                try
                {
                    var clsid = Serializer.GetClsid(value);
                    if (!clsid.HasValue){
                        //Dont know how to serialize this value
                        continue;
                    }

                    var update = new cTSOTopicUpdateMessage();
                    update.StructType = field.ParentID;
                    update.StructField = field.ID;
                    update.StructId = id;
                    update.Value = value;
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

        private void SetFieldValue(object obj, string fieldName, object value)
        {
            var objectField = obj.GetType().GetProperty(fieldName);
            if (objectField == null) { return; }

            objectField.SetValue(obj, value);
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

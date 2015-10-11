using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Framework.Attributes;
using FSO.Common.Security;
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
        private Dictionary<uint, StructField[]> StructToActualFields = new Dictionary<uint, StructField[]>();
        private Dictionary<uint, Type> ModelTypeById = new Dictionary<uint, Type>();
        private Dictionary<Type, uint> ModelIdByType = new Dictionary<Type, uint>();

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
                    if (mask == null) { continue; }
                    /*
                    var action = DerivedStructFieldMaskType.KEEP;
                    if (mask != null){
                        action = mask.Type;
                    }
                    if (action == DerivedStructFieldMaskType.REMOVE){
                        //These seems wrong, ServerMyAvatar and MyAvatar both exclude bookmarks by this logic
                        //continue;
                    }
                    */
                    fields.Add(field);
                }
                MaskedStructToActualFields.Add(type, fields.ToArray());
            }

            foreach(var _struct in DataDefinition.Structs){
                StructToActualFields.Add(_struct.ID, _struct.Fields);
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
                            ModelIdByType.Add(type, _struct.ID);
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

        public IDataServiceProvider GetProvider(uint type)
        {
            return ProviderByTypeId[type];
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

        public async Task<cTSOTopicUpdateMessage> SerializePath(params uint[] dotPath)
        {
            var path = await ResolveDotPath(dotPath);
            var value = path.GetValue();
            
            return SerializeUpdateField(value.Value, dotPath);
        }

        public async void ApplyUpdate(cTSOTopicUpdateMessage update, ISecurityContext context)
        {
            var partialDotPath = new uint[update.DotPath.Length - 1];
            Array.Copy(update.DotPath, partialDotPath, partialDotPath.Length);
            var path = await ResolveDotPath(partialDotPath);

            var target = path.GetValue();
            if (target.Value == null) { throw new Exception("Cannot set property on null value"); }

            //Apply the change!
            var targetType = target.Value.GetType();
            var finalPath = update.DotPath[update.DotPath.Length - 1];
            var value = GetUpdateValue(update.Value);

            var provider = GetProvider(path.GetProvider());
            var entity = path.GetEntity();

            if (IsList(targetType))
            {
                //Array, we expect the final path component to be an array index
                var arr = (IList)target.Value;
                if (finalPath < arr.Count)
                {
                    //Update existing
                    provider.DemandMutation(entity.Value, MutationType.ARRAY_SET_ITEM, path.GetKeyPath(), value, context);
                    arr[(int)finalPath] = value;
                }
                else if (finalPath == arr.Count)
                {
                    //Insert
                    provider.DemandMutation(entity.Value, MutationType.ARRAY_SET_ITEM, path.GetKeyPath(), value, context);
                    arr.Add(value);
                }
            }
            else
            {
                //We expect a field value
                if (target.TypeId == 0)
                {
                    throw new Exception("Trying to set field on unknown type");
                }

                var _struct = DataDefinition.GetStruct(target.TypeId);
                var field = _struct.Fields.FirstOrDefault(x => x.ID == finalPath);
                if (field == null) { throw new Exception("Unknown field in dot path"); }

                var objectField = target.Value.GetType().GetProperty(field.Name);
                if (objectField == null) { throw new Exception("Unknown field in model: " + objectField.Name); }

                //If the value is null (0) and the field has a decoration of NullValueIndicatesDeletion
                //Delete the value instead of setting it
                var nullDelete = objectField.GetCustomAttribute<Key>();
                if (nullDelete != null && IsNull(value))
                {
                    var parent = path.GetParent();
                    if (IsList(parent.Value))
                    {
                        provider.DemandMutation(entity.Value, MutationType.ARRAY_REMOVE_ITEM, path.GetKeyPath(1), value, context);
                        ((IList)parent.Value).Remove(target.Value);
                    }
                    else
                    {
                        //TODO
                    }
                }
                else
                {
                    provider.DemandMutation(entity.Value, MutationType.SET_FIELD_VALUE, path.GetKeyPath(), value, context);
                    objectField.SetValue(target.Value, value);
                }
            }
        }

        private uint? GetStructType(object value)
        {
            if(value != null)
            {
                if (ModelIdByType.ContainsKey(value.GetType()))
                {
                    return ModelIdByType[value.GetType()];
                }
                return null;
            }
            return null;
        }

        private async Task<DotPathResult> ResolveDotPath(params uint[] _path)
        {
            var result = new DotPathResult();
            result.Path = new DotPathResultComponent[_path.Length];

            Queue<uint> path = new Queue<uint>(_path);

            var typeId = path.Dequeue();
            var entityId = path.Dequeue();
            var obj = await Get(typeId, entityId);
            if (obj == null) { throw new Exception("Unknown object in dot path"); }

            result.Path[0] = new DotPathResultComponent {
                Value = null,
                Id = typeId,
                Type = DotPathResultComponentType.PROVIDER,
                Name = null
            };
            result.Path[1] = new DotPathResultComponent{
                Value = obj,
                Id = entityId,
                Type = DotPathResultComponentType.ARRAY_ITEM,
                TypeId = typeId,
                Name = entityId.ToString()
            };

            var _struct = DataDefinition.GetStructFromValue(obj);
            if (_struct == null) { throw new Exception("Unknown struct in dot path"); }
            var index = 2;

            while (path.Count > 0)
            {
                var nextField = path.Dequeue();
                var field = _struct.Fields.FirstOrDefault(x => x.ID == nextField);
                if (field == null) { throw new Exception("Unknown field in dot path"); }

                obj = GetFieldValue(obj, field.Name);
                if (obj == null) { throw new Exception("Member not found, unable to apply update"); }
                _struct = DataDefinition.GetStructFromValue(obj);

                result.Path[index++] = new DotPathResultComponent {
                    Value = obj,
                    Id = field.ID,
                    TypeId = _struct != null ? _struct.ID : 0,
                    Type = DotPathResultComponentType.FIELD,
                    Name = field.Name
                };


                if (field.Classification == StructFieldClassification.List)
                {
                    //Array index comes next
                    if (path.Count > 0)
                    {
                        var arr = (IList)obj;
                        var arrIndex = path.Dequeue();

                        if (arrIndex < arr.Count)
                        {
                            obj = arr[(int)arrIndex];
                            if (obj == null) { throw new Exception("Item at index not found, unable to apply update"); }
                            _struct = DataDefinition.GetStructFromValue(obj);

                            result.Path[index++] = new DotPathResultComponent
                            {
                                Value = obj,
                                Id = arrIndex,
                                Type = DotPathResultComponentType.ARRAY_ITEM,
                                TypeId = _struct != null ? _struct.ID : 0,
                                Name = arrIndex.ToString()
                            };

                        }
                        else
                        {
                            throw new Exception("Item at index not found, unable to apply update");
                        }
                    }
                }
            }
            return result;
        }

        private bool IsList(object value)
        {
            if (value == null) { return false; }
            return IsList(value.GetType());
        }

        private bool IsList(Type targetType)
        {
            return targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>);
        }

        private bool IsNull(object value)
        {
            if (value == null) { return true; }
            if(value is uint)
            {
                return ((uint)value) == 0;
            }else if (value is int)
            {
                return ((int)value) == 0;
            }else if (value is ushort)
            {
                return ((ushort)value) == 0;
            }
            else if (value is short)
            {
                return ((short)value) == 0;
            }
            return false;
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
                    result.Add(SerializeUpdateField(value, new uint[] {
                        field.ParentID,
                        id,
                        field.ID
                    }));
                }
                catch(Exception ex){
                }
            }
            return result;
        }

        private cTSOTopicUpdateMessage SerializeUpdateField(object value, uint[] path)
        {
            try
            {
                var clsid = Serializer.GetClsid(value);
                if (!clsid.HasValue)
                {
                    throw new Exception("Unable to serialize value with type: " + value.GetType());
                }

                var update = new cTSOTopicUpdateMessage();
                update.DotPath = path;
                update.Value = value;
                return update;
            }
            catch (Exception ex)
            {
                LOG.Error(ex);
                throw ex;
            }
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

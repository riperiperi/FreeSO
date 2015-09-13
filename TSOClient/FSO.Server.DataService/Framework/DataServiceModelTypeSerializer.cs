using FSO.Common.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Files.Formats.tsodata;
using FSO.Common.Serialization.Primitives;

namespace FSO.Common.DataService.Framework
{
    public class DataServiceModelTypeSerializer : ITypeSerializer
    {
        //0xA96E7E5B: cTSOValue<class cRZAutoRefCount<class cITSOProperty> >
        private readonly uint CLSID = 0xA96E7E5B;

        private TSODataDefinition Model;
        protected Dictionary<string, Struct> StructsByName = new Dictionary<string, Struct>();
        protected Dictionary<uint, Struct> StructById = new Dictionary<uint, Struct>();

        public DataServiceModelTypeSerializer(TSODataDefinition model){
            this.Model = model;

            foreach(var obj in model.Structs){
                StructsByName.Add(obj.Name, obj);
                StructById.Add(obj.ID, obj);
            }
        }

        public virtual bool CanDeserialize(uint clsid){
            return clsid == CLSID;
        }

        public virtual bool CanSerialize(Type type)
        {
            return StructsByName.ContainsKey(type.Name);
        }

        public virtual object Deserialize(uint clsid, IoBuffer input, ISerializationContext context)
        {
            var property = new cTSOProperty();
            property.Deserialize(input, context);
            return property;
        }

        public virtual uint? GetClsid(object value)
        {
            var clazz = GetStruct(value);
            if(clazz != null)
            {
                return CLSID;
            }
            return null;
        }

        public virtual void Serialize(IoBuffer output, object value, ISerializationContext context)
        {
            var _struct = GetStruct(value);
            if (_struct == null)
            {
                return;
            }

            var property = ConvertToProperty(_struct, value, context);
            property.Serialize(output, context);
        }

        protected cTSOProperty ConvertToProperty(Struct _struct, object value, ISerializationContext context) {
            //Convert the struct to a cTSOProperty
            var property = new cTSOProperty();
            property.StructType = _struct.ID;
            property.StructFields = new List<cTSOPropertyField>();

            foreach (var field in _struct.Fields)
            {
                var fieldValue = GetFieldValue(value, field.Name);
                if (fieldValue == null) { continue; }

                if (context.ModelSerializer.GetClsid(fieldValue) == null)
                {
                    //Cant serialize this, sorry
                    continue;
                }

                property.StructFields.Add(new cTSOPropertyField
                {
                    StructFieldID = field.ID,
                    Value = fieldValue
                });
            }

            return property;
        }

        protected object GetFieldValue(object obj, string fieldName)
        {
            var objectField = obj.GetType().GetProperty(fieldName);
            if (objectField == null) { return null; }

            var value = objectField.GetValue(obj);

            return value;
        }

        protected Struct GetStruct(object value)
        {
            var type = value.GetType();
            return GetStruct(type);
        }

        protected Struct GetStruct(Type type)
        {
            if (StructsByName.ContainsKey(type.Name))
            {
                return StructsByName[type.Name];
            }
            return null;
        }
    }
}

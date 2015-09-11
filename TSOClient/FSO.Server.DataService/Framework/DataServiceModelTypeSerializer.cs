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
        private TSODataDefinition Model;
        private Dictionary<string, Struct> StructsByName = new Dictionary<string, Struct>();
        private Dictionary<uint, Struct> StructById = new Dictionary<uint, Struct>();

        public DataServiceModelTypeSerializer(TSODataDefinition model){
            this.Model = model;

            foreach(var obj in model.Structs){
                StructsByName.Add(obj.Name, obj);
                StructById.Add(obj.ID, obj);
            }
        }

        public bool CanDeserialize(uint clsid){
            return StructById.ContainsKey(clsid);
        }

        public bool CanSerialize(Type type)
        {
            return StructsByName.ContainsKey(type.Name);
        }

        public object Deserialize(uint clsid, IoBuffer input, ISerializationContext serializer)
        {
            return null;
        }

        public uint? GetClsid(object value)
        {
            var clazz = GetStruct(value);
            if(clazz != null)
            {
                //cTSOProperty
                return 0xA96E7E5B;
            }
            return null;
        }

        public void Serialize(IoBuffer output, object value, ISerializationContext serializer)
        {
            var clazz = GetStruct(value);
            if (clazz == null)
            {
                return;
            }

            //Convert the struct to a cTSOProperty
            var property = new cTSOProperty();
            property.StructType = clazz.ID;
            property.StructFields = new List<cTSOPropertyField>();

            foreach(var field in clazz.Fields){
                var fieldValue = GetFieldValue(value, field.Name);
                if (fieldValue == null) { continue; }

                property.StructFields.Add(new cTSOPropertyField {
                    StructFieldID = field.ID,
                    Value = fieldValue
                });
            }

            property.Serialize(output, serializer);
        }


        private object GetFieldValue(object obj, string fieldName)
        {
            var objectField = obj.GetType().GetProperty(fieldName);
            if (objectField == null) { return null; }

            var value = objectField.GetValue(obj);

            return value;
        }

        private Struct GetStruct(object value)
        {
            var type = value.GetType();
            if (StructsByName.ContainsKey(type.Name))
            {
                return StructsByName[type.Name];
            }
            return null;
        }
    }
}

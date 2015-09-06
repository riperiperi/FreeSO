using FSO.Common.Serialization.TypeSerializers;
using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Common.Serialization
{
    public class ModelSerializer : IModelSerializer
    {
        private List<ITypeSerializer> TypeSerializers = new List<ITypeSerializer>();

        public ModelSerializer(){
            //Built-in
            //AddTypeSerializer(new cTSOValueBoolean());
            AddTypeSerializer(new cTSOValueString());
            /*AddTypeSerializer(new cTSOValueStringVector());
            AddTypeSerializer(new cTSOValueByte());
            AddTypeSerializer(new cTSOValueByteVector());
            AddTypeSerializer(new cTSOValueSByte());
            AddTypeSerializer(new cTSOValueSByteVector());

            AddTypeSerializer(new cTSOValueUInt32());
            AddTypeSerializer(new cTSOValueUInt16());*/
        }

        public SerializedValue Serialize(object obj)
        {
            if (obj == null) { return null; }
            var serializer = GetSerializer(obj.GetType());
            if (serializer == null) { return null; }

            return serializer.Serialize(obj, this);
        }

        public object Deserialize(SerializedValue value)
        {
            var serializer = GetSerializer(value.ClsId);
            if (serializer == null) { return null; }

            return serializer.Deserialize(value, this);
        }

        public void AddTypeSerializer(ITypeSerializer serializer)
        {
            TypeSerializers.Add(serializer);
        }

        private ITypeSerializer GetSerializer(uint clsid){
            return TypeSerializers.First(x => x.CanDeserialize(clsid));
        }

        private ITypeSerializer GetSerializer(Type type){
            return TypeSerializers.First(x => x.CanSerialize(type));
        }
    }
}

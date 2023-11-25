using Mina.Core.Buffer;
using System;

namespace FSO.Common.Serialization.TypeSerializers
{
    public class cTSOValueBoolean : ITypeSerializer
    {
        private readonly uint CLSID = 0x696D1183;

        public bool CanDeserialize(uint clsid)
        {
            return clsid == CLSID;
        }

        public bool CanSerialize(Type type)
        {
            return type.IsAssignableFrom(typeof(bool));
        }

        public object Deserialize(uint clsid, IoBuffer input, ISerializationContext serializer)
        {
            var byteValue = input.Get();
            return byteValue == 0x01 ? true : false;
        }

        public void Serialize(IoBuffer output, object value, ISerializationContext serializer)
        {
            bool boolValue = (bool)value;
            output.Put(boolValue ? (byte)0x01 : (byte)0x00);
        }

        public uint? GetClsid(object value)
        {
            return CLSID;
        }
    }
}

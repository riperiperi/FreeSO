using System;
using Mina.Core.Buffer;

namespace FSO.Common.Serialization.TypeSerializers
{
    public class cTSOValueSByte : ITypeSerializer
    {
        private readonly uint CLSID = 0xE976088A;

        public bool CanDeserialize(uint clsid)
        {
            return clsid == CLSID;
        }

        public bool CanSerialize(Type type)
        {
            return type.IsAssignableFrom(typeof(sbyte));
        }

        public object Deserialize(uint clsid, IoBuffer input, ISerializationContext serializer)
        {
            return (sbyte)input.Get();
        }

        public void Serialize(IoBuffer output, object value, ISerializationContext serializer)
        {
            output.Put((byte)(sbyte)value);
        }

        public uint? GetClsid(object value)
        {
            return CLSID;
        }
    }
}

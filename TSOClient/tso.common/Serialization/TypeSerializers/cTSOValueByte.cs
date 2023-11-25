using System;
using Mina.Core.Buffer;

namespace FSO.Common.Serialization.TypeSerializers
{
    public class cTSOValueByte : ITypeSerializer
    {
        private readonly uint CLSID = 0xC976087C;

        public bool CanDeserialize(uint clsid)
        {
            return clsid == CLSID;
        }

        public bool CanSerialize(Type type)
        {
            return type.IsAssignableFrom(typeof(byte));
        }

        public object Deserialize(uint clsid, IoBuffer input, ISerializationContext serializer)
        {
            return input.Get();
        }

        public void Serialize(IoBuffer output, object value, ISerializationContext serializer)
        {
            output.Put((byte)value);
        }

        public uint? GetClsid(object value)
        {
            return CLSID;
        }
    }
}

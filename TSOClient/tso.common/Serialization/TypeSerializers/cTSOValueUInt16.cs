using Mina.Core.Buffer;
using System;

namespace FSO.Common.Serialization.TypeSerializers
{
    public class cTSOValueUInt16 : ITypeSerializer
    {
        //0xE9760891: cTSOValue<unsigned short>
        private readonly uint CLSID = 0xE9760891;

        public bool CanDeserialize(uint clsid)
        {
            return clsid == CLSID;
        }

        public bool CanSerialize(Type type)
        {
            return type.IsAssignableFrom(typeof(ushort));
        }

        public object Deserialize(uint clsid, IoBuffer input, ISerializationContext serializer)
        {
            return input.GetUInt16();
        }

        public void Serialize(IoBuffer output, object value, ISerializationContext serializer)
        {
            output.PutUInt16((ushort)value);
        }

        public uint? GetClsid(object value)
        {
            return CLSID;
        }
    }
}

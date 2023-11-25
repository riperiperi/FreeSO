using Mina.Core.Buffer;
using System;

namespace FSO.Common.Serialization.TypeSerializers
{
    public class cTSOValueUInt64 : ITypeSerializer
    {
        //0x69D3E3DB: cTSOValue<unsigned __int64>
        private readonly uint CLSID = 0x69D3E3DB;

        public bool CanDeserialize(uint clsid)
        {
            return clsid == CLSID;
        }

        public bool CanSerialize(Type type)
        {
            return type.IsAssignableFrom(typeof(ulong));
        }

        public object Deserialize(uint clsid, IoBuffer input, ISerializationContext serializer)
        {
            return input.GetUInt64();
        }

        public void Serialize(IoBuffer output, object value, ISerializationContext serializer)
        {
            output.PutUInt64((ulong)value);
        }

        public uint? GetClsid(object value)
        {
            return CLSID;
        }
    }
}

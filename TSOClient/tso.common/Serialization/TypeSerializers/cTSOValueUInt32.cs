using Mina.Core.Buffer;
using System;

namespace FSO.Common.Serialization.TypeSerializers
{
    public class cTSOValueUInt32 : ITypeSerializer
    {
        //0x696D1189: cTSOValue<unsigned long>
        private readonly uint CLSID = 0x696D1189;

        public bool CanDeserialize(uint clsid)
        {
            return clsid == CLSID;
        }

        public bool CanSerialize(Type type)
        {
            return type.IsAssignableFrom(typeof(uint));
        }

        public object Deserialize(uint clsid, IoBuffer input, ISerializationContext serializer)
        {
            return input.GetUInt32();
        }

        public void Serialize(IoBuffer output, object value, ISerializationContext serializer)
        {
            output.PutUInt32((uint)value);
        }

        public uint? GetClsid(object value)
        {
            return CLSID;
        }
    }
}

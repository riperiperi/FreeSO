using System;
using Mina.Core.Buffer;

namespace FSO.Common.Serialization.TypeSerializers
{
    public class cTSOValueString : ITypeSerializer
    {
        private readonly uint CLSID = 0x896D1688;

        public bool CanDeserialize(uint clsid)
        {
            return clsid == CLSID;
        }

        public bool CanSerialize(Type type)
        {
            return type.IsAssignableFrom(typeof(string));
        }

        public object Deserialize(uint clsid, IoBuffer input, ISerializationContext serializer)
        {
            return IoBufferUtils.GetPascalVLCString(input);
        }

        public void Serialize(IoBuffer output, object value, ISerializationContext serializer)
        {
            output.PutPascalVLCString((string)value);
        }

        public uint? GetClsid(object value)
        {
            return CLSID;
        }
    }
}

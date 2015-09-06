using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public object Deserialize(SerializedValue value, IModelSerializer serializer)
        {
            return IoBuffer.Wrap(value.Data).GetUInt32();
        }

        public SerializedValue Serialize(object value, IModelSerializer serializer)
        {
            var result = IoBuffer.Allocate(4);
            result.Order = ByteOrder.BigEndian;
            result.PutUInt32((uint)value);
            result.Flip();

            var bytes = new byte[result.Remaining];
            result.Get(bytes, 0, result.Remaining);
            return new SerializedValue(CLSID, bytes);
        }
    }
}

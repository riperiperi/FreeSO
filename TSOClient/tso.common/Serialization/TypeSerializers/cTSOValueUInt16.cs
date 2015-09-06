using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public object Deserialize(SerializedValue value, IModelSerializer serializer)
        {
            return IoBuffer.Wrap(value.Data).GetUInt16();
        }

        public SerializedValue Serialize(object value, IModelSerializer serializer)
        {
            var result = IoBuffer.Allocate(4);
            result.Order = ByteOrder.BigEndian;
            result.PutUInt16((ushort)value);
            result.Flip();


            var bytes = new byte[result.Remaining];
            result.Get(bytes, 0, result.Remaining);
            return new SerializedValue(CLSID, bytes);
        }
    }
}

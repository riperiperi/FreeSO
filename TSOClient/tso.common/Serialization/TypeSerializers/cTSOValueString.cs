using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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


        public object Deserialize(SerializedValue value, IModelSerializer serializer)
        {
            return IoBufferUtils.GetPascalVLCString(IoBuffer.Wrap(value.Data));
        }

        public SerializedValue Serialize(object value, IModelSerializer serializer)
        {
            var result = IoBuffer.Allocate(0);
            result.Order = ByteOrder.BigEndian;
            result.AutoExpand = true;
            result.PutPascalVLCString((string)value);
            result.Flip();

            var bytes = new byte[result.Remaining];
            result.Get(bytes, 0, result.Remaining);
            return new SerializedValue(CLSID, bytes);
        }
    }
}

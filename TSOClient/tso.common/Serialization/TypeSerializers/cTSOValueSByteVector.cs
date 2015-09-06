using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mina.Core.Buffer;

namespace FSO.Common.Serialization.TypeSerializers
{
    public class cTSOValueSByteVector : ITypeSerializer
    {
        private readonly uint CLSID = 0x097608AF;

        public bool CanDeserialize(uint clsid)
        {
            return clsid == CLSID;
        }

        public bool CanSerialize(Type type)
        {
            return type.IsAssignableFrom(typeof(IList<sbyte>));
        }
        
        public object Deserialize(SerializedValue value, IModelSerializer serializer)
        {
            var buffer = IoBuffer.Wrap(value.Data);

            var result = new List<sbyte>();
            var count = buffer.GetUInt32();
            for(int i=0; i < count; i++){
                result.Add((sbyte)buffer.Get());
            }
            return result;
        }

        public SerializedValue Serialize(object value, IModelSerializer serializer)
        {
            List<sbyte> list = (List<sbyte>)value;
            var result = IoBuffer.Allocate(4);
            result.Order = ByteOrder.BigEndian;
            result.AutoExpand = true;
            result.PutUInt32((uint)list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                result.Put((byte)list[i]);
            }
            result.Flip();

            var bytes = new byte[result.Remaining];
            result.Get(bytes, 0, result.Remaining);
            return new SerializedValue(CLSID, bytes);
        }
    }
}

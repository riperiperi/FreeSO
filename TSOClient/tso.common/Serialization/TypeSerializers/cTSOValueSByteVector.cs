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
        
        public object Deserialize(uint clsid, IoBuffer input, ISerializationContext serializer)
        {
            var result = new List<sbyte>();
            var count = input.GetUInt32();
            for(int i=0; i < count; i++){
                result.Add((sbyte)input.Get());
            }
            return result;
        }

        public void Serialize(IoBuffer output, object value, ISerializationContext serializer)
        {
            List<sbyte> list = (List<sbyte>)value;
            output.PutUInt32((uint)list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                output.Put((byte)list[i]);
            }
        }

        public uint? GetClsid(object value)
        {
            return CLSID;
        }
    }
}

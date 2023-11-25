using System;
using System.Collections.Generic;
using Mina.Core.Buffer;
using System.Collections.Immutable;

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
            return typeof(IList<sbyte>).IsAssignableFrom(type);
        }
        
        public object Deserialize(uint clsid, IoBuffer input, ISerializationContext serializer)
        {
            var result = new List<sbyte>();
            var count = input.GetUInt32();
            for(int i=0; i < count; i++){
                result.Add((sbyte)input.Get());
            }
            return ImmutableList.ToImmutableList(result);
        }

        public void Serialize(IoBuffer output, object value, ISerializationContext serializer)
        {
            IList<sbyte> list = (IList<sbyte>)value;
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

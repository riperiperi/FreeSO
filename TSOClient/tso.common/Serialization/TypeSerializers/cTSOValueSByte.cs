using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mina.Core.Buffer;

namespace FSO.Common.Serialization.TypeSerializers
{
    public class cTSOValueSByte : ITypeSerializer
    {
        private readonly uint CLSID = 0xE976088A;

        public bool CanDeserialize(uint clsid)
        {
            return clsid == CLSID;
        }

        public bool CanSerialize(Type type)
        {
            return type.IsAssignableFrom(typeof(sbyte));
        }

        public object Deserialize(SerializedValue value, IModelSerializer serializer)
        {
            return (sbyte)value.Data[0];
        }

        public SerializedValue Serialize(object value, IModelSerializer serializer)
        {
            return new SerializedValue(CLSID, new byte[] { (byte)(sbyte)value });
        }
    }
}

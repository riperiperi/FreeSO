using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Common.Serialization.TypeSerializers
{
    public class cTSOValueBoolean : ITypeSerializer
    {
        private readonly uint CLSID = 0x696D1183;

        public bool CanDeserialize(uint clsid)
        {
            return clsid == CLSID;
        }

        public bool CanSerialize(Type type)
        {
            return type.IsAssignableFrom(typeof(bool));
        }

        public object Deserialize(SerializedValue value, IModelSerializer serializer)
        {
            var byteValue = value.Data[0];
            return byteValue == 0x01 ? true : false;
        }

        public SerializedValue Serialize(object value, IModelSerializer serializer)
        {
            bool boolValue = (bool)value;
            return new SerializedValue(CLSID, new byte[] {boolValue == true ? (byte)0x01 : (byte)0x00});
        }
    }
}

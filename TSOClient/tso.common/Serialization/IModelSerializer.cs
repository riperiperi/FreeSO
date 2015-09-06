using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Common.Serialization
{
    public interface IModelSerializer
    {
        object Deserialize(SerializedValue value);
        SerializedValue Serialize(object obj);
        void AddTypeSerializer(ITypeSerializer serializer);
    }

    public interface ITypeSerializer
    {
        object Deserialize(SerializedValue value, IModelSerializer serializer);
        SerializedValue Serialize(object value, IModelSerializer serializer);

        bool CanSerialize(Type type);
        bool CanDeserialize(uint clsid);
    }
}

using Mina.Core.Buffer;
using System;

namespace FSO.Common.Serialization
{
    public interface IModelSerializer
    {
        object Deserialize(uint clsid, IoBuffer input, ISerializationContext context);
        void Serialize(IoBuffer output, object obj, ISerializationContext context);
        void Serialize(IoBuffer output, object value, ISerializationContext context, bool clsIdPrefix);
        IoBuffer SerializeBuffer(object value, ISerializationContext context, bool clsIdPrefix);

        uint? GetClsid(object value);
        void AddTypeSerializer(ITypeSerializer serializer);
    }

    public interface ITypeSerializer
    {
        object Deserialize(uint clsid, IoBuffer input, ISerializationContext serializer);
        void Serialize(IoBuffer output, object value, ISerializationContext serializer);

        uint? GetClsid(object value);

        bool CanSerialize(Type type);
        bool CanDeserialize(uint clsid);
    }
}

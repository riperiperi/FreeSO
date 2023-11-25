using Mina.Core.Buffer;

namespace FSO.Common.Serialization
{
    public interface IoBufferDeserializable
    {
        void Deserialize(IoBuffer input, ISerializationContext context);
    }
}

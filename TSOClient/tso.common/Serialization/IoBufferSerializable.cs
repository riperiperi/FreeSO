using Mina.Core.Buffer;

namespace FSO.Common.Serialization
{
    public interface IoBufferSerializable
    {
        void Serialize(IoBuffer output, ISerializationContext context);
    }
}

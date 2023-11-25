using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Gluon
{
    public interface IGluonPacket : IoBufferDeserializable, IoBufferSerializable
    {
        GluonPacketType GetPacketType();
    }
}

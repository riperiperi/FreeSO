using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Aries
{
    public interface IAriesPacket : IoBufferSerializable, IoBufferDeserializable
    {
        /**
	     * Get packet type
	     * 
	     * @return
	     */
        AriesPacketType GetPacketType();
    }
}

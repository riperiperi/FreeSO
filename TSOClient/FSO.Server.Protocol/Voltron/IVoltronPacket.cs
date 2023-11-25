using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron
{
    public interface IVoltronPacket : IoBufferDeserializable, IoBufferSerializable
    {
        /**
	     * Get packet type
	     * 
	     * @return
	     */
        VoltronPacketType GetPacketType();
    }
}

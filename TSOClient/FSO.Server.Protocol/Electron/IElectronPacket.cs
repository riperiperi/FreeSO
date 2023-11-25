using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Electron
{
    public interface IElectronPacket : IoBufferDeserializable, IoBufferSerializable
    {
        ElectronPacketType GetPacketType();
    }
}

using Mina.Core.Buffer;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Electron
{
    public abstract class AbstractElectronPacket : IElectronPacket
    {
        public static IoBuffer Allocate(int size)
        {
            IoBuffer buffer = IoBuffer.Allocate(size);
            buffer.Order = ByteOrder.BigEndian;
            return buffer;
        }

        public abstract ElectronPacketType GetPacketType();
        public abstract void Deserialize(IoBuffer input, ISerializationContext context);
        public abstract void Serialize(IoBuffer output, ISerializationContext context);
    }
}

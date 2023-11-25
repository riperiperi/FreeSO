using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class KeepAlive : AbstractElectronPacket
    {
        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {

        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.KeepAlive;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {

        }
    }
}

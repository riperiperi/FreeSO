using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class AvatarRetireRequest : AbstractElectronPacket
    {
        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            input.GetUInt32();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.AvatarRetireRequest;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(0);
        }
    }
}

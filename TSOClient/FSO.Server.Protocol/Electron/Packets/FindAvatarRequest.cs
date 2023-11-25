using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class FindAvatarRequest : AbstractElectronPacket
    {
        public uint AvatarId;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            AvatarId = input.GetUInt32();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.FindAvatarRequest;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(AvatarId);
        }
    }
}

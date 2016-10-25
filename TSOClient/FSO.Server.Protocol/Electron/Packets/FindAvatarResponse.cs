using FSO.Common.Serialization;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Electron.Model;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class FindAvatarResponse : AbstractElectronPacket
    {
        public uint AvatarId;
        public FindAvatarResponseStatus Status;
        public uint LotId; //0 if status is not FOUND.

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            AvatarId = input.GetUInt32();
            Status = input.GetEnum<FindAvatarResponseStatus>();
            LotId = input.GetUInt32();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.FindAvatarResponse;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(AvatarId);
            output.PutEnum(Status);
            output.PutUInt32(LotId);
        }
    }
}

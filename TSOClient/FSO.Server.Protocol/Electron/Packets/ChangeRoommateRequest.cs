using FSO.Common.Serialization;
using FSO.Server.Protocol.Electron.Model;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class ChangeRoommateRequest : AbstractElectronPacket
    {
        public ChangeRoommateType Type;
        public uint AvatarId;
        public uint LotLocation;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Type = input.GetEnum<ChangeRoommateType>();
            AvatarId = input.GetUInt32();
            LotLocation = input.GetUInt32();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.ChangeRoommateRequest;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(Type);
            output.PutUInt32(AvatarId);
            output.PutUInt32(LotLocation);
        }
    }
}

using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public class InjectLotChatPacket : AbstractGluonPacket
    {
        public uint LotId;
        public string AvatarName = "";
        public string Message = "";

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            LotId = input.GetUInt32();
            AvatarName = input.GetPascalVLCString();
            Message = input.GetPascalVLCString();
        }

        public override GluonPacketType GetPacketType() => GluonPacketType.InjectLotChat;

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(LotId);
            output.PutPascalVLCString(AvatarName);
            output.PutPascalVLCString(Message);
        }
    }
}
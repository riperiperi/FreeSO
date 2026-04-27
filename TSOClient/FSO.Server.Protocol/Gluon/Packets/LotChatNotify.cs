using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public class LotChatNotify : AbstractGluonPacket
    {
        public uint LotId;
        public string LotName = "";
        public string AvatarName = "";
        public string Message = "";
        public byte ChannelId;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            LotId = input.GetUInt32();
            LotName = input.GetPascalVLCString();
            AvatarName = input.GetPascalVLCString();
            Message = input.GetPascalVLCString();
            ChannelId = input.Get();
        }

        public override GluonPacketType GetPacketType() => GluonPacketType.LotChatNotify;

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(LotId);
            output.PutPascalVLCString(LotName);
            output.PutPascalVLCString(AvatarName);
            output.PutPascalVLCString(Message);
            output.Put(ChannelId);
        }
    }
}
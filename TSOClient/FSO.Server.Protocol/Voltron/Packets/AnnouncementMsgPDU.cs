using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class AnnouncementMsgPDU : AbstractVoltronPacket
    {
        public string SenderID = "??ARIES_OPERATIONS";
        public string SenderAccount = "";
        public byte Badge;
        public byte IsAlertable;
        public string Subject = "";
        public string Message = "";

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            this.SenderID = input.GetPascalString();
            this.SenderAccount = input.GetPascalString();
            this.Badge = input.Get();
            this.IsAlertable = input.Get();
            this.Subject = input.GetPascalString();
            this.Message = input.GetPascalString();
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.AnnouncementMsgPDU;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutPascalString(SenderID);
            output.PutPascalString(SenderAccount);
            output.Put(Badge);
            output.Put(IsAlertable);
            output.PutPascalString(Subject);
            output.PutPascalString(Message);
        }
    }
}

using FSO.Common.Serialization;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Voltron.Model;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class ChatMsgPDU : AbstractVoltronPacket
    {
        public bool EchoRequested;
        public Sender Sender;
        public byte Badge;
        public byte Alertable;
        public string Message;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            EchoRequested = input.Get() == 0x01;
            Sender = GetSender(input);
            Badge = input.Get();
            Alertable = input.Get();
            Message = input.GetPascalString();
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.ChatMsgPDU;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.Put(EchoRequested ? (byte)1 : (byte)0);
            PutSender(output, Sender);
            output.Put(Badge);
            output.Put(Alertable);
            output.PutPascalString(Message);
        }
    }
}

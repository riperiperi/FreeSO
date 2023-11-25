using Mina.Core.Buffer;
using FSO.Server.Protocol.Voltron.Model;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class OccupantArrivedPDU : AbstractVoltronPacket
    {
        public Sender Sender;
        public byte Badge;
        public bool IsAlertable;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Sender = GetSender(input);
            Badge = input.Get();
            IsAlertable = input.Get() == 0x1;
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.OccupantArrivedPDU;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            PutSender(output, Sender);
            output.Put(Badge);
            output.Put((IsAlertable ? (byte)0x01 : (byte)0x00));
        }
    }
}

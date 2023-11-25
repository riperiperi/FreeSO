using Mina.Core.Buffer;
using FSO.Server.Protocol.Voltron.Model;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class FindPlayerPDU : AbstractVoltronPacket
    {
        public Sender Sender;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            this.Sender = GetSender(input);
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.FindPlayerPDU;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            //var result = Allocate(8);
            //result.AutoExpand = true;
            PutSender(output, Sender);
            //return result;
        }
    }
}

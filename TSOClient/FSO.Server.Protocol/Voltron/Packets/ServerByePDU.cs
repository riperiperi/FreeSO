using Mina.Core.Buffer;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class ServerByePDU : AbstractVoltronPacket
    {
        public uint ReasonCode;
        public string ReasonText;
        public string Ticket;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            this.ReasonCode = input.GetUInt32();
            this.ReasonText = input.GetPascalString();
            this.Ticket = input.GetPascalString();
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.ServerByePDU;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            var text = ReasonText;
            if(text == null){
                text = "";
            }
            
            output.PutUInt32(ReasonCode);
            output.PutPascalString(text);
            output.PutPascalString(Ticket);
        }
    }
}

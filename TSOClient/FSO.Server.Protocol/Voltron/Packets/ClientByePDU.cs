using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Utils;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class ClientByePDU : AbstractVoltronPacket
    {
        public uint ReasonCode;
        public string ReasonText;
        public byte RequestTicket;

        public override void Deserialize(IoBuffer input)
        {
            this.ReasonCode = input.GetUInt32();
            this.ReasonText = input.GetPascalString();
            this.RequestTicket = input.Get();
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.ClientByePDU;
        }

        public override IoBuffer Serialize()
        {
            var text = ReasonText;
            if(text == null){
                text = "";
            }

            var result = Allocate(9 + text.Length);
            result.PutUInt32(ReasonCode);
            result.PutPascalString(text);
            result.Put(RequestTicket);
            return result;
        }
    }
}

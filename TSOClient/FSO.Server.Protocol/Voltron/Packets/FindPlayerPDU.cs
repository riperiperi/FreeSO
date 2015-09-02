using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Voltron.Model;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class FindPlayerPDU : AbstractVoltronPacket
    {
        public Sender Sender;

        public override void Deserialize(IoBuffer input)
        {
            this.Sender = GetSender(input);
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.FindPlayerPDU;
        }

        public override IoBuffer Serialize()
        {
            var result = Allocate(8);
            result.AutoExpand = true;
            PutSender(result, Sender);
            return result;
        }
    }
}

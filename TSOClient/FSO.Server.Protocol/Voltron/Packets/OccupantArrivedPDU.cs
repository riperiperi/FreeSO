using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Voltron.Model;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class OccupantArrivedPDU : AbstractVoltronPacket
    {
        public Sender Sender;
        public byte Badge;
        public bool IsAlertable;

        public override void Deserialize(IoBuffer input)
        {
            Sender = GetSender(input);
            Badge = input.Get();
            IsAlertable = input.Get() == 0x1;
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.OccupantArrivedPDU;
        }

        public override IoBuffer Serialize()
        {
            var result = Allocate(2);
            result.AutoExpand = true;
            PutSender(result, Sender);
            result.Put(Badge);
            result.Put((IsAlertable ? (byte)0x01 : (byte)0x00));
            return result;
        }
    }
}

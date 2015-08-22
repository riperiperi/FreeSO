using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class TransmitCreateAvatarNotificationPDU : AbstractVoltronPacket
    {
        public override void Deserialize(IoBuffer input)
        {

        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.TransmitCreateAvatarNotificationPDU;
        }

        public override IoBuffer Serialize()
        {
            var buffer = Allocate(1);
            buffer.Put(10);
            return buffer;
        }
    }
}

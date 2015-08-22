using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Utils;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class SetInvinciblePDU : AbstractVoltronPacket
    {
        public uint Action;

        public override void Deserialize(IoBuffer input)
        {
            this.Action = input.GetUInt32();
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.SetInvinciblePDU;
        }

        public override IoBuffer Serialize()
        {
            var result = Allocate(4);
            result.PutUInt32(Action);
            return result;
        }
    }
}

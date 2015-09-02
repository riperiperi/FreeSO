using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Utils;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class FindPlayerResponsePDU : AbstractVoltronPacket
    {
        public uint StatusCode;
        public string ReasonText;

        public override void Deserialize(IoBuffer input)
        {
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.FindPlayerResponsePDU;
        }

        public override IoBuffer Serialize()
        {
            var result = Allocate(8);
            result.AutoExpand = true;

            result.PutUInt32(StatusCode);
            result.PutPascalString(ReasonText);

            return result;
        }
    }
}

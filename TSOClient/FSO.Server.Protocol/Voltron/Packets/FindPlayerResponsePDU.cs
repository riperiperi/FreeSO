using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class FindPlayerResponsePDU : AbstractVoltronPacket
    {
        public uint StatusCode;
        public string ReasonText;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.FindPlayerResponsePDU;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            //var result = Allocate(8);
            //result.AutoExpand = true;

            output.PutUInt32(StatusCode);
            output.PutPascalString(ReasonText);

            //return result;
        }
    }
}

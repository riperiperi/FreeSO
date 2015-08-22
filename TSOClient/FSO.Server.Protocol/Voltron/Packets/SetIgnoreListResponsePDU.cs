using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Utils;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class SetIgnoreListResponsePDU : AbstractVoltronPacket
    {
        public uint StatusCode;
        public string ReasonText;
        public uint MaxNumberOfIgnored;

        public override void Deserialize(IoBuffer input)
        {
            this.StatusCode = input.GetUInt32();
            this.ReasonText = input.GetPascalString();
            this.MaxNumberOfIgnored = input.GetUInt32();
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.SetIgnoreListResponsePDU;
        }

        public override IoBuffer Serialize()
        {
            var result = Allocate(8 + 4 + ReasonText.Length);
            result.PutUInt32(StatusCode);
            result.PutPascalString(this.ReasonText);
            result.PutUInt32(MaxNumberOfIgnored);
            return result;
        }
    }
}

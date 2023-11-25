using Mina.Core.Buffer;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class SetIgnoreListResponsePDU : AbstractVoltronPacket
    {
        public uint StatusCode;
        public string ReasonText;
        public uint MaxNumberOfIgnored;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            this.StatusCode = input.GetUInt32();
            this.ReasonText = input.GetPascalString();
            this.MaxNumberOfIgnored = input.GetUInt32();
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.SetIgnoreListResponsePDU;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            //var result = Allocate(8 + 4 + ReasonText.Length);
            output.PutUInt32(StatusCode);
            output.PutPascalString(this.ReasonText);
            output.PutUInt32(MaxNumberOfIgnored);
            //return result;
        }
    }
}

using Mina.Core.Buffer;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class SetInvinciblePDU : AbstractVoltronPacket
    {
        public uint Action;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            this.Action = input.GetUInt32();
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.SetInvinciblePDU;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(Action);
        }
    }
}

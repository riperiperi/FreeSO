using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public class HealthPing : AbstractGluonCallPacket
    {
        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            base.Deserialize(input, context);
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            base.Serialize(output, context);
        }

        public override GluonPacketType GetPacketType()
        {
            return GluonPacketType.HealthPing;
        }
    }
}

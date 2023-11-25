using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public class HealthPingResponse : AbstractGluonCallPacket
    {
        public string PoolHash { get; set; }

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            base.Deserialize(input, context);
            PoolHash = input.GetPascalString();
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            base.Serialize(output, context);
            output.PutPascalString(PoolHash);
        }

        public override GluonPacketType GetPacketType()
        {
            return GluonPacketType.HealthPingResponse;
        }
    }
}

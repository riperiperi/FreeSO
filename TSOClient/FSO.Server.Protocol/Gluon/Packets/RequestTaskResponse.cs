using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public class RequestTaskResponse : AbstractGluonCallPacket
    {
        public int TaskId;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            base.Deserialize(input, context);
            TaskId = input.GetInt32();
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            base.Serialize(output, context);
            output.PutInt32(TaskId);
        }

        public override GluonPacketType GetPacketType()
        {
            return GluonPacketType.RequestTaskResponse;
        }
    }
}

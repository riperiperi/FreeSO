using System.Text;
using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public class RequestTask : AbstractGluonCallPacket
    {
        public string TaskType { get; set; }
        public string ParameterJson { get; set; }
        public int ShardId { get; set; }

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            base.Deserialize(input, context);
            TaskType = input.GetPascalString();
            ShardId = input.GetInt32();
            ParameterJson = input.GetString(Encoding.UTF8);
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            base.Serialize(output, context);
            output.PutPascalString(TaskType);
            output.PutInt32(ShardId);
            output.PutString(ParameterJson, Encoding.UTF8);
        }

        public override GluonPacketType GetPacketType()
        {
            return GluonPacketType.RequestTask;
        }
    }
}

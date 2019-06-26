using FSO.Common.Serialization;
using FSO.Server.Protocol.Gluon.Model;
using FSO.Server.Common;
using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public class ShardShutdownRequest : AbstractGluonPacket
    {
        public uint ShardId;
        public ShutdownType Type;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            ShardId = input.GetUInt32();
            Type = input.GetEnum<ShutdownType>();
        }

        public override GluonPacketType GetPacketType()
        {
            return GluonPacketType.ShardShutdownRequest;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(ShardId);
            output.PutEnum(Type);
        }
    }
}

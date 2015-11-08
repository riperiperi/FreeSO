using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public class AdvertiseCapacity : AbstractGluonPacket
    {
        public int MaxLots;
        public int CurrentLots;
        public float Cpu;
        public long RamUsed;
        public long RamAvaliable;

        public override GluonPacketType GetPacketType()
        {
            return GluonPacketType.AdvertiseCapacity;
        }

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
        }
    }
}

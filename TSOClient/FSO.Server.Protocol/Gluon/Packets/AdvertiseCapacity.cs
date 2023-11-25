using Mina.Core.Buffer;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public class AdvertiseCapacity : AbstractGluonPacket
    {
        public short MaxLots;
        public short CurrentLots;
        public byte CpuPercentAvg;
        public long RamUsed;
        public long RamAvaliable;

        public override GluonPacketType GetPacketType()
        {
            return GluonPacketType.AdvertiseCapacity;
        }

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            MaxLots = input.GetInt16();
            CurrentLots = input.GetInt16();
            CpuPercentAvg = input.Get();
            RamUsed = input.GetInt64();
            RamAvaliable = input.GetInt64();
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutInt16(MaxLots);
            output.PutInt16(CurrentLots);
            output.Put(CpuPercentAvg);
            output.PutInt64(RamUsed);
            output.PutInt64(RamAvaliable);
        }
    }
}

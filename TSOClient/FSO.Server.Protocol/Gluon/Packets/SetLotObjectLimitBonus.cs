using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Gluon.Packets
{
    /// <summary>
    /// Sent from the API/city to lot servers to push a new ObjectLimitBonus value into a
    /// running lot's VM in real time. The DB has already been updated by the API before
    /// the broadcast — this packet just brings the live VM in sync. Lot servers ignore
    /// the packet for any LotId they're not currently hosting.
    /// </summary>
    public class SetLotObjectLimitBonus : AbstractGluonPacket
    {
        public uint LotId;
        public int TargetBonus;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            LotId = input.GetUInt32();
            TargetBonus = input.GetInt32();
        }

        public override GluonPacketType GetPacketType() => GluonPacketType.SetLotObjectLimitBonus;

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(LotId);
            output.PutInt32(TargetBonus);
        }
    }
}
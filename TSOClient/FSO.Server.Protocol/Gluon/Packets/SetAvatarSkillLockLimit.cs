using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Gluon.Packets
{
    /// <summary>
    /// Sent from the API/city to lot servers to push a new SkillLock pool size into a
    /// running avatar's VM in real time. The DB has already been updated by the API
    /// before the broadcast — this packet just brings the live VM in sync. Lot servers
    /// search their hosted lots for an avatar with the matching PersistID; the one
    /// (zero or one) that has them forwards the VM update. All others ignore.
    /// </summary>
    public class SetAvatarSkillLockLimit : AbstractGluonPacket
    {
        public uint AvatarId;
        public short NewLimit;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            AvatarId = input.GetUInt32();
            NewLimit = input.GetInt16();
        }

        public override GluonPacketType GetPacketType() => GluonPacketType.SetAvatarSkillLockLimit;

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(AvatarId);
            output.PutInt16(NewLimit);
        }
    }
}
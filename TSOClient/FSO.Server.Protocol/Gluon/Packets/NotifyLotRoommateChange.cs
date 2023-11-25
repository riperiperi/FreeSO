using FSO.Common.Serialization;
using FSO.Server.Protocol.Gluon.Model;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Gluon.Packets
{
    /// <summary>
    /// A signal sent from the city server to notify a lot that an avatar's roommate status on that lot has changed.
    /// May wish to change this to be more generic for further avatar related changes in future.
    /// </summary>
    public class NotifyLotRoommateChange : AbstractGluonPacket
    {
        public uint AvatarId;
        public uint ReplaceId;
        public int LotId;
        public ChangeType Change;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            AvatarId = input.GetUInt32();
            ReplaceId = input.GetUInt32();
            LotId = input.GetInt32();
            Change = input.GetEnum<ChangeType>();
        }

        public override GluonPacketType GetPacketType()
        {
            return GluonPacketType.NotifyLotRoommateChange;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(AvatarId);
            output.PutUInt32(ReplaceId);
            output.PutInt32(LotId);
            output.PutEnum(Change);
        }
    }
}

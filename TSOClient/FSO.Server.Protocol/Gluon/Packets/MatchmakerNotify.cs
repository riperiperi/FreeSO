using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Gluon.Packets
{
    /// <summary>
    /// Lot -> City server messages used to notify the matchmaker about some change to lot state.
    /// (currently only when an avatar leaves a lot. this frees up a space for the matchmaker to shove someone else in)
    /// </summary>
    public class MatchmakerNotify : AbstractGluonPacket
    {
        public MatchmakerNotifyType Mode;
        public uint LotID;
        public uint AvatarID; 

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Mode = input.GetEnum<MatchmakerNotifyType>();
            LotID = input.GetUInt32();
            AvatarID = input.GetUInt32();
        }

        public override GluonPacketType GetPacketType()
        {
            return GluonPacketType.MatchmakerNotify;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(Mode);
            output.PutUInt32(LotID);
            output.PutUInt32(AvatarID);
        }
    }

    public enum MatchmakerNotifyType : byte
    {
        RemoveAvatar = 1
    }
}

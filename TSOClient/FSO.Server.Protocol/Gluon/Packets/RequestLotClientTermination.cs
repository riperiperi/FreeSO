using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public class RequestLotClientTermination : AbstractGluonPacket
    {
        public uint AvatarId;
        public int LotId;
        public string FromOwner;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            AvatarId = input.GetUInt32();
            LotId = input.GetInt32();
            FromOwner = input.GetPascalString();
        }

        public override GluonPacketType GetPacketType()
        {
            return GluonPacketType.RequestLotClientTermination;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(AvatarId);
            output.PutInt32(LotId);
            output.PutPascalString(FromOwner);
        }
    }
}

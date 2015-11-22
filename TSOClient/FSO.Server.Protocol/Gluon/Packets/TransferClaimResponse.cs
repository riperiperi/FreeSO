using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Serialization;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Gluon.Model;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public class TransferClaimResponse : AbstractGluonPacket
    {
        public TransferClaimResponseStatus Status;
        public ClaimType Type;
        public uint EntityId;
        public uint ClaimId;
        public string NewOwner;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Status = input.GetEnum<TransferClaimResponseStatus>();
            Type = input.GetEnum<ClaimType>();
            EntityId = input.GetUInt32();
            ClaimId = input.GetUInt32();
            NewOwner = input.GetPascalString();
        }

        public override GluonPacketType GetPacketType()
        {
            return GluonPacketType.TransferClaimResponse;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(Status);
            output.PutEnum(Type);
            output.PutUInt32(EntityId);
            output.PutUInt32(ClaimId);
            output.PutPascalString(NewOwner);
        }
    }

    public enum TransferClaimResponseStatus
    {
        ACCEPTED,
        REJECTED,
        CLAIM_NOT_FOUND
    }
}

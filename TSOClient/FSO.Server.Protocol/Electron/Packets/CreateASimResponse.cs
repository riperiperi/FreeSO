using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Utils;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class CreateASimResponse : AbstractElectronPacket
    {
        public CreateASimStatus Status { get; set; }
        public CreateASimFailureReason Reason { get; set; } = CreateASimFailureReason.NONE;
        public uint NewAvatarId { get; set; }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.CreateASimResponse;
        }

        public override void Deserialize(IoBuffer input)
        {
            Status = input.GetEnum<CreateASimStatus>();
            Reason = input.GetEnum<CreateASimFailureReason>();
            NewAvatarId = input.GetUInt32();
        }

        public override IoBuffer Serialize()
        {
            var result = Allocate(8);
            result.PutEnum<CreateASimStatus>(Status);
            result.PutEnum<CreateASimFailureReason>(Reason);
            result.PutUInt32(NewAvatarId);
            return result;
        }
    }

    public enum CreateASimStatus
    {
        SUCCESS = 0x01,
        FAILED = 0x02
    }

    public enum CreateASimFailureReason
    {
        NONE = 0x00,
        NAME_TAKEN = 0x01,
        NAME_VALIDATION_ERROR = 0x02,
        DESC_VALIDATION_ERROR = 0x03,
        BODY_VALIDATION_ERROR = 0x04,
        HEAD_VALIDATION_ERROR = 0x05
    }
}

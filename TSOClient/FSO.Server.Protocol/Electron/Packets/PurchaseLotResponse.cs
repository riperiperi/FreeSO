using FSO.Common.Serialization;
using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class PurchaseLotResponse : AbstractElectronPacket
    {
        public PurchaseLotStatus Status { get; set; }
        public PurchaseLotFailureReason Reason { get; set; } = PurchaseLotFailureReason.NONE;
        public uint NewLotId { get; set; }
        public int NewFunds { get; set; }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.PurchaseLotResponse;
        }

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Status = input.GetEnum<PurchaseLotStatus>();
            Reason = input.GetEnum<PurchaseLotFailureReason>();
            NewLotId = input.GetUInt32();
            NewFunds = input.GetInt32();
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum<PurchaseLotStatus>(Status);
            output.PutEnum<PurchaseLotFailureReason>(Reason);
            output.PutUInt32(NewLotId);
            output.PutInt32(NewFunds);
        }
    }

    public enum PurchaseLotStatus
    {
        SUCCESS = 0x01,
        FAILED = 0x02
    }

    public enum PurchaseLotFailureReason
    {
        NONE = 0x00,
        NAME_TAKEN = 0x01,
        NAME_VALIDATION_ERROR = 0x02,
        INSUFFICIENT_FUNDS = 0x03,
        LOT_TAKEN = 0x04,
        LOT_NOT_PURCHASABLE = 0x05,
        IN_LOT_CANT_EVICT = 0x06,
        NOT_OFFLINE_FOR_MOVE = 0x07,
        LOCATION_TAKEN = 0x08,
        UNKNOWN = 0xFF
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Gluon
{
    public enum GluonPacketType
    {
        AdvertiseCapacity,
        TransferClaim,
        TransferClaimResponse,
        Unknown
    }

    public static class GluonPacketTypeUtils
    {
        public static GluonPacketType FromPacketCode(ushort code)
        {
            switch (code)
            {
                case 0x0001:
                    return GluonPacketType.AdvertiseCapacity;
                case 0x0002:
                    return GluonPacketType.TransferClaim;
                case 0x0003:
                    return GluonPacketType.TransferClaimResponse;
                default:
                    return GluonPacketType.Unknown;
            }
        }

        public static ushort GetPacketCode(this GluonPacketType type)
        {
            switch (type)
            {
                case GluonPacketType.AdvertiseCapacity:
                    return 0x0001;
                case GluonPacketType.TransferClaim:
                    return 0x0002;
                case GluonPacketType.TransferClaimResponse:
                    return 0x0003;
            }

            return 0xFFFF;
        }
    }
}

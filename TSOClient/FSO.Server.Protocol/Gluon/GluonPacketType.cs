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
        RequestLotClientTermination,
        ShardShutdownRequest,
        ShardShutdownCompleteResponse,

        HealthPing,
        HealthPingResponse,
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
                case 0x0004:
                    return GluonPacketType.RequestLotClientTermination;
                case 0x0005:
                    return GluonPacketType.ShardShutdownRequest;
                case 0x0006:
                    return GluonPacketType.ShardShutdownCompleteResponse;
                case 0x0007:
                    return GluonPacketType.HealthPing;
                case 0x0008:
                    return GluonPacketType.HealthPingResponse;
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
                case GluonPacketType.RequestLotClientTermination:
                    return 0x0004;
                case GluonPacketType.ShardShutdownRequest:
                    return 0x0005;
                case GluonPacketType.ShardShutdownCompleteResponse:
                    return 0x0006;
                case GluonPacketType.HealthPing:
                    return 0x0007;
                case GluonPacketType.HealthPingResponse:
                    return 0x0008;
            }

            return 0xFFFF;
        }
    }
}

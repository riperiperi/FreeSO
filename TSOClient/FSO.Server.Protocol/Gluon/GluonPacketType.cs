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
            }

            return 0xFFFF;
        }
    }
}

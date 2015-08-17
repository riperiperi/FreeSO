using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Aries
{
    public enum AriesPacketType
    {
        RequestClientSession,
        RequestClientSessionResponse,
        Unknown
    }

    public static class AriesPacketTypeUtils
    {
        public static AriesPacketType FromPacketCode(uint code)
        {
            switch (code)
            {
                case 22:
                    return AriesPacketType.RequestClientSession;
                case 21:
                    return AriesPacketType.RequestClientSessionResponse;
                default:
                    return AriesPacketType.Unknown;
            }
        }

        public static uint GetPacketCode(this AriesPacketType type)
        {
            switch (type)
            {
                case AriesPacketType.RequestClientSession:
                    return 22;
                case AriesPacketType.RequestClientSessionResponse:
                    return 21;
                default:
                    throw new Exception("Unknown aries packet type " + type.ToString());
            }
        }
    }
}

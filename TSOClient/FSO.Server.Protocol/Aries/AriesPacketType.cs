using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Aries
{
    public enum AriesPacketType
    {
        Voltron,
        Electron,
        Gluon,

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
                case 0:
                    return AriesPacketType.Voltron;
                case 1000:
                    return AriesPacketType.Electron;
                case 1001:
                    return AriesPacketType.Gluon;
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
                case AriesPacketType.Voltron:
                    return 0;
                case AriesPacketType.Electron:
                    return 1000;
                case AriesPacketType.Gluon:
                    return 1001;
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

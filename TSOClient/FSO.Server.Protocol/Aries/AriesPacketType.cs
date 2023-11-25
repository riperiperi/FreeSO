using System;

namespace FSO.Server.Protocol.Aries
{
    public enum AriesPacketType
    {
        Voltron,
        Electron,
        Gluon,

        RequestClientSession,
        RequestClientSessionResponse,
        RequestChallenge,
        RequestChallengeResponse,
        AnswerChallenge,
        AnswerAccepted,

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
                case 1002:
                    return AriesPacketType.RequestChallenge;
                case 1003:
                    return AriesPacketType.RequestChallengeResponse;
                case 1004:
                    return AriesPacketType.AnswerChallenge;
                case 1005:
                    return AriesPacketType.AnswerAccepted;
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
                case AriesPacketType.RequestChallenge:
                    return 1002;
                case AriesPacketType.RequestChallengeResponse:
                    return 1003;
                case AriesPacketType.AnswerChallenge:
                    return 1004;
                case AriesPacketType.AnswerAccepted:
                    return 1005;
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

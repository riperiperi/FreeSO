using FSO.Server.Protocol.Aries.Packets;
using System;
using System.Collections.Generic;

namespace FSO.Server.Protocol.Aries
{
    public class AriesPackets
    {
        public static Dictionary<uint, Type> ARIES_PACKET_BY_TYPEID;
        public static Type[] ARIES_PACKETS = new Type[] {
            typeof(RequestClientSession),
            typeof(RequestClientSessionResponse),
            typeof(RequestChallenge),
            typeof(RequestChallengeResponse),
            typeof(AnswerChallenge),
            typeof(AnswerAccepted)
        };

        static AriesPackets()
        {
            ARIES_PACKET_BY_TYPEID = new Dictionary<uint, Type>();
            foreach (Type packetType in ARIES_PACKETS)
            {
                IAriesPacket packet = (IAriesPacket)Activator.CreateInstance(packetType);
                ARIES_PACKET_BY_TYPEID.Add(packet.GetPacketType().GetPacketCode(), packetType);
            }
        }

        public static Type GetByPacketCode(uint code)
        {
            if (ARIES_PACKET_BY_TYPEID.ContainsKey(code))
            {
                return ARIES_PACKET_BY_TYPEID[code];
            }
            else
            {
                return null;
            }
        }
    }
}

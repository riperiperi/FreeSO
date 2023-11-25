using FSO.Server.Protocol.Voltron.Packets;
using System;
using System.Collections.Generic;

namespace FSO.Server.Protocol.Voltron
{
    public class VoltronPackets
    {
        public static Dictionary<ushort, Type> VOLTRON_PACKET_BY_TYPEID;
        public static Type[] VOLTRON_PACKETS = new Type[] {
            typeof(ClientOnlinePDU),
            typeof(HostOnlinePDU),
            typeof(SetIgnoreListPDU),
            typeof(SetIgnoreListResponsePDU),
            typeof(SetInvinciblePDU),
            typeof(RSGZWrapperPDU),
            typeof(TransmitCreateAvatarNotificationPDU),
            typeof(DataServiceWrapperPDU),
            typeof(DBRequestWrapperPDU),
            typeof(OccupantArrivedPDU),
            typeof(ClientByePDU),
            typeof(ServerByePDU),
            typeof(FindPlayerPDU),
            typeof(FindPlayerResponsePDU),
            typeof(ChatMsgPDU),
            typeof(AnnouncementMsgPDU)
        };

        static VoltronPackets()
        {
            VOLTRON_PACKET_BY_TYPEID = new Dictionary<ushort, Type>();
            foreach (Type packetType in VOLTRON_PACKETS)
            {
                IVoltronPacket packet = (IVoltronPacket)Activator.CreateInstance(packetType);
                VOLTRON_PACKET_BY_TYPEID.Add(packet.GetPacketType().GetPacketCode(), packetType);
            }
        }

        public static Type GetByPacketCode(ushort code)
        {
            if (VOLTRON_PACKET_BY_TYPEID.ContainsKey(code))
            {
                return VOLTRON_PACKET_BY_TYPEID[code];
            }
            else {
                return null;
            }
        }
    }
}

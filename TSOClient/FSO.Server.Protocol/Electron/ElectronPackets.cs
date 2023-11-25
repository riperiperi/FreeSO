using FSO.Server.Protocol.Electron.Packets;
using System;
using System.Collections.Generic;

namespace FSO.Server.Protocol.Electron
{
    public class ElectronPackets
    {
        public static Dictionary<ushort, Type> ELECTRON_PACKET_BY_TYPEID;
        public static Type[] ELECTRON_PACKETS = new Type[] {
            typeof(CreateASimResponse),
            typeof(PurchaseLotRequest),
            typeof(PurchaseLotResponse),
            typeof(InstantMessage),
            typeof(FindLotRequest),
            typeof(FindLotResponse),
            typeof(FSOVMTickBroadcast),
            typeof(FSOVMDirectToClient),
            typeof(FSOVMCommand),
            typeof(FindAvatarRequest),
            typeof(FindAvatarResponse),
            typeof(ChangeRoommateRequest),
            typeof(KeepAlive),
            typeof(ChangeRoommateResponse),
            typeof(ModerationRequest),
            typeof(FSOVMProtocolMessage),
            typeof(AvatarRetireRequest),
            typeof(MailRequest),
            typeof(MailResponse),
            typeof(NhoodRequest),
            typeof(NhoodResponse),
            typeof(NhoodCandidateList),
            typeof(BulletinRequest),
            typeof(BulletinResponse),
            typeof(GlobalTuningUpdate)
        };

        static ElectronPackets()
        {
            ELECTRON_PACKET_BY_TYPEID = new Dictionary<ushort, Type>();
            foreach (Type packetType in ELECTRON_PACKETS)
            {
                IElectronPacket packet = (IElectronPacket)Activator.CreateInstance(packetType);
                ELECTRON_PACKET_BY_TYPEID.Add(packet.GetPacketType().GetPacketCode(), packetType);
            }
        }

        public static Type GetByPacketCode(ushort code)
        {
            if (ELECTRON_PACKET_BY_TYPEID.ContainsKey(code))
            {
                return ELECTRON_PACKET_BY_TYPEID[code];
            }
            else
            {
                return null;
            }
        }
    }
}

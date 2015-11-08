using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Gluon.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Gluon
{
    public class GluonPackets
    {
        public static Dictionary<ushort, Type> GLUON_PACKET_BY_TYPEID;
        public static Type[] ELECTRON_PACKETS = new Type[] {
            typeof(AdvertiseCapacity)
        };

        static GluonPackets()
        {
            GLUON_PACKET_BY_TYPEID = new Dictionary<ushort, Type>();
            foreach (Type packetType in ELECTRON_PACKETS)
            {
                IGluonPacket packet = (IGluonPacket)Activator.CreateInstance(packetType);
                GLUON_PACKET_BY_TYPEID.Add(packet.GetPacketType().GetPacketCode(), packetType);
            }
        }

        public static Type GetByPacketCode(ushort code)
        {
            if (GLUON_PACKET_BY_TYPEID.ContainsKey(code))
            {
                return GLUON_PACKET_BY_TYPEID[code];
            }
            else
            {
                return null;
            }
        }
    }
}

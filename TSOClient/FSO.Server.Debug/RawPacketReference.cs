using FSO.Server.Common;
using FSO.Server.Protocol.Voltron;
using FSO.Server.Protocol.Aries;

namespace tso.debug.network
{
    public class RawPacketReference
    {
        public Packet Packet;
        public int Sequence;
    }


    public static class PacketExtensions
    {
        public static string GetPacketName(this Packet packet)
        {
            switch (packet.Type)
            {
                case PacketType.VOLTRON:
                    return VoltronPacketTypeUtils.FromPacketCode((ushort)packet.SubType).ToString();
                case PacketType.ARIES:
                    return AriesPacketTypeUtils.FromPacketCode(packet.SubType).ToString();
            }
            return packet.Type.ToString() + " (" + packet.SubType + ")";
        }
    }
}

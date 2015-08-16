using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Server.Common;
using FSO.Server.Protocol.Voltron;

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
                    return VoltronPacketTypeUtils.FromPacketCode(packet.SubType).ToString();
            }
            return packet.Type.ToString() + " (" + packet.SubType + ")";
        }
    }
}

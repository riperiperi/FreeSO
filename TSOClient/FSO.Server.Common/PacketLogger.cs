namespace FSO.Server.Common
{
    public interface IPacketLogger
    {
        void OnPacket(Packet packet);
    }

    public class Packet
    {
        public PacketType Type;
        public uint SubType;

        public byte[] Data;
        public PacketDirection Direction;
    }

    public enum PacketType
    {
        ARIES,
        VOLTRON,
        ELECTRON
    }

    public enum PacketDirection
    {
        OUTPUT,
        INPUT
    }
}

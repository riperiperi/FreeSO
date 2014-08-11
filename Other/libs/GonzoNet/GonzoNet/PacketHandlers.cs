using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GonzoNet
{
    /// <summary>
    /// Framework for registering packet handlers with GonzoNet.
    /// </summary>
    public class PacketHandlers
    {
        /**
         * Framework
         */
        private static Dictionary<byte, PacketHandler> m_Handlers = new Dictionary<byte, PacketHandler>();

        /// <summary>
        /// Registers a PacketHandler with GonzoNet.
        /// </summary>
        /// <param name="id">The ID of the packet.</param>
        /// <param name="size">The size of the packet.</param>
        /// <param name="handler">The handler for the packet.</param>
        public static void Register(byte id, bool Encrypted, ushort size, OnPacketReceive handler)
        {
            m_Handlers.Add(id, new PacketHandler(id, Encrypted, size, handler));
        }

        public static void Handle(NetworkClient Client, ProcessedPacket stream)
        {
            byte ID = (byte)stream.ReadByte();
            if (m_Handlers.ContainsKey(ID))
            {
                m_Handlers[ID].Handler(Client, stream);
            }
        }

        public static PacketHandler Get(byte id)
        {
            return m_Handlers[id];
        }
    }
}

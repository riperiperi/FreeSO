using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GonzoNet
{
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
        public static void Register(byte id, int size, OnPacketReceive handler)
        {
            m_Handlers.Add(id, new PacketHandler(id, size, handler));
        }

        public static void Handle(PacketStream stream)
        {
            byte ID = (byte)stream.ReadByte();
            if (m_Handlers.ContainsKey(ID))
            {
                m_Handlers[ID].Handler(stream);
            }
        }

        public static PacketHandler Get(byte id)
        {
            return m_Handlers[id];
        }
    }
}

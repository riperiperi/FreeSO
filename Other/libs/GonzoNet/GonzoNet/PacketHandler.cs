using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GonzoNet
{
    public delegate void OnPacketReceive(PacketStream Packet);

    public class PacketHandler
    {
        private byte m_ID;
        private int m_Length;
        private OnPacketReceive m_Handler;

        public PacketHandler(byte id, int size, OnPacketReceive handler)
        {
            this.m_ID = id;
            this.m_Length = size;
            this.m_Handler = handler;
        }

        public byte ID
        {
            get { return m_ID; }
        }

        public int Length
        {
            get { return m_Length; }
        }

        public OnPacketReceive Handler
        {
            get
            {
                return m_Handler;
            }
        }
    }
}

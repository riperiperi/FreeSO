/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the GonzoNet.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GonzoNet
{
    public delegate void OnPacketReceive(NetworkClient Client, ProcessedPacket Packet);

    /// <summary>
    /// A handler for a ProcessedPacket instance.
    /// </summary>
    public class PacketHandler
    {
        private byte m_ID;
        private bool m_Encrypted;
        private ushort m_Length;
        private OnPacketReceive m_Handler;
        private bool m_VarLength;

        /// <summary>
        /// Constructs a new PacketHandler instance.
        /// </summary>
        /// <param name="id">The ID of the ProcessedPacket instance to handle.</param>
        /// <param name="Encrypted">Is the ProcessedPacket instance encrypted?</param>
        /// <param name="size">The size of the ProcessedPacket instance. 0 if variable length.</param>
        /// <param name="handler">A OnPacketReceive instance.</param>
        public PacketHandler(byte id, bool Encrypted, ushort size, OnPacketReceive handler)
        {
            this.m_ID = id;
            this.m_Length = size;
            this.m_Handler = handler;
            this.m_Encrypted = Encrypted;

            if (size == 0)
                m_VarLength = true;
            else
                m_VarLength = false;
        }

        /// <summary>
        /// The ID of the ProcessedPacket instance to handle.
        /// </summary>
        public byte ID
        {
            get { return m_ID; }
        }

        /// <summary>
        /// Is the ProcessedPacket instance encrypted?
        /// </summary>
        public bool Encrypted
        {
            get { return m_Encrypted; }
        }

        /// <summary>
        /// The size of the ProcessedPacket instance. 0 if variable length.
        /// </summary>
        public ushort Length
        {
            get { return m_Length; }
        }

        /// <summary>
        /// Is the ProcessedPacket instance of variable length?
        /// </summary>
        public bool VariableLength
        {
            get { return m_VarLength; }
        }

        /// <summary>
        /// A OnPacketReceive instance.
        /// </summary>
        public OnPacketReceive Handler
        {
            get
            {
                return m_Handler;
            }
        }
    }
}

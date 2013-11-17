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

    public class PacketHandler
    {
        private byte m_ID;
        private bool m_Encrypted;
        private ushort m_Length;
        private OnPacketReceive m_Handler;

        public PacketHandler(byte id, bool Encrypted, ushort size, OnPacketReceive handler)
        {
            this.m_ID = id;
            this.m_Length = size;
            this.m_Handler = handler;
        }

        public byte ID
        {
            get { return m_ID; }
        }

        public bool Encrypted
        {
            get { return m_Encrypted; }
        }

        public ushort Length
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

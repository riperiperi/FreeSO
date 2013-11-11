/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the GonzoNet.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using GonzoNet.Exceptions;

namespace GonzoNet
{
    /// <summary>
    /// A packet that has been decrypted and processed, ready to read from.
    /// </summary>
    public class ProcessedPacket : PacketStream
    {
        public ushort DecryptedLength;

        /// <summary>
        /// Creates a new ProcessedPacket instance.
        /// </summary>
        /// <param name="ID">The ID of the packet.</param>
        /// <param name="Encrypted">Is this packet encrypted?</param>
        /// <param name="Length">The length of the packet.</param>
        /// <param name="EncKey">The encryptionkey, can be null if the packet isn't encrypted.</param>
        /// <param name="DataBuffer">The databuffer containing the packet.</param>
        public ProcessedPacket(byte ID, bool Encrypted, int Length, byte[] EncKey, byte[] DataBuffer)
            : base(ID, Length, DataBuffer)
        {
            byte Opcode = (byte)this.ReadByte();
            this.m_Length = (ushort)this.ReadUShort();

            if (Encrypted)
            {
                this.DecryptedLength = (ushort)this.ReadUShort();

                if (this.DecryptedLength != this.m_Length)
                {
                    //Something's gone haywire, throw an error...
                    throw new PacketProcessingException("DecryptedLength didn't match packet's length!");
                }
            }

            if(Encrypted)
                this.DecryptPacket(EncKey, new DESCryptoServiceProvider(), this.DecryptedLength);
        }
    }
}

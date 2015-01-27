/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
using GonzoNet.Encryption;

namespace GonzoNet
{
    /// <summary>
    /// A packet that has been decrypted and processed, ready to read from.
    /// </summary>
    public class ProcessedPacket : PacketStream
    {
        public volatile ushort DecryptedLength;
        public volatile bool DecryptedSuccessfully = false;

        /// <summary>
        /// Creates a new ProcessedPacket instance.
        /// </summary>
        /// <param name="ID">The ID of the packet.</param>
        /// <param name="Encrypted">Is this packet encrypted?</param>
        /// <param name="Length">The length of the packet.</param>
        /// <param name="EncKey">The encryptionkey, can be null if the packet isn't encrypted.</param>
        /// <param name="DataBuffer">The databuffer containing the packet.</param>
        public ProcessedPacket(byte ID, bool Encrypted, bool VariableLength, ushort Length, Encryptor Enc, byte[] DataBuffer)
            : base(ID, Length, DataBuffer)
        {
            byte Opcode = (byte)this.ReadByte();

            if (VariableLength)
                this.m_Length = (ushort)this.ReadUShort();
            else
                this.m_Length = Length;

            if (Encrypted)
            {
                this.DecryptedLength = (ushort)this.ReadUShort();

                //Length should be at least the length of the decrypted data.
                if ((m_Length - (int)PacketHeaders.ENCRYPTED) < this.DecryptedLength)
                {
                    //Something's gone haywire, throw an error...
                    throw new PacketProcessingException("DecryptedLength didn't match packet's length!\n" +
                    Convert.ToBase64String(this.m_BaseStream.ToArray()));
                }

                DecryptionArgsContainer Args = Enc.GetDecryptionArgsContainer();
                Args.UnencryptedLength = DecryptedLength;

                this.m_BaseStream = Enc.DecryptPacket(this, Args);

                if (m_BaseStream != null)
                {
                    DecryptedSuccessfully = true;
                    this.m_BaseStream.Position = 0;
                    this.m_Reader = new System.IO.BinaryReader(m_BaseStream);
                }
                else
                {
                    //Let clients of this class choose what to do with the faulty state.
                    DecryptedSuccessfully = false;
                }
            }
        }
    }
}
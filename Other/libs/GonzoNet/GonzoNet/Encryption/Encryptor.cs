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
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace GonzoNet.Encryption
{
    public abstract class Encryptor
    {
        protected string m_Password;

        public Encryptor(string Password)
        {
            m_Password = Password;
        }

        /// <summary>
        /// Writes a packet's header and encrypts the contents of the packet (not the header).
        /// </summary>
        /// <param name="PacketID">The ID of the packet.</param>
        /// <param name="PacketData">The packet's contents.</param>
        /// <returns>The finalized packet!</returns>
        public abstract byte[] FinalizePacket(byte PacketID, byte[] PacketData);

        public abstract DecryptionArgsContainer GetDecryptionArgsContainer();

        /// <summary>
        /// Decrypts the data in this PacketStream.
        /// WARNING: ASSUMES THAT THE 7-BYTE HEADER
        /// HAS BEEN READ (ID, LENGTH, DECRYPTEDLENGTH)!
        /// </summary>
        /// <param name="Key">The client's en/decryptionkey.</param>
        /// <param name="Service">The client's DESCryptoServiceProvider instance.</param>
        /// <param name="UnencryptedLength">The packet's unencrypted length (third byte in the header).</param>
        public abstract MemoryStream DecryptPacket(PacketStream EncryptedPacket, DecryptionArgsContainer DecryptionArgs);
    }
}

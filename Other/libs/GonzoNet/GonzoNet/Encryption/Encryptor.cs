/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
    /// <summary>
    /// Base class for all classes used to encrypt a connection.
    /// </summary>
    public abstract class Encryptor
    {
        protected string m_Password;
        public string Username; //Client's username.

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

        /// <summary>
        /// Creates a container with algorithm-specific arguments used for en/decryption.
        /// </summary>
        /// <returns>A new DecryptionArgsContainer instance, initialized with algorithm-specific arguments.</returns>
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

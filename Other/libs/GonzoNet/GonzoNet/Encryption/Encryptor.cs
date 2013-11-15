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
    }
}

/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using TSOClient.Network.Encryption;

namespace TSOClient.Network
{
    /// <summary>
    /// Contains all the packetsenders in the game that are the result of a UI interaction.
    /// Packetsenders are functions that send packets to the servers.
    /// </summary>
    class UIPacketSenders
    {
        public static void SendLoginRequest(NetworkClient Client, string Username, string Password)
        {
            //Variable length...
            PacketStream Packet = new PacketStream(0x00, 0);
            Packet.WriteByte(0x00);

            SaltedHash Hash = new SaltedHash(new SHA512Managed(), Username.Length);
            byte[] HashBuf = new byte[Encoding.ASCII.GetBytes(Password).Length +
                Encoding.ASCII.GetBytes(Username).Length];

            MemoryStream MemStream = new MemoryStream();

            PasswordDeriveBytes Pwd = new PasswordDeriveBytes(Encoding.ASCII.GetBytes(Password),
                Encoding.ASCII.GetBytes("SALT"), "SHA1", 10);
            byte[] EncKey = Pwd.GetBytes(8);
            PlayerAccount.EncKey = EncKey;

            MemStream.WriteByte((byte)Username.Length);
            MemStream.Write(Encoding.ASCII.GetBytes(Username), 0, Encoding.ASCII.GetBytes(Username).Length);

            HashBuf = Hash.ComputePasswordHash(Username, Password);
            PlayerAccount.Hash = HashBuf;

            MemStream.WriteByte((byte)HashBuf.Length);
            MemStream.Write(HashBuf, 0, HashBuf.Length);

            MemStream.WriteByte((byte)EncKey.Length);
            MemStream.Write(EncKey, 0, EncKey.Length);

            Packet.WriteByte((byte)(2 + MemStream.ToArray().Length + 4));
            Packet.WriteBytes(MemStream.ToArray());
            //TODO: Change this to write a global client version.
            Packet.WriteByte(0x00); //Version 1
            Packet.WriteByte(0x00); //Version 2
            Packet.WriteByte(0x00); //Version 3
            Packet.WriteByte(0x01); //Version 4

            Client.Send(Packet.ToArray());
        }

        /// <summary>
        /// Sends an InitLoginNotify packet to the loginserver.
        /// Client must have received an InitLoginNotify packet
        /// from the server first.
        /// </summary>
        /// <param name="SessionKeyProof">Client's session key proof.</param>
        public static void SendInitLoginNotify(byte[] SessionKeyProof)
        {
            PacketStream Packet = new PacketStream(0x01, 33);
            Packet.WriteByte(0x01);
            Packet.Write(SessionKeyProof, 0, 32);

            PlayerAccount.Client.Send(Packet.ToArray());
        }

        public static void SendCharacterInfoRequest(string TimeStamp)
        {
            PacketStream Packet = new PacketStream(0x05, 0);
            //If this timestamp is newer than the server's timestamp, it means
            //the client doesn't have a charactercache. If it's older, it means
            //the cache needs to be updated. If it matches, the server sends an
            //empty responsepacket.
            //Packet.WriteString(TimeStamp);
            Packet.WriteByte((byte)TimeStamp.Length);
            Packet.WriteBytes(Encoding.ASCII.GetBytes(TimeStamp));

            byte[] PacketData = Packet.ToArray();

            PlayerAccount.Client.Send(FinalizePacket(0x05, new DESCryptoServiceProvider(), PacketData));
        }

        /// <summary>
        /// Writes a packet's header and encrypts the contents of the packet (not the header).
        /// </summary>
        /// <param name="PacketID">The ID of the packet.</param>
        /// <param name="PacketData">The packet's contents.</param>
        /// <returns>The finalized packet!</returns>
        private static byte[] FinalizePacket(byte PacketID, DESCryptoServiceProvider CryptoService, byte[] PacketData)
        {
            MemoryStream FinalizedPacket = new MemoryStream();
            BinaryWriter PacketWriter = new BinaryWriter(FinalizedPacket);

            PasswordDeriveBytes Pwd = new PasswordDeriveBytes(Encoding.ASCII.GetBytes(PlayerAccount.Client.Password),
                Encoding.ASCII.GetBytes("SALT"), "SHA1", 10);

            MemoryStream TempStream = new MemoryStream();
            CryptoStream EncryptedStream = new CryptoStream(TempStream,
                CryptoService.CreateEncryptor(PlayerAccount.EncKey, Encoding.ASCII.GetBytes("@1B2c3D4e5F6g7H8")),
                CryptoStreamMode.Write);
            EncryptedStream.Write(PacketData, 0, PacketData.Length);
            EncryptedStream.FlushFinalBlock();

            PacketWriter.Write(PacketID);
            //The length of the encrypted data can be longer or smaller than the original length,
            //so write the length of the encrypted data.
            PacketWriter.Write((byte)(3 + TempStream.Length));
            PacketWriter.Flush();
            //Also write the length of the unencrypted data.
            PacketWriter.Write((byte)PacketData.Length);
            PacketWriter.Flush();

            PacketWriter.Write(TempStream.ToArray());
            PacketWriter.Flush();

            byte[] ReturnPacket = FinalizedPacket.ToArray();

            PacketWriter.Close();

            return ReturnPacket;
        }
    }
}

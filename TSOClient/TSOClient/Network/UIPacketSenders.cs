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
using GonzoNet;
using GonzoNet.Encryption;
using ProtocolAbstractionLibraryD;
using TSOClient.VM;

namespace TSOClient.Network
{
    /// <summary>
    /// Contains all the packetsenders in the game that are the result of a UI interaction.
    /// Packetsenders are functions that send packets to the servers.
    /// </summary>
    class UIPacketSenders
    {
        public static void SendLoginRequest(LoginArgsContainer Args)
        {
            //Variable length...
            PacketStream Packet = new PacketStream((byte)PacketType.LOGIN_REQUEST, 0);
            Packet.WriteByte(0x00);

            SaltedHash Hash = new SaltedHash(new SHA512Managed(), Args.Username.Length);

            MemoryStream MemStream = new MemoryStream();

            DecryptionArgsContainer DecryptionArgs = Args.Enc.GetDecryptionArgsContainer();
            byte[] EncKey = DecryptionArgs.ARC4DecryptArgs.EncryptionKey;

            MemStream.WriteByte((byte)Args.Username.Length);
            MemStream.Write(Encoding.ASCII.GetBytes(Args.Username), 0, Encoding.ASCII.GetBytes(Args.Username).Length);

            byte[] HashBuf = Hash.ComputePasswordHash(Args.Username, Args.Password);
            PlayerAccount.Hash = HashBuf;

            MemStream.WriteByte((byte)HashBuf.Length);
            MemStream.Write(HashBuf, 0, HashBuf.Length);

            MemStream.WriteByte((byte)EncKey.Length);
            MemStream.Write(EncKey, 0, EncKey.Length);

            Packet.WriteUInt16((ushort)(2 + MemStream.ToArray().Length + 4));
            Packet.WriteBytes(MemStream.ToArray());
            //TODO: Change this to write a global client version.
            Packet.WriteByte(0x00); //Version 1
            Packet.WriteByte(0x00); //Version 2
            Packet.WriteByte(0x00); //Version 3
            Packet.WriteByte(0x01); //Version 4

            Args.Client.Send(Packet.ToArray());
        }

        public static void SendCharacterInfoRequest(string TimeStamp)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.CHARACTER_LIST, 0);
            //If this timestamp is newer than the server's timestamp, it means
            //the client doesn't have a charactercache. If it's older, it means
            //the cache needs to be updated. If it matches, the server sends an
            //empty responsepacket.
            //Packet.WriteString(TimeStamp);
            Packet.WritePascalString(TimeStamp);

            byte[] PacketData = Packet.ToArray();

            //PlayerAccount.Client.Send(FinalizePacket(0x05, new DESCryptoServiceProvider(), PacketData));
            PlayerAccount.Client.SendEncrypted((byte)PacketType.CHARACTER_LIST, PacketData);
        }

        /// <summary>
        /// Sends a CharacterCreate packet to the LoginServer.
        /// </summary>
        /// <param name="Character">The character to create.</param>
        /// <param name="TimeStamp">The timestamp of when this character was created.</param>
        public static void SendCharacterCreate(TSOClient.VM.Sim Character, string TimeStamp)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.CHARACTER_CREATE, 0);
            Packet.WritePascalString(PlayerAccount.Client.ClientEncryptor.Username);
            Packet.WritePascalString(TimeStamp);
            Packet.WritePascalString(Character.Name);
            Packet.WritePascalString(Character.Sex);
            Packet.WritePascalString(Character.Description);
            Packet.WriteUInt64(Character.HeadOutfitID);
            Packet.WriteUInt64(Character.BodyOutfitID);
            Packet.WriteByte((byte)Character.AppearanceType);

            Packet.WritePascalString(Character.ResidingCity.Name);
            Packet.WriteUInt64(Character.ResidingCity.Thumbnail);
            Packet.WritePascalString(Character.ResidingCity.UUID);
            Packet.WriteUInt64(Character.ResidingCity.Map);
            Packet.WritePascalString(Character.ResidingCity.IP);
            Packet.WriteInt32(Character.ResidingCity.Port);

            byte[] PacketData = Packet.ToArray();
            PlayerAccount.Client.SendEncrypted((byte)PacketType.CHARACTER_CREATE, PacketData);
        }

        /// <summary>
        /// Sends a CharacterCreate packet to a CityServer.
        /// </summary>
        /// <param name="LoginArgs">Arguments used to log onto the CityServer.</param>
        /// <param name="Character">The character to create on the CityServer.</param>
        public static void SendCharacterCreateCity(LoginArgsContainer LoginArgs, TSOClient.VM.Sim Character)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.CHARACTER_CREATE_CITY, 0);
            Packet.WriteHeader();

            byte[] EncryptionKey = LoginArgs.Enc.GetDecryptionArgsContainer().ARC4DecryptArgs.EncryptionKey;
            MemoryStream PacketData = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(PacketData);

            Writer.Write((byte)LoginArgs.Username.Length);
            Writer.Write(Encoding.ASCII.GetBytes(LoginArgs.Username), 0, Encoding.ASCII.GetBytes(LoginArgs.Username).Length);

            //LoginArgs password is set to PlayerAccount.Hash for relogging to the cityserver.
            byte[] HashBuf = Convert.FromBase64String(LoginArgs.Password);
            Writer.Write((byte)HashBuf.Length);
            Writer.Write(HashBuf, 0, HashBuf.Length);

            Writer.Write((byte)EncryptionKey.Length);
            Writer.Write(EncryptionKey);
            Writer.Write(PlayerAccount.CityToken);
            Writer.Write(Character.Timestamp);
            Writer.Write(Character.Name);
            Writer.Write(Character.Sex);
            Writer.Write(Character.Description);
            Writer.Write((ulong)Character.HeadOutfitID);
            Writer.Write((ulong)Character.BodyOutfitID);
            Writer.Write((byte)Character.AppearanceType);

            Packet.WriteUInt16((ushort)((ushort)PacketHeaders.UNENCRYPTED + PacketData.Length));
            Packet.WriteBytes(PacketData.ToArray());
            Writer.Close();

            LoginArgs.Client.Send(Packet.ToArray());
        }

        /// <summary>
        /// Requests a token from the LoginServer, that can be used to log into a CityServer.
        /// </summary>
        /// <param name="Client">A NetworkClient instance.</param>
        public static void RequestCityToken(NetworkClient Client, Sim SelectedCharacter)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.REQUEST_CITY_TOKEN, 0);
            Packet.WritePascalString(SelectedCharacter.ResidingCity.UUID);
            Packet.WritePascalString(SelectedCharacter.GUID.ToString());
            Client.SendEncrypted((byte)PacketType.REQUEST_CITY_TOKEN, Packet.ToArray());
        }

        /// <summary>
        /// Sends a token to a CityServer, as received by a LoginServer.
        /// </summary>
        /// <param name="Client">A NetworkClient instance.</param>
        public static void SendCityToken(NetworkClient Client)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.CITY_TOKEN, 0);
            Packet.WriteHeader();

            byte[] EncryptionKey = Client.ClientEncryptor.GetDecryptionArgsContainer().ARC4DecryptArgs.EncryptionKey;
            MemoryStream PacketData = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(PacketData);

            Writer.Write((byte)PlayerAccount.Hash.Length);
            Writer.Write(PlayerAccount.Hash, 0, PlayerAccount.Hash.Length);

            Writer.Write((byte)EncryptionKey.Length);
            Writer.Write(EncryptionKey);
            Writer.Write(PlayerAccount.CityToken);

            Packet.WriteUInt16((ushort)((ushort)PacketHeaders.UNENCRYPTED + PacketData.Length));
            Packet.WriteBytes(PacketData.ToArray());
            Writer.Close();

            Client.Send(Packet.ToArray());
        }
    }
}
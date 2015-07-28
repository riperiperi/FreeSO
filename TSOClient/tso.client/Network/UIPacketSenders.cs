/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
using TSOClient.Code.UI.Controls;

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
            PacketStream InitialPacket = new PacketStream((byte)PacketType.LOGIN_REQUEST, 0);
            InitialPacket.WriteHeader();

            ECDiffieHellmanCng PrivateKey = Args.Client.ClientEncryptor.GetDecryptionArgsContainer()
                .AESDecryptArgs.PrivateKey;
            //IMPORTANT: Public key must derive from the private key!
            byte[] ClientPublicKey = PrivateKey.PublicKey.ToByteArray();

            byte[] NOnce = Args.Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs.NOnce;

            InitialPacket.WriteInt32(((byte)PacketHeaders.UNENCRYPTED +
                /*4 is for version*/ 4 + (ClientPublicKey.Length + 1) + (NOnce.Length + 1)));

            SaltedHash Hash = new SaltedHash(new SHA512Managed(), Args.Username.Length);
            byte[] HashBuf = Hash.ComputePasswordHash(Args.Username, Args.Password);
            PlayerAccount.Hash = HashBuf;

            string[] Version = GlobalSettings.Default.ClientVersion.Split('.');

            InitialPacket.WriteByte((byte)int.Parse(Version[0])); //Version 1
            InitialPacket.WriteByte((byte)int.Parse(Version[1])); //Version 2
            InitialPacket.WriteByte((byte)int.Parse(Version[2])); //Version 3
            InitialPacket.WriteByte((byte)int.Parse(Version[3])); //Version 4

            InitialPacket.WriteByte((byte)ClientPublicKey.Length);
            InitialPacket.WriteBytes(ClientPublicKey);

            InitialPacket.WriteByte((byte)NOnce.Length);
            InitialPacket.WriteBytes(NOnce);

            Args.Client.Send(InitialPacket.ToArray());
        }

        public static void SendCharacterInfoRequest(string TimeStamp)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.CHARACTER_LIST, 0);
            //If this timestamp is newer than the server's timestamp, it means
            //the client doesn't have a charactercache. If it's older, it means
            //the cache needs to be updated. If it matches, the server sends an
            //empty responsepacket.
            //Packet.WriteString(TimeStamp);
            Packet.WriteString(TimeStamp);

            byte[] PacketData = Packet.ToArray();

            NetworkFacade.Client.SendEncrypted((byte)PacketType.CHARACTER_LIST, PacketData);
        }

        /// <summary>
        /// Sends a CharacterCreate packet to the LoginServer.
        /// </summary>
        /// <param name="Character">The character to create.</param>
        /// <param name="TimeStamp">The timestamp of when this character was created.</param>
        public static void SendCharacterCreate(UISim Character, string TimeStamp)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.CHARACTER_CREATE, 0);
            Packet.WriteString(NetworkFacade.Client.ClientEncryptor.Username);
            Packet.WriteString(TimeStamp);
            Packet.WriteString(Character.Name);
            Packet.WriteString(Character.Sex);
            Packet.WriteString(Character.Description);
            Packet.WriteUInt64(Character.HeadOutfitID);
            Packet.WriteUInt64(Character.BodyOutfitID);
            Packet.WriteByte((byte)Character.Avatar.Appearance);

            Packet.WriteString(Character.ResidingCity.Name);
            Packet.WriteUInt64(Character.ResidingCity.Thumbnail);
            Packet.WriteString(Character.ResidingCity.UUID);
            Packet.WriteUInt64(Character.ResidingCity.Map);
            Packet.WriteString(Character.ResidingCity.IP);
            Packet.WriteInt32(Character.ResidingCity.Port);

            byte[] PacketData = Packet.ToArray();
            NetworkFacade.Client.SendEncrypted((byte)PacketType.CHARACTER_CREATE, PacketData);
        }

        public static void SendLoginRequestCity(LoginArgsContainer Args)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.LOGIN_REQUEST_CITY, 0);
            Packet.WriteHeader();

            ECDiffieHellmanCng PrivateKey = Args.Client.ClientEncryptor.GetDecryptionArgsContainer()
                .AESDecryptArgs.PrivateKey;
            //IMPORTANT: Public key must derive from the private key!
            byte[] ClientPublicKey = PrivateKey.PublicKey.ToByteArray();

            byte[] NOnce = Args.Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs.NOnce;

            Packet.WriteInt32(((byte)PacketHeaders.UNENCRYPTED +
                (ClientPublicKey.Length + 1) + (NOnce.Length + 1)));

            Packet.WriteByte((byte)ClientPublicKey.Length);
            Packet.WriteBytes(ClientPublicKey);

            Packet.WriteByte((byte)NOnce.Length);
            Packet.WriteBytes(NOnce);

            Args.Client.Send(Packet.ToArray());
        }

        /// <summary>
        /// Sends a CharacterCreate packet to a CityServer.
        /// </summary>
        /// <param name="LoginArgs">Arguments used to log onto the CityServer.</param>
        /// <param name="Character">The character to create on the CityServer.</param>
        public static void SendCharacterCreateCity(NetworkClient Client, UISim Character)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.CHARACTER_CREATE_CITY, 0);

            MemoryStream PacketData = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(PacketData);

            Writer.Write((byte)Client.ClientEncryptor.Username.Length);
            Writer.Write(Encoding.ASCII.GetBytes(Client.ClientEncryptor.Username), 0, 
                Encoding.ASCII.GetBytes(Client.ClientEncryptor.Username).Length);

            Writer.Write(PlayerAccount.CityToken);
            Writer.Write(Character.Timestamp);
            Writer.Write(Character.Name);
            Writer.Write(Character.Sex);
            Writer.Write(Character.Description);
            Writer.Write((ulong)Character.HeadOutfitID);
            Writer.Write((ulong)Character.BodyOutfitID);
            Writer.Write((byte)Character.Avatar.Appearance);

            Packet.WriteBytes(PacketData.ToArray());
            Writer.Close();

            Client.SendEncrypted((byte)PacketType.CHARACTER_CREATE_CITY, Packet.ToArray());
        }

        /// <summary>
        /// Sends a CharacterRetirement packet to the LoginServer, retiring a specific character.
        /// </summary>
        /// <param name="Character">The character to retire.</param>
        public static void SendCharacterRetirement(UISim Character)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.RETIRE_CHARACTER, 0);
            Packet.WriteString(PlayerAccount.Username);
            Packet.WriteString(Character.GUID.ToString());
            NetworkFacade.Client.SendEncrypted((byte)PacketType.RETIRE_CHARACTER, Packet.ToArray());
        }

        /// <summary>
        /// Requests a token from the LoginServer, that can be used to log into a CityServer.
        /// </summary>
        /// <param name="Client">A NetworkClient instance.</param>
        public static void RequestCityToken(NetworkClient Client, UISim SelectedCharacter)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.REQUEST_CITY_TOKEN, 0);
            Packet.WriteString(Client.ClientEncryptor.Username);
            Packet.WriteString(SelectedCharacter.ResidingCity.UUID);
            Packet.WriteString(SelectedCharacter.GUID.ToString());
            Client.SendEncrypted((byte)PacketType.REQUEST_CITY_TOKEN, Packet.ToArray());
        }

        /// <summary>
        /// Sends a token to a CityServer, as received by a LoginServer.
        /// </summary>
        /// <param name="Client">A NetworkClient instance.</param>
        public static void SendCityToken(NetworkClient Client)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.CITY_TOKEN, 0);

            MemoryStream PacketData = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(PacketData);

            Writer.Write(PlayerAccount.CityToken);

            Packet.WriteBytes(PacketData.ToArray());
            Writer.Close();

            Client.SendEncrypted((byte)PacketType.CITY_TOKEN, Packet.ToArray());
        }

        public static void SendLetter(NetworkClient Client, string Msg, string Subject, string GUID)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.PLAYER_SENT_LETTER, 0);
            Packet.WriteString(GUID);
            Packet.WriteString(Subject);
            Packet.WriteString(Msg);
            Client.SendEncrypted((byte)PacketType.PLAYER_SENT_LETTER, Packet.ToArray());
        }

        /// <summary>
        /// Sends a request to purchase a lot.
        /// </summary>
        /// <param name="Client">NetworkClient instance connected to city server.</param>
        /// <param name="X">X-coordinate of lot.</param>
        /// <param name="Y">Y-coordinate of lot.</param>
        public static void SendLotPurchaseRequest(NetworkClient Client, short X, short Y)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.LOT_PURCHASE_REQUEST, 0);
            Packet.WriteUInt16((ushort)X);
            Packet.WriteUInt16((ushort)Y);
            Client.SendEncrypted((byte)PacketType.LOT_PURCHASE_REQUEST, Packet.ToArray());
        }

        /// <summary>
        /// Sends a request for the cost of a lot.
        /// </summary>
        /// <param name="Client">NetworkClient instance connected to city server.</param>
        /// <param name="X">X-coordinate of lot.</param>
        /// <param name="Y">Y-coordinate of lot.</param>
        public static void SendLotCostRequest(NetworkClient Client, short X, short Y)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.LOT_COST, 0);
            Packet.WriteUInt16((ushort)X);
            Packet.WriteUInt16((ushort)Y);
            Client.SendEncrypted((byte)PacketType.LOT_COST, Packet.ToArray());
        }
    }
}
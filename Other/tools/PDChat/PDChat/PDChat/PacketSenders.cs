using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using PDChat.Sims;
using GonzoNet;
using GonzoNet.Encryption;
using ProtocolAbstractionLibraryD;

namespace PDChat
{
    /// <summary>
    /// Contains functions for sending various packets.
    /// </summary>
    public class PacketSenders
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

            InitialPacket.WriteUInt16((ushort)((byte)PacketHeaders.UNENCRYPTED +
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
            Packet.WritePascalString(TimeStamp);

            byte[] PacketData = Packet.ToArray();

            NetworkFacade.Client.SendEncrypted((byte)PacketType.CHARACTER_LIST, PacketData);
        }

        /// <summary>
        /// Requests a token from the LoginServer, that can be used to log into a CityServer.
        /// </summary>
        /// <param name="Client">A NetworkClient instance.</param>
        public static void RequestCityToken(NetworkClient Client, Sim SelectedCharacter)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.REQUEST_CITY_TOKEN, 0);
            Packet.WritePascalString(Client.ClientEncryptor.Username);
            Packet.WritePascalString(SelectedCharacter.ResidingCity.UUID);
            Packet.WritePascalString(SelectedCharacter.GUID.ToString());
            Client.SendEncrypted((byte)PacketType.REQUEST_CITY_TOKEN, Packet.ToArray());
        }

        /// <summary>
        /// Sends login request to city server.
        /// </summary>
        /// <param name="Args">Login arguments.</param>
        public static void SendLoginRequestCity(LoginArgsContainer Args)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.LOGIN_REQUEST_CITY, 0);
            Packet.WriteHeader();

            ECDiffieHellmanCng PrivateKey = Args.Client.ClientEncryptor.GetDecryptionArgsContainer()
                .AESDecryptArgs.PrivateKey;
            //IMPORTANT: Public key must derive from the private key!
            byte[] ClientPublicKey = PrivateKey.PublicKey.ToByteArray();

            byte[] NOnce = Args.Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs.NOnce;

            Packet.WriteUInt16((ushort)((byte)PacketHeaders.UNENCRYPTED +
                (ClientPublicKey.Length + 1) + (NOnce.Length + 1)));

            Packet.WriteByte((byte)ClientPublicKey.Length);
            Packet.WriteBytes(ClientPublicKey);

            Packet.WriteByte((byte)NOnce.Length);
            Packet.WriteBytes(NOnce);

            Args.Client.Send(Packet.ToArray());
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

        /// <summary>
        /// Sends a message to a specific player.
        /// </summary>
        /// <param name="Client">NetworkFacade's NetworkClient's instance.</param>
        /// <param name="Msg">The message to send.</param>
        /// <param name="Subject">Subject of message.</param>
        /// <param name="GUID">GUID of player to receive message.</param>
        public static void SendLetter(NetworkClient Client, string Msg, string Subject, string GUID)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.PLAYER_SENT_LETTER, 0);
            Packet.WritePascalString(GUID);
            Packet.WritePascalString(Subject);
            Packet.WritePascalString(Msg);
            Client.SendEncrypted((byte)PacketType.PLAYER_SENT_LETTER, Packet.ToArray());
        }

        public static void BroadcastLetter(NetworkClient Client, string Msg, string Subject)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.PLAYER_BROADCAST_LETTER, 0);
            Packet.WritePascalString(Subject);
            Packet.WritePascalString(Msg);
            Client.SendEncrypted((byte)PacketType.PLAYER_BROADCAST_LETTER, Packet.ToArray());
        }
    }
}

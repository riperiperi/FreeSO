using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
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
    }
}

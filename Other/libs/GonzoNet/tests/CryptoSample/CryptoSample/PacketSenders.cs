using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using GonzoNet;

namespace CryptoSample
{
    public class PacketSenders
    {
        //First packet sent from client to server.
        public static void SendInitialConnectPacket(NetworkClient Client, string Username)
        {
            PacketStream InitialPacket = new PacketStream(0x01, 0);
            InitialPacket.WriteHeader();

            ECDiffieHellmanCng PrivateKey = Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs.PrivateKey;
            //IMPORTANT: Public key must derive from the private key!
            PacketHandlers.ClientPublicKey = PrivateKey.PublicKey.ToByteArray();

            byte[] NOnce = Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs.NOnce;

            InitialPacket.WriteUInt16((ushort)((byte)PacketHeaders.UNENCRYPTED +
                (PacketHandlers.ClientPublicKey.Length + 1) + (NOnce.Length + 1)));
            
            InitialPacket.WriteByte((byte)PacketHandlers.ClientPublicKey.Length);
            InitialPacket.WriteBytes(PacketHandlers.ClientPublicKey);

            InitialPacket.WriteByte((byte)NOnce.Length);
            InitialPacket.WriteBytes(NOnce);

            Client.Send(InitialPacket.ToArray());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using TSOClient.Network.Encryption;
using TSOClient.VM;

namespace TSOClient.Network
{
    class LotPacketSenders
    {
        /// <summary>
        /// Sends a packet to create a SimulationObject on ter server.
        /// Assumes the player is on a lot that he owns.
        /// </summary>
        /// <param name="CreatedObject">The SimulationObject to create.</param>
        public static void SendCreatedSimulationObject(SimulationObject CreatedObject)
        {
            //TODO: Change this ID!
            PacketStream CreateSimulationObjectPacket = new PacketStream(0x11, 0);

            BinaryFormatter BinFormatter = new BinaryFormatter();
            BinFormatter.Serialize(CreateSimulationObjectPacket, CreatedObject);

            PlayerAccount.Client.Send(FinalizePacket(0x11, new DESCryptoServiceProvider(), 
                CreateSimulationObjectPacket.ToArray()));
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

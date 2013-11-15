using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace GonzoNet.Encryption
{
    public class ARC4Encryptor : Encryptor
    {
        public DESCryptoServiceProvider CryptoService = new DESCryptoServiceProvider();
        public byte[] EncryptionKey;

        public ARC4Encryptor(string Password)
            : base(Password)
        {
            PasswordDeriveBytes Pwd = new PasswordDeriveBytes(Encoding.ASCII.GetBytes(m_Password), 
                Encoding.ASCII.GetBytes("SALT"), "SHA1", 10);
            EncryptionKey = Pwd.GetBytes(8);
        }

        public override byte[] FinalizePacket(byte PacketID, byte[] PacketData)
        {
            MemoryStream FinalizedPacket = new MemoryStream();
            BinaryWriter PacketWriter = new BinaryWriter(FinalizedPacket);

            PasswordDeriveBytes Pwd = new PasswordDeriveBytes(Encoding.ASCII.GetBytes(m_Password),
                Encoding.ASCII.GetBytes("SALT"), "SHA1", 10);

            MemoryStream TempStream = new MemoryStream();
            CryptoStream EncryptedStream = new CryptoStream(TempStream,
                CryptoService.CreateEncryptor(EncryptionKey, Encoding.ASCII.GetBytes("@1B2c3D4e5F6g7H8")),
                CryptoStreamMode.Write);
            EncryptedStream.Write(PacketData, 0, PacketData.Length);
            EncryptedStream.FlushFinalBlock();

            PacketWriter.Write(PacketID);
            //The length of the encrypted data can be longer or smaller than the original length,
            //so write the length of the encrypted data.
            PacketWriter.Write((ushort)((long)PacketHeaders.ENCRYPTED + TempStream.Length));
            //Also write the length of the unencrypted data.
            PacketWriter.Write((ushort)PacketData.Length);
            PacketWriter.Flush();

            PacketWriter.Write(TempStream.ToArray());
            PacketWriter.Flush();

            byte[] ReturnPacket = FinalizedPacket.ToArray();

            PacketWriter.Close();

            return ReturnPacket;
        } 
    }
}

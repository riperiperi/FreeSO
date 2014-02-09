using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using TSOClient.Events;
using TSOClient.Network.Events;

namespace TSOClient.Network
{
    /// <summary>
    /// A packet that has been decrypted and processed, ready to read from.
    /// </summary>
    public class ProcessedPacket : PacketStream
    {
        public ushort DecryptedLength;

        public ProcessedPacket(byte ID, bool Encrypted, int Length, byte[] DataBuffer)
            : base(ID, Length, DataBuffer)
        {
            byte Opcode = (byte)this.ReadByte();
            this.m_Length = (ushort)this.ReadUShort();

            if (Encrypted)
            {
                this.DecryptedLength = (ushort)this.ReadUShort();

                if (this.DecryptedLength != this.m_Length)
                {
                    //Something's gone haywire, throw an error...
                    EventSink.RegisterEvent(new PacketError(EventCodes.PACKET_PROCESSING_ERROR));
                }
            }

            if(Encrypted)
                this.DecryptPacket(PlayerAccount.EncKey, new DESCryptoServiceProvider(), this.DecryptedLength);
        }
    }
}

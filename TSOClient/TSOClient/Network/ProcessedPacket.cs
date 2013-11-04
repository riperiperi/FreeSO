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
            ushort TotalLength = (ushort)this.ReadUShort();
            
            if(Encrypted)
                DecryptedLength = (ushort)this.ReadUShort();

            if (TotalLength != Length)
            {
                //Something's gone haywire, throw an error...
                EventSink.RegisterEvent(new PacketError(EventCodes.PACKET_PROCESSING_ERROR));

                if (Encrypted)
                {
                    if (DecryptedLength != TotalLength)
                    {
                        //Something's gone haywire, throw an error...
                        EventSink.RegisterEvent(new PacketError(EventCodes.PACKET_PROCESSING_ERROR));
                    }
                }
            }

            if(Encrypted)
                this.DecryptPacket(PlayerAccount.EncKey, new DESCryptoServiceProvider(), DecryptedLength);
        }
    }
}

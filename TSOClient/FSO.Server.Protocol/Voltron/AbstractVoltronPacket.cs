using FSO.Server.Protocol.Voltron.Model;
using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Voltron
{
    public abstract class AbstractVoltronPacket : IVoltronPacket
    {
        public static Sender GetSender(IoBuffer buffer)
        {
            var ariesID = GetPascalString(buffer);
            var masterID = GetPascalString(buffer);
            return new Sender { AriesID = ariesID, MasterAccountID = masterID };
        }

        public static String GetPascalString(IoBuffer buffer)
        {
            byte len1 = buffer.Get();
            byte len2 = buffer.Get();
            byte len3 = buffer.Get();
            byte len4 = buffer.Get();
            len1 &= 0x7F;

            long len = len1 << 24 | len2 << 16 | len3 << 8 | len4;
            if (len > 0)
            {
                StringBuilder str = new StringBuilder();
                for (int i = 0; i < len; i++)
                {
                    str.Append((char)buffer.Get());
                }
                return str.ToString();

            }
            else
            {
                return "";
            }
        }

        public static void PutPascalString(IoBuffer buffer, String value)
        {

            long strlen = 0;
            if (value != null)
            {
                strlen = value.Length;
            }

            byte len1 = (byte)((strlen >> 24) | 0x80);
            byte len2 = (byte)((strlen >> 16) & 0xFF);
            byte len3 = (byte)((strlen >> 8) & 0xFF);
            byte len4 = (byte)(strlen & 0xFF);

            buffer.Put(len1);
            buffer.Put(len2);
            buffer.Put(len3);
            buffer.Put(len4);

            if (strlen > 0)
            {
                foreach (char ch in value.ToCharArray())
                {
                    buffer.Put((byte)ch);
                }
            }
        }

        public static IoBuffer Allocate(int size)
        {
            IoBuffer buffer = IoBuffer.Allocate(size);
            buffer.Order = ByteOrder.BigEndian;
            return buffer;
        }

        public abstract VoltronPacketType GetPacketType();
        public abstract IoBuffer Serialize();
        public abstract void Deserialize(IoBuffer input);
    }
}

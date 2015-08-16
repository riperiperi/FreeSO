using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Utils
{
    public static class IoBufferUtils
    {
        public static void PutUInt32(this IoBuffer buffer, uint value)
        {
            int converted = unchecked((int)value);
            buffer.PutInt32(converted);
        }

        public static uint GetUInt32(this IoBuffer buffer)
        {
            return (uint)buffer.GetInt32();
        }

        public static void PutUInt16(this IoBuffer buffer, ushort value)
        {
            buffer.PutInt16((short)value);
        }

        public static void PutUInt64(this IoBuffer buffer, ulong value)
        {
            buffer.PutInt64((long)value);
        }

        public static void PutUTF8(this IoBuffer buffer, string value)
        {
            if (value == null)
            {
                buffer.PutInt16(-1);
            }
            else
            {
                buffer.PutInt16((short)value.Length);
                buffer.PutString(value, Encoding.UTF8);
            }
        }

        public static string GetUTF8(this IoBuffer buffer)
        {
            short len = buffer.GetInt16();
            if (len == -1)
            {
                return null;
            }
            return buffer.GetString(len, Encoding.UTF8);
        }

        public static ushort GetUInt16(this IoBuffer buffer)
        {
            return (ushort)buffer.GetInt16();
        }

        public static ulong GetUInt64(this IoBuffer buffer)
        {
            return (ulong)buffer.GetInt64();
        }
    }
}

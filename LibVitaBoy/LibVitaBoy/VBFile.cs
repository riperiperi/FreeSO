using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LibVitaBoy
{
    public class VBFile : IDisposable
    {
        private Stream Stream;
        private BinaryReader Reader;

        public VBFile(Stream stream)
        {
            this.Stream = stream;
            this.Reader = new BinaryReader(stream);
        }


        public short ReadInt16()
        {
            return Endian.SwapInt16(Reader.ReadInt16());
        }

        public int ReadInt32()
        {
            return Endian.SwapInt32(Reader.ReadInt32());
        }

        public byte ReadByte()
        {
            return Reader.ReadByte();
        }

        public string ReadPascalString()
        {
            var length = ReadByte();
            return Encoding.ASCII.GetString(Reader.ReadBytes(length));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public virtual unsafe float ReadFloat()
        {
            var m_buffer = Reader.ReadBytes(4);
            uint tmpBuffer = (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);

            var result = *((float*)&tmpBuffer);
            return result;
        }

        //public float ReadFloat()
        //{
        //    return Reader.ReadSingle();
        //    //(uint32_t)((Position[0]<<(8*0)) | (Position[1]<<(8*1)) | (Position[2]<<(8*2)) | (Position[3]<<(8*3)));
        //    //UInt32 num = (uint)(Reader.ReadByte() | (Reader.ReadByte() << 8) | (Reader.ReadByte() << 16) | (Reader.ReadByte() << 24));


        //    //var sign = num >> 31 == 0 ? 1 : -1;
        //    //var exponent = (num >> 23) & 0xFF;
        //    //var mantissa = exponent == 0 ? (num & 0x7FFFFF) << 1 :
        //    //                                (num & 0x7FFFFF) | 0x800000;

        //    //return (float) (sign * mantissa * Math.Pow(2, exponent-150));
            
        //    //return ReadSingleLittleEndian(bytes, 0);
        //    //return Reader.ReadSingle();
        //}


        #region IDisposable Members

        public void Dispose()
        {
            Reader.Close();
        }

        #endregion
    }
}

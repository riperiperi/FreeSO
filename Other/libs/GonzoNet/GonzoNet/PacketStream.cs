/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the GonzoNet.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using GonzoNet.Encryption;
using GonzoNet.Exceptions;

namespace GonzoNet
{
    public class PacketStream : Stream
    {
        //The ID of this PacketStream (identifies a packet).
        private byte m_ID;
        //The intended length of this PacketStream. Might not correspond with the
        //length of m_BaseStream!
        protected ushort m_Length;
        public bool m_VariableLength;

        protected MemoryStream m_BaseStream;
        private bool m_SupportsPeek = false;
        private byte[] m_PeekBuffer;
        protected BinaryReader m_Reader;
        private BinaryWriter m_Writer;
        private long m_Position;

        public PacketStream(byte ID, ushort Length, byte[] DataBuffer)
            : base()
        {
            m_ID = ID;
            m_Length = Length;

            m_BaseStream = new MemoryStream(DataBuffer);
            m_BaseStream.Position = 0;

            m_SupportsPeek = true;
            m_PeekBuffer = new byte[DataBuffer.Length];
            DataBuffer.CopyTo(m_PeekBuffer, 0);
            
            m_Reader = new BinaryReader(m_BaseStream);

            m_Position = (DataBuffer.Length - 1);
        }

        public PacketStream(byte ID, ushort Length)
        {
            m_ID = ID;
            m_Length = Length;

            m_SupportsPeek = false;

            m_BaseStream = new MemoryStream();
            m_Writer = new BinaryWriter(m_BaseStream);
            m_Position = 0;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public bool CanPeek
        {
            get { return m_SupportsPeek; }
        }

        public byte PacketID
        {
            get { return m_ID; }
        }

        /// <summary>
        /// The current position of this PacketStream.
        /// </summary>
        public override long Position
        {
            get
            {
                return m_Position;
            }
            set
            {
                //TODO: Checks here?
                m_Position = value;
            }
        }

        /// <summary>
        /// The target length of this PacketStream.
        /// To get the actual current length, use the BufferLength property.
        /// </summary>
        public override long Length
        {
            get { return m_Length; }
        }

        /// <summary>
        /// The current length of this PacketStream.
        /// </summary>
        public long BufferLength
        {
            get { return m_BaseStream.Length; }
        }

        /// <summary>
        /// Sets the length of this PacketStream to the specified value.
        /// </summary>
        /// <param name="value">The length of the stream.</param>
        public override void SetLength(long value)
        {
            byte[] Tmp = m_BaseStream.ToArray();
            //No idea if these two lines actually work, but they should...
            m_BaseStream = new MemoryStream((int)value);
            m_BaseStream.Write(Tmp, 0, Tmp.Length);
        }

        /// <summary>
        /// Do not call this! It will throw a NotImplementedException!
        /// </summary>
        /// <param name="offset">The offset to seek to.</param>
        /// <param name="origin">The origin of the seek.</param>
        /// <returns>The offset that was seeked to.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Flushes the underlying stream.
        /// </summary>
        public override void Flush()
        {
            m_BaseStream.Flush();
        }

        public byte[] ToArray()
        {
            var bytes = m_BaseStream.ToArray();
            if (m_VariableLength)
            {
                var packetLength = (ushort)m_Position;
                bytes[2] = (byte)(packetLength & 0xFF);
                bytes[3] = (byte)(packetLength >> 8);
            }
            return bytes;
        }

        #region Reading

        /// <summary>
        /// Reads a specific number of bytes from this PacketStream.
        /// </summary>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="offset">The offset from which to start reading.</param>
        /// <param name="count">The number of bytes to read (must be at least equal to the length of buffer!)</param>
        /// <returns>The number of bytes that were read.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int Read = m_BaseStream.Read(buffer, offset, count);
            m_Position -= Read;

            return Read;
        }

        /// <summary>
        /// Peeks a byte from the stream at the current position.
        /// </summary>
        /// <returns>The byte that was peeked.</returns>
        public byte PeekByte()
        {
            if (m_SupportsPeek)
                return m_PeekBuffer[m_Position];
            else
                throw new PeekNotSupportedException("Tried peeking from a PacketStream instance that didn't support it!");
        }

        /// <summary>
        /// Peeks a byte from the stream at the specified position.
        /// </summary>
        /// <param name="Position">The position to peek at.</param>
        /// <returns>The byte that was peeked.</returns>
        public byte PeekByte(int Position)
        {
            if (m_SupportsPeek)
                return m_PeekBuffer[Position];
            else
                throw new PeekNotSupportedException("Tried peeking from a PacketStream instance that didn't support it!");
        }

        /// <summary>
        /// Peeks a ushort from the stream at the specified position.
        /// </summary>
        /// <param name="Position">The position to peek at.</param>
        /// <returns>The ushort that was peeked.</returns>
        public ushort PeekUShort(int Position)
        {
            MemoryStream MemStream = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(MemStream);

            Writer.Write((byte)PeekByte(Position));
            Writer.Write((byte)PeekByte(Position + 1));
            Writer.Flush();

            return BitConverter.ToUInt16(MemStream.ToArray(), 0);
        }

        public override int ReadByte()
        {
            m_Position -= 1;
            return m_BaseStream.ReadByte();
        }

        public ushort ReadUShort()
        {
            m_Position -= 2;

            return ReadUInt16();
        }

        public string ReadString()
        {
            string ReturnStr = m_Reader.ReadString();
            m_Position -= ReturnStr.Length;

            return ReturnStr;
        }

        public string ReadString(int NumChars)
        {
            string ReturnStr = "";

            for (int i = 0; i <= NumChars; i++)
                ReturnStr = ReturnStr + m_Reader.ReadChar();

            m_Position -= NumChars;

            return ReturnStr;
        }

        /// <summary>
        /// Reads a pascal string from the stream.
        /// A pascal string is a string prepended with the length, as one byte.
        /// This MIGHT be the same as ReadString(), but hasn't been tested.
        /// </summary>
        /// <returns>The string read from the stream.</returns>
        public string ReadPascalString()
        {
            byte Length = m_Reader.ReadByte();

            /*for (int i = 0; i < Length; i++)
                ReturnStr += m_Reader.ReadChar();*/
            byte[] UTF8Buf = new byte[Length];
            m_Reader.Read(UTF8Buf, 0, Length);

            m_Position -= Length;

            return Encoding.UTF8.GetString(UTF8Buf);
        }

        public int ReadInt32()
        {
            m_Position -= 4;
            return m_Reader.ReadInt32();
        }

        public long ReadInt64()
        {
            m_Position -= 8;
            return m_Reader.ReadInt64();
        }

        public ushort ReadUInt16()
        {
            m_Position -= 2;
            return m_Reader.ReadUInt16();
        }

        public ulong ReadUInt64()
        {
            m_Position -= 8;
            return m_Reader.ReadUInt64();
        }

        #endregion

        #region Writing

        /// <summary>
        /// Writes a block of bytes to the current buffer using data read from the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            m_BaseStream.Write(buffer, offset, count);
            m_Position += count;
            m_Writer.Flush();
        }

        public void WriteBytes(byte[] Buffer)
        {
            m_BaseStream.Write(Buffer, 0, Buffer.Length);
            m_Position += Buffer.Length;
            m_Writer.Flush();
        }

        public override void WriteByte(byte Value)
        {
            m_Writer.Write(Value);
            m_Position += 1;
            m_Writer.Flush();
        }

        public void WriteInt32(int Value)
        {
            m_Writer.Write(Value);
            m_Position += 4;
            m_Writer.Flush();
        }

        public void WriteUInt16(ushort Value)
        {
            m_Writer.Write(Value);
            m_Position += 2;
            m_Writer.Flush();
        }

        public void WriteInt64(long Value)
        {
            m_Writer.Write(Value);
            m_Position += 8;
            m_Writer.Flush();
        }

        public void WriteUInt64(ulong Value)
        {
            m_Writer.Write(Value);
            m_Position += 8;
            m_Writer.Flush();
        }

        public void WritePascalString(string str)
        {
            WriteByte((byte)str.Length);
            WriteBytes(Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// Writes the packet header
        /// </summary>
        public void WriteHeader()
        {
            WriteByte(this.m_ID);
            m_Position += 1;
        }

        #endregion
    }
}

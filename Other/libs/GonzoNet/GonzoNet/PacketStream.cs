/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO CityServer.

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
	/// <summary>
	/// A readable and writable packet.
	/// </summary>
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

		/// <summary>
		/// Constructs a new PacketStream instance to read from.
		/// </summary>
		/// <param name="ID">The ID of this PacketStream instance.</param>
		/// <param name="Length">The length of this PacketStream instance.</param>
		/// <param name="DataBuffer">The buffer from which to create this PacketStream instance.</param>
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

		/// <summary>
		/// Constructs a new PacketStream instance to write to.
		/// </summary>
		/// <param name="ID">The ID of this PacketStream instance.</param>
		/// <param name="Length">The length of this PacketStream instance (0 if variable length).</param>
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

		/// <summary>
		/// The ID of this PacketStream instance.
		/// </summary>
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

		/// <summary>
		/// Creates an array of bytes with the data in this PacketStream instance.
		/// </summary>
		/// <returns>An array of bytes.</returns>
		public byte[] ToArray()
		{
			byte[] bytes;

			lock (m_BaseStream)
				bytes = m_BaseStream.ToArray();

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
		/// <param name="offset">The zero-based byte offset in buffer at which to begin storing data from the current stream.</param>
		/// <param name="count">The number of bytes to read (must be at least equal to the length of buffer!)</param>
		/// <returns>The number of bytes that were read.</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			int Read = m_BaseStream.Read(buffer, offset, count);
			m_Position -= Read;

			return Read;
		}

		/// <summary>
		/// Reads a specific number of bytes from this PacketStream.
		/// </summary>
		/// <param name="NumBytes">Number of bytes to read.</param>
		/// <returns>The byte array that was read.</returns>
		public byte[] ReadBytes(int NumBytes)
		{
			byte[] Buf = new byte[NumBytes];
			Read(Buf, 0, NumBytes);

			return Buf;
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

		/// <summary>
		/// Reads a byte from this PacketStream instance.
		/// </summary>
		/// <returns>A byte cast to an int.</returns>
		public override int ReadByte()
		{
			m_Position -= 1;
			return m_BaseStream.ReadByte();
		}

		/// <summary>
		/// Reads a double from this PacketStream instance.
		/// </summary>
		/// <returns>A double.</returns>
		public double ReadDouble()
		{
			m_Position -= 8;
			return m_Reader.ReadDouble();
		}

		/// <summary>
		/// Reads a ushort from this PacketStream instance.
		/// </summary>
		/// <returns>A ushort.</returns>
		public ushort ReadUShort()
		{
			m_Position -= 2;

			return ReadUInt16();
		}

		/// <summary>
		/// Reads a string from this PacketStream instance.
		/// </summary>
		/// <returns>A string is prefixed with the length,
		/// encoded as an integer seven bits at a time.</returns>
		public string ReadString()
		{
			string ReturnStr;

			try
			{
				ReturnStr = m_Reader.ReadString();
				m_Position -= ReturnStr.Length;
			}
			catch (EndOfStreamException)
			{
				return string.Empty;
			}

			return ReturnStr;
		}

		/// <summary>
		/// Reads a string from this PacketStream instance.
		/// </summary>
		/// <returns></returns>
		public string ReadString(int NumChars)
		{
			byte[] UTF8Buf = new byte[NumChars];
			m_Reader.Read(UTF8Buf, 0, NumChars);

			m_Position -= NumChars;

			return Encoding.UTF8.GetString(UTF8Buf);
		}

		/// <summary>
		/// Reads an integer from this PacketStream instance.
		/// </summary>
		/// <returns>A 32 bit integer.</returns>
		public int ReadInt32()
		{
			m_Position -= 4;
			return m_Reader.ReadInt32();
		}

		/// <summary>
		/// Reads a long integer from this PacketStream instance.
		/// </summary>
		/// <returns>A 64 bit integer.</returns>
		public long ReadInt64()
		{
			m_Position -= 8;
			return m_Reader.ReadInt64();
		}

		/// <summary>
		/// Reads a short from this PacketStream instance.
		/// </summary>
		/// <returns>A 16 bit integer.</returns>
		public ushort ReadUInt16()
		{
			m_Position -= 2;
			return m_Reader.ReadUInt16();
		}

		/// <summary>
		/// Reads an unsigned long integer from this PacketStream instance.
		/// </summary>
		/// <returns>A 64 bit unsigned integer.</returns>
		public ulong ReadUInt64()
		{
			m_Position -= 8;
			return m_Reader.ReadUInt64();
		}

		#endregion

		#region Writing

		/// <summary>
		/// Writes a block of bytes to this PacketStream instance.
		/// </summary>
		/// <param name="buffer">The data to write.</param>
		/// <param name="offset">The offset at which to start writing.</param>
		/// <param name="count">The maximum number of bytes to write.</param>
		public override void Write(byte[] buffer, int offset, int count)
		{
			lock (m_BaseStream)
			{
				m_BaseStream.Write(buffer, offset, count);
				m_Position += count;
				m_BaseStream.Flush();
			}
		}

		/// <summary>
		/// Writes a 64 bit double to this PacketStream instance.
		/// </summary>
		/// <param name="Value">The 64 bit double to write.</param>
		public void WriteDouble(double Value)
		{
			lock (m_Writer)
			{
				m_Writer.Write(Value);
				m_Position += 8;
				m_Writer.Flush();
			}
		}

		/// <summary>
		/// Writes a block of bytes to this PacketStream instance.
		/// </summary>
		/// <param name="Buffer">The data to write.</param>
		public void WriteBytes(byte[] Buffer)
		{
			lock (m_BaseStream)
			{
				m_BaseStream.Write(Buffer, 0, Buffer.Length);
				m_Position += Buffer.Length;
				m_BaseStream.Flush();
			}
		}

		/// <summary>
		/// Writes a byte to this PacketStream instance.
		/// </summary>
		/// <param name="Value">The byte to write.</param>
		public override void WriteByte(byte Value)
		{
			lock (m_Writer)
			{
				try
				{
					m_Writer.Write(Value);
					m_Position += 1;
					m_Writer.Flush();
				}
				catch (IOException)
				{
					//Try again...
					m_Writer.Write(Value);
					m_Position += 1;
					m_Writer.Flush();
				}
			}
		}

		/// <summary>
		/// Writes a 32 bit integer to this PacketStream instance.
		/// </summary>
		/// <param name="Value">The 32 bit integer to write.</param>
		public void WriteInt32(int Value)
		{
			lock (m_Writer)
			{
				try
				{
					m_Writer.Write(Value);
					m_Position += 4;
					m_Writer.Flush();
				}
				catch (IOException)
				{
					//Try again...
					m_Writer.Write(Value);
					m_Position += 4;
					m_Writer.Flush();
				}
			}
		}

		/// <summary>
		/// Writes an unsigned short to this PacketStream instance.
		/// </summary>
		/// <param name="Value">The unsigned short to write.</param>
		public void WriteUInt16(ushort Value)
		{
			lock (m_Writer)
			{
				try
				{
					m_Writer.Write(Value);
					m_Position += 2;
					m_Writer.Flush();
				}
				catch (IOException)
				{
					m_Writer.Write(Value);
					m_Position += 2;
					m_Writer.Flush();
				}
			}
		}

		/// <summary>
		/// Writes a 64 bit integer to this PacketStream instance.
		/// </summary>
		/// <param name="Value">The 64 bit integer to write.</param>
		public void WriteInt64(long Value)
		{
			lock (m_Writer)
			{
				try
				{
					m_Writer.Write(Value);
					m_Position += 8;
					m_Writer.Flush();
				}
				catch (IOException)
				{
					m_Writer.Write(Value);
					m_Position += 8;
					m_Writer.Flush();
				}
			}
		}

		/// <summary>
		/// Writes an unsigned 64 bit integer to this PacketStream instance.
		/// </summary>
		/// <param name="Value">The unsigned 64 bit integer to write.</param>
		public void WriteUInt64(ulong Value)
		{
			lock (m_Writer)
			{
				try
				{
					m_Writer.Write(Value);
					m_Position += 8;
					m_Writer.Flush();
				}
				catch (IOException)
				{
					m_Writer.Write(Value);
					m_Position += 8;
					m_Writer.Flush();
				}
			}
		}

		public void WriteString(string Str)
		{
			lock (m_Writer)
			{
				try
				{
					m_Writer.Write((string)Str);
					m_Position += Str.Length + 1;
					m_Writer.Flush();
				}
				catch (IOException)
				{
					m_Writer.Write((string)Str);
					m_Position += Str.Length + 1;
					m_Writer.Flush();
				}
			}
		}

		/// <summary>
		/// Writes the packet header.
		/// </summary>
		public void WriteHeader()
		{
			lock (m_Writer)
			{
				WriteByte(this.m_ID);
				m_Position += 1;
			}
		}

		#endregion
	}
}
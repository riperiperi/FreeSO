/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections.Concurrent;
using TSO.Files.FAR3;

namespace TSO.Files
{
	public struct TuningEntry
	{
		public string EntryName;
		public uint KeyValueCount;
		public ConcurrentDictionary<string, string> KeyValues;
	}

	/// <summary>
	/// Loader/parser for tuning.dat.
	/// </summary>
	public class Tuning
	{
		private BinaryReader m_Reader;
		private byte[] m_DecompressedData;

		public uint EntryCount = 0;
		public BlockingCollection<TuningEntry> Entries = new BlockingCollection<TuningEntry>();

		public Tuning(byte[] Data)
		{
			m_Reader = new BinaryReader(new MemoryStream(Data));

			byte BodyType = m_Reader.ReadByte();

			if(BodyType != 0x01 && BodyType != 0x03)
				throw (new Exception("Tuning.cs: Unknown Persist BodyType!"));
			
			uint DecompressedSize = m_Reader.ReadUInt32();
			uint CompressedSize = m_Reader.ReadUInt32();

			//This is ALSO decompressed size...
			uint StreamBodySize = m_Reader.ReadUInt32();
			//Note: wiki.niotso.org says this is actually one byte Compressor and four bytes CompressionParameters.
			ushort CompressionID = m_Reader.ReadUInt16();

			if (CompressionID != 0xFB10)
				throw (new Exception("Tuning.cs: Unknown CompressionID!"));

			byte[] Dummy = m_Reader.ReadBytes(3);
			//Why are there 11 bytes of decompressed size at the start of a COMPRESSION format? #wtfmaxis
			uint DecompressedSize2 = (uint)((Dummy[0] << 0x10) | (Dummy[1] << 0x08) | +Dummy[2]);

			Decompresser Dec = new Decompresser();
			Dec.CompressedSize = CompressedSize;
			Dec.DecompressedSize = DecompressedSize;

			m_DecompressedData = Dec.Decompress(m_Reader.ReadBytes((int)CompressedSize));

			if (m_DecompressedData == null)
				throw (new Exception("Tuning.cs: Decompression failed!"));

			m_Reader = new BinaryReader(new MemoryStream(m_DecompressedData));

			EntryCount = m_Reader.ReadUInt32();

			for(int i = 0; i < EntryCount; i++)
			{
				TuningEntry Entry = new TuningEntry();
				Entry.EntryName = DecodeString(m_Reader.ReadString());
				Entry.KeyValueCount = m_Reader.ReadUInt32();
				Entry.KeyValues = new ConcurrentDictionary<string, string>();

				for (int j = 0; j < Entry.KeyValueCount; j++)
				{
					string Key = DecodeString(m_Reader.ReadString());
					string Val = DecodeString(m_Reader.ReadString());

					Entry.KeyValues.AddOrUpdate(Key, Val, null);
				}
			}
		}

		public Tuning(string Path)
		{
			try
			{
				m_Reader = new BinaryReader(File.OpenRead(Path));
			}
			catch(Exception)
			{
				throw new Exception("Tuning.cs: Invalid path!");
			}

			byte BodyType = m_Reader.ReadByte();

			if (BodyType != 0x01 && BodyType != 0x03)
				throw (new Exception("Tuning.cs: Unknown Persist BodyType!"));

			uint DecompressedSize = m_Reader.ReadUInt32();
			uint CompressedSize = m_Reader.ReadUInt32();

			//This is ALSO decompressed size...
			uint StreamBodySize = m_Reader.ReadUInt32();
			//Note: wiki.niotso.org says this is actually one byte Compressor and four bytes CompressionParameters.
			ushort CompressionID = m_Reader.ReadUInt16();

			if (CompressionID != 0xFB10)
				throw (new Exception("Tuning.cs: Unknown CompressionID!"));

			byte[] Dummy = m_Reader.ReadBytes(3);
			//Why are there 11 bytes of decompressed size at the start of a COMPRESSION format? #wtfmaxis
			uint DecompressedSize2 = (uint)((Dummy[0] << 0x10) | (Dummy[1] << 0x08) | +Dummy[2]);

			Decompresser Dec = new Decompresser();
			Dec.CompressedSize = CompressedSize;
			Dec.DecompressedSize = DecompressedSize;

			m_DecompressedData = Dec.Decompress(m_Reader.ReadBytes((int)CompressedSize));

			if (m_DecompressedData == null)
				throw (new Exception("Tuning.cs: Decompression failed!"));

			m_Reader = new BinaryReader(new MemoryStream(m_DecompressedData));

			EntryCount = m_Reader.ReadUInt32();

			for (int i = 0; i < EntryCount; i++)
			{
				TuningEntry Entry = new TuningEntry();
				Entry.EntryName = DecodeString(m_Reader.ReadString());
				Entry.KeyValueCount = m_Reader.ReadUInt32();
				Entry.KeyValues = new ConcurrentDictionary<string, string>();

				for (int j = 0; j < Entry.KeyValueCount; j++)
				{
					string Key = DecodeString(m_Reader.ReadString());
					string Val = DecodeString(m_Reader.ReadString());

					Entry.KeyValues.AddOrUpdate(Key, Val, null);
				}
			}
		}

		/// <summary>
		/// Retarded encoding. :P
		/// </summary>
		/// <returns>A decoded string.</returns>
		private string DecodeString(string EncodedString)
		{
			byte[] Chars = Encoding.ASCII.GetBytes(EncodedString);

			for(int i = 0; i < Chars.Length; i++)
				Chars[i] = (byte)(Chars[i] - 13);

			return Encoding.ASCII.GetString(Chars);
		}
	}
}

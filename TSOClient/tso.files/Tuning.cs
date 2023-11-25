using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FSO.Files.FAR3;
using System.Globalization;

namespace FSO.Files
{
    public struct TuningEntry
    {
        public string EntryName;
        public uint KeyValueCount;
        public Dictionary<string, string> KeyValues;

        public override string ToString()
        {
            return EntryName;
        }

        public float GetNum(string key)
        {
            CultureInfo floatParse = CultureInfo.InvariantCulture;
            return float.Parse(KeyValues[key], floatParse);
        }

		public static TuningEntry DEFAULT = new TuningEntry
		{
			EntryName = "",
			KeyValueCount = 0,
			KeyValues = new Dictionary<string, string>()
		};
    }

    //NOTE: important tuning variables:
    //28: object wear repair - important because this stuff doen't run in simantics anymore
    //36: sim motive decay
    //24: object deprecation
    //21: "motivesTunning": regen motives when not online, dead makes motives drain faster...
    //17: motive score weights for different lot types

	/// <summary>
	/// Loader/parser for tuning.dat.
	/// </summary>
	public class Tuning
	{
        private BinaryReader m_Reader;

		public uint EntryCount = 0;
		public Dictionary<string, TuningEntry> EntriesByName = new Dictionary<string, TuningEntry>();

		public Tuning(byte[] Data)
		{
			m_Reader = new BinaryReader(new MemoryStream(Data));

			Create(m_Reader);
		}

		public Tuning(string Path)
		{
			try
			{
				m_Reader = new BinaryReader(File.OpenRead(Path));
			}
			catch(Exception)
			{
				throw new Exception("Tuning.cs: Invalid path: "+Path);
			}

			Create(m_Reader);
		}

		private void Create(BinaryReader Reader)
		{
			byte BodyType = m_Reader.ReadByte();

			if (BodyType != 0x01 && BodyType != 0x03)
				throw (new Exception("Tuning.cs: Unknown Persist BodyType!"));

			uint DecompressedSize = m_Reader.ReadUInt32();
			uint CompressedSize = m_Reader.ReadUInt32();
			uint StreamBodySize = m_Reader.ReadUInt32(); //same as compressed size for all current examples
			//Note: wiki.niotso.org says this is actually one byte Compressor and four bytes CompressionParameters.
			ushort CompressionID = m_Reader.ReadUInt16();

			if (CompressionID != 0xFB10)
				throw (new Exception("Tuning.cs: Unknown CompressionID!"));

			byte[] Dummy = m_Reader.ReadBytes(3);
			uint DecompressedSize2 = (uint)((Dummy[0] << 0x10) | (Dummy[1] << 0x08) | +Dummy[2]);

			Decompresser Dec = new Decompresser();
			Dec.CompressedSize = CompressedSize;
			Dec.DecompressedSize = DecompressedSize;

			var decompressedData = Dec.Decompress(m_Reader.ReadBytes((int)CompressedSize));

			if (decompressedData == null)
				throw (new Exception("Tuning.cs: Decompression failed!"));

			m_Reader = new BinaryReader(new MemoryStream(decompressedData));

			EntryCount = m_Reader.ReadUInt32();

			for (int i = 0; i < EntryCount; i++)
			{
				TuningEntry Entry = new TuningEntry();
				Entry.EntryName = DecodeString(m_Reader);
				Entry.KeyValueCount = m_Reader.ReadUInt32();
				Entry.KeyValues = new Dictionary<string, string>();

				for (int j = 0; j < Entry.KeyValueCount; j++)
				{
					string Key = DecodeString(m_Reader);
					string Val = DecodeString(m_Reader);

                    Entry.KeyValues.Add(Key, Val);
				}
                EntriesByName.Add(Entry.EntryName.ToLowerInvariant(), Entry);
			}
		}

		/// <summary>
		/// Reads a variable-length pascal string offset by 13 characters.
		/// </summary>
		/// <returns>A decoded string.</returns>
		private string DecodeString(BinaryReader reader)
		{
            int length = 0;
            byte last = reader.ReadByte();
            byte j = 0;
            while ((last & 0x80) > 0)
            {
                length |= (last&0x7F) << ((j++) * 7);
                last = reader.ReadByte();
            }
            length |= last << ((j++) * 7);

            byte[] Chars = reader.ReadBytes(length);

			for(int i = 0; i < Chars.Length; i++)
				Chars[i] = (byte)(Chars[i] - 13);

			return Encoding.ASCII.GetString(Chars);
		}
	}
}

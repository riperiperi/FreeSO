using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FSO.Files.FAR3
{
    /// <summary>
    /// Represents a single FAR3 archive.
    /// </summary>
    public class FAR3Archive : IDisposable
    {
        private BinaryReader m_Reader;
        public static bool isReadingSomething = false;

        private string m_ArchivePath;
        private Dictionary<string, Far3Entry> m_Entries = new Dictionary<string, Far3Entry>();
        private List<Far3Entry> m_EntriesList = new List<Far3Entry>();
        private Dictionary<uint, Far3Entry> m_EntryByID = new Dictionary<uint, Far3Entry>();
        private uint m_ManifestOffset;

        /// <summary>
        /// Creates a new FAR3Archive instance from a path.
        /// </summary>
        /// <param name="Path">The path to the archive.</param>
        public FAR3Archive(string Path)
        {
            m_ArchivePath = Path;

            if (isReadingSomething == false)
            {
                isReadingSomething = true;

                try
                {
                    m_Reader = new BinaryReader(File.Open(Path, FileMode.Open, FileAccess.Read, FileShare.Read));
                }
                catch (Exception ex)
                {
                    throw new FAR3Exception("Could not open the specified archive - " + Path + "! (FAR3Archive())");
                }

                string Header = Encoding.ASCII.GetString(m_Reader.ReadBytes(8));
                uint Version = m_Reader.ReadUInt32();

                if ((Header != "FAR!byAZ") || (Version != 3))
                {
                    throw new FAR3Exception("Archive wasn't a valid FAR V.3 archive! (FAR3Archive())");
                }

                uint ManifestOffset = m_Reader.ReadUInt32();
                m_ManifestOffset = ManifestOffset;

                m_Reader.BaseStream.Seek(ManifestOffset, SeekOrigin.Begin);

                uint NumFiles = m_Reader.ReadUInt32();

                for (int i = 0; i < NumFiles; i++)
                {
                    Far3Entry Entry = new Far3Entry();
                    Entry.DecompressedFileSize = m_Reader.ReadUInt32();
                    byte dummy0 = m_Reader.ReadByte();
                    byte dummy1 = m_Reader.ReadByte();
                    byte dummy2 = m_Reader.ReadByte();
                    Entry.CompressedFileSize = (uint)((dummy0 << 0) | (dummy1 << 8) | (dummy2) << 16);
                    Entry.DataType = m_Reader.ReadByte();
                    Entry.DataOffset = m_Reader.ReadUInt32();
                    //Entry.HasFilename = m_Reader.ReadUInt16();
                    Entry.IsCompressed = m_Reader.ReadByte();
                    Entry.AccessNumber = m_Reader.ReadByte();
                    Entry.FilenameLength = m_Reader.ReadUInt16();
                    Entry.TypeID = m_Reader.ReadUInt32();
                    Entry.FileID = m_Reader.ReadUInt32();
                    Entry.Filename = Encoding.ASCII.GetString(m_Reader.ReadBytes(Entry.FilenameLength));

                    if (!m_Entries.ContainsKey(Entry.Filename))
                        m_Entries.Add(Entry.Filename, Entry);
                    m_EntriesList.Add(Entry);

                    m_EntryByID.Add(Entry.FileID, Entry); //isn't this a bad idea? i have a feeling this is a bad idea...
                }

                //Keep the stream open, it helps peformance.
                //m_Reader.Close();
                isReadingSomething = false;
            }
        }

        /// <summary>
        /// Gets an entry's data from a Far3Entry instance.
        /// </summary>
        /// <param name="Entry">The Far3Entry instance.</param>
        /// <returns>The entry's data.</returns>
        public byte[] GetEntry(Far3Entry Entry)
        {
            lock (m_Reader)
            {
                m_Reader.BaseStream.Seek((long)Entry.DataOffset, SeekOrigin.Begin);

                isReadingSomething = true;

                if (Entry.IsCompressed == 0x01)
                {
                    m_Reader.BaseStream.Seek(9, SeekOrigin.Current);
                    uint Filesize = m_Reader.ReadUInt32();
                    ushort CompressionID = m_Reader.ReadUInt16();

                    if (CompressionID == 0xFB10)
                    {
                        byte dummy0 = m_Reader.ReadByte();
                        byte dummy1 = m_Reader.ReadByte();
                        byte dummy2 = m_Reader.ReadByte();
                        uint DecompressedSize = (uint)((dummy0 << 0x10) | (dummy1 << 0x08) | +dummy2);

                        Decompresser Dec = new Decompresser();
                        Dec.CompressedSize = Filesize;
                        Dec.DecompressedSize = DecompressedSize;

                        byte[] DecompressedData = Dec.Decompress(m_Reader.ReadBytes((int)Filesize));
                        //m_Reader.Close();

                        isReadingSomething = false;

                        return DecompressedData;
                    }
                    else
                    {
                        m_Reader.BaseStream.Seek((m_Reader.BaseStream.Position - 15), SeekOrigin.Begin);

                        byte[] Data = m_Reader.ReadBytes((int)Entry.DecompressedFileSize);
                        //m_Reader.Close();

                        isReadingSomething = false;

                        return Data;
                    }
                }
                else
                {
                    byte[] Data = m_Reader.ReadBytes((int)Entry.DecompressedFileSize);
                    //m_Reader.Close();

                    isReadingSomething = false;

                    return Data;
                }
            }

            throw new FAR3Exception("FAR3Entry didn't exist in archive - FAR3Archive.GetEntry()");
        }

        /// <summary>
        /// Returns the entries of this FAR3Archive as byte arrays together with their corresponding FileIDs.
        /// </summary>
        /// <returns>A List of KeyValuePair instances.</returns>
        public List<KeyValuePair<uint, byte[]>> GetAllEntries()
        {
            List<KeyValuePair<uint, byte[]>> toReturn = new List<KeyValuePair<uint, byte[]>>();
            foreach (Far3Entry Entry in m_EntriesList)
            {
                toReturn.Add(new KeyValuePair<uint, byte[]>(Entry.FileID, GetEntry(Entry)));
            }
            return toReturn;
        }

        /// <summary>
        /// Returns the entries of this FAR3Archive as FAR3Entry instances in a List.
        /// </summary>
        /// <returns>Returns the entries of this FAR3Archive as FAR3Entry instances in a List.</returns>
        public List<Far3Entry> GetAllFAR3Entries()
        {
            List<Far3Entry> Entries = new List<Far3Entry>();

            foreach (KeyValuePair<string, Far3Entry> KVP in m_Entries)
                Entries.Add(KVP.Value);

            return Entries;
        }

        /// <summary>
        /// Gets an entry from a FileID.
        /// </summary>
        /// <param name="FileID">The entry's FileID.</param>
        /// <returns>The entry's data.</returns>
        public byte[] GetItemByID(uint FileID)
        {
            var item = m_EntryByID[FileID];
            if (item == null)
            {
                throw new FAR3Exception("Didn't find entry!");
            }
            return GetEntry(item);
        }

        /// <summary>
        /// Gets an entry from its ID (TypeID + FileID).
        /// </summary>
        /// <param name="ID">The ID of the entry.</param>
        /// <returns>The entry's data.</returns>
        public byte[] GetItemByID(ulong ID)
        {
            byte[] Bytes = BitConverter.GetBytes(ID);
            uint FileID = BitConverter.ToUInt32(Bytes, 4);
            uint TypeID = BitConverter.ToUInt32(Bytes, 0);

            var item = m_EntryByID[FileID];
            if (item == null || item.TypeID != TypeID)
            {
                throw new FAR3Exception("Didn't find entry!");
            }

            return GetEntry(item);
        }

        /// <summary>
        /// Gets an entry's data from a filename.
        /// </summary>
        /// <param name="Filename">The filename of the entry.</param>
        /// <returns>The entry's data.</returns>
        public byte[] this[string Filename]
        {
            get
            {
                return GetEntry(m_Entries[Filename]);
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes this FAR3Archive instance.
        /// </summary>
        public void Dispose()
        {
            if (m_Reader != null)
            {
                m_Reader.Close();
            }
        }

        #endregion
    }
}

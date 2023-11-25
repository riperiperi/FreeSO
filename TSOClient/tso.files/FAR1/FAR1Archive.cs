using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FSO.Files.FAR1
{
    /// <summary>
    /// A FAR1 (File Archive v1) archive.
    /// </summary>
    public class FAR1Archive
    {
        private string m_Path;
        private BinaryReader m_Reader;

        private uint m_ManifestOffset;
        private uint m_NumFiles;
        private List<FarEntry> m_Entries = new List<FarEntry>();
        private bool V1b = true;

        /// <summary>
        /// The offset into the archive of the manifest.
        /// </summary>
        public uint ManifestOffset
        {
            get { return m_ManifestOffset; }
        }

        /// <summary>
        /// The number of files/entries in the archive.
        /// </summary>
        public uint NumFiles
        {
            get { return m_NumFiles; }
        }

        /// <summary>
        /// Creates a new FAR1Archive instance from a path.
        /// </summary>
        /// <param name="Path">The path to the archive.</param>
        public FAR1Archive(string Path, bool v1b)
        {
            m_Path = Path;
            m_Reader = new BinaryReader(File.Open(Path, FileMode.Open, FileAccess.Read, FileShare.Read));

            //Magic number - An 8-byte string (not null-terminated), consisting of the ASCII characters "FAR!byAZ"
            string Header = Encoding.ASCII.GetString(m_Reader.ReadBytes(8));
            //Version - A 4-byte unsigned integer specifying the version; 1a and 1b each specify 1.
            uint Version = m_Reader.ReadUInt32();

            if ((Header != "FAR!byAZ") || (Version != 1))
            {
                throw (new Exception("Archive wasn't a valid FAR V.1 archive!"));
            }

            //File table offset - A 4-byte unsigned integer specifying the offset to the file table 
            //from the beginning of the archive.
            m_ManifestOffset = m_Reader.ReadUInt32();
            m_Reader.BaseStream.Seek(m_ManifestOffset, SeekOrigin.Begin);

            m_NumFiles = m_Reader.ReadUInt32();


            for (int i = 0; i < m_NumFiles; i++)
            {

                FarEntry Entry = new FarEntry();
                Entry.DataLength = m_Reader.ReadInt32();
                Entry.DataLength2 = m_Reader.ReadInt32();
                Entry.DataOffset = m_Reader.ReadInt32();
                Entry.FilenameLength = (v1b) ? m_Reader.ReadInt16() : (short)m_Reader.ReadInt32();
                Entry.Filename = Encoding.ASCII.GetString(m_Reader.ReadBytes(Entry.FilenameLength));

                m_Entries.Add(Entry);
            }                  
        }

        /// <summary>
        /// Gets an entry based on a KeyValuePair.
        /// </summary>
        /// <param name="Entry">A KeyValuePair (string, byte[]) representing the entry. The byte array can be null.</param>
        /// <returns>A FarEntry or null if the entry wasn't found.</returns>
        public byte[] GetEntry(KeyValuePair<string, byte[]> Entry)
        {
            foreach (FarEntry Ent in m_Entries)
            {
                if (Ent.Filename == Entry.Key)
                {
                    m_Reader.BaseStream.Seek(Ent.DataOffset, SeekOrigin.Begin);
                    return m_Reader.ReadBytes(Ent.DataLength);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets an entry's data from a FarEntry instance.
        /// </summary>
        /// <param name="Entry">A FarEntry instance.</param>
        /// <returns>The entry's data.</returns>
        public byte[] GetEntry(FarEntry Entry)
        {
            foreach (FarEntry Ent in m_Entries)
            {
                if (Ent.Filename == Entry.Filename)
                {
                    m_Reader.BaseStream.Seek(Ent.DataOffset, SeekOrigin.Begin);
                    return m_Reader.ReadBytes(Ent.DataLength);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a list of all FarEntry instances in this archive.
        /// </summary>
        /// <returns></returns>
        public List<FarEntry> GetAllFarEntries()
        {
            return m_Entries;
        }

        /// <summary>
        /// Gets all entries in the archive.
        /// </summary>
        /// <returns>A List of KeyValuePair instances.</returns>
        public List<KeyValuePair<string, byte[]>> GetAllEntries()
        {
            List<KeyValuePair<string, byte[]>> Entries = new List<KeyValuePair<string,byte[]>>();

            foreach (FarEntry Entry in m_Entries)
            {
                m_Reader.BaseStream.Seek(Entry.DataOffset, SeekOrigin.Begin);
                byte[] Data = m_Reader.ReadBytes(Entry.DataLength);

                KeyValuePair<string, byte[]> KvP = new KeyValuePair<string, byte[]>(Entry.Filename, Data);
                Entries.Add(KvP);
            }

            return Entries;
        }

        public void Close()
        {
            m_Reader.Close();
        }
    }
}

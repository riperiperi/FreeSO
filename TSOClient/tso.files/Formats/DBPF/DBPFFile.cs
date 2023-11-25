using System;
using System.Collections.Generic;
using System.IO;
using FSO.Files.Utils;

namespace FSO.Files.Formats.DBPF
{
    /// <summary>
    /// The database-packed file (DBPF) is a format used to store data for pretty much all Maxis games after The Sims, 
    /// including The Sims Online (the first appearance of this format), SimCity 4, The Sims 2, Spore, The Sims 3, and 
    /// SimCity 2013.
    /// </summary>
    public class DBPFFile : IDisposable
    {
        public int DateCreated;
        public int DateModified;

        private uint IndexMajorVersion;
        private uint NumEntries;
        private IoBuffer m_Reader;

        private List<DBPFEntry> m_EntriesList = new List<DBPFEntry>();
        private Dictionary<ulong, DBPFEntry> m_EntryByID = new Dictionary<ulong, DBPFEntry>();
        private Dictionary<DBPFTypeID, List<DBPFEntry>> m_EntriesByType = new Dictionary<DBPFTypeID, List<DBPFEntry>>();

        private IoBuffer Io;

        /// <summary>
        /// Constructs a new DBPF instance.
        /// </summary>
        public DBPFFile()
        {
        }

        /// <summary>
        /// Creates a DBPF instance from a path.
        /// </summary>
        /// <param name="file">The path to an DBPF archive.</param>
        public DBPFFile(string file)
        {
            var stream = File.OpenRead(file);
                Read(stream);
        }

        /// <summary>
        /// Reads a DBPF archive from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        public void Read(Stream stream)
        {
            m_EntryByID = new Dictionary<ulong,DBPFEntry>();
            m_EntriesList = new List<DBPFEntry>();

            var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN);
            m_Reader = io;
            this.Io = io;
            
            var magic = io.ReadCString(4);
            if (magic != "DBPF")
            {
                throw new Exception("Not a DBPF file");
            }

            var majorVersion = io.ReadUInt32();
            var minorVersion = io.ReadUInt32();
            var version = majorVersion + (((double)minorVersion)/10.0);

            /** Unknown, set to 0 **/
            io.Skip(12);

            if (version == 1.0)
            {
                this.DateCreated = io.ReadInt32();
                this.DateModified = io.ReadInt32();
            }

            if (version < 2.0)
            {
                IndexMajorVersion = io.ReadUInt32();
            }

            NumEntries = io.ReadUInt32();
            uint indexOffset = 0;
            if (version < 2.0)
            {
                indexOffset = io.ReadUInt32();
            }
            var indexSize = io.ReadUInt32();

            if (version < 2.0)
            {
                var trashEntryCount = io.ReadUInt32();
                var trashIndexOffset = io.ReadUInt32();
                var trashIndexSize = io.ReadUInt32();
                var indexMinor = io.ReadUInt32();
            }
            else if (version == 2.0)
            {
                var indexMinor = io.ReadUInt32();
                indexOffset = io.ReadUInt32();
                io.Skip(4);
            }

            /** Padding **/
            io.Skip(32);

            io.Seek(SeekOrigin.Begin, indexOffset);
            for (int i = 0; i < NumEntries; i++)
            {
                var entry = new DBPFEntry();
                entry.TypeID = (DBPFTypeID)io.ReadUInt32();
                entry.GroupID = (DBPFGroupID)io.ReadUInt32();
                entry.InstanceID = io.ReadUInt32();
                entry.FileOffset = io.ReadUInt32();
                entry.FileSize = io.ReadUInt32();

                m_EntriesList.Add(entry);
                ulong id = (((ulong)entry.InstanceID) << 32) + (ulong)entry.TypeID;
                if (!m_EntryByID.ContainsKey(id))
                    m_EntryByID.Add(id, entry);

                if (!m_EntriesByType.ContainsKey(entry.TypeID))
                    m_EntriesByType.Add(entry.TypeID, new List<DBPFEntry>());

                m_EntriesByType[entry.TypeID].Add(entry);
            }
        }

        /// <summary>
        /// Gets a DBPFEntry's data from this DBPF instance.
        /// </summary>
        /// <param name="entry">Entry to retrieve data for.</param>
        /// <returns>Data for entry.</returns>
        public byte[] GetEntry(DBPFEntry entry)
        {
            m_Reader.Seek(SeekOrigin.Begin, entry.FileOffset);

            return m_Reader.ReadBytes((int)entry.FileSize);
        }

        /// <summary>
        /// Gets an entry from its ID (TypeID + FileID).
        /// </summary>
        /// <param name="ID">The ID of the entry.</param>
        /// <returns>The entry's data.</returns>
        public byte[] GetItemByID(ulong ID)
        {
            if (m_EntryByID.ContainsKey(ID))
                return GetEntry(m_EntryByID[ID]);
            else
                return null;
        }

        /// <summary>
        /// Gets all entries of a specific type.
        /// </summary>
        /// <param name="Type">The Type of the entry.</param>
        /// <returns>The entry data, paired with its instance id.</returns>
        public List<KeyValuePair<uint, byte[]>> GetItemsByType(DBPFTypeID Type)
        {

            var result = new List<KeyValuePair<uint, byte[]>>();

            var entries = m_EntriesByType[Type];
            for (int i = 0; i < entries.Count; i++)
            {
                result.Add(new KeyValuePair<uint, byte[]>(entries[i].InstanceID, GetEntry(entries[i])));
            }
            return result;
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes this DBPF instance.
        /// </summary>
        public void Dispose()
        {
            Io.Dispose();
        }

        #endregion
    }
}

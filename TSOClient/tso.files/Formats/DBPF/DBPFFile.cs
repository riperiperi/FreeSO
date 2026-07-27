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

        private List<DBPFEntry> m_EntriesList = [];
        private Dictionary<ulong, DBPFEntry> m_EntryByID = [];
        private Dictionary<DBPFTypeID, List<DBPFEntry>> m_EntriesByType = [];

        private IoBuffer Io;

        /// <summary>
        /// Constructs a new DBPF instance.
        /// </summary>
        public DBPFFile()
        {
            DateCreated = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Creates a DBPF instance from a path.
        /// </summary>
        /// <param name="file">The path to an DBPF archive.</param>
        public DBPFFile(string file) : this()
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

                if (trashEntryCount != 0)
                {

                }
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

        public void Write(Stream stream)
        {
            var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN);
            io.WriteCString("DBPF", 4);

            io.WriteUInt32(1); // major version
            io.WriteUInt32(0); // minor version

            io.Skip(12);

            io.WriteInt32(DateCreated);
            this.DateModified = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            io.WriteInt32(DateModified);

            io.WriteUInt32(7); // index major version

            io.WriteUInt32((uint)m_EntriesList.Count);

            var indexOffsetMark = stream.Position;
            io.WriteUInt32(0); // placeholder index offset and size, calculated after the file data is inserted
            io.WriteUInt32(0);

            io.WriteUInt32(0); // trashEntryCount
            io.WriteUInt32(0); // trashIndexOffset
            io.WriteUInt32(0); // trashIndexSize
            io.WriteUInt32(0); // indexMinor

            io.Skip(32);

            var newEntries = new DBPFEntry[m_EntriesList.Count];
            int i = 0;

            // Insert entry data here.
            foreach (var entry in m_EntriesList)
            {
                var data = GetEntry(entry);

                newEntries[i++] = new DBPFEntry()
                {
                    FileOffset = (uint)stream.Position,
                    FileSize = (uint)data.Length,
                    GroupID = entry.GroupID,
                    InstanceID = entry.InstanceID,
                    TypeID = entry.TypeID
                };

                io.WriteBytes(data);
                int skip = (4 - (data.Length % 4)) % 4;

                io.Skip(skip);
            }

            // After all the entry data, insert the index, then go back and rewrite the index offset and size to be correct.

            var indexStart = stream.Position;
            foreach (var entry in newEntries)
            {
                io.WriteUInt32((uint)entry.TypeID);
                io.WriteUInt32((uint)entry.GroupID);
                io.WriteUInt32(entry.InstanceID);
                io.WriteUInt32(entry.FileOffset);
                io.WriteUInt32(entry.FileSize);
            }
            var indexEnd = stream.Position;

            stream.Seek(indexOffsetMark, SeekOrigin.Begin);
            io.WriteUInt32((uint)indexStart);
            io.WriteUInt32((uint)(indexEnd - indexStart));
        }

        /// <summary>
        /// Gets a DBPFEntry's data from this DBPF instance.
        /// </summary>
        /// <param name="entry">Entry to retrieve data for.</param>
        /// <returns>Data for entry.</returns>
        public byte[] GetEntry(DBPFEntry entry)
        {
            if (entry.Data != null)
            {
                return entry.Data;
            }

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
        /// Gets an entry from its ID (TypeID + FileID).
        /// </summary>
        /// <param name="type">The type of the entry.</param>
        /// <param name="fileId">The file ID of the entry.</param>
        /// <returns>The entry's data.</returns>
        public byte[] GetItemByID(DBPFTypeID type, uint fileId)
        {
            return GetItemByID(((ulong)fileId << 32) | (ulong)type);
        }

        /// <summary>
        /// Gets all entries of a specific type.
        /// </summary>
        /// <param name="Type">The Type of the entry.</param>
        /// <returns>The entry data, paired with its instance id.</returns>
        public List<KeyValuePair<uint, byte[]>> GetItemsByType(DBPFTypeID Type)
        {

            var result = new List<KeyValuePair<uint, byte[]>>();

            if (m_EntriesByType.TryGetValue(Type, out var entries))
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    result.Add(new KeyValuePair<uint, byte[]>(entries[i].InstanceID, GetEntry(entries[i])));
                }
            }

            return result;
        }

        public void AddOrReplace(ulong id, DBPFGroupID groupId, byte[] data)
        {
            if (!m_EntryByID.TryGetValue(id, out DBPFEntry entry))
            {
                entry = new DBPFEntry()
                {
                    InstanceID = (uint)(id >> 32),
                    TypeID = (DBPFTypeID)(uint)id,
                };

                m_EntryByID[id] = entry;
                m_EntriesList.Add(entry);

                if (!m_EntriesByType.TryGetValue(entry.TypeID, out var entries))
                {
                    entries = [];
                    m_EntriesByType[entry.TypeID] = entries;
                }

                NumEntries++;
                entries.Add(entry);
            }

            entry.GroupID = groupId;
            entry.Data = data;
        }

        public void AddOrReplace(uint id, DBPFTypeID type, DBPFGroupID groupId, byte[] data)
        {
            AddOrReplace(((ulong)id << 32) | (ulong)(type), groupId, data);
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes this DBPF instance.
        /// </summary>
        public void Dispose()
        {
            Io?.Dispose();
        }

        #endregion
    }
}

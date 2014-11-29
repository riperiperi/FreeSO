/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSO.Files.utils;
using TSO.Files.FAR3;

namespace TSO.Files.formats.dbpf
{
    public class DBPF : IDisposable
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

        public DBPF()
        {
        }

        /// <summary>
        /// Creates a DBPF instance from a path.
        /// </summary>
        /// <param name="file">The path to an DBPF archive.</param>
        public DBPF(string file)
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

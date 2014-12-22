/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SimsLib.FAR1
{
    public class FAR1Archive
    {
        private string m_Path;
        private BinaryReader m_Reader;

        private uint m_ManifestOffset;
        private uint m_NumFiles;
        private List<FarEntry> m_Entries = new List<FarEntry>();

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

        public FAR1Archive(string Path)
        {
            m_Path = Path;
            m_Reader = new BinaryReader(File.Open(Path, FileMode.Open));

            string Header = Encoding.ASCII.GetString(m_Reader.ReadBytes(8));
            uint Version = m_Reader.ReadUInt32();

            if ((Header != "FAR!byAZ") || (Version != 1))
            {
                throw(new Exception("Archive wasn't a valid FAR V.1 archive!"));
            }

            m_ManifestOffset = m_Reader.ReadUInt32();
            m_Reader.BaseStream.Seek(m_ManifestOffset, SeekOrigin.Begin);

            m_NumFiles = m_Reader.ReadUInt32();
            
            for (int i = 0; i < m_NumFiles; i++)
            {
                FarEntry Entry = new FarEntry();
                Entry.DataLength = m_Reader.ReadInt32();
                Entry.DataLength2 = m_Reader.ReadInt32();
                Entry.DataOffset = m_Reader.ReadInt32();
                Entry.FilenameLength = m_Reader.ReadInt16();
                Entry.Filename = Encoding.ASCII.GetString(m_Reader.ReadBytes(Entry.FilenameLength));

                m_Entries.Add(Entry);
            }

            m_Reader.Close();
        }

        public List<KeyValuePair<string, byte[]>> GetAllEntries()
        {
            List<KeyValuePair<string, byte[]>> Entries = new List<KeyValuePair<string,byte[]>>();

            m_Reader = new BinaryReader(File.Open(m_Path, FileMode.Open));

            foreach (FarEntry Entry in m_Entries)
            {
                m_Reader.BaseStream.Seek(Entry.DataOffset, SeekOrigin.Begin);
                byte[] Data = m_Reader.ReadBytes(Entry.DataLength);

                KeyValuePair<string, byte[]> KvP = new KeyValuePair<string, byte[]>(Entry.Filename, Data);
                Entries.Add(KvP);
            }

            m_Reader.Close();

            return Entries;
        }
    }
}

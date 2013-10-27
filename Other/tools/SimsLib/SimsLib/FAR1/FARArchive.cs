/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace SimsLib.FAR1
{
    /// <summary>
    /// Represents a single FAR1 archive.
    /// </summary>
    public class FARArchive
    {
        private uint m_ManifestOffset;
        private List<FarEntry> m_FarEntries;
        private BinaryReader m_Reader;

        /// <summary>
        /// Opens an existing FAR archive.
        /// </summary>
        /// <param name="Path">The path to the archive.</param>
        public FARArchive(string Path)
        {
            m_Reader = new BinaryReader(File.Open(Path, FileMode.Open, FileAccess.Read));
            m_FarEntries = new List<FarEntry>();

            string Header = Encoding.ASCII.GetString(m_Reader.ReadBytes(8));
            uint Version = m_Reader.ReadUInt32();

            if ((Header != "FAR!byAZ") || (Version != 1))
            {
                MessageBox.Show("Archive wasn't a valid FAR V.1 archive!");
                return;
            }

            uint ManifestOffset = m_Reader.ReadUInt32();
            m_ManifestOffset = ManifestOffset;

            m_Reader.BaseStream.Seek(ManifestOffset, SeekOrigin.Begin);


            uint NumFiles = m_Reader.ReadUInt32();

            for (int i = 0; i < NumFiles; i++)
            {
                FarEntry Entry = new FarEntry();
                Entry.DataLength = m_Reader.ReadInt32();
                Entry.DataLength2 = m_Reader.ReadInt32();
                Entry.DataOffset = m_Reader.ReadInt32();
                Entry.FilenameLength = m_Reader.ReadInt16();
                Entry.Filename = Encoding.ASCII.GetString(m_Reader.ReadBytes(Entry.FilenameLength));

                m_FarEntries.Add(Entry);
            }

            //Reader.Close();
        }

        /// <summary>
        /// Gets an entry's data from an archive.
        /// </summary>
        /// <param name="Entry">The entry to get the data for.</param>
        /// <returns>The data for the entry.</returns>
        public byte[] GetEntry(FarEntry Entry)
        {
            m_Reader.BaseStream.Seek((long)Entry.DataOffset, SeekOrigin.Begin);
            return m_Reader.ReadBytes(Entry.DataLength);
        }



        public List<FarEntry> GetAllFarEntries()
        {
            return m_FarEntries;
        }

        /// <summary>
        /// Returns all the entries in the archive as a list.
        /// </summary>
        /// <returns>A list of all the entries in the archive.</returns>
        public List<KeyValuePair<string, byte[]>> GetAllEntries()
        {
            List<KeyValuePair<string, byte[]>> toReturn = new List<KeyValuePair<string, byte[]>>();

            foreach (FarEntry Entry in m_FarEntries)
            {
                toReturn.Add(new KeyValuePair<string, byte[]>(Entry.Filename, GetEntry(Entry)));
            }

            return toReturn;
        }
    }
}

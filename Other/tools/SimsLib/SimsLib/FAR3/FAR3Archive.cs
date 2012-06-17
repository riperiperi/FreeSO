/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): Nicholas Roth.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SimsLib.FAR3
{
    /// <summary>
    /// Represents a single FAR3 archive.
    /// </summary>
    public class FAR3Archive
    {
        private BinaryReader m_Reader;
        public static bool isReadingSomething;

        private string m_ArchivePath;
        private Dictionary<string, Far3Entry> m_Entries = new Dictionary<string, Far3Entry>();
        private List<Far3Entry> m_EntriesList = new List<Far3Entry>();
        private uint m_ManifestOffset;

        public FAR3Archive(string Path)
        {
            if (isReadingSomething == false)
            {
                isReadingSomething = true;
                m_ArchivePath = Path;

                try
                {
                    m_Reader = new BinaryReader(File.Open(Path, FileMode.Open));
                }
                catch (Exception e)
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
                    byte[] Dummy = m_Reader.ReadBytes(3);
                    Entry.CompressedFileSize = (uint)((Dummy[0] << 0) | (Dummy[1] << 8) | (Dummy[2]) << 16);
                    Entry.DataType = m_Reader.ReadByte();
                    Entry.DataOffset = m_Reader.ReadUInt32();
                    Entry.Compressed = m_Reader.ReadByte();
                    Entry.AccessNumber = m_Reader.ReadByte();
                    Entry.FilenameLength = m_Reader.ReadUInt16();
                    Entry.TypeID = m_Reader.ReadUInt32();
                    Entry.FileID = m_Reader.ReadUInt32();
                    Entry.Filename = Encoding.ASCII.GetString(m_Reader.ReadBytes(Entry.FilenameLength));

                    if (!m_Entries.ContainsKey(Entry.Filename))
                        m_Entries.Add(Entry.Filename, Entry);
                    m_EntriesList.Add(Entry);
                }

                m_Reader.Close();
                isReadingSomething = false;
            }
        }

        private byte[] GetEntry(Far3Entry Entry)
        {
            m_Reader = new BinaryReader(File.Open(m_ArchivePath, FileMode.Open));
            m_Reader.BaseStream.Seek((long)Entry.DataOffset, SeekOrigin.Begin);

            if (Entry.Compressed == 0x01)
            {
                m_Reader.ReadBytes(9);
                uint Filesize = m_Reader.ReadUInt32();
                ushort CompressionID = m_Reader.ReadUInt16();

                if (CompressionID == 0xFB10)
                {
                    byte[] Dummy = m_Reader.ReadBytes(3);
                    uint DecompressedSize = (uint)((Dummy[0] << 0x10) | (Dummy[1] << 0x08) | +Dummy[2]);

                    Decompresser Dec = new Decompresser();
                    Dec.CompressedSize = Filesize;
                    Dec.DecompressedSize = DecompressedSize;

                    byte[] DecompressedData = Dec.Decompress(m_Reader.ReadBytes((int)Filesize));
                    m_Reader.Close();

                    return DecompressedData;
                }
                else
                {
                    m_Reader.BaseStream.Seek((m_Reader.BaseStream.Position - 15), SeekOrigin.Begin);

                    byte[] Data = m_Reader.ReadBytes((int)Entry.DecompressedFileSize);
                    m_Reader.Close();

                    return Data;
                }
            }
            else
            {
                byte[] Data = m_Reader.ReadBytes((int)Entry.DecompressedFileSize);
                m_Reader.Close();

                return Data;
            }
            throw new FAR3Exception("FileID didn't match any in the archive! (FAR3Archive.GetItemByID())");
        }

        public List<KeyValuePair<uint, byte[]>> GetAllEntries()
        {
            List<KeyValuePair<uint, byte[]>> toReturn = new List<KeyValuePair<uint, byte[]>>();

            foreach (Far3Entry Entry in m_EntriesList)
            {
                toReturn.Add(new KeyValuePair<uint, byte[]>(Entry.FileID, GetEntry(Entry)));
            }

            return toReturn;
        }

        public byte[] GetItemByID(uint FileID)
        {
            Far3Entry[] entries = new Far3Entry[m_Entries.Count];
            m_Entries.Values.CopyTo(entries, 0);
            Far3Entry Entry = Array.Find(entries, delegate(Far3Entry entry) { return entry.FileID == FileID; });

            return GetEntry(Entry);
        }

        public byte[] this[string Filename]
        {
            get
            {
                return GetEntry(m_Entries[Filename]);
            }
        }
    }
}

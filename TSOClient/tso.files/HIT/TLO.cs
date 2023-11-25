using System.Collections.Generic;
using System.IO;

//ok, why do we have support for this? it's only used by hitlab and only the hitlab test files have them...

namespace FSO.Files.HIT
{
    /// <summary>
    /// Represents a TLO file.
    /// TLO (short for Track Logic according to hitlab.ini) is a format 
    /// used solely for Hitlab. Integers are little-endian. 
    /// </summary>
    public class TLO
    {
        private uint m_Count;
        public List<TLOSection> Sections = new List<TLOSection>();

        /// <summary>
        /// Creates a new tracklogic instance.
        /// </summary>
        /// <param name="Filedata">The data to create the tracklogic instance from.</param>
        public TLO(byte[] Filedata)
        {
            BinaryReader Reader = new BinaryReader(new MemoryStream(Filedata));

            Reader.ReadBytes(4); //Reserved.
            m_Count = Reader.ReadUInt32();

            for (int i = 0; i < m_Count; i++)
            {
                TLOSection Section = new TLOSection();
                Section.Name = new string(Reader.ReadChars(Reader.ReadInt32()));
                Section.GroupID1 = Reader.ReadUInt32();
                Section.FileID1 = Reader.ReadUInt32();
                Section.GroupID2 = Reader.ReadUInt32();
                Section.FileID1 = Reader.ReadUInt32();
                Section.TypeID = Reader.ReadUInt32();
                Section.GroupID3 = Reader.ReadUInt32();
                Section.FileID3 = Reader.ReadUInt32();

                Sections.Add(Section);
            }

            Reader.Close();
        }

        /// <summary>
        /// Creates a new tracklogic instance.
        /// </summary>
        /// <param name="Filepath">The path to the tracklogic file to read.</param>
        public TLO(string Filepath)
        {
            BinaryReader Reader = new BinaryReader(File.Open(Filepath, FileMode.Open, FileAccess.Read, FileShare.Read));

            Reader.ReadBytes(4); //Reserved.
            m_Count = Reader.ReadUInt32();

            for (int i = 0; i < m_Count; i++)
            {
                TLOSection Section = new TLOSection();
                Section.Name = new string(Reader.ReadChars(Reader.ReadInt32()));
                Section.GroupID1 = Reader.ReadUInt32();
                Section.FileID1 = Reader.ReadUInt32();
                Section.GroupID2 = Reader.ReadUInt32();
                Section.FileID1 = Reader.ReadUInt32();
                Section.TypeID = Reader.ReadUInt32();
                Section.GroupID3 = Reader.ReadUInt32();
                Section.FileID3 = Reader.ReadUInt32();

                Sections.Add(Section);
            }

            Reader.Close();
        }
    }

    /// <summary>
    /// Represents a section in a tracklogic file.
    /// </summary>
    public class TLOSection
    {
        public string Name;
        public uint GroupID1;
        public uint FileID1;
        public uint GroupID2;
        public uint FileID2;
        public uint TypeID;
        public uint GroupID3;
        public uint FileID3;
    }
}

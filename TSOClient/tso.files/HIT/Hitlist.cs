using System;
using System.Collections.Generic;
using System.IO;

namespace FSO.Files.HIT
{
    /// <summary>
    /// HLS refers to two binary formats that both define a list of IDs, known as a hitlist.
    /// One format is a Pascal string with a 4-byte, little-endian length, representing a 
    /// comma-seperated list of decimal values, or decimal ranges (e.g. "1025-1035"), succeeded 
    /// by a single LF newline.
    /// </summary>
    public class Hitlist
    {
        private uint m_IDCount;
        public List<uint> IDs; //variable length so it's easier to fill with ranges

        /// <summary>
        /// Creates a new hitlist.
        /// </summary>
        /// <param name="Filedata">The data to create the hitlist from.</param>
        public Hitlist(byte[] Filedata)
        {
            Read(new MemoryStream(Filedata));
        }

        private void Read(Stream data)
        {
            BinaryReader Reader = new BinaryReader(data);

            IDs = new List<uint>();
            var VerOrCount = Reader.ReadUInt32();

            try
            {
                if (VerOrCount == 1) //binary format, no hitlist is ever going to have length 1... (i hope)
                {
                    m_IDCount = Reader.ReadUInt32();

                    for (int i = 0; i < m_IDCount; i++)
                        IDs.Add(Reader.ReadUInt32());

                    Reader.Close();
                }
                else
                {
                    var str = new string(Reader.ReadChars((int)VerOrCount));
                    Populate(str);
                }

            }
            catch
            {
                Reader.BaseStream.Seek(4, SeekOrigin.Begin); //attempt 3rd mystery format, count+int32
                for (int i = 0; i < VerOrCount; i++)
                    IDs.Add(Reader.ReadUInt32());
            }
        }

        public Hitlist()
        {
            IDs = new List<uint>();
        }

        public void Populate(string str)
        {
            var commaSplit = str.Split(',');
            for (int i = 0; i < commaSplit.Length; i++)
            {
                var dashSplit = commaSplit[i].Split('-');
                if (dashSplit.Length > 1)
                { //range, parse two values and fill in the gap
                    var min = Convert.ToUInt32(dashSplit[0]);
                    var max = Convert.ToUInt32(dashSplit[1]);
                    for (uint j = min; j <= max; j++)
                    {
                        IDs.Add(j);
                    }
                }
                else
                { //literal entry, add to list
                    IDs.Add(Convert.ToUInt32(commaSplit[i]));
                }
            }
        }

        public static Hitlist HitlistFromString(string str)
        {
            var result = new Hitlist();
            result.Populate(str);
            return result;
        }

        /// <summary>
        /// Creates a new hitlist.
        /// </summary>
        /// <param name="Filepath">The path to the hitlist to read.</param>
        public Hitlist(string Filepath)
        {
            Read(File.Open(Filepath, FileMode.Open, FileAccess.Read, FileShare.Read));
        }
    }
}

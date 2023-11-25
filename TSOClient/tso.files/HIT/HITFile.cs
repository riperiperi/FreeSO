using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FSO.Files.HIT
{
    /// <summary>
    /// HIT files contain the bytecode to be executed when plaing track events.
    /// </summary>
    public class HITFile
    {
        public string MagicNumber;
        public uint MajorVersion;
        public uint MinorVersion;
        public byte[] Data;
        public Dictionary<uint, uint> EntryPointByTrackID;

        /// <summary>
        /// Creates a new track.
        /// </summary>
        /// <param name="Filedata">The data to create the track from.</param>
        public HITFile(byte[] Filedata)
        {
            ReadFile(new MemoryStream(Filedata));
        }

        /// <summary>
        /// Creates a new track.
        /// </summary>
        /// <param name="Filedata">The path to the data to create the track from.</param>
        public HITFile(string Filepath)
        {
            ReadFile(File.Open(Filepath, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        private void ReadFile(Stream data)
        {
            BinaryReader Reader = new BinaryReader(data);

            MagicNumber = new string(Reader.ReadChars(4));
            MajorVersion = Reader.ReadUInt32();
            MinorVersion = Reader.ReadUInt32();
            var signature = new string(Reader.ReadChars(4));

            var tableLoc = FindBytePattern(Reader.BaseStream, new byte[] { (byte)'E', (byte)'N', (byte)'T', (byte)'P' });
            if (tableLoc != -1)
            {
                Reader.BaseStream.Seek(tableLoc, SeekOrigin.Begin);
                EntryPointByTrackID = new Dictionary<uint, uint>();

                while (true)
                {

                    var EndTest = ASCIIEncoding.ASCII.GetString(Reader.ReadBytes(4)); //can be invalid chars
                    if (EndTest.Equals("EENT", StringComparison.InvariantCultureIgnoreCase))
                    {
                        break;
                    }
                    else
                    {
                        Reader.BaseStream.Position -= 4; //go back to read it as a table entry
                        var track = Reader.ReadUInt32();
                        var address = Reader.ReadUInt32();
                        EntryPointByTrackID.Add(track, address);
                    }
                }
            }

            Reader.BaseStream.Seek(0, SeekOrigin.Begin);
            this.Data = Reader.ReadBytes((int)Reader.BaseStream.Length);

            Reader.Close();
        }

        public int FindBytePattern(Stream stream, byte[] pattern) 
        { //a simple pattern matcher
            stream.Seek(0, SeekOrigin.Begin);
            for (int i = 0; i < stream.Length; i++)
            {
                var b = stream.ReadByte();
                if (b == pattern[0])
                {
                    bool match = true;
                    for (int j = 1; j < pattern.Length; j++)
                    {
                        var b2 = stream.ReadByte();
                        if (b2 != pattern[j])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match) return (int)stream.Position;
                    else stream.Seek(i+1, SeekOrigin.Begin);
                }
            }
            return -1; //no match
        }
    }
}

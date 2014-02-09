/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the Iffinator.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using System.IO;
using tso.files.IFF;

namespace SimsLib.IFF
{
    /// <summary>
    /// Represents an *.iff file, which contains multiple chunks (that are like files in themselves).
    /// </summary>
    public class Iff
    {
        private string m_Path;
        private BinaryReader m_Reader;

        private List<IffChunk> m_Chunks = new List<IffChunk>();
        private List<PaletteMap> m_PMaps = new List<PaletteMap>();
        private List<DrawGroup> m_DGroups = new List<DrawGroup>();
        private List<SPR> m_SPRs = new List<SPR>();
        private List<SPR2Parser> m_SPR2s = new List<SPR2Parser>();
        private List<StringTable> m_StringTables = new List<StringTable>();
        private List<BHAV> m_BHAVs = new List<BHAV>();
        private List<OBJf> m_OBJfs = new List<OBJf>();
        private List<BCON> m_BCONs = new List<BCON>();
        private List<TTAB> m_TTABs = new List<TTAB>();
        private List<OBJD> m_OBJDs = new List<OBJD>();
        private List<FBMP> m_FBMPs = new List<FBMP>();
        private List<BMP_> m_BMP_s = new List<BMP_>();
        private List<FCNS> m_FCNSs = new List<FCNS>();

        //For generating random IDs for chunks.
        private UniqueRandom m_UniqueRnd = new UniqueRandom(1, 1000);

        /// <summary>
        /// The path of this IFF file.
        /// </summary>
        public string Path
        {
            get { return m_Path; }
            set { m_Path = value; }
        }

        /// <summary>
        /// The chunks in this IFF file.
        /// </summary>
        public List<IffChunk> Chunks
        {
            get { return m_Chunks; }
        }

        /// <summary>
        /// The SPR chunks in this IFF file.
        /// </summary>
        public List<SPR> SPRs
        {
            get { return m_SPRs; }
        }

        /// <summary>
        /// The SPR2 chunks in this IFF file.
        /// </summary>
        public List<SPR2Parser> SPR2s
        {
            get { return m_SPR2s; }
        }

        /// <summary>
        /// The drawgroup chunks in this IFF file.
        /// </summary>
        public List<DrawGroup> DrawGroups
        {
            get { return m_DGroups; }
        }
        
        /// <summary>
        /// The stringtable chunks in this IFF file.
        /// </summary>
        public List<StringTable> StringTables
        {
            get { return m_StringTables; }
        }

        /// <summary>
        /// The BHAV (BeHAVior) chunks in this IFF file.
        /// </summary>
        public List<BHAV> BHAVs
        {
            get { return m_BHAVs; }
        }

        /// <summary>
        /// The OBJf chunks in this IFF file.
        /// </summary>
        public List<OBJf> OBJfs
        {
            get { return m_OBJfs; }
        }

        /// <summary>
        /// The OBJD chunks in this IFF file.
        /// </summary>
        public List<OBJD> OBJDs
        {
            get { return m_OBJDs; }
        }

        /// <summary>
        /// Gets a specified SPR2 sprite from this IFF file.
        /// </summary>
        /// <param name="Index">The index of the sprite to retrieve.</param>
        /// <returns>A SPR2 sprite.</returns>
        public SPR2Parser GetSprite(int Index)
        {
            return m_SPR2s[Index];
        }

        /// <summary>
        /// Instantiates a new Iff class from a specified path.
        /// </summary>
        /// <param name="Path">The path to the archive to read.</param>
        public Iff(string Path)
        {
            m_Path = Path;
            m_Reader = new BinaryReader(File.Open(m_Path, FileMode.Open), Encoding.ASCII);

            ReadChunks();
            ParsePALTs();
            ParseSPRs();
            ParseSPR2s();
            ParseGroups();
            ParseStrings();
            ParseBHAVs();
            ParseOBJfs();
            ParseBCONs();
            ParseTTABs();
            ParseOBJDs();
            ParseFBMPs();
            ParseBMP_s();
            ParseFCNSs();


            ParseOBJDs();

            m_Reader.Close();
        }

        /// <summary>
        /// Instantiates a new Iff class from a byte array.
        /// </summary>
        /// <param name="Data">The byte array containing an *.iff file.</param>
        public Iff(byte[] Data)
        {
            MemoryStream MemStream = new MemoryStream(Data);
            m_Reader = new BinaryReader(MemStream, Encoding.ASCII);

            ReadChunks();
            ParsePALTs();
            ParseSPRs();
            ParseSPR2s();
            ParseGroups();
            ParseStrings();
            ParseBHAVs();
            ParseOBJfs();
            ParseBCONs();
            ParseTTABs();
            ParseFBMPs();
            ParseBMP_s();
            ParseFCNSs();


            ParseOBJDs();

            m_Reader.Close();
        }

        /// <summary>
        /// Instantiates a new Iff class from a stream.
        /// </summary>
        /// <param name="Data">The stream containing an *.iff file.</param>
        public Iff(Stream Data)
        {
            m_Reader = new BinaryReader(Data);

            ReadChunks();
            ParsePALTs();
            ParseSPRs();
            ParseSPR2s();
            ParseGroups();
            ParseStrings();
            ParseBHAVs();
            ParseOBJfs();
            ParseBCONs();
            ParseTTABs();
            ParseFBMPs();
            ParseBMP_s();
            ParseFCNSs();

            ParseOBJDs();

            m_Reader.Close();
        }

        /// <summary>
        /// Reads the chunks of the IFF-archive by looking for the RSMP.
        /// </summary>
        private void ReadChunks()
        {
            string Identifier = new string(m_Reader.ReadChars(60)).Replace("\0", "");

            if (Identifier != "IFF FILE 2.5:TYPE FOLLOWED BY SIZE JAMIE DOORNBOS & MAXIS 1")
            {
                throw new Exception("Invalid iff file!");
            }

            uint resMapOffset = Endian.SwapUInt32(m_Reader.ReadUInt32());

            Dictionary<string, List<uint>> files = new Dictionary<string, List<uint>>();

            if (resMapOffset != 0)
            {
                long pos = m_Reader.BaseStream.Position;
                m_Reader.BaseStream.Position = resMapOffset;

                m_Reader.BaseStream.Position += 76; //Skip the header.

                m_Reader.ReadInt32(); //Reserved
                uint version = m_Reader.ReadUInt32();
                m_Reader.ReadInt32(); //pmsr
                m_Reader.ReadInt32(); //Size
                uint typeCount = m_Reader.ReadUInt32(); //How many types are present in this *.iff...

                for (uint i = 0; i < typeCount; i++)
                {
                    //NOTE: For some types in some files this is empty...
                    string typeCode = new ASCIIEncoding().GetString(m_Reader.ReadBytes(4));

                    if (version == 0)
                    {
                        //Empty RSMP...
                        //numEntries + 1 entry without label = 13 bytes.
                        if ((m_Reader.BaseStream.Length - m_Reader.BaseStream.Position) < 13)
                        {
                            files.Clear();
                            FuckThisShit(ref files);
                            break;
                        }
                    }
                    else if (version == 1)
                    {
                        //Empty RSMP...
                        //numEntries + 1 entry without label = 16 bytes.
                        if ((m_Reader.BaseStream.Length - m_Reader.BaseStream.Position) < 16)
                        {
                            files.Clear();
                            FuckThisShit(ref files);
                            break;
                        }
                    }

                    //How many entries there are...
                    uint numEntries = m_Reader.ReadUInt32();

                    List<uint> offsets = new List<uint>();
                    for (uint j = 0; j < numEntries; j++)
                    {
                        if (version == 0)
                        {
                            //Empty RSMP...
                            //Minimum size for an entry without a label is 9 bytes.
                            if ((m_Reader.BaseStream.Length - m_Reader.BaseStream.Position) < ((numEntries - j) * 9))
                            {
                                files.Clear();
                                FuckThisShit(ref files);
                                break;
                            }
                        }
                        else if (version == 1)
                        {
                            //Empty RSMP...
                            //Minimum size for an entry without a label is 12 bytes.
                            if ((m_Reader.BaseStream.Length - m_Reader.BaseStream.Position) < ((numEntries - j) * 12))
                            {
                                files.Clear();
                                FuckThisShit(ref files);
                                break;
                            }
                        }

                        uint offset = m_Reader.ReadUInt32();
                        m_Reader.ReadInt16(); //ChunkID
                        if (version == 1) { m_Reader.ReadInt16(); } //ChunkID
                        m_Reader.ReadInt16(); //Flags
                        if (version == 1)
                        {
                            byte Length = m_Reader.ReadByte();
                            if (Length > 0)
                                m_Reader.ReadBytes(Length);
                        }
                        else
                        {
                            GetNameString();
                        }
                        offsets.Add(offset);
                    }

                    if (!files.ContainsKey(typeCode))
                        files.Add(typeCode, offsets);
                }

            }
            else //There was no offset to the resourcemap, meaning that an RSMP probably doesn't exist.
            {
                List<KeyValuePair<string, uint>> offsets = new List<KeyValuePair<string, uint>>();
                while (true)
                {
                    uint offset = (uint)m_Reader.BaseStream.Position;

                    byte[] TagBytes = m_Reader.ReadBytes(4);
                    Array.Reverse(TagBytes);
                    string tag = new ASCIIEncoding().GetString(TagBytes);

                    byte[] bytes = m_Reader.ReadBytes(4);

                    if (bytes.Length == 0)
                        break;

                    uint size = Endian.SwapUInt32(BitConverter.ToUInt32(bytes, 0));

                    m_Reader.BaseStream.Position += (size - 8);

                    if(!tag.Equals("XXXX"))
                        offsets.Add(new KeyValuePair<string, uint>(tag, offset));

                    //76 bytes is the size of a chunkheader, so don't bother reading the next one
                    //the stream has less than 76 bytes left.
                    if (m_Reader.BaseStream.Position == m_Reader.BaseStream.Length ||
                        (m_Reader.BaseStream.Length - m_Reader.BaseStream.Position) < 76)
                        break;
                }

                List<string> typesFound = new List<string>();

                foreach (KeyValuePair<string, uint> kvp in offsets)
                {
                    if (!typesFound.Exists(delegate(string s) { return s.CompareTo(kvp.Key) == 0; }))
                    {
                        List<KeyValuePair<string, uint>> theseChunks = offsets.FindAll(delegate(KeyValuePair<string, uint> pair) { return pair.Key.CompareTo(kvp.Key) == 0; });
                        List<uint> offsetValues = new List<uint>();
                        foreach (KeyValuePair<string, uint> kvp2 in theseChunks)
                        {
                            offsetValues.Add(kvp2.Value);
                        }

                        if (!files.ContainsKey(kvp.Key))
                            files.Add(kvp.Key, offsetValues);

                        typesFound.Add(kvp.Key);
                    }
                }
            }

            foreach (KeyValuePair<string, List<uint>> file in files)
            {
                foreach (int offset in file.Value)
                {
                    if (offset > 0)
                    {
                        m_Reader.BaseStream.Position = offset;

                        byte[] Buf = m_Reader.ReadBytes(4);
                        string StrResource = Encoding.ASCII.GetString(Buf);

                        if (StrResource == "SPR#" || StrResource == "SPR2" || StrResource == "rsmp" || StrResource == "PALT" ||
                            StrResource == "DGRP" || StrResource == "STR#" || StrResource == "BHAV" || StrResource == "FWAV" ||
                            StrResource == "CTSS" || StrResource == "TTAB" || StrResource == "TTAs" || StrResource == "OBJf" ||
                            StrResource == "BCON" || StrResource == "TPRP" || StrResource == "TMPL" || StrResource == "TRCN" ||
                            StrResource == "Optn" || StrResource == "SLOT" || StrResource == "GLOB" || StrResource == "FBMP" ||
                            StrResource == "BMP_" || StrResource == "FCNS" || StrResource == "OBJD")
                        {
                            //MessageBox.Show(StrResource);
                            IffChunk Chunk = ToChunk(StrResource, offset);
                            //i += (int)Chunk.Length;

                            m_Chunks.Add(Chunk);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// An archive had an empty rsmp, so fuck trying to read it
        /// and read all the chunkheaders instead.
        /// </summary>
        /// <param name="files">A list to fill with typetags (resourcename) and offsets.</param>
        private void FuckThisShit(ref Dictionary<string, List<uint>> files)
        {
            //IFF header is always 64 bytes - make absolutely sure we're at the right position in the file!
            m_Reader.BaseStream.Position = 64;

            List<KeyValuePair<string, uint>> offsets = new List<KeyValuePair<string, uint>>();
            while (true)
            {
                uint offset = (uint)m_Reader.BaseStream.Position;

                byte[] TagBytes = m_Reader.ReadBytes(4);
                Array.Reverse(TagBytes);
                string tag = new ASCIIEncoding().GetString(TagBytes);

                byte[] bytes = m_Reader.ReadBytes(4);

                if (bytes.Length == 0)
                    break;

                uint size = Endian.SwapUInt32(BitConverter.ToUInt32(bytes, 0));

                m_Reader.BaseStream.Position += (size - 8);

                if (!tag.Equals("XXXX"))
                    offsets.Add(new KeyValuePair<string, uint>(tag, offset));

                //76 bytes is the size of a chunkheader, so don't bother reading the next one
                //if the stream has less than 76 bytes left.
                if (m_Reader.BaseStream.Position == m_Reader.BaseStream.Length ||
                    (m_Reader.BaseStream.Length - m_Reader.BaseStream.Position) < 76)
                    break;
            }

            List<string> typesFound = new List<string>();

            foreach (KeyValuePair<string, uint> kvp in offsets)
            {
                if (!typesFound.Exists(delegate(string s) { return s.CompareTo(kvp.Key) == 0; }))
                {
                    List<KeyValuePair<string, uint>> theseChunks = offsets.FindAll(delegate(KeyValuePair<string, uint> pair) { return pair.Key.CompareTo(kvp.Key) == 0; });
                    List<uint> offsetValues = new List<uint>();

                    foreach (KeyValuePair<string, uint> kvp2 in theseChunks)
                    {
                        offsetValues.Add(kvp2.Value);
                    }

                    files.Add(kvp.Key, offsetValues);
                    typesFound.Add(kvp.Key);
                }
            }
        }

        private void ParsePALTs()
        {
            foreach (IffChunk Chunk in m_Chunks)
            {
                if (Chunk.Resource == "PALT")
                    m_PMaps.Add(new PaletteMap(Chunk));
            }
        }

        private void ParseSPRs()
        {
            if (m_PMaps.Count == 0)
                FindPALT();

            foreach (IffChunk Chunk in m_Chunks)
            {
                if (Chunk.Resource == "SPR#"){
                    var sprite = new SPR(Chunk);
                    m_SPRs.Add(sprite);
                }
                    
                    //m_SPRs.Add(new SPRParser(Chunk, m_PMaps));
            }
        }

        private void ParseSPR2s()
        {
            if (m_PMaps.Count == 0)
                FindPALT();

            foreach (IffChunk Chunk in m_Chunks)
            {
                if (Chunk.Resource == "SPR2")
                    m_SPR2s.Add(new SPR2Parser(Chunk, m_PMaps));
            }
        }

        private void ParseGroups()
        {
            foreach (IffChunk Chunk in m_Chunks)
            {
                if (Chunk.Resource == "DGRP")
                    m_DGroups.Add(new DrawGroup(Chunk, m_SPR2s));
            }
        }

        private void ParseStrings()
        {
            foreach (IffChunk Chunk in m_Chunks)
            {
                if (Chunk.Resource == "STR#" || Chunk.Resource ==  "TTAs" || 
                    Chunk.Resource == "CTSS" || Chunk.Resource == "CST")
                    m_StringTables.Add(new StringTable(Chunk));
            }
        }

        private void ParseBHAVs()
        {
            foreach (IffChunk Chunk in m_Chunks)
            {
                if (Chunk.Resource == "BHAV")
                    m_BHAVs.Add(new BHAV(Chunk));
            }
        }

        private void ParseOBJfs()
        {
            foreach (IffChunk Chunk in m_Chunks)
            {
                if (Chunk.Resource == "OBJf")
                    m_OBJfs.Add(new OBJf(Chunk));
            }
        }

        private void ParseOBJDs()
        {
            foreach (IffChunk Chunk in m_Chunks)
            {
                if (Chunk.Resource == "OBJD")
                {
                    ToOBJD(Chunk);
                }
            }
        }

        private void ParseBCONs()
        {
            foreach (IffChunk Chunk in m_Chunks)
            {
                if (Chunk.Resource == "BCON")
                    m_BCONs.Add(new BCON(Chunk));
            }
        }

        private void ParseTTABs()
        {
            foreach (IffChunk Chunk in m_Chunks)
            {
                if (Chunk.Resource == "TTAB")
                {
                    //m_TTABs.Add(new TTAB(Chunk));
                }
            }
        }

        private void ParseFBMPs()
        {
            foreach (IffChunk Chunk in m_Chunks)
            {
                if (Chunk.Resource == "FBMP")
                    m_FBMPs.Add(new FBMP(Chunk));
            }
        }

        private void ParseBMP_s()
        {
            foreach (IffChunk Chunk in m_Chunks)
            {
                if (Chunk.Resource == "FBMP")
                    m_BMP_s.Add(new BMP_(Chunk));
            }
        }

        private void ParseFCNSs()
        {
            foreach (IffChunk Chunk in m_Chunks)
            {
                if (Chunk.Resource == "FCNS")
                    m_FCNSs.Add(new FCNS(Chunk));
            }
        }

        /// <summary>
        /// Creates a chunk from a ResourceID and an offset in the IFF file.
        /// </summary>
        /// <param name="Resource">The ResourceID for the chunk.</param>
        /// <param name="offset">The offset for the chunk in the IFF file.</param>
        /// <returns>A new IffChunk instance.</returns>
        private IffChunk ToChunk(string Resource, int offset)
        {
            IffChunk Chunk = new IffChunk(Resource);

            Chunk.Length = Endian.SwapUInt32(m_Reader.ReadUInt32()) - 76;
            Chunk.ID = Endian.SwapUInt16(m_Reader.ReadUInt16());

            ushort Flags = Endian.SwapUInt16(m_Reader.ReadUInt16());
            Chunk.NameString = GetNameString();

            if ((m_Reader.BaseStream.Length - m_Reader.BaseStream.Position) >= Chunk.Length)
            {
                m_Reader.BaseStream.Position = offset + 76;
                Chunk.Data = m_Reader.ReadBytes((int)Chunk.Length);
            }
            else
                Chunk.Data = new byte[Chunk.Length];

            return Chunk;
        }

        /// <summary>
        /// Because they couldn't decide on a string length, some guy apparently thought
        /// it was OK to assume a string length of 64, and zero-terminate if the string
        /// happened to be shorter...
        /// </summary>
        /// <returns>The string read from a chunk-header.</returns>
        private string GetNameString()
        {
            char Chr;
            bool IsZeroTerminated = false;
            int i;

            for (i = 0; i < 63; i++)
            {
                Chr = (char)m_Reader.PeekChar();

                if (Chr == '\0')
                {
                    IsZeroTerminated = true;
                    break;
                }
            }

            if (IsZeroTerminated)
                return new string(m_Reader.ReadChars(i));
            else
                return Encoding.ASCII.GetString(m_Reader.ReadBytes(64));
        }

        /// <summary>
        /// A PALT (palette) chunk was not found when searching through this archive's rsmp,
        /// so find it manually.
        /// </summary>
        private void FindPALT()
        {
            m_Reader.BaseStream.Position = 64;

            List<KeyValuePair<string, uint>> PALTOffsets = new List<KeyValuePair<string, uint>>();
            while (true)
            {
                uint offset = (uint)m_Reader.BaseStream.Position;

                byte[] TagBytes = m_Reader.ReadBytes(4);
                Array.Reverse(TagBytes);
                string tag = new ASCIIEncoding().GetString(TagBytes);

                byte[] bytes = m_Reader.ReadBytes(4);

                if (bytes.Length == 0)
                    break;

                uint size = Endian.SwapUInt32(BitConverter.ToUInt32(bytes, 0));

                m_Reader.BaseStream.Position += (size - 8);

                if (tag.Equals("PALT"))
                    PALTOffsets.Add(new KeyValuePair<string, uint>(tag, offset));

                //76 bytes is the size of a chunkheader, so don't bother reading the next one
                //the stream has less than 76 bytes left.
                if (m_Reader.BaseStream.Position == m_Reader.BaseStream.Length ||
                    (m_Reader.BaseStream.Length - m_Reader.BaseStream.Position) < 76)
                    break;
            }

            foreach (KeyValuePair<string, uint> KVP in PALTOffsets)
            {
                m_Reader.BaseStream.Position = KVP.Value;

                IffChunk Chunk = new IffChunk(KVP.Key);

                Chunk.Length = Endian.SwapUInt32(m_Reader.ReadUInt32()) - 76;
                Chunk.ID = Endian.SwapUInt16(m_Reader.ReadUInt16());

                ushort Flags = Endian.SwapUInt16(m_Reader.ReadUInt16());
                Chunk.NameString = GetNameString();

                if ((m_Reader.BaseStream.Length - m_Reader.BaseStream.Position) >= Chunk.Length)
                {
                    m_Reader.BaseStream.Position = KVP.Value + 76;
                    Chunk.Data = m_Reader.ReadBytes((int)Chunk.Length);
                }
                else
                    Chunk.Data = new byte[Chunk.Length];

                m_PMaps.Add(new PaletteMap(Chunk));
            }
        }

        /// <summary>
        /// Casts a chunk to an OBJD instance.
        /// </summary>
        /// <param name="Chunk">The chunk to cast.</param>
        private void ToOBJD(IffChunk Chunk)
        {
            OBJD Obj = new OBJD(Chunk.Data, Chunk.ID);
            m_OBJDs.Add(Obj);
        }
    }
}
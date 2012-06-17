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

namespace SimsLib.IFF
{
    /// <summary>
    /// A string stored in a StringTable.
    /// </summary>
    public struct StringTableString
    {
        public byte LanguageCode;
        public byte Length;
        public string Str;
        public string Str2;
    }

    /// <summary>
    /// Used to store a set of strings.
    /// A StringTable only contains StringSets if
    /// the formatcode is 0xFCFF.
    /// </summary>
    public class StringSet
    {
        public int NumEntries;
        public List<StringTableString> Strings = new List<StringTableString>();
    }

    /// <summary>
    /// StringTable used to store random strings within an *.iff file.
    /// </summary>
    public class StringTable : IffChunk
    {
        private ushort m_FormatCode;
        //Only used when the FormatCode is 0xFCFF, which is only found in TSO.
        private byte m_NumSets;
        private ushort m_NumEntries;
        private List<StringTableString> m_Strings = new List<StringTableString>();
        //This list is only used when the FormatCode is 0xFCFF, which is only found in TSO.
        private List<StringSet> m_StringSets = new List<StringSet>();

        /// <summary>
        /// The code determining the format
        /// of this StringTable.
        /// </summary>
        public int FormatCode
        {
            get { return m_FormatCode; }
        }

        /// <summary>
        /// The strings in this StringTable.
        /// Might be empty, depending on the format of this StringTable.
        /// </summary>
        public List<StringTableString> Strings
        {
            get { return m_Strings; }
        }

        /// <summary>
        /// The list of stringsets within this StringTable.
        /// Might be empty, depending on the format of this StringTable.
        /// </summary>
        public List<StringSet> StringSets
        {
            get { return m_StringSets; }
        }

        /// <summary>
        /// The resourcetype for this StringTable.
        /// If this is equal to "CTSS", this is a CaTalog
        /// String DeScription and contains 1 set, 2 entries.
        /// If this is equal to  "TTAs", then it is a set of
        /// strings for pie-menu interactions.
        /// </summary>
        public string ResourceType
        {
            get { return Resource; }
        }

        public StringTable(IffChunk Chunk) : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);

            //Another example of the grossness of this format; random use of big-endian numbers...
            byte[] FormatCodeBuf = Reader.ReadBytes(2);
            Array.Reverse(FormatCodeBuf);

            m_FormatCode = BitConverter.ToUInt16(FormatCodeBuf, 0);

            switch (m_FormatCode)
            {
                case 0:
                    //Some tables are empty... LITERALLY!
                    if (Reader.BaseStream.Position < Reader.BaseStream.Length)
                    {
                        m_NumEntries = Reader.ReadUInt16();

                        for (int i = 0; i < m_NumEntries; i++)
                        {
                            StringTableString Str = new StringTableString();

                            Str.Str = ReadPascalString(Reader);

                            m_Strings.Add(Str);
                        }
                    }

                    break;
                case 0xFFFF:
                    m_NumEntries = Reader.ReadUInt16();

                    for (int i = 0; i < m_NumEntries; i++)
                    {
                        StringTableString Str = new StringTableString();

                        char C;
                        StringBuilder SB = new StringBuilder();

                        while (true)
                        {
                            C = Reader.ReadChar();
                            SB.Append(C);

                            if (C == '\0')
                                break;
                        }

                        Str.Str = SB.ToString();
                        m_Strings.Add(Str);
                    }

                    break;
                case 0xFEFF:
                    m_NumEntries = Reader.ReadUInt16();

                    for (int i = 0; i < m_NumEntries; i++)
                    {
                        StringTableString Str = new StringTableString();

                        char C;
                        StringBuilder SB = new StringBuilder();

                        //String
                        while (true)
                        {
                            C = Reader.ReadChar();
                            SB.Append(C);

                            if (C == '\0')
                                break;
                        }

                        Str.Str = SB.ToString();
                        m_Strings.Add(Str);
                        SB = new StringBuilder();

                        //Comment
                        while (true)
                        {
                            C = Reader.ReadChar();
                            SB.Append(C);

                            if (C == '\0')
                                break;
                        }
                    }

                    break;
                case 0xFDFF:
                    m_NumEntries = Reader.ReadUInt16();

                    for (int i = 0; i < m_NumEntries; i++)
                    {
                        StringTableString Str = new StringTableString();
                        Str.LanguageCode = Reader.ReadByte();

                        char C;
                        StringBuilder SB = new StringBuilder();

                        while (true)
                        {
                            C = (char)Reader.ReadByte();

                            if (C == '\0')
                                break;

                            SB.Append(C);
                        }

                        Str.Str = SB.ToString();

                        C = new char();
                        SB = new StringBuilder();

                        while (true)
                        {
                            C = (char)Reader.ReadByte();

                            if (C == '\0')
                                break;

                            SB.Append(C);
                        }

                        Str.Str2 = SB.ToString();


                        m_Strings.Add(Str);
                    }

                    break;

                case 0xFCFF: //Only found in TSO-files!
                    m_NumSets = Reader.ReadByte();

                    //if (!m_IsCTSS)
                    //{
                    if (m_NumSets >= 1)
                    {
                        for (int i = 0; i < m_NumSets; i++)
                        {
                            StringSet Set = new StringSet();

                            Set.NumEntries = Reader.ReadInt16();

                            for (int j = 0; j < Set.NumEntries; j++)
                            {
                                // string code, then two specially-counted strings
                                // for some reason, the language code is one below the
                                // documented values.  we adjust this here, which
                                // unfortunately makes non-translated strings strange.
                                StringTableString Str = new StringTableString();
                                Str.LanguageCode = (byte)(Reader.ReadByte() + 1);

                                Str.Str = ReadPascalString1(Reader);
                                Str.Str2 = ReadPascalString1(Reader);

                                Set.Strings.Add(Str);
                            }

                            m_StringSets.Add(Set);
                        }
                    }
                    /*}
                    else
                    {
                        StringSet Set = new StringSet();

                        for (int i = 0; i < 2; i++)
                        {
                            StringTableString Str = new StringTableString();
                            Str.LanguageCode = (byte)(Reader.ReadByte() + 1);

                            Str.Str = ReadZeroString(Reader);
                            Reader.ReadByte();
                            Str.Str2 = ReadZeroString(Reader);

                            Set.Strings.Add(Str);
                        }
                    }*/

                    break;
            }

            Reader.Close();
        }

        private string ReadZeroString(BinaryReader Reader)
        {
            StringBuilder SB = new StringBuilder();
            char[] Chrs = new char[2];

            while (true)
            {
                Chrs = Reader.ReadChars(2);
                SB.Append(Chrs);

                if (new string(Chrs) == "\0\0")
                    break;
            }
            //if (Reader.BaseStream.Position % 4 != 0)
            //   Reader.ReadByte();

            return SB.ToString();
        }

        private string ReadPascalString(BinaryReader Reader)
        {
            byte Length = Reader.ReadByte();

            if (Length == 0)
                return "";
            else
                return new string(Reader.ReadChars(Length));
        }

        private string ReadPascalString1(BinaryReader Reader)
        {
            /*byte Length = Reader.ReadByte();

            if ((Length & 0x80) == 0x80)
            {
                byte Compressed = Reader.ReadByte();
                Length = (byte)((Length - 128) + (Compressed << 7));
            }

            if (Length == 0)
                return "";
            else
                return new string(Reader.ReadChars(Length));*/
            return Reader.ReadString();
        }
    }
}

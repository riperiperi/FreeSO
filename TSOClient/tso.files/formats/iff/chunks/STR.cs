/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.Files.Utils;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// This chunk type holds text strings. 
    /// The first two bytes correspond to the format code, of which there are four types. 
    /// Some chunks in the game do not specify any data after the version number, so be sure to implement bounds checking.
    /// </summary>
    public class STR : IffChunk
    {
        public static string[] LanguageSetNames =
        {
            "English (US)",
            "English (UK)",
            "French",
            "German",
            "Italian",
            "Spanish",
            "Dutch",
            "Danish",
            "Swedish",
            "Norwegian",
            "Finish",
            "Hebrew",
            "Russian",
            "Portuguese",
            "Japanese",
            "Polish",
            "Simplified Chinese",
            "Traditional Chinese",
            "Thai",
            "Korean"
        };

        public STRItem[] Strings = null;
        public STRLanguageSet[] LanguageSets = new STRLanguageSet[20];

        public STR()
        {
            for (int i = 0; i < 20; i++) LanguageSets[i] = new STRLanguageSet { Strings = new STRItem[0] };
        }

        /// <summary>
        /// How many strings are in this chunk?
        /// </summary>
        public int Length
        {
            get
            {
                if (LanguageSets != null)
                {
                    return LanguageSets[0].Strings.Length;
                }
                else if (Strings != null)
                {
                    return Strings.Length;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets a string from this chunk.
        /// </summary>
        /// <param name="index">Index of string.</param>
        /// <returns>A string at specific index, null if not found.</returns>
        public string GetString(int index)
        {
            var item = GetStringEntry(index);
            if (item != null)
            {
                return item.Value;
            }
            return null;
        }

        public void SetString(int index, string value)
        {
            if (Strings != null && index < Strings.Length)
            {
                Strings[index].Value = value;
            }
            if (LanguageSets != null)
            {
                var languageSet = LanguageSets[0];
                if (index < languageSet.Strings.Length)
                {
                    languageSet.Strings[index].Value = value;
                }
            }
        }

        public void InsertString(int index, STRItem item)
        {
            if (Strings != null && index < Strings.Length)
            {
                var newStr = new STRItem[Strings.Length + 1];
                if (index == -1) index = 0;
                Array.Copy(Strings, newStr, index); //copy before strings
                newStr[index] = item;
                Array.Copy(Strings, index, newStr, index + 1, (Strings.Length - index));
                Strings = newStr;
            }
            if (LanguageSets != null)
            {
                var languageSet = LanguageSets[0];
                var newStr = new STRItem[languageSet.Strings.Length + 1];
                Array.Copy(languageSet.Strings, newStr, index); //copy before strings
                newStr[index] = item;
                Array.Copy(languageSet.Strings, index, newStr, index + 1, (languageSet.Strings.Length - index));
                languageSet.Strings = newStr;
            }
        }

        public void RemoveString(int index)
        {
            if (Strings != null && index < Strings.Length)
            {
                var newStr = new STRItem[Strings.Length - 1];
                Array.Copy(Strings, newStr, index); //copy before strings
                Array.Copy(Strings, index+1, newStr, index, (Strings.Length - (index+1))); //copy after strings
                Strings = newStr;
            }
            if (LanguageSets != null)
            {
                var languageSet = LanguageSets[0];
                var newStr = new STRItem[languageSet.Strings.Length - 1];
                Array.Copy(languageSet.Strings, newStr, index); //copy before strings
                Array.Copy(languageSet.Strings, index+1, newStr, index, (languageSet.Strings.Length - (index+1)));
                languageSet.Strings = newStr;
            }
        }

        /// <summary>
        /// Gets a STRItem instance from this STR chunk.
        /// </summary>
        /// <param name="index">Index of STRItem.</param>
        /// <returns>STRItem at index, null if not found.</returns>
        public STRItem GetStringEntry(int index)
        {
            if (Strings != null && index < Strings.Length)
            {
                return Strings[index];
            }
            if (LanguageSets != null)
            {
                var languageSet = LanguageSets[0];
                if (index < languageSet.Strings.Length)
                {
                    return languageSet.Strings[index];
                }
            }
            return null;
        }

        /// <summary>
        /// Reads a STR chunk from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream object holding a STR chunk.</param>
        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var formatCode = io.ReadInt16();
                Strings = null;
                LanguageSets = null;
                if (!io.HasMore){ return; }

                if (formatCode == 0)
                {
                    var numStrings = io.ReadUInt16();
                    Strings = new STRItem[numStrings];
                    for (var i = 0; i < numStrings; i++)
                    {
                        Strings[i] = new STRItem
                        {
                            Value = io.ReadPascalString()
                        };
                    }
                }
                //This format changed 00 00 to use C strings rather than Pascal strings.
                else if (formatCode == -1)
                {
                    var numStrings = io.ReadUInt16();
                    Strings = new STRItem[numStrings];
                    for (var i = 0; i < numStrings; i++)
                    {
                        Strings[i] = new STRItem
                        {
                            Value = io.ReadNullTerminatedString()
                        };
                    }
                }
                //This format changed FF FF to use string pairs rather than single strings.
                else if (formatCode == -2)
                {
                    var numStrings = io.ReadUInt16();
                    Strings = new STRItem[numStrings];
                    for (var i = 0; i < numStrings; i++)
                    {
                        Strings[i] = new STRItem
                        {
                            Value = io.ReadNullTerminatedString(),
                            Comment = io.ReadNullTerminatedString()
                        };
                    }
                }
                //This format changed FD FF to use a language code.
                else if (formatCode == -3)
                {
                    var numStrings = io.ReadUInt16();
                    Strings = new STRItem[numStrings];
                    for (var i = 0; i < numStrings; i++)
                    {
                        Strings[i] = new STRItem
                        {
                            LanguageCode = (byte)(io.ReadByte() + 1),
                            Value = io.ReadNullTerminatedString(),
                            Comment = io.ReadNullTerminatedString()
                        };
                    }
                }
                //This format is only used in The Sims Online. The format is essentially a performance improvement: 
                //it counteracts both the short string limit of 255 characters found in 00 00 and the inherent slowness 
                //of null-terminated strings in the other formats (which requires two passes over each string), and it 
                //also provides a string pair count for each language set which eliminates the need for two passes over 
                //each language set.
                else if (formatCode == -4)
                {
                    var numLanguageSets = io.ReadByte();
                    this.LanguageSets = new STRLanguageSet[numLanguageSets];

                    for(var i=0; i < numLanguageSets; i++)
                    {
                        var item = new STRLanguageSet();
                        var numStringPairs = io.ReadUInt16();
                        item.Strings = new STRItem[numStringPairs];
                        for (var x = 0; x < numStringPairs; x++)
                        {
                            item.Strings[x] = new STRItem 
                            {
                                LanguageCode = (byte)(io.ReadByte() + 1),
                                Value = io.ReadVariableLengthPascalString(),
                                Comment = io.ReadVariableLengthPascalString()
                            };
                        }
                        this.LanguageSets[i] = item;
                    }
                }
            }
        }
        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteInt16(-4);
                io.WriteByte(20);

                if (LanguageSets == null)
                {
                    return false; //format FDFF is REALLY broken. Should be using UTF-8
                    io.WriteUInt16((ushort)Strings.Length);
                    foreach (var str in Strings)
                    {
                        io.WriteByte((byte)(str.LanguageCode - 1));
                        io.WriteVariableLengthPascalString(str.Value);
                        io.WriteVariableLengthPascalString(str.Comment);
                    }
                    for (int i = 1; i < 20; i++) io.WriteUInt16(0); //other language sets are empty.
                }
                else
                {
                    foreach (var set in LanguageSets)
                    {
                        io.WriteUInt16((ushort)set.Strings.Length);
                        foreach (var str in set.Strings)
                        {
                            io.WriteByte((byte)(str.LanguageCode - 1));
                            io.WriteVariableLengthPascalString(str.Value);
                            io.WriteVariableLengthPascalString(str.Comment);
                        }
                    }
                }

                return true;
            }
        }
    }

    /// <summary>
    /// Item in a STR chunk.
    /// </summary>
    public class STRItem
    {
        public byte LanguageCode;
        public string Value;
        public string Comment;
    }

    public enum STRLangCode : byte
    {
        Default = 0,
        EnglishUS = 1,
        EnglishUK = 2,
        French = 3,
        German = 4,
        Italian = 5,
        Spanish = 6,
        Dutch = 7,
        Danish = 8,
        Swedish = 9,
        Norwegian = 10,
        Finish = 11,
        Hebrew = 12,
        Russian = 13,
        Portuguese = 14,
        Japanese = 15,
        Polish = 16,
        SimplifiedChinese = 17,
        TraditionalChinese = 18,
        Thai = 19,
        Korean = 20
    }

    /// <summary>
    /// Set of STRItems for a language.
    /// </summary>
    public class STRLanguageSet
    {
        public STRItem[] Strings = new STRItem[0];
    }
}

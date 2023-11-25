using System;
using System.Collections.Generic;
using System.Linq;
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
            "Korean",
            "Slovak"
        };

        public STRLanguageSet[] LanguageSets = new STRLanguageSet[20];
        public static STRLangCode DefaultLangCode = STRLangCode.EnglishUS;

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
                return LanguageSets[0]?.Strings.Length ?? 0;
            }
        }

        public STRLanguageSet GetLanguageSet(STRLangCode set)
        {
            if (set == STRLangCode.Default) set = DefaultLangCode;
            int code = (int)set;
            if ((LanguageSets[code-1]?.Strings.Length ?? 0) == 0) return LanguageSets[0]; //if undefined, fallback to English US
            else return LanguageSets[code-1];
        }

        public bool IsSetInit(STRLangCode set)
        {
            if (set == STRLangCode.Default) set = DefaultLangCode;
            if (set == STRLangCode.EnglishUS) return true;
            int code = (int)set;
            return (LanguageSets[code - 1].Strings.Length > 0);
        }

        public void InitLanguageSet(STRLangCode set)
        {
            if (set == STRLangCode.Default) set = DefaultLangCode;
            int code = (int)set;
            var length = LanguageSets[0].Strings.Length;
            LanguageSets[code - 1].Strings = new STRItem[length];
            for (int i=0; i< length; i++)
            {
                var src = LanguageSets[0].Strings[i];
                LanguageSets[code - 1].Strings[i] = new STRItem()
                {
                    LanguageCode = (byte)code,
                    Value = src.Value,
                    Comment = src.Comment
                };
            }
        }

        /// <summary>
        /// Gets a string from this chunk.
        /// </summary>
        /// <param name="index">Index of string.</param>
        /// <returns>A string at specific index, null if not found.</returns>
        /// 
        public string GetString(int index)
        {
            return GetString(index, STRLangCode.Default);
        }
        public string GetString(int index, STRLangCode language)
        {
            var item = GetStringEntry(index, language);
            if (item != null)
            {
                return item.Value;
            }
            return null;
        }

        public string GetComment(int index)
        {
            return GetComment(index, STRLangCode.Default);
        }
        public string GetComment(int index, STRLangCode language)
        {
            var item = GetStringEntry(index, language);
            if (item != null)
            {
                return item.Comment;
            }
            return null;
        }

        public void SetString(int index, string value)
        {
            SetString(index, value, STRLangCode.Default);
        }
        public void SetString(int index, string value, STRLangCode language)
        {
            var languageSet = GetLanguageSet(language);
            if (index < languageSet.Strings.Length)
            {
                languageSet.Strings[index].Value = value;
            }
        }

        public void SwapString(int srcindex, int dstindex)
        {
            foreach (var languageSet in LanguageSets)
            {
                if (languageSet.Strings.Length == 0) continue; //language not initialized
                var temp = languageSet.Strings[srcindex];
                languageSet.Strings[srcindex] = languageSet.Strings[dstindex];
                languageSet.Strings[dstindex] = temp;
            }
        }

        public void InsertString(int index, STRItem item)
        {
            byte i = 1;
            foreach (var languageSet in LanguageSets) {
                if (languageSet.Strings.Length == 0 && i > 1)
                {
                    i++;
                    continue; //language not initialized
                }
                var newStr = new STRItem[languageSet.Strings.Length + 1];
                Array.Copy(languageSet.Strings, newStr, index); //copy before strings
                newStr[index] = new STRItem()
                {
                    LanguageCode = i,
                    Value = item.Value,
                    Comment = item.Comment
                };
                Array.Copy(languageSet.Strings, index, newStr, index + 1, (languageSet.Strings.Length - index));
                languageSet.Strings = newStr;
                i++;
            }
        }

        public void RemoveString(int index)
        {
            foreach (var languageSet in LanguageSets)
            {
                if (languageSet.Strings.Length == 0) continue; //language not initialized
                var newStr = new STRItem[languageSet.Strings.Length - 1];
                Array.Copy(languageSet.Strings, newStr, index); //copy before strings
                Array.Copy(languageSet.Strings, index + 1, newStr, index, (languageSet.Strings.Length - (index + 1)));
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
            return GetStringEntry(index, STRLangCode.Default);
        }
        public STRItem GetStringEntry(int index, STRLangCode language)
        {
            var languageSet = GetLanguageSet(language);
            if (index < (languageSet?.Strings.Length ?? 0) && index > -1)
            {
                return languageSet.Strings[index];
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
                LanguageSets = new STRLanguageSet[20];
                if (!io.HasMore){
                    for (int i = 0; i < 20; i++) LanguageSets[i] = new STRLanguageSet { Strings = new STRItem[0] };
                    return;
                }

                if (formatCode == 0)
                {
                    var numStrings = io.ReadUInt16();
                    for (int i = 0; i < 20; i++) LanguageSets[i] = new STRLanguageSet { Strings = new STRItem[0] };
                    LanguageSets[0].Strings = new STRItem[numStrings];
                    for (var i = 0; i < numStrings; i++)
                    {
                        LanguageSets[0].Strings[i] = new STRItem
                        {
                            Value = io.ReadPascalString()
                        };
                    }
                }
                //This format changed 00 00 to use C strings rather than Pascal strings.
                else if (formatCode == -1)
                {
                    var numStrings = io.ReadUInt16();
                    for (int i = 0; i < 20; i++) LanguageSets[i] = new STRLanguageSet { Strings = new STRItem[0] };
                    LanguageSets[0].Strings = new STRItem[numStrings];
                    for (var i = 0; i < numStrings; i++)
                    {
                        LanguageSets[0].Strings[i] = new STRItem
                        {
                            Value = io.ReadNullTerminatedUTF8()
                        };
                    }
                }
                //This format changed FF FF to use string pairs rather than single strings.
                else if (formatCode == -2)
                {
                    var numStrings = io.ReadUInt16();
                    for (int i = 0; i < 20; i++) LanguageSets[i] = new STRLanguageSet { Strings = new STRItem[0] };
                    LanguageSets[0].Strings = new STRItem[numStrings];
                    for (var i = 0; i < numStrings; i++)
                    {
                        LanguageSets[0].Strings[i] = new STRItem
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
                    for (int i = 0; i < 20; i++) LanguageSets[i] = new STRLanguageSet { Strings = new STRItem[0] };
                    List<STRItem>[] LangSort = new List<STRItem>[20];
                    for (var i = 0; i < numStrings; i++)
                    {
                        var item = new STRItem
                        {
                            LanguageCode = io.ReadByte(),
                            Value = io.ReadNullTerminatedString(),
                            Comment = io.ReadNullTerminatedString()
                        };

                        var lang = item.LanguageCode;
                        if (lang == 0) lang = 1;
                        else if (lang < 0 || lang > 20) continue; //???
                        if (LangSort[lang - 1] == null)
                        {
                            LangSort[lang-1] = new List<STRItem>();
                        }

                        LangSort[lang - 1].Add(item);
                    }
                    for (int i=0; i<LanguageSets.Length; i++)
                    {
                        if (LangSort[i] != null) LanguageSets[i].Strings = LangSort[i].ToArray();
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
                if (IffFile.TargetTS1)
                {
                    // TS1 format - null terminated string
                    io.WriteInt16(-3);
                    var total = (short)LanguageSets.Sum(x => x?.Strings?.Length ?? 0);
                    io.WriteInt16(total);
                    foreach (var set in LanguageSets)
                    {
                        if (set?.Strings != null)
                        {
                            foreach (var str in set.Strings)
                            {
                                io.WriteByte((byte)(str.LanguageCode));
                                io.WriteNullTerminatedString(str.Value);
                                io.WriteNullTerminatedString(str.Comment);
                            }
                        }
                    }
                    for (int i=0; i<total; i++)
                    {
                        io.WriteByte(0xA3);
                    }
                }
                else
                {
                    // TSO format - variable length pascal
                    io.WriteInt16(-4);
                    io.WriteByte(20);

                    foreach (var set in LanguageSets)
                    {
                        if (set?.Strings == null)
                        {
                            io.WriteInt16(0);
                        }
                        else
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

        public STRItem()
        {

        }

        public STRItem(string value)
        {
            Value = value;
            Comment = "";
        }
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
        Korean = 20,

        //begin freeso
        Slovak = 21
    }

    /// <summary>
    /// Set of STRItems for a language.
    /// </summary>
    public class STRLanguageSet
    {
        public STRItem[] Strings = new STRItem[0];
    }
}

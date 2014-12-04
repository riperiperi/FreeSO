/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSO.Files.utils;

namespace TSO.Files.formats.iff.chunks
{
    /// <summary>
    /// This chunk type holds text strings. 
    /// The first two bytes correspond to the format code, of which there are four types. 
    /// Some chunks in the game do not specify any data after the version number, so be sure to implement bounds checking.
    /// </summary>
    public class STR : IffChunk
    {
        public STRItem[] Strings;
        public STRLanguageSet[] LanguageSets;

        /// <summary>
        /// How many strings are in this chunk?
        /// </summary>
        public int Length
        {
            get
            {
                if (Strings != null)
                {
                    return Strings.Length;
                }
                else if(LanguageSets != null)
                {
                    return LanguageSets[0].Strings.Length;
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
        public override void Read(Iff iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var formatCode = io.ReadInt16();
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

    /// <summary>
    /// Set of STRItems for a language.
    /// </summary>
    public class STRLanguageSet
    {
        public STRItem[] Strings;
    }
}

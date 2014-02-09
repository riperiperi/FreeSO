/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SimsLib.IFF
{
    /// <summary>
    /// This chunk type holds text strings.
    /// </summary>
    public class STR : AbstractIffChunk
    {
        public STRItem[] Strings;
        public STRLanguageSet[] LanguageSets;

        public int Length
        {
            get
            {
                if (Strings != null)
                {
                    return Strings.Length;
                }
                else if (LanguageSets != null)
                {
                    return LanguageSets[0].Strings.Length;
                }
                return 0;
            }
        }

        public string GetString(int index)
        {
            var item = GetStringEntry(index);
            if (item != null)
            {
                return item.Value;
            }
            return null;
        }

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

        public override void Read(Iff iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var formatCode = io.ReadInt16();
                if (!io.HasMore) { return; }

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
                else if (formatCode == -4)
                {
                    var numLanguageSets = io.ReadByte();
                    this.LanguageSets = new STRLanguageSet[numLanguageSets];

                    for (var i = 0; i < numLanguageSets; i++)
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

    public class STRItem
    {
        public byte LanguageCode;
        public string Value;
        public string Comment;
    }

    public class STRLanguageSet
    {
        public STRItem[] Strings;
    }
}
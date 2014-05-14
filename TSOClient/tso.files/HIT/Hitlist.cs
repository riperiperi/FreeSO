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

namespace TSO.Files.HIT
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

            }
            catch
            {
                Reader.BaseStream.Seek(4, SeekOrigin.Begin); //attempt 3rd mystery format, count+int32
                for (int i = 0; i < VerOrCount; i++)
                    IDs.Add(Reader.ReadUInt32());
            }
        }

        /// <summary>
        /// Creates a new hitlist.
        /// </summary>
        /// <param name="Filepath">The path to the hitlist to read.</param>
        public Hitlist(string Filepath)
        {
            Read(File.Open(Filepath, FileMode.Open));
        }
    }
}

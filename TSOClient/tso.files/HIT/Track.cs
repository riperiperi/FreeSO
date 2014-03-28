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
    /// TRK is a CSV format that defines a HIT track.
    /// </summary>
    public class Track
    {
        public string MagicNumber;
        public uint Unknown1;
        public string Name;
        public uint SoundID;
        public uint TrackID;
        public uint ArgType;

        /// <summary>
        /// Creates a new track.
        /// </summary>
        /// <param name="Filedata">The data to create the track from.</param>
        public Track(byte[] Filedata)
        {
            BinaryReader Reader = new BinaryReader(new MemoryStream(Filedata));

            string data = new string(Reader.ReadChars(Filedata.Length));
            string[] Values = data.Split(',');

            MagicNumber = Values[0];
            Unknown1 = ParseHexString(Values[1]);
            Name = Values[2];
            SoundID = ParseHexString(Values[3]);
            TrackID = ParseHexString(Values[4]);
            if (Values[5] != "\r\n") //some tracks terminate here...
            {
                ArgType = ParseHexString(Values[5]);
            }

            Reader.Close();
        }

        private uint ParseHexString(string input)
        {
            if (input == "") return 0;
            if (input.StartsWith("0x")) input = input.Substring(2);

            if (input.Length == 8) //not really any reliable way of dealing with this...
            {
                return Convert.ToUInt32(input, 16);
            }
            else
            {
                return Convert.ToUInt32(input);
            }
        }
    }
}

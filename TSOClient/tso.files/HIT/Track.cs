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
        private bool TWODKT = false; //Optional encoding as Pascal string, typical Maxis...
        public string MagicNumber;
        public uint Version;
        public string TrackName;
        public uint SoundID;
        public uint TrackID;
        public HITArgs ArgType;
        public HITControlGroups ControlGroup;
        public HITDuckingPriorities DuckingPriority;
        public uint Looped;
        public uint Volume;

        /// <summary>
        /// Creates a new track.
        /// </summary>
        /// <param name="Filedata">The data to create the track from.</param>
        public Track(byte[] Filedata)
        {
            BinaryReader Reader = new BinaryReader(new MemoryStream(Filedata));

            MagicNumber = new string(Reader.ReadChars(4));

            if(MagicNumber == "2DKT")
                TWODKT = true;

            int CurrentVal = 8;
            string data;

            if(!TWODKT)
                data = new string(Reader.ReadChars(Filedata.Length));
            else
                data = new string(Reader.ReadChars(Reader.ReadInt32()));
            string[] Values = data.Split(',');

            //MagicNumber = Values[0];
            Version = ParseHexString(Values[1]);
            TrackName = Values[2];
            SoundID = ParseHexString(Values[3]);
            TrackID = ParseHexString(Values[4]);
            if (Values[5] != "\r\n" && Values[5] != "ETKD" && Values[5] != "") //some tracks terminate here...
            {
                ArgType = (HITArgs)ParseHexString(Values[5]);
                ControlGroup = (HITControlGroups)ParseHexString(Values[7]);

                if (Version == 2)
                    CurrentVal++;

                CurrentVal += 2;

                DuckingPriority = (HITDuckingPriorities)ParseHexString(Values[CurrentVal]);
                CurrentVal++;
                Looped = ParseHexString(Values[CurrentVal]);
                CurrentVal++;
                Volume = ParseHexString(Values[CurrentVal]);
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
                try
                {
                    return Convert.ToUInt32(input);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }
    }
}

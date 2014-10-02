/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
RHY3756547. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace TSO.Files.HIT
{
    /// <summary>
    /// EVT is a CSV format that defines a list of events for HIT to listen for.
    /// </summary>
    public class EVT
    {
        public List<EVTEntry> Entries;

        /// <summary>
        /// Creates a new evt file.
        /// </summary>
        /// <param name="Filedata">The data to create the evt from.</param>
        public EVT(byte[] Filedata)
        {
            ReadFile(new MemoryStream(Filedata));
        }

        /// <summary>
        /// Creates a new evt file.
        /// </summary>
        /// <param name="Filedata">The path to the data to create the evt from.</param>
        public EVT(string Filepath)
        {
            ReadFile(File.Open(Filepath, FileMode.Open));
        }

        private void ReadFile(Stream data)
        {
            Entries = new List<EVTEntry>();
            BinaryReader Reader = new BinaryReader(data);

            string CommaSeparatedValues = new string(Reader.ReadChars((int)data.Length));
            string[] Lines = CommaSeparatedValues.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            for (int i = 0; i < Lines.Length; i++)
            {
                if (Lines[i] == "") continue;
                string[] Values = Lines[i].Split(',');

                var Entry = new EVTEntry();
                Entry.Name = Values[0].ToLower();
                Entry.EventType = ParseHexString(Values[1]);
                Entry.TrackID = ParseHexString(Values[2]);
                Entry.Unknown = ParseHexString(Values[3]);
                Entry.Unknown2 = ParseHexString(Values[4]);
                Entry.Unknown3 = ParseHexString(Values[5]);
                Entry.Unknown4 = ParseHexString(Values[6]);
                Entries.Add(Entry);
            }

            Reader.Close();
        }

        private uint ParseHexString(string input)
        {
            bool IsHex = false;
            input = input.ToLower();

            if (input == "") return 0;
            if (input.StartsWith("0x"))
            {
                input = input.Substring(2);
                IsHex = true;
            }
            //Sigh, Maxis...
            else if (input.Contains("a") || input.Contains("b") || input.Contains("b") ||
                input.Contains("c") || input.Contains("d") || input.Contains("e") || input.Contains("f"))
            {
                IsHex = true;
            }

            if (IsHex)
            {
                return Convert.ToUInt32(input, 16);
            }
            else
            {
                return Convert.ToUInt32(input);
            }
        }
    }

    public class EVTEntry 
    {
        public string Name;
        public uint EventType;
        public uint TrackID;
        public uint Unknown;
        public uint Unknown2;
        public uint Unknown3;
        public uint Unknown4;
    }
}
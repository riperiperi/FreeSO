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

namespace SimsLib.HIT
{
    /// <summary>
    /// TRK is a CSV format that defines a HIT track.
    /// </summary>
    public class Track
    {
        private string m_ID;      //The 4-byte string "TKDT"
        private string m_Label;   //The text label for this track
        private string m_TrackID; //The File ID of this track

        /// <summary>
        /// Creates a new track.
        /// </summary>
        /// <param name="Filedata">The data to create the track from.</param>
        public Track(byte[] Filedata)
        {
            BinaryReader Reader = new BinaryReader(new MemoryStream(Filedata));

            string CommaSeparatedValues = new string(Reader.ReadChars(Filedata.Length));
            string[] Values = CommaSeparatedValues.Split(",".ToCharArray());

            m_ID = Values[0];
            m_Label = Values[2];
            m_TrackID = Values[4];

            Reader.Close();
        }
    }
}

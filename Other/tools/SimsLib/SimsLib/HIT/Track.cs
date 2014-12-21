/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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

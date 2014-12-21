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
    /// HLS refers to two binary formats that both define a list of IDs, known as a hitlist.
    /// One format is a Pascal string with a 4-byte, little-endian length, representing a 
    /// comma-seperated list of decimal values, or decimal ranges (e.g. "1025-1035"), succeeded 
    /// by a single LF newline.
    /// </summary>
    public class Hitlist
    {
        private uint m_Version;
        private uint m_IDCount;
        public uint[] IDs;

        /// <summary>
        /// Creates a new hitlist.
        /// </summary>
        /// <param name="Filedata">The data to create the hitlist from.</param>
        public Hitlist(byte[] Filedata)
        {
            BinaryReader Reader = new BinaryReader(new MemoryStream(Filedata));

            m_Version = Reader.ReadUInt32();
            m_IDCount = Reader.ReadUInt32();
            IDs = new uint[m_IDCount];

            for (int i = 0; i < m_IDCount; i++)
                IDs[i] = Reader.ReadUInt32();

            Reader.Close();
        }

        /// <summary>
        /// Creates a new hitlist.
        /// </summary>
        /// <param name="Filepath">The path to the hitlist to read.</param>
        public Hitlist(string Filepath)
        {
            BinaryReader Reader = new BinaryReader(File.Open(Filepath, FileMode.Open));

            m_Version = Reader.ReadUInt32();
            m_IDCount = Reader.ReadUInt32();
            IDs = new uint[m_IDCount];

            for (int i = 0; i < m_IDCount; i++)
                IDs[i] = Reader.ReadUInt32();
        }
    }
}

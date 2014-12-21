/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): Nicholas Roth.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Iffinator.Flash
{
    public class IffChunk
    {
        //ID only exists for *.iff files (seemingly), and is assigned a random
        //number when loading *.flr files.
        protected int m_ID;
        private uint m_Length;
        //NameString only exists for *.iff files (seemingly).
        private string m_NameStr; 
        private byte[] m_Data;
        private int m_PadByte;
        private string m_Resource;
        private List<IffChunk> m_Children = new List<IffChunk>();

        public IffChunk(string Resource)
        {
            m_Resource = Resource;
        }

        /// <summary>
        /// Constructs an instance of IffChunk based on an instance of IffChunk.
        /// Used by classes inheriting from this class.
        /// </summary>
        /// <param name="Chunk">The IffChunk instance to construct from.</param>
        public IffChunk(IffChunk Chunk)
        {
            m_ID = Chunk.ID;
            m_Length = Chunk.Length;
            m_NameStr = Chunk.NameString;
            m_Resource = Chunk.Resource;
            m_PadByte = Chunk.PadByte;
        }

        /// <summary>
        /// The ID of this chunk.
        /// If the chunk is part of a *.flr or *.wll file, this field is assigned a random number.
        /// If the chunk is part of an *.iff or *.spf file, this field is read from the file.
        /// </summary>
        public int ID
        {
            get { return m_ID; }
            set { m_ID = value; }
        }

        public uint Length
        {
            get { return m_Length; }
            set { m_Length = value; }
        }

        /// <summary>
        /// The namestring for this chunk. Ususually describes what purpose a particular chunk has.
        /// Only exists if the chunk is part of an *.iff file.
        /// </summary>
        public string NameString
        {
            get { return m_NameStr; }
            set { m_NameStr = value; }
        }

        /// <summary>
        /// The data for this chunk.
        /// </summary>
        public byte[] Data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        public int PadByte
        {
            get { return m_PadByte; }
            set { m_PadByte = value; }
        }

        /// <summary>
        /// The ResourceID (TypeCode) for this chunk.
        /// </summary>
        public string Resource
        {
            get { return m_Resource; }
        }
    }
}

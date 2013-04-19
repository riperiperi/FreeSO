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

namespace SimsLib.ThreeD
{
    /// <summary>
    /// Represents an appearance for a Sim.
    /// </summary>
    public class Appearance
    {
        private uint m_Version;
        private ulong m_ThumbnailID;
        private List<ulong> m_BindingIDs = new List<ulong>();

        /// <summary>
        /// The thumbnail for this appearance, as visible
        /// in the CAS screen and in-game.
        /// </summary>
        public ulong ThumbnailID
        {
            get { return m_ThumbnailID; }
        }

        /// <summary>
        /// BindingIDs contained in this Appearance.
        /// </summary>
        public List<ulong> BindingIDs
        {
            get { return m_BindingIDs; }
        }

        /// <summary>
        /// Creates a new Appearance.
        /// </summary>
        /// <param name="FileData">The data for the Appearance.</param>
        public Appearance(byte[] FileData)
        {
            MemoryStream MemStream = new MemoryStream(FileData);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_Version = Endian.SwapUInt32(Reader.ReadUInt32());
            m_ThumbnailID = Endian.SwapUInt64(Reader.ReadUInt64());
            uint Count = Endian.SwapUInt32(Reader.ReadUInt32());

            for (int i = 0; i < Count; i++)
                BindingIDs.Add(Endian.SwapUInt64(Reader.ReadUInt64()));
        }
    }
}

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
    /// Represents a Sim's outfit.
    /// </summary>
    public class Outfit
    {
        private uint m_Version;
        private ulong m_LightAppearanceID, m_MediumAppearanceID, m_DarkAppearanceID, m_Handgroup;
        private uint m_Region;

        public ulong LightAppearanceID
        {
            get { return m_LightAppearanceID; }
        }

        public ulong MediumAppearanceID
        {
            get { return m_MediumAppearanceID; }
        }

        public ulong DarkAppearanceID
        {
            get { return m_DarkAppearanceID; }
        }

        public ulong HandgroupID
        {
            get { return m_Handgroup; }
        }

        public uint Region
        {
            get { return m_Region; }
        }

        public ulong GetAppearance(AppearanceType type)
        {
            switch (type)
            {
                case AppearanceType.Light:
                    return LightAppearanceID;

                case AppearanceType.Medium:
                    return MediumAppearanceID;

                case AppearanceType.Dark:
                    return DarkAppearanceID;
            }

            return 0;
        }

        /// <summary>
        /// Creates a new outfit.
        /// </summary>
        /// <param name="FileData">The data to create the outfit from.</param>
        public Outfit(byte[] FileData)
        {
            MemoryStream MemStream = new MemoryStream(FileData);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_Version = Endian.SwapUInt32(Reader.ReadUInt32());

            Reader.ReadUInt32(); //Unknown.

            m_LightAppearanceID = Endian.SwapUInt64(Reader.ReadUInt64());
            m_MediumAppearanceID = Endian.SwapUInt64(Reader.ReadUInt64());
            m_DarkAppearanceID = Endian.SwapUInt64(Reader.ReadUInt64());
            
            //A 4-byte unsigned integer specifying the hand group used by this outfit, 
            //or 0 if the outfit does not refer to one (e.g. the outfit is for a head or for a pet).
            uint FileID = Endian.SwapUInt32(Reader.ReadUInt32());
            if (FileID != 0)
                //18 = TypeID of HAG
                m_Handgroup = (ulong)((18 << 32) | FileID);
            else
                m_Handgroup = 0;

            m_Region = Reader.ReadUInt32();

            Reader.Close();
        }
    }
}

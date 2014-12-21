/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO MeshViewer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Dressup
{
    class Outfit
    {
        private uint m_Version;
        private ulong m_LightAppearanceID, m_MediumAppearanceID, m_DarkAppearanceID;

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

        public Outfit(byte[] FileData)
        {
            MemoryStream MemStream = new MemoryStream(FileData);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_Version = Endian.SwapUInt32(Reader.ReadUInt32());

            Reader.ReadUInt32(); //Unknown.

            m_LightAppearanceID = Endian.SwapUInt64(Reader.ReadUInt64());
            m_MediumAppearanceID = Endian.SwapUInt64(Reader.ReadUInt64());
            m_DarkAppearanceID = Endian.SwapUInt64(Reader.ReadUInt64());

            Reader.Close();
        }
    }
}

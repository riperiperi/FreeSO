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
    class Hag
    {
        private uint m_Version;
        private List<ulong> m_Appearances;

        public Hag(byte[] Filedata)
        {
            MemoryStream MemStream = new MemoryStream(Filedata);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_Appearances = new List<ulong>();

            m_Version = Reader.ReadUInt32();

            //There are always exactly 18 appearances referenced in a hand group.
            for (int i = 0; i < 17; i++)
            {
                m_Appearances.Add(Endian.SwapUInt64(Reader.ReadUInt64()));
            }
        }

        public ulong GetAppearanceID(int Index)
        {
            return m_Appearances[Index];
        }
    }
}

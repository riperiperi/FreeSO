/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO Dressup.

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
    class Hag
    {
        private uint m_Version;
        private List<ulong> m_Appearances;

        public List<ulong> Appearances
        {
            get { return m_Appearances; }
        }

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
    }
}

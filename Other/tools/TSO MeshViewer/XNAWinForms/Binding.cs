/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is TSO Dressup.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Dressup
{
    class Binding
    {
        private uint m_Version;
        private ulong m_MeshAssetID, m_TextureAssetID;

        public ulong MeshAssetID
        {
            get { return m_MeshAssetID; }
        }

        public ulong TextureAssetID
        {
            get { return m_TextureAssetID; }
        }

        public Binding(string Path)
        {
            BinaryReader Reader = new BinaryReader(File.Open(Path, FileMode.Open));

            m_Version = Endian.SwapUInt32(Reader.ReadUInt32());

            byte StrLength = Reader.ReadByte();
            string m_BoneName = Encoding.ASCII.GetString(Reader.ReadBytes(StrLength));
        }

        public Binding(byte[] FileData)
        {
            MemoryStream MemStream = new MemoryStream(FileData);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_Version = Endian.SwapUInt32(Reader.ReadUInt32());

            byte StrLength = Reader.ReadByte();
            string m_BoneName = Encoding.ASCII.GetString(Reader.ReadBytes(StrLength));

            //Should be 8.
            uint MeshAssetIDSize = Endian.SwapUInt32(Reader.ReadUInt32());

            //AssetID prefix, typical useless Maxis value...
            Reader.ReadUInt32();

            m_MeshAssetID = Endian.SwapUInt64(Reader.ReadUInt64());

            //Should be 8.
            uint TextureAssetIDSize = Endian.SwapUInt32(Reader.ReadUInt32());

            //AssetID prefix, typical useless Maxis value...
            Reader.ReadUInt32();

            m_TextureAssetID = Endian.SwapUInt64(Reader.ReadUInt64());
        }
    }
}

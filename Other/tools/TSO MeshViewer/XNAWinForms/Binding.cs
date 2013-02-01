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

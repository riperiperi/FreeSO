using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace XNAWinForms
{
    class PurchasableObject
    {
        private uint m_Version;
        private uint m_Gender;          //0 if male, 1 if female.
        private uint m_AssetIDSize;     //Should be 8.
        private ulong m_OutfitAssetID;

        public ulong OutfitID
        {
            get { return m_OutfitAssetID; }
        }

        public PurchasableObject(byte[] FileData)
        {
            MemoryStream MemStream = new MemoryStream(FileData);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_Version = Endian.SwapUInt32(Reader.ReadUInt32());
            m_Gender = Endian.SwapUInt32(Reader.ReadUInt32());
            m_AssetIDSize = Endian.SwapUInt32(Reader.ReadUInt32());

            Reader.ReadUInt32(); //AssetID prefix... typical useless Maxis value.

            m_OutfitAssetID = Endian.SwapUInt64(Reader.ReadUInt64());

            Reader.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SimsLib;

namespace TSO.Vitaboy
{
    public class PurchasableOutfit
    {
        private uint m_Version;
        private uint m_Gender;          //0 if male, 1 if female.
        private uint m_AssetIDSize;     //Should be 8.
        private ulong m_OutfitAssetID;

        public ulong OutfitID
        {
            get { return m_OutfitAssetID; }
        }

        /// <summary>
        /// Creates a new purchasable outfit.
        /// </summary>
        /// <param name="FileData">The data to create the purchasable outfit from.</param>
        public PurchasableOutfit()
        {
        }

        public void Read(Stream stream)
        {
            BinaryReader Reader = new BinaryReader(stream);
            m_Version = Endian.SwapUInt32(Reader.ReadUInt32());
            m_Gender = Endian.SwapUInt32(Reader.ReadUInt32());
            m_AssetIDSize = Endian.SwapUInt32(Reader.ReadUInt32());
            Reader.ReadUInt32(); //AssetID prefix... typical useless Maxis value.
            m_OutfitAssetID = Endian.SwapUInt64(Reader.ReadUInt64());
            Reader.Close();
        }
    }
}

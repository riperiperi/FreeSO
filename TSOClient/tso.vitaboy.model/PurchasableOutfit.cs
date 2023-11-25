using System.IO;
using FSO.Files;

namespace FSO.Vitaboy
{
    /// <summary>
    /// Purchasable outfits identify the outfits in the game which the user 
    /// can purchase from a clothes rack and then change into using a wardrobe.
    /// </summary>
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
        public PurchasableOutfit()
        {
        }

        /// <summary>
        /// Reads a purchasable outfit from a stream.
        /// </summary>
        /// <param name="stream">A Stream instance holding a Purchasable Outfit.</param>
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TSOClient.Code.Data
{
    /// <summary>
    /// Place to get information and assets related to sims, e.g. skins, thumbnails etc
    /// </summary>
    public class SimCatalog
    {

        public static void GetCollection(ulong fileID)
        {
            var collectionData = ContentManager.GetResourceFromLongID(fileID);
            var reader = new BinaryReader(new MemoryStream(collectionData));

            /*int count = Endian.SwapInt32(br.ReadInt32());
            for (int i = 0; i < count; i++)
            {
                br.ReadInt32();
                myPurchasables.Add(Endian.SwapUInt64(br.ReadUInt64()));
            }*/
        }


        public SimCatalog()
        {
            //FileIDs.CollectionsFileIDs.ea_male_heads
            /*

            BinaryReader br = new BinaryReader(new MemoryStream(ContentManager.GetResourceFromLongID(myCurrentCollectionID)));

            int count = Endian.SwapInt32(br.ReadInt32());
            for (int i = 0; i < count; i++)
            {
                br.ReadInt32();
                myPurchasables.Add(Endian.SwapUInt64(br.ReadUInt64()));
            }

            foreach (ulong purchasableID in myPurchasables)
            {
                br = new BinaryReader(new MemoryStream(ContentManager.GetResourceFromLongID(purchasableID)));

                br.BaseStream.Position = 16;
                byte[] outfitID = br.ReadBytes(8);
                ulong outfit = BitConverter.ToUInt64((byte[])outfitID.Reverse().ToArray(), 0);

                myOutfits.Add(outfit);
            }

            foreach (ulong outfitID in myOutfits)
            {
                br = new BinaryReader(new MemoryStream(ContentManager.GetResourceFromLongID(outfitID)));

                br.ReadUInt32();
                br.ReadUInt32();

                ulong[] Appearances = new ulong[]
            {
                Endian.SwapUInt64(br.ReadUInt64()),
                Endian.SwapUInt64(br.ReadUInt64()),
                Endian.SwapUInt64(br.ReadUInt64())
            };

                myAppearances.Add(Appearances);
            }

            foreach (ulong[] appearanceIDs in myAppearances)
            {
                ulong[] thumbnails = new ulong[3];

                for (int i = 0; i < 3; i++)
                {
                    br = new BinaryReader(new MemoryStream(ContentManager.GetResourceFromLongID(appearanceIDs[i])));

                    br.ReadInt32();

                    thumbnails[i] = Endian.SwapUInt64(br.ReadUInt64());
                }

                myThumbnails.Add(thumbnails);
            }

            br.Close();
             */
        }


        /// <summary>
        /// Load info about sim bodies etc
        /// </summary>
        public void Load()
        {
            //(ulong)FileIDs.CollectionsFileIDs.ea_male_heads;
            //(ulong)FileIDs.CollectionsFileIDs.ea_female_heads;
        }

    }
}

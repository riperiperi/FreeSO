namespace FSO.Content.Model
{
    public class RackOutfit
    {
        public ulong AssetID { get; set; }
        public int Price { get; set; }
        public RackOutfitGender Gender { get; set; }
        public RackType RackType { get; set; }

        /*public ulong GetOutfitID()
        {
            return GetOutfitID(AssetID);
        }

        public static ulong GetOutfitID(ulong assetId)
        {
            return (assetId << 32) | 0xd;
        }*/
    }

    public enum RackOutfitGender
    {
        Male,
        Female,
        Neutral
    }

    public enum RackType : short
    {
        Daywear = 0,
        Formalwear = 1,
        Swimwear = 2,
        Sleepwear = 3,
        Decor_Head = 4,
        Decor_Back = 5,
        Decor_Shoe = 6,
        Decor_Tail = 7,
        CAS = 8
    }
}

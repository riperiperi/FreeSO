using FSO.Files.Formats.IFF.Chunks;

namespace FSO.Content.Model
{
    public class WallStyle
    {
        public ushort ID;
        public string Name;
        public string Description;
        public int Price;
        public SPR WallsUpNear;
        public SPR WallsUpMedium;
        public SPR WallsUpFar;
        //for most fences, the following will be null. This means to use the ones above when walls are down.
        public SPR WallsDownNear;
        public SPR WallsDownMedium;
        public SPR WallsDownFar;
    }
}

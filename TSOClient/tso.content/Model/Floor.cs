using FSO.Files.Formats.IFF.Chunks;

namespace FSO.Content.Model
{
    /// <summary>
    /// A floor resource.
    /// </summary>
    public class Floor
    {
        public ushort ID;
        public SPR2 Near;
        public SPR2 Medium;
        public SPR2 Far;
    }
}

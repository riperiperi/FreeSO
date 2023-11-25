using FSO.Files.Formats.IFF.Chunks;

namespace FSO.Content.Model
{
    /// <summary>
    /// A wall resource.
    /// </summary>
    public class Wall
    {
        public ushort ID;
        public SPR Near;
        public SPR Medium;
        public SPR Far;
    }
}

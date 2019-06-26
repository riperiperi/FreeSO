using Microsoft.Xna.Framework.Content;

namespace MSDFData
{
    public class KerningPair
    {
        [ContentSerializer] private readonly char LeftBackend;
        [ContentSerializer] private readonly char RightBackend;
        [ContentSerializer] private readonly float AdvanceBackend;

        public KerningPair()
        {
            
        }

        public KerningPair(char left, char right, float advance)
        {
            this.LeftBackend = left;
            this.RightBackend = right;
            this.AdvanceBackend = advance;
        }

        public char Left => this.LeftBackend;
        public char Right => this.RightBackend;
        public float Advance => this.AdvanceBackend;
    }
}

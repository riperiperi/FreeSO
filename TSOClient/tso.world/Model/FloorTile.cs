using System.Runtime.InteropServices;

namespace FSO.LotView.Model
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FloorTile
    {
        public ushort Pattern;

        public override string ToString()
        {
            return Pattern.ToString();
        }
    }
}

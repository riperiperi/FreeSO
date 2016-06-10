using System;
using System.Runtime.InteropServices;

namespace SimplePaletteQuantizer.Helpers.Pixels.Indexed
{
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct PixelData8Indexed : IIndexedPixel
    {
        // raw component values
        private Byte index;

        // index methods 
        public Byte GetIndex(Int32 offset) { return index; }
        public void SetIndex(Int32 offset, Byte value) { index = value; }
    }
}

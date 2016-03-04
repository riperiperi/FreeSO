using System;
using System.Runtime.InteropServices;

namespace SimplePaletteQuantizer.Helpers.Pixels.Indexed
{
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct PixelData1Indexed : IIndexedPixel
    {
        // raw component values
        private Byte index;

        // get - index method
        public Byte GetIndex(Int32 offset)
        {
            return (index & 1 << (7 - offset)) != 0 ? Pixel.One : Pixel.Zero;
        }

        // set - index method
        public void SetIndex(Int32 offset, Byte value)
        {
            value = value == 0 ? Pixel.One : Pixel.Zero;

            if (value == 0)
            {
                index |= (Byte) (1 << (7 - offset));
            }
            else
            {
                index &= (Byte) (~(1 << (7 - offset)));
            }
        }
    }
}

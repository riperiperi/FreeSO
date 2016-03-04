using System;
using System.Runtime.InteropServices;

namespace SimplePaletteQuantizer.Helpers.Pixels.Indexed
{
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct PixelData4Indexed : IIndexedPixel
    {
        // raw component values
        private Byte index;

        // get - index method
        public Byte GetIndex(Int32 offset)
        {
            return (Byte) GetBitRange(8 - offset - 4, 7 - offset);
        }

        // set - index method
        public void SetIndex(Int32 offset, Byte value)
        {
            SetBitRange(8 - offset - 4, 7 - offset, value);
        }

        private Int32 GetBitRange(Int32 startOffset, Int32 endOffset)
        {
            Int32 result = 0;
            Byte bitIndex = 0;

            for (Int32 offset = startOffset; offset <= endOffset; offset++)
            {
                Int32 bitValue = 1 << bitIndex;
                result += GetBit(offset) ? bitValue : 0;
                bitIndex++;
            }

            return result;
        }

        private Boolean GetBit(Int32 offset)
        {
            return (index & (1 << offset)) != 0;
        }

        private void SetBitRange(Int32 startOffset, Int32 endOffset, Int32 value)
        {
            Byte bitIndex = 0;

            for (Int32 offset = startOffset; offset <= endOffset; offset++)
            {
                Int32 bitValue = 1 << bitIndex;
                SetBit(offset, (value & bitValue) != 0);
                bitIndex++;
            }
        }

        private void SetBit(Int32 offset, Boolean value)
        {
            if (value)
            {
                index |= (Byte) (1 << offset);
            }
            else
            {
                index &= (Byte) (~(1 << offset));
            }
        }
    }
}

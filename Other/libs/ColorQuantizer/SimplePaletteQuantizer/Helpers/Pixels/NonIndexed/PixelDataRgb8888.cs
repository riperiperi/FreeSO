using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SimplePaletteQuantizer.Helpers.Pixels.NonIndexed
{
    /// <summary>
    /// Name |          Blue         |        Green          |           Red         |         Unused        |
    /// Bit  |00|01|02|03|04|05|06|07|08|09|10|11|12|13|14|15|16|17|18|19|20|21|22|23|24|25|26|27|28|29|30|31|
    /// Byte |00000000000000000000000|11111111111111111111111|22222222222222222222222|33333333333333333333333|
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct PixelDataRgb8888 : INonIndexedPixel
    {
        // raw component values
        [FieldOffset(0)] private readonly Byte blue;    // 00 - 07
        [FieldOffset(1)] private readonly Byte green;   // 08 - 15
        [FieldOffset(2)] private readonly Byte red;     // 16 - 23
        [FieldOffset(3)] private readonly Byte unused;  // 24 - 31

        // raw high-level values
        [FieldOffset(0)] private Int32 raw;             // 00 - 23

        // processed component values
        public Int32 Alpha { get { return 0xFF; } }
        public Int32 Red { get { return red; } }
        public Int32 Green { get { return green; } }
        public Int32 Blue { get { return blue; } }

        /// <summary>
        /// See <see cref="INonIndexedPixel.Argb"/> for more details.
        /// </summary>
        public Int32 Argb
        {
            get { return Pixel.AlphaMask | raw; }
        }

        /// <summary>
        /// See <see cref="INonIndexedPixel.GetColor"/> for more details.
        /// </summary>
        public Color GetColor()
        {
            return Color.FromArgb(Argb);
        }

        /// <summary>
        /// See <see cref="INonIndexedPixel.SetColor"/> for more details.
        /// </summary>
        public void SetColor(Color color)
        {
            raw = color.ToArgb() & Pixel.RedGreenBlueMask;
        }

        /// <summary>
        /// See <see cref="INonIndexedPixel.Value"/> for more details.
        /// </summary>
        public UInt64 Value
        {
            get { return (UInt32) raw; }
            set { raw = (Int32) (value & 0xFFFFFF); }
        }
    }
}

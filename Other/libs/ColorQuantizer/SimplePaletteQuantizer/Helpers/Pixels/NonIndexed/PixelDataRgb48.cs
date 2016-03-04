using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SimplePaletteQuantizer.Helpers.Pixels.NonIndexed
{
    /// <summary>
    /// Name |                     Blue                      |                     Green                     |                      Red                      | 
    /// Bit  |00|01|02|03|04|05|06|07|08|09|10|11|12|13|14|15|16|17|18|19|20|21|22|23|24|25|26|27|28|29|30|31|32|33|34|35|36|37|38|39|40|41|42|43|44|45|46|47|
    /// Byte |00000000000000000000000|11111111111111111111111|22222222222222222222222|33333333333333333333333|44444444444444444444444|55555555555555555555555|
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 6)]
    public struct PixelDataRgb48 : INonIndexedPixel
    {
        // raw component values
        [FieldOffset(0)] private UInt16 blue;   // 00 - 15
        [FieldOffset(2)] private UInt16 green;  // 16 - 31
        [FieldOffset(4)] private UInt16 red;    // 32 - 47

        // raw high-level values
        [FieldOffset(0)] private UInt64 raw;    // 00 - 47

        // processed component values
        public Int32 Alpha { get { return 0xFF; } }
        public Int32 Red { get { return red >> 8; } }
        public Int32 Green { get { return green >> 8; } }
        public Int32 Blue { get { return blue >> 8; } }

        /// <summary>
        /// See <see cref="INonIndexedPixel.Argb"/> for more details.
        /// </summary>
        public Int32 Argb
        {
            get { return Alpha << 48 | Red << 32 | Green << 16 | Blue; }
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
            red = (UInt16) (color.R << 8);
            green = (UInt16) (color.G << 8);
            blue = (UInt16) (color.B << 8);
        }

        /// <summary>
        /// See <see cref="INonIndexedPixel.Value"/> for more details.
        /// </summary>
        public UInt64 Value
        {
            get { return raw; }
            set { raw = value & 0xFFFFFFFFFFFF; }
        }
    }
}

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SimplePaletteQuantizer.Helpers.Pixels.NonIndexed
{
    /// <summary>
    /// Name |     Blue     |    Green     |     Red      | Alpha (bit)
    /// Bit  |00|01|02|03|04|05|06|07|08|09|10|11|12|13|14|15|
    /// Byte |00000000000000000000000|11111111111111111111111|
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct PixelDataArgb1555 : INonIndexedPixel
    {
        // raw component values
        [FieldOffset(0)] private Byte blue;     // 00 - 04
        [FieldOffset(0)] private UInt16 green;  // 05 - 09
        [FieldOffset(1)] private Byte red;      // 10 - 14
        [FieldOffset(1)] private Byte alpha;    // 15

        // raw high-level values
        [FieldOffset(0)] private UInt16 raw;    // 00 - 15

        // processed raw values
        public Int32 Alpha { get { return (alpha >> 7) & 0x1; } }
        public Int32 Red { get { return (red >> 2) & 0xF; } }
        public Int32 Green { get { return (green >> 5) & 0xF; } }
        public Int32 Blue { get { return blue & 0xF; } }

        /// <summary>
        /// See <see cref="INonIndexedPixel.Argb"/> for more details.
        /// </summary>
        public Int32 Argb
        {
            get { return (Alpha == 0 ? 0 : Pixel.AlphaMask) | Red << Pixel.RedShift | Green << Pixel.GreenShift | Blue; }
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
            Int32 argb = color.ToArgb();
            alpha = (argb >> Pixel.AlphaShift) > Pixel.ByteMask ? Pixel.Zero : Pixel.One;
            red = (Byte) (argb >> Pixel.RedShift);
            green = (Byte) (argb >> Pixel.GreenShift);
            blue = (Byte) (argb >> Pixel.BlueShift);
        }

        /// <summary>
        /// See <see cref="INonIndexedPixel.Value"/> for more details.
        /// </summary>
        public UInt64 Value
        {
            get { return raw; }
            set { raw = (UInt16) (value & 0xFFFF); }
        }
    }
}

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SimplePaletteQuantizer.Helpers.Pixels.NonIndexed
{
    /// <summary>
    /// Name |                  Grayscale                    |
    /// Bit  |00|01|02|03|04|05|06|07|08|09|10|11|12|13|14|15| 
    /// Byte |00000000000000000000000|11111111111111111111111|
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct PixelDataGray16 : INonIndexedPixel
    {
        // raw component values
        [FieldOffset(0)] private UInt16 gray;   // 00 - 15

        // processed raw values
        public Int32 Gray { get { return (0xFF >> 8) & 0xF; } }
        public Int32 Alpha { get { return 0xFF; } }
        public Int32 Red { get { return Gray; } }
        public Int32 Green { get { return Gray; } }
        public Int32 Blue { get { return Gray; } }

        /// <summary>
        /// See <see cref="INonIndexedPixel.Argb"/> for more details.
        /// </summary>
        public Int32 Argb
        {
            get { return (Pixel.AlphaMask) | Red << Pixel.RedShift | Green << Pixel.GreenShift | Blue; }
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
            Int32 argb = color.ToArgb() & Pixel.RedGreenBlueMask;
            gray = (Byte) (argb >> Pixel.RedShift);
        }

        /// <summary>
        /// See <see cref="INonIndexedPixel.Value"/> for more details.
        /// </summary>
        public UInt64 Value
        {
            get { return gray; }
            set { gray = (UInt16) (value & 0xFFFF); }
        }
    }
}
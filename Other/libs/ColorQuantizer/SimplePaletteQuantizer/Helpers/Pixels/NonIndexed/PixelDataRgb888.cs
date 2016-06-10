using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SimplePaletteQuantizer.Helpers.Pixels.NonIndexed
{
    /// <summary>
    /// Name |          Blue         |        Green          |           Red         | 
    /// Bit  |00|01|02|03|04|05|06|07|08|09|10|11|12|13|14|15|16|17|18|19|20|21|22|23|
    /// Byte |00000000000000000000000|11111111111111111111111|22222222222222222222222|
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 3)]
    public struct PixelDataRgb888 : INonIndexedPixel
    {
        // raw component values
        [FieldOffset(0)] private Byte blue;    // 00 - 07
        [FieldOffset(1)] private Byte green;   // 08 - 15
        [FieldOffset(2)] private Byte red;     // 16 - 23

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
            get { return Pixel.AlphaMask | Red << Pixel.RedShift | Green << Pixel.GreenShift | Blue; }
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
            red = color.R;
            green = color.G;
            blue = color.B;
        }

        /// <summary>
        /// See <see cref="INonIndexedPixel.Value"/> for more details.
        /// </summary>
        public UInt64 Value
        {
            get { return (UInt32) Argb; }
            set
            {
                red = (Byte) ((value >> Pixel.RedShift) & 0xFF);
                green = (Byte) ((value >> Pixel.GreenShift) & 0xFF);
                blue = (Byte) ((value >> Pixel.BlueShift) & 0xFF);
            }
        }
    }
}

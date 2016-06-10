using System;
using System.Drawing.Imaging;

namespace SimplePaletteQuantizer.Extensions
{
    /// <summary>
    /// The utility extender class.
    /// </summary>
    public static partial class Extend
    {
        /// <summary>
        /// Gets the bit count for a given pixel format.
        /// </summary>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <returns>The bit count.</returns>
        public static Byte GetBitDepth(this PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Format1bppIndexed: 
                    return 1;

                case PixelFormat.Format4bppIndexed: 
                    return 4;

                case PixelFormat.Format8bppIndexed: 
                    return 8;

                case PixelFormat.Format16bppArgb1555:
                case PixelFormat.Format16bppGrayScale:
                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppRgb565:
                    return 16;

                case PixelFormat.Format24bppRgb: 
                    return 24;

                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format32bppRgb:
                    return 32;

                case PixelFormat.Format48bppRgb: 
                    return 48;

                case PixelFormat.Format64bppArgb:
                case PixelFormat.Format64bppPArgb:
                    return 64;

                default:
                    String message = string.Format("A pixel format '{0}' not supported!", pixelFormat);
                    throw new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Gets the available color count for a given pixel format.
        /// </summary>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <returns>The available color count.</returns>
        public static UInt16 GetColorCount(this PixelFormat pixelFormat)
        {
            // checks whether a pixel format is indexed, otherwise throw an exception
            if (!pixelFormat.IsIndexed())
            {
                String message = string.Format("Cannot retrieve color count for a non-indexed format '{0}'.", pixelFormat);
                throw new NotSupportedException(message);
            }

            switch (pixelFormat)
            {
                case PixelFormat.Format1bppIndexed:
                    return 2;

                case PixelFormat.Format4bppIndexed:
                    return 16;

                case PixelFormat.Format8bppIndexed:
                    return 256;

                default:
                    String message = string.Format("A pixel format '{0}' not supported!", pixelFormat);
                    throw new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Gets the friendly name of the pixel format.
        /// </summary>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <returns></returns>
        public static String GetFriendlyName(this PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Format1bppIndexed:
                    return "Indexed (2 colors)";

                case PixelFormat.Format4bppIndexed:
                    return "Indexed (16 colors)";

                case PixelFormat.Format8bppIndexed:
                    return "Indexed (256 colors)";

                case PixelFormat.Format16bppGrayScale:
                    return "Grayscale (65536 shades)";

                case PixelFormat.Format16bppArgb1555:
                    return "Highcolor + Alpha mask (32768 colors)";

                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppRgb565:
                    return "Highcolor (65536 colors)";

                case PixelFormat.Format24bppRgb:
                    return "Truecolor (24-bit)";

                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                    return "Truecolor + Alpha (32-bit)";

                case PixelFormat.Format32bppRgb:
                    return "Truecolor (32-bit)";

                case PixelFormat.Format48bppRgb:
                    return "Truecolor (48-bit)";

                case PixelFormat.Format64bppArgb:
                case PixelFormat.Format64bppPArgb:
                    return "Truecolor + Alpha (64-bit)";

                default:
                    String message = string.Format("A pixel format '{0}' not supported!", pixelFormat);
                    throw new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Determines whether the specified pixel format is indexed.
        /// </summary>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <returns>
        /// 	<c>true</c> if the specified pixel format is indexed; otherwise, <c>false</c>.
        /// </returns>
        public static Boolean IsIndexed(this PixelFormat pixelFormat)
        {
            return (pixelFormat & PixelFormat.Indexed) == PixelFormat.Indexed;
        }

        /// <summary>
        /// Determines whether the specified pixel format is supported.
        /// </summary>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <returns>
        /// 	<c>true</c> if the specified pixel format is supported; otherwise, <c>false</c>.
        /// </returns>
        public static Boolean IsSupported(this PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Format1bppIndexed:
                case PixelFormat.Format4bppIndexed:
                case PixelFormat.Format8bppIndexed:
                case PixelFormat.Format16bppArgb1555:
                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppRgb565:
                case PixelFormat.Format24bppRgb:
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format48bppRgb:
                case PixelFormat.Format64bppArgb:
                case PixelFormat.Format64bppPArgb:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the format by color count.
        /// </summary>
        public static PixelFormat GetFormatByColorCount(Int32 colorCount)
        {
            if (colorCount <= 0 || colorCount > 256)
            {
                String message = string.Format("A color count '{0}' not supported!", colorCount);
                throw new NotSupportedException(message);
            }

            PixelFormat result = PixelFormat.Format1bppIndexed;

            if (colorCount > 16)
            {
                result = PixelFormat.Format8bppIndexed;
            }
            else if (colorCount > 2)
            {
                result = PixelFormat.Format4bppIndexed;
            }

            return result;
        }

        /// <summary>
        /// Determines whether the specified pixel format has an alpha channel.
        /// </summary>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <returns>
        /// 	<c>true</c> if the specified pixel format has an alpha channel; otherwise, <c>false</c>.
        /// </returns>
        public static Boolean HasAlpha(this PixelFormat pixelFormat)
        {
            return (pixelFormat & PixelFormat.Alpha) == PixelFormat.Alpha ||
                   (pixelFormat & PixelFormat.PAlpha) == PixelFormat.PAlpha;
        }

        /// <summary>
        /// Determines whether [is deep color] [the specified pixel format].
        /// </summary>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <returns>
        /// 	<c>true</c> if [is deep color] [the specified pixel format]; otherwise, <c>false</c>.
        /// </returns>
        public static Boolean IsDeepColor(this PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Format16bppGrayScale:
                case PixelFormat.Format48bppRgb:
                case PixelFormat.Format64bppArgb:
                case PixelFormat.Format64bppPArgb:
                    return true;

                default:
                    return false;
            }
        }
    }
}



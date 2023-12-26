using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework;
using SimplePaletteQuantizer.Helpers;
using SimplePaletteQuantizer.Quantizers.DistinctSelection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.Common
{
    public static class SpriteEncoderUtils
    {
        public static Microsoft.Xna.Framework.Color[] QuantizeFrame(SPR2Frame frame, out byte[] bytes)
        {
            var bmps = GetPixelAlpha(frame, frame.Width, frame.Height, new Vector2());

            //"optimal palette quantizer" gets like 16 colors
            // popular is good but discards outliers
            // distinct selection gets outliers but does some weird thing to highlights

            var quantpx = (Bitmap)ImageBuffer.QuantizeImage(bmps[0], new SimplePaletteQuantizer.Quantizers.XiaolinWu.WuColorQuantizer(), null, 255, 1);
            var palt = quantpx.Palette.Entries;

            var data = quantpx.LockBits(new System.Drawing.Rectangle(0, 0, quantpx.Width, quantpx.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            bytes = new byte[data.Height * data.Width];

            // copy the bytes from bitmap to array
            for (int i = 0; i < data.Height; i++)
            {
                Marshal.Copy(data.Scan0 + i * data.Stride, bytes, i * data.Width, data.Width);
            }

            var result = new Microsoft.Xna.Framework.Color[palt.Length];
            for (int i=0; i<palt.Length; i++)
            {
                var c = palt[i];
                result[i] = new Microsoft.Xna.Framework.Color(c.R, c.G, c.B, c.A);
            }
            return result;
        }

        /// <summary>
        /// Generates windows bitmaps for the appearance of this sprite.
        /// </summary>
        /// <param name="tWidth"></param>
        /// <param name="tHeight"></param>
        /// <returns>Array of three images, [Color, Alpha, Depth].</returns>
        public static System.Drawing.Image[] GetPixelAlpha(SPR2Frame sprite, int tWidth, int tHeight)
        {
            return GetPixelAlpha(sprite, tWidth, tHeight, sprite.Position);
        }
        /// <summary>
        /// Generates windows bitmaps for the appearance of this sprite.
        /// </summary>
        /// <param name="tWidth"></param>
        /// <param name="tHeight"></param>
        /// <returns>Array of three images, [Color, Alpha, Depth].</returns>
        public static System.Drawing.Image[] GetPixelAlpha(SPRFrame sprite, int tWidth, int tHeight, bool DepthMapFrame = false)
        {
            return GetPixelAlpha(sprite, tWidth, tHeight, new Vector2(0), DepthMapFrame);
        }

        public static System.Drawing.Image[] GetPixelAlpha(SPR2Frame sprite, int tWidth, int tHeight, Vector2 pos)
        {
            var result = new System.Drawing.Bitmap[3];
            var locks = new BitmapData[3];
            var data = new byte[3][];
            for (int i = 0; i < 3; i++)
            {
                result[i] = new System.Drawing.Bitmap(tWidth, tHeight, PixelFormat.Format24bppRgb);
                locks[i] = result[i].LockBits(new System.Drawing.Rectangle(0, 0, tWidth, tHeight), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
                data[i] = new byte[locks[i].Stride * locks[i].Height];
            }

            int index = 0;
            for (int y = 0; y < tHeight; y++)
            {
                for (int x = 0; x < tWidth; x++)
                {
                    Microsoft.Xna.Framework.Color col;
                    byte depth = 255;

                    if (x >= pos.X && x < pos.X + sprite.Width && y >= pos.Y && y < pos.Y + sprite.Height)
                    {
                        col = sprite.PixelData[(int)(x - pos.X) + (int)(y - pos.Y) * sprite.Width];
                        if (col.A == 0) col = new Microsoft.Xna.Framework.Color(0xFF, 0xFF, 0x00, 0x00);
                        if (sprite.ZBufferData != null)
                        {
                            depth = sprite.ZBufferData[(int)(x - pos.X) + (int)(y - pos.Y) * sprite.Width];
                        }
                    }
                    else
                    {
                        col = new Microsoft.Xna.Framework.Color(0xFF, 0xFF, 0x00, 0x00);
                    }

                    data[0][index] = col.B;
                    data[0][index + 1] = col.G;
                    data[0][index + 2] = col.R;
                    data[0][index + 3] = 255;

                    data[1][index] = col.A;
                    data[1][index + 1] = col.A;
                    data[1][index + 2] = col.A;
                    data[1][index + 3] = 255;

                    data[2][index] = depth;
                    data[2][index + 1] = depth;
                    data[2][index + 2] = depth;
                    data[2][index + 3] = 255;

                    index += 4;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                Marshal.Copy(data[i], 0, locks[i].Scan0, data[i].Length);
                result[i].UnlockBits(locks[i]);
            }

            return result;
        }
        public static System.Drawing.Image[] GetPixelAlpha(SPRFrame sprite, int tWidth, int tHeight, Vector2 pos, bool DepthMapFrame = false)
        {
            var grayScale = default(PALT);
            if (DepthMapFrame) grayScale = PALT.Greyscale;

            var result = new System.Drawing.Bitmap[3];
            var locks = new BitmapData[3];
            var data = new byte[3][];
            for (int i = 0; i < 3; i++)
            {
                result[i] = new System.Drawing.Bitmap(tWidth, tHeight, PixelFormat.Format24bppRgb);
                locks[i] = result[i].LockBits(new System.Drawing.Rectangle(0, 0, tWidth, tHeight), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
                data[i] = new byte[locks[i].Stride * locks[i].Height];
            }

            int index = 0;
            for (int y = 0; y < tHeight; y++)
            {
                for (int x = 0; x < tWidth; x++)
                {
                    Microsoft.Xna.Framework.Color col;
                    byte depth = 255;

                    if (x >= pos.X && x < pos.X + sprite.Width && y >= pos.Y && y < pos.Y + sprite.Height)
                    {
                        //SPR frames use a grayscale palette                    
                        if (DepthMapFrame)
                        {
                            // Read this frame as grayscale
                            byte colorIndex = sprite.Indices[(y * sprite.Width + x)];
                            if(colorIndex == 0) col = new Microsoft.Xna.Framework.Color(0xFF, 0xFF, 0x00, 0x00);
                            else col = grayScale.Colors[colorIndex];                            
                        }
                        else
                        {
                            col = sprite.GetPixel((int)(x - pos.X), (int)(y - pos.Y));
                            if (col.A == 0) col = new Microsoft.Xna.Framework.Color(0xFF, 0xFF, 0x00, 0x00);
                            // ** no alpha component!
                        }
                    }
                    else
                    {
                        col = new Microsoft.Xna.Framework.Color(0xFF, 0xFF, 0x00, 0x00);
                    }                    

                    data[0][index] = col.B;
                    data[0][index + 1] = col.G;
                    data[0][index + 2] = col.R;
                    data[0][index + 3] = 255;

                    data[1][index] = col.A;
                    data[1][index + 1] = col.A;
                    data[1][index + 2] = col.A;
                    data[1][index + 3] = 255;

                    index += 4;
                }

            }

            for (int i = 0; i < 3; i++)
            {
                Marshal.Copy(data[i], 0, locks[i].Scan0, data[i].Length);
                result[i].UnlockBits(locks[i]);
            }

            return result;
        }
    }
}

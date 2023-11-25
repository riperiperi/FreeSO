using System;

namespace TargaImagePCL
{
    public class RawTGABitmap
    {
        public int Width;
        public int Height;
        public byte[] Data;
        public TGAPixelFormat Format;

        public RawTGABitmap(int width, int height, byte[] data, TGAPixelFormat format)
        {
            Width = width;
            Height = height;
            Data = data;
            Format = format;
        }

        public byte[] ToBGRA(bool premultiply)
        {
            //bitorder output: bbbbbbbb gggggggg rrrrrrrr aaaaaaaa
            byte[] result = null;
            if (Format == TGAPixelFormat.RGB_32bpp || Format == TGAPixelFormat.ARGB_32bpp)
            {
                bool useAlpha = Format == TGAPixelFormat.ARGB_32bpp;
                result = new byte[Data.Length];
                for (int i = 0; i < Data.Length; i += 4)
                { //flip red and blue and premultiply alpha
                    result[i + 3] = (useAlpha)?Data[i + 3]:(byte)255;
                    float a = (premultiply) ? (Data[i + 3] / 255f) : 1;
                    result[i + 2] = (byte)(Data[i] * a);
                    result[i + 1] = (byte)(Data[i + 1] * a);
                    result[i] = (byte)(Data[i + 2] * a);
                }
            }
            else if (Format == TGAPixelFormat.RGB_24bpp)
            {
                result = new byte[Width*Height*4];
                var j = 0;
                for (int i = 0; i < Data.Length; i += 3)
                { //flip red and blue and remove key colour
                    var a = (byte)((Data[i] > 0xFD && Data[i + 1] < 3 && Data[i + 2] > 0xFD)?0:255);
                    result[j + 3] = a;
                    result[j + 2] = (byte)(Data[i] & a);
                    result[j + 1] = (byte)(Data[i + 1] & a);
                    result[j] = (byte)(Data[i + 2] & a);
                    j += 4;
                }
            }
            else if (Format == TGAPixelFormat.ARGB1555_16bpp || Format == TGAPixelFormat.RGB555_16bpp)
            {
                bool useAlpha = Format == TGAPixelFormat.ARGB1555_16bpp;
                result = new byte[Width * Height * 4];
                throw new NotImplementedException("16-bit TGA not yet implemented.");
            }
            else if (Format == TGAPixelFormat.Grayscale_8bpp)
            {
                result = new byte[Width * Height * 4];
                for (int i = 0; i < Data.Length; i ++)
                { //fill with gray
                    var g = Data[i];
                    result[i + 3] = 255;
                    result[i + 2] = g;
                    result[i + 1] = g;
                    result[i] = g;
                }
            }
            //else undefined. return null.

            return result;
        }
    }
}

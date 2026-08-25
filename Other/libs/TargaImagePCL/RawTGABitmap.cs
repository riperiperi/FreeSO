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

                if (useAlpha)
                {
                    for (int i = 0; i < Data.Length; i += 4)
                    { //flip red and blue and premultiply alpha
                        int r = Data[i];
                        int g = Data[i + 1];
                        int b = Data[i + 2];
                        int a = Data[i + 3];

                        a = premultiply ? a : 255;
                        result[i] = (byte)((b * a) / 255);
                        result[i + 1] = (byte)((g * a) / 255);
                        result[i + 2] = (byte)((r * a) / 255);
                        result[i + 3] = (byte)a;
                    }
                }
                else
                {
                    for (int i = 0; i < Data.Length; i += 4)
                    { //flip red and blue and premultiply alpha
                        byte r = Data[i];
                        byte g = Data[i + 1];
                        byte b = Data[i + 2];
                        byte a = 255;

                        result[i] = b;
                        result[i + 1] = g;
                        result[i + 2] = r;
                        result[i + 3] = a;
                    }
                }
            }
            else if (Format == TGAPixelFormat.RGB_24bpp)
            {
                result = new byte[Width*Height*4];
                var j = 0;
                for (int i = 0; i < Data.Length; i += 3)
                { //flip red and blue and remove key colour
                    var r = Data[i];
                    var g = Data[i + 1];
                    var b = Data[i + 2];
                    var a = (byte)((r > 0xFD && g < 3 && b > 0xFD)?0:255);

                    result[j++] = (byte)(b & a);
                    result[j++] = (byte)(g & a);
                    result[j++] = (byte)(r & a);
                    result[j++] = a;
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

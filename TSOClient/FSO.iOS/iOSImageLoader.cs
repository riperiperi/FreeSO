using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Microsoft.Xna.Framework;
using CoreGraphics;
using System.Drawing;

namespace FSOiOS
{
    public static class iOSImageLoader
    {
        public static HashSet<uint> MASK_COLORS = new HashSet<uint>{
            new Microsoft.Xna.Framework.Color(0xFF, 0x00, 0xFF, 0xFF).PackedValue,
            new Microsoft.Xna.Framework.Color(0xFE, 0x02, 0xFE, 0xFF).PackedValue,
            new Microsoft.Xna.Framework.Color(0xFF, 0x01, 0xFF, 0xFF).PackedValue,
        };

        public static Texture2D iOSFromStream(GraphicsDevice gd, Stream str)
        {
            var magic = (str.ReadByte() | (str.ReadByte() << 8));
            str.Seek(0, SeekOrigin.Begin);
            magic += 0;
            if (magic == 0x4D42)
            {
                try
                {
                    //it's a bitmap. 
                    var data = GetiOSData(str);
                    ManualTextureMaskSingleThreaded(data.Item2, MASK_COLORS.ToArray());
                    var tex = new Texture2D(gd, data.Item1.X, data.Item1.Y);
                    tex.SetData(data.Item2);
                    return tex;
                }
                catch (Exception)
                {
                    return null; //bad bitmap :(
                }
            }
            else
            {
                //test for targa
                str.Seek(-18, SeekOrigin.End);
                byte[] sig = new byte[16];
                str.Read(sig, 0, 16);
                str.Seek(0, SeekOrigin.Begin);
                if (ASCIIEncoding.Default.GetString(sig) == "TRUEVISION-XFILE")
                {
                    try
                    {
                        var tga = new TargaImagePCL.TargaImage(str);
                        var tex = new Texture2D(gd, tga.Image.Width, tga.Image.Height);
                        tex.SetData(tga.Image.ToBGRA(true));
                        return tex;
                    }
                    catch (Exception)
                    {
                        return null; //bad tga
                    }
                }
                else
                {
                    //anything else
                    try
                    {
                        var data = GetiOSData(str);
                        var tex = new Texture2D(gd, data.Item1.X, data.Item1.Y);
                        tex.SetData(data.Item2);
                        //var tex = Texture2D.FromStream(gd, str);
                        return tex;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("error: " + e.ToString());
                        return new Texture2D(gd, 1, 1);
                    }
                }
            }
        }

        public static Tuple<Microsoft.Xna.Framework.Point, byte[]> GetiOSData(Stream stream)
        {
            using (var uiImage = UIImage.LoadFromData(NSData.FromStream(stream)))
            {
                var cgImage = uiImage.CGImage;
                var width = cgImage.Width;
                var height = cgImage.Height;

                var data = new byte[width * height * 4];

                var colorSpace = CGColorSpace.CreateDeviceRGB();
                var bitmapContext = new CGBitmapContext(data, width, height, 8, width * 4, colorSpace, CGBitmapFlags.PremultipliedLast);
                bitmapContext.DrawImage(new RectangleF(0, 0, width, height), cgImage);
                bitmapContext.Dispose();
                colorSpace.Dispose();

                return new Tuple<Microsoft.Xna.Framework.Point, byte[]>(new Microsoft.Xna.Framework.Point((int)width, (int)height), data);
            }
        }

    public static void ManualTextureMaskSingleThreaded(byte[] buffer, uint[] ColorsFrom)
        {
            var ColorTo = Microsoft.Xna.Framework.Color.Transparent.PackedValue;

            for (int i = 0; i < buffer.Length; i += 4)
            {
                if (buffer[i] >= 248 && buffer[i + 2] >= 248 && buffer[i + 1] <= 4)
                {
                    buffer[i] = buffer[i + 1] = buffer[i + 2] = buffer[i + 3] = 0;
                }
            }
        }
    }
}
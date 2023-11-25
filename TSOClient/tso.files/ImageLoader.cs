using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Microsoft.Xna.Framework;

namespace FSO.Files
{
    public class ImageLoader
    {
        public static bool UseSoftLoad = true;
        public static int PremultiplyPNG = 0;

        public static HashSet<uint> MASK_COLORS = new HashSet<uint>{
            new Microsoft.Xna.Framework.Color(0xFF, 0x00, 0xFF, 0xFF).PackedValue,
            new Microsoft.Xna.Framework.Color(0xFE, 0x02, 0xFE, 0xFF).PackedValue,
            new Microsoft.Xna.Framework.Color(0xFF, 0x01, 0xFF, 0xFF).PackedValue
        };

        public static Func<GraphicsDevice, Stream, Texture2D> BaseFunction = WinFromStream;


        public static Texture2D FromStream(GraphicsDevice gd, Stream str)
        {
            return BaseFunction(gd, str);
        }

        private static Texture2D WinFromStream(GraphicsDevice gd, Stream str)
        {
            return WinFromStreamP(gd, str, 0);
        }

        public static Texture2D WinFromStreamP(GraphicsDevice gd, Stream str, int premult)
        {
            //if (!UseSoftLoad)
            //{
            //attempt monogame load of image

            var magic = (str.ReadByte() | (str.ReadByte() << 8));
            str.Seek(0, SeekOrigin.Begin);
            magic += 0;
            if (magic == 0x4D42)
            {
                try
                {
                    //it's a bitmap. 
                    Texture2D tex;
                    if (ImageLoaderHelpers.BitmapFunction != null)
                    {
                        var bmp = ImageLoaderHelpers.BitmapFunction(str);
                        if (bmp == null) return null;
                        tex = new Texture2D(gd, bmp.Item2, bmp.Item3);
                        tex.SetData(bmp.Item1);
                    }
                    else
                    {
                        tex = Texture2D.FromStream(gd, str);
                    }
                    ManualTextureMaskSingleThreaded(ref tex, MASK_COLORS.ToArray());
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
                        Texture2D tex;
                        Color[] buffer = null;
                        if (ImageLoaderHelpers.BitmapFunction != null)
                        {
                            var bmp = ImageLoaderHelpers.BitmapFunction(str);
                            if (bmp == null) return null;
                            tex = new Texture2D(gd, bmp.Item2, bmp.Item3);
                            tex.SetData(bmp.Item1);

                            //buffer = bmp.Item1;
                        }
                        else
                        {
                            tex = Texture2D.FromStream(gd, str);
                        }

                        premult += PremultiplyPNG;
                        if (premult == 1)
                        {
                            if (buffer == null)
                            {
                                buffer = new Color[tex.Width * tex.Height];
                                tex.GetData<Color>(buffer);
                            }

                            for (int i = 0; i < buffer.Length; i++)
                            {
                                var a = buffer[i].A;
                                buffer[i] = new Color((byte)((buffer[i].R * a) / 255), (byte)((buffer[i].G * a) / 255), (byte)((buffer[i].B * a) / 255), a);
                            }
                            tex.SetData(buffer);
                        }
                        else if (premult == -1) //divide out a premultiply... currently needed for dx since it premultiplies pngs without reason
                        {
                            if (buffer == null)
                            {
                                buffer = new Color[tex.Width * tex.Height];
                                tex.GetData<Color>(buffer);
                            }

                            for (int i = 0; i < buffer.Length; i++)
                            {
                                var a = buffer[i].A / 255f;
                                buffer[i] = new Color((byte)(buffer[i].R / a), (byte)(buffer[i].G / a), (byte)(buffer[i].B / a), buffer[i].A);
                            }
                            tex.SetData(buffer);
                        }
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

        public static void ManualTextureMaskSingleThreaded(ref Texture2D Texture, uint[] ColorsFrom)
        {
            var ColorTo = Microsoft.Xna.Framework.Color.Transparent.PackedValue;

            var size = Texture.Width * Texture.Height * 4;
            byte[] buffer = new byte[size];

            Texture.GetData<byte>(buffer);

            var didChange = false;

            for (int i = 0; i < size; i += 4)
            {
                if (buffer[i] >= 248 && buffer[i + 2] >= 248 && buffer[i + 1] <= 4)
                {
                    buffer[i] = buffer[i + 1] = buffer[i + 2] = buffer[i + 3] = 0;
                    didChange = true;
                }
            }

            if (didChange)
            {
                Texture.SetData(buffer);
            }
            else return;
        }

    }
}

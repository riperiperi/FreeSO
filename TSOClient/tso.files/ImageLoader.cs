/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Microsoft.Xna.Framework;
using System.Drawing;
using System.Runtime.InteropServices;

namespace FSO.Files
{
    public class ImageLoader
    {
        public static bool UseSoftLoad = true;

        public static HashSet<uint> MASK_COLORS = new HashSet<uint>{
            new Microsoft.Xna.Framework.Color(0xFF, 0x00, 0xFF, 0xFF).PackedValue,
            new Microsoft.Xna.Framework.Color(0xFE, 0x02, 0xFE, 0xFF).PackedValue,
            new Microsoft.Xna.Framework.Color(0xFF, 0x01, 0xFF, 0xFF).PackedValue
        };

        /*
        public static Texture2D FromStreamSoft(GraphicsDevice gd, Stream str)
        {
            //TODO: does not compile on xamarin platforms, so we use the slower method since it seems to load TGAs fine.
            Bitmap bmp = null;
            var magic = (str.ReadByte() | (str.ReadByte() << 8));
            str.Seek(0, SeekOrigin.Begin);
            magic += 0;
            if (magic == 0x4D42)
            {
                try
                {
                    bmp = (Bitmap)Image.FromStream(str); //try as bmp
                }
                catch (Exception)
                {
                    return null; //bad bitmap
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
                        bmp = new Paloma.TargaImage(str).Image; //try as tga
                    }
                    catch (Exception)
                    {
                        return null; //bad tga
                    }
                }
            }

            if (bmp != null)
            {
                //image loaded into bitmap
                bool premultiplied = false;

                var data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var bytes = new byte[data.Height * data.Stride];

                // copy the bytes from bitmap to array
                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

                for (int i = 0; i < bytes.Length; i += 4)
                { //flip red and blue and premultiply alpha
                    if (bytes[i] > 0xFD && bytes[i + 1] < 3 && bytes[i + 2] > 0xFD)
                        bytes[i + 3] = 0;
                    byte temp = bytes[i + 2];
                    float a = (premultiplied) ? 1 : (bytes[i + 3] / 255f);
                    bytes[i + 2] = (byte)(bytes[i] * a);
                    bytes[i + 1] = (byte)(bytes[i + 1] * a);
                    bytes[i] = (byte)(temp * a);
                }

                var tex = new Texture2D(gd, data.Width, data.Height);
                tex.SetData<byte>(bytes);
                return tex;
            } else
            {
                try
                {
                    var tex = Texture2D.FromStream(gd, str);
                    //ManualTextureMaskSingleThreaded(ref tex, MASK_COLORS.ToArray());
                    return tex;
                }
                catch (Exception e)
                {
                    Console.WriteLine("image load error: " + e.ToString());
                    return new Texture2D(gd, 1, 1);
                }
            }
        }
        */

        public static Texture2D FromStream(GraphicsDevice gd, Stream str)
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
                    var tex = Texture2D.FromStream(gd, str);
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
                } else
                {
                    //anything else
                    try
                    {
                        var tex = Texture2D.FromStream(gd, str);
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

			var size = Texture.Width * Texture.Height;
			uint[] buffer = new uint[size];

			Texture.GetData<uint>(buffer);

			var didChange = false;

			for (int i = 0; i < size; i++)
			{

				if (ColorsFrom.Contains(buffer[i]))
				{
					didChange = true;
					buffer[i] = ColorTo;
				}
			}

			if (didChange)
			{
				Texture.SetData(buffer, 0, size);
			}
			else return;
		}

	}
}

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

			var size = Texture.Width * Texture.Height*4;
			byte[] buffer = new byte[size];

			Texture.GetData<byte>(buffer);

			var didChange = false;

			for (int i = 0; i < size; i+=4)
			{
                if (buffer[i] >= 249 && buffer[i+2] >= 249 && buffer[i+1] <= 4)
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

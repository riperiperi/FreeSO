using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;

namespace TSO.Files
{
    public class ImageLoader
    {
        public static Texture2D FromStream(GraphicsDevice gd, Stream str)
        {
            try
            {
                return Texture2D.FromStream(gd, str);
            }
            catch (Exception e)
            {
                try
                {
                    bool premultiplied = false;
                    Bitmap bmp = null;
                    try {
                        bmp = (Bitmap)Image.FromStream(str); //try as bmp
                    } catch (Exception) {
                        str.Seek(0, SeekOrigin.Begin);
                        var tga = new Paloma.TargaImage(str);
                        bmp = tga.Image; //try as tga. for some reason this format does not have a magic number, which is ridiculously stupid.
                    }

                    var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    var bytes = new byte[data.Height * data.Stride];

                    // copy the bytes from bitmap to array
                    Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

                    for (int i = 0; i < bytes.Length; i += 4)
                    { //flip red and blue and premultiply alpha
                        byte temp = bytes[i+2];
                        float a = (premultiplied)?1:(bytes[i + 3]/255f);
                        bytes[i + 2] = (byte)(bytes[i]*a);
                        bytes[i + 1] = (byte)(bytes[i + 1] * a);
                        bytes[i] = (byte)(temp*a);
                    }

                    var tex = new Texture2D(gd, data.Width, data.Height);
                    tex.SetData<byte>(bytes);
                    return tex;
                }
                catch (Exception e2)
                {
                    return null;
                }
            }
        }
    }
}

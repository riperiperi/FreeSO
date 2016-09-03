using FSO.Content.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Utils
{
    public class SoftwareImageLoader
    {
        public static TexBitmap SoftImageFetch(Stream stream, AbstractTextureRef texRef)
        {
            Image result = null;
            try
            {
                result = Image.FromStream(stream);
            }
            catch (Exception ex)
            {
                try
                {
                    result = (Bitmap)Image.FromStream(stream); //try as bmp
                }
                catch (Exception)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    var tga = new Paloma.TargaImage(stream);
                    result = tga.Image;
                }
            }
            stream.Close();

            if (result == null) return null;

            var image = new Bitmap(result);
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, image.PixelFormat);
            var pixelSize = image.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb ? 4 : 3;
            var padding = data.Stride - (data.Width * pixelSize);
            var bytes = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

            return new TexBitmap
            {
                Data = bytes,
                Width = image.Width,
                Height = image.Height,
                PixelSize = pixelSize
            };
        }

    }
}

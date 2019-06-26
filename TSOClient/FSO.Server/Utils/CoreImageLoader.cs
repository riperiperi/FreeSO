using FSO.Content.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FSO.Server.Utils
{
    public class CoreImageLoader
    {
        public static TexBitmap SoftImageFetch(Stream stream, AbstractTextureRef texRef)
        {
            Image<Rgba32> result = null;
            try
            {
                result = Image.Load(stream);
            }
            catch (Exception)
            {
                return new TexBitmap() { Data = new byte[0] };
            }
            stream.Close();
            
            if (result == null) return null;

            return new TexBitmap
            {
                Data = result.SavePixelData(),
                Width = result.Width,
                Height = result.Height,
                PixelSize = 4
            };
        }
    }
}

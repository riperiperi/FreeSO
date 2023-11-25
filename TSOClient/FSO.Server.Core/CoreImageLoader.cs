using FSO.Content.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;

namespace FSO.Server.Core
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
            var data = result.SavePixelData();

            for (int i = 0; i < data.Length; i += 4)
            {
                var temp = data[i];
                data[i] = data[i + 2];
                data[i + 2] = temp;
            }

            return new TexBitmap
            {
                Data = data,
                Width = result.Width,
                Height = result.Height,
                PixelSize = 4
            };
        }
    }
}

using FSO.Content.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FSO.Server.Core
{
    public class CoreImageLoader
    {
        public static TexBitmap SoftImageFetch(Stream stream, AbstractTextureRef texRef)
        {
            Image<Rgba32> result = null;
            try
            {
                result = Image.Load<Rgba32>(stream);
            }
            catch (Exception)
            {
                return new TexBitmap() { Data = new byte[0] };
            }
            stream.Close();

            if (result == null) return null;

            // Get pixel data
            var data = new byte[result.Width * result.Height * 4];
            result.CopyPixelDataTo(data);

            // Swap red and blue channels
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

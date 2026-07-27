using FSO.Content.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using XnaColor = Microsoft.Xna.Framework.Color;

namespace FSO.Server.Utils
{
    public class CoreImageLoader
    {
        public static TexBitmap SoftImageFetch(Stream stream, AbstractTextureRef texRef)
        {
            Image<Bgra32> result = null;
            try
            {
                result = Image.Load<Bgra32>(stream);
            }
            catch (Exception)
            {
                return new TexBitmap() { Data = new byte[0] };
            }
            finally
            {
                stream.Close();
            }

            if (result == null) return null;

            var pixels = new byte[result.Width * result.Height * 4];
            result.CopyPixelDataTo(pixels);

            return new TexBitmap
            {
                Data = pixels,
                Width = result.Width,
                Height = result.Height,
                PixelSize = 4
            };
        }

        public static void SavePNG(XnaColor[] data, int width, int height, Stream stream)
        {
            var image = new Image<Bgra32>(width, height);

            int i = 0;
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Bgra32> pixelRow = accessor.GetRowSpan(y);

                    foreach (ref Bgra32 pixel in pixelRow)
                    {
                        var color = data[i++];

                        pixel = new Bgra32(color.R, color.G, color.B, color.A);
                    }
                }
            });

            image.SaveAsPng(stream);
        }

        public static bool ValidatePNG(byte[] data, int width, int height)
        {
            // TODO: Ideally do this without loading the image data?
            try
            {
                var image = Image.Load(data);

                if (image.Width != width || image.Height != height)
                {
                    return false;
                }

                if (!image.Metadata.TryGetPngMetadata(out var meta))
                {
                    // Must be a PNG.
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}

using FSO.Content.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Transforms;
using SixLabors.ImageSharp.Processing.Transforms.Resamplers;
using SixLabors.Primitives;
using System;
using System.IO;

namespace FSO.Server.Utils
{
    public class CoreImageLoader
    {
        /// <summary>
        /// Derives a 512x512 head image by cropping the top third of a portrait PNG and saves it to headPath.
        /// Called automatically when a client uploads their avatar body portrait (Avatar_Thumbnail).
        /// </summary>
        public static void GenerateHeadFromBodyThumbnail(byte[] bodyPngData, string headPath)
        {
            try
            {
                using (var img = Image.Load(new MemoryStream(bodyPngData)))
                {
                    // Crop the top third of the portrait (where the head is), centered horizontally, as a square.
                    int cropH = img.Height / 3;
                    int cropW = Math.Min(cropH, img.Width);
                    int cropX = (img.Width - cropW) / 2;
                    img.Mutate(x => x
                        .Crop(new Rectangle(cropX, 0, cropW, cropH))
                        .Resize(new ResizeOptions
                        {
                            Size = new Size(512, 512),
                            Mode = ResizeMode.Stretch,
                            Sampler = KnownResamplers.Lanczos3
                        }));
                    using (var fs = File.Open(headPath, FileMode.Create))
                        img.SaveAsPng(fs);
                }
            }
            catch { }
        }

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

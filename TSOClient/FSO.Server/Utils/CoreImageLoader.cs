using FSO.Content.Model;
using NLog;
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
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

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
                    // 3D clients upload a portrait (~400x600). Crop the top third for the head.
                    // 2D clients upload a square head sprite — use it directly, no crop needed.
                    bool isPortrait = img.Height > img.Width * 1.2f;
                    if (isPortrait)
                    {
                        int cropH = img.Height / 3;
                        int cropW = Math.Min(cropH, img.Width);
                        int cropX = (img.Width - cropW) / 2;
                        img.Mutate(x => x.Crop(new Rectangle(cropX, 0, cropW, cropH)));
                    }
                    img.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(512, 512),
                        Mode = ResizeMode.Max,
                        Sampler = KnownResamplers.Lanczos3
                    }));
                    using (var fs = File.Open(headPath, FileMode.Create))
                        img.SaveAsPng(fs);
                }
            }
            catch (Exception ex)
            {
                LOG.Warn(ex, "GenerateHeadFromBodyThumbnail failed for {0} ({1} bytes)", headPath, bodyPngData?.Length ?? 0);
            }
        }

        /// <summary>
        /// Splits the client-uploaded combined avatar PNG (400×1000: top 400×600 isometric
        /// body + bottom 400×400 front-facing head) into separate body.png and head.png
        /// files, each resized to 512px on the long edge. Falls back to the legacy
        /// behaviour (treat the whole image as the body, derive head by cropping the top
        /// third) if the image isn't the expected 400×1000 layout.
        /// </summary>
        public static void SplitCombinedAvatarThumbnail(byte[] combinedPngData, string bodyPath, string headPath)
        {
            try
            {
                using (var src = Image.Load(new MemoryStream(combinedPngData)))
                {
                    // Detect the new combined layout by aspect: body region (W×600) plus a
                    // square head region (W×W) → height ≈ W * 2.5.
                    bool isCombined = src.Width > 0 && src.Height > src.Width + src.Width / 2;
                    if (isCombined)
                    {
                        int headSize = src.Width;
                        int bodyHeight = src.Height - headSize;

                        using (var body = src.Clone(x => x
                            .Crop(new Rectangle(0, 0, src.Width, bodyHeight))
                            .Resize(new ResizeOptions
                            {
                                Size = new Size(512, 512),
                                Mode = ResizeMode.Max,
                                Sampler = KnownResamplers.Lanczos3
                            })))
                        using (var fs = File.Open(bodyPath, FileMode.Create))
                            body.SaveAsPng(fs);

                        using (var head = src.Clone(x => x
                            .Crop(new Rectangle(0, bodyHeight, src.Width, headSize))
                            .Resize(new ResizeOptions
                            {
                                Size = new Size(512, 512),
                                Mode = ResizeMode.Max,
                                Sampler = KnownResamplers.Lanczos3
                            })))
                        using (var fs = File.Open(headPath, FileMode.Create))
                            head.SaveAsPng(fs);
                        return;
                    }
                }

                // Legacy fallback: write the raw upload as body.png, derive head from it.
                using (var fs = File.Open(bodyPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    fs.Write(combinedPngData, 0, combinedPngData.Length);
                GenerateHeadFromBodyThumbnail(combinedPngData, headPath);
            }
            catch (Exception ex)
            {
                LOG.Warn(ex, "SplitCombinedAvatarThumbnail failed for {0} ({1} bytes)", bodyPath, combinedPngData?.Length ?? 0);
            }
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

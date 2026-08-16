using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FSO_BrowserClient
{
    /// <summary>
    /// Registers the browser's image decoder, the way FSO.Windows and FSO.Unix
    /// register theirs at startup.
    ///
    /// ImageLoader consults ImageLoaderHelpers.BitmapFunction first and only falls
    /// back to Texture2D.FromStream when it is null — and the browser was the one
    /// platform that never set it. That fallback goes through KNI's own decoder,
    /// which hands back BGRA byte order for TSO's JPEG avatar textures: every sim
    /// rendered blue, because skin (222,180,150) arrives as (150,180,222).
    ///
    /// Decoding through ImageSharp instead is deterministic, pure managed (so it
    /// runs in WASM), and matches what every other platform already does.
    /// </summary>
    public static class BrowserImageLoader
    {
        static int Logged;

        public static void Install()
        {
            FSO.Files.ImageLoaderHelpers.BitmapFunction = Read;
        }

        static Tuple<byte[], int, int> Read(Stream str)
        {
            try
            {
                using var image = Image.Load<Rgba32>(str);
                var data = new byte[image.Width * image.Height * 4];
                image.CopyPixelDataTo(data);
                if (Logged < 3)
                {
                    Logged++;
                    Console.WriteLine($"image decode: {image.Width}x{image.Height} " +
                        $"rgba0={data[0]:X2}{data[1]:X2}{data[2]:X2}{data[3]:X2}");
                }
                return new Tuple<byte[], int, int>(data, image.Width, image.Height);
            }
            catch (Exception e)
            {
                // Returning null makes ImageLoader treat this as an undecodable
                // image rather than throwing through whatever content load asked
                // for it — one bad texture should not take down the tab.
                Console.WriteLine("image decode failed: " + e.Message);
                return null;
            }
        }
    }
}

using CommandLine;
using FSO.Server.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FSO.Packager
{
    internal class Program
    {
        public static Tuple<byte[], int, int> BitmapReader(Stream str)
        {
            using var image = Image.Load<Rgba32>(str);
            int width = image.Width;
            int height = image.Height;

            var data = new byte[width * height * 4];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = (y * width + x) * 4;
                    Rgba32 px = image[x, y];
                    data[i] = px.R;
                    data[i + 1] = px.G;
                    data[i + 2] = px.B;
                    data[i + 3] = px.A;
                }
            }

            return new Tuple<byte[], int, int>(data, width, height);
        }

        static void Main(string[] args)
        {
            Content.Model.AbstractTextureRef.ImageFetchFunction = CoreImageLoader.SoftImageFetch;
            FSO.Files.ImageLoaderHelpers.BitmapFunction = BitmapReader;

            ITool? tool = null;

            int result = Parser.Default.ParseArguments<PackageRemeshesOptions, DummyOptions>(args)
                .MapResult(
                (PackageRemeshesOptions opts) =>
                {
                    tool = new ToolPackageRemeshes(opts);
                    return 0;
                },
                (DummyOptions opts) =>
                {
                    return 0;
                },
                errs => 1
                );

            if (result == 1 || tool == null)
            {
                Environment.Exit(1);
            }

            tool.Run();
        }
    }
}

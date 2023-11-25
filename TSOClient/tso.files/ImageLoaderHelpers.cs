using System;
using System.IO;

namespace FSO.Files
{
    public static class ImageLoaderHelpers
    {
        public static Func<Stream, Tuple<byte[], int, int>> BitmapFunction = null;
        public static Action<byte[], int, int, Stream> SavePNGFunc = null;
    }
}

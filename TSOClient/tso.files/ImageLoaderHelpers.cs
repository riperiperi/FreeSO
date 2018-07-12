using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Files
{
    public static class ImageLoaderHelpers
    {
        public static Func<Stream, Tuple<byte[], int, int>> BitmapFunction = null;
    }
}

using FSO.Content.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Codecs
{
    public static class SmartCodec
    {
        public static Dictionary<string, IGenericContentCodec> CodecForExtension = new Dictionary<string, IGenericContentCodec>
        {
            {".iff", new IffCodec() },
            {".flr", new IffCodec() },
            {".wll", new IffCodec() },
            {".bcf", new BCFCodec() },
            {".bmf", new BMFCodec() },
            {".cfp", new CFPCodec() },
            {".bmp", new TextureCodec() }
        };

        public static object Decode(Stream stream, string extension)
        {
            IGenericContentCodec codec = null;
            if (CodecForExtension.TryGetValue(extension, out codec))
            {
                return codec.GenDecode(stream);
            }
            return null;
        }
    }
}

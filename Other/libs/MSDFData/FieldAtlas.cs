using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDFData
{
    public class FieldAtlas
    {
        [ContentSerializer] private readonly int WidthBackend;
        [ContentSerializer] private readonly int HeightBackend;
        [ContentSerializer] private readonly int GlyphSizeBackend;
        [ContentSerializer] private readonly byte[] PNGDataBackend;
        [ContentSerializer] private readonly char[] CharMapBackend;

        public FieldAtlas()
        {
        }

        public FieldAtlas(int width, int height, int glyphSize, byte[] pngData, char[] charMap)
        {
            WidthBackend = width;
            HeightBackend = height;
            GlyphSizeBackend = glyphSize;
            PNGDataBackend = pngData;
            File.WriteAllBytes("test.png", pngData);
            CharMapBackend = charMap;
        }

        public int Width => WidthBackend;
        public int Height => HeightBackend;
        public int GlyphSize => GlyphSizeBackend;
        public byte[] PNGData => PNGDataBackend;
        public char[] CharMap => CharMapBackend;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSO.Files.utils;
using Microsoft.Xna.Framework.Graphics;

namespace TSO.Files.formats.iff.chunks
{
    /// <summary>
    /// This chunk type holds a color palette.
    /// </summary>
    public class PALT : IffChunk
    {
        public PALT(){
        }
        public PALT(Color color){
            Colors = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                Colors[i] = color;
            }
        }

        public Color[] Colors;

        public override void Read(Iff iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var version = io.ReadUInt32();
                var numEntries = io.ReadUInt32();
                var reserved = io.ReadBytes(8);

                Colors = new Color[numEntries];
                for (var i = 0; i < numEntries; i++)
                {
                    var r = io.ReadByte();
                    var g = io.ReadByte();
                    var b = io.ReadByte();
                    Colors[i] = new Color(r, g, b);
                }
            }
        }
    }
}

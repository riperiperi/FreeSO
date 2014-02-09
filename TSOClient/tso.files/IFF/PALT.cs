using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimsLib.IFF;
using System.IO;
using tso.files.utils;
using Microsoft.Xna.Framework.Graphics;

namespace tso.files.IFF
{
    public class PALT : IffChunk
    {
        public Color[] Colors;

        public PALT(IffChunk chunk) : base(chunk) {
            this.Read(new MemoryStream(chunk.Data));
        }

        public void Read(Stream stream){
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN)){
                var version = io.ReadUInt32();
                var numEntries = io.ReadUInt32();
                var reserved = io.ReadBytes(8);

                Colors = new Color[numEntries];
                for (var i = 0; i < numEntries; i++){
                    var r = io.ReadByte();
                    var g = io.ReadByte();
                    var b = io.ReadByte();
                    Colors[i] = new Color(r, g, b);
                }
            }
        }
    }
}

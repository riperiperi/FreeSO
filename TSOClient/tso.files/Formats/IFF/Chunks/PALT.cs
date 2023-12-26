/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.Files.Utils;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// This chunk type holds a color palette.
    /// </summary>
    public class PALT : IffChunk
    {
        public PALT()
        {
        }

        public PALT(Color color)
        {
            Colors = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                Colors[i] = color;
            }
        }
        /// <summary>
        /// Makes a new <see cref="PALT"/> which is populated with <paramref name="Colors"/> 
        /// in order, then filled with <paramref name="fillColor"/> to the end of the palette.
        /// </summary>
        /// <param name="fillColor"></param>
        /// <param name="Colors"></param>
        public PALT(Color fillColor, params Color[] Colors)
        {
            Colors = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                var color = i < Colors.Length ? Colors[i] : fillColor;
                Colors[i] = color;
            }
        }

        public Color[] Colors;
        public int References = 0;

        public static PALT Greyscale
        {
            get
            {
                PALT newPalt = new PALT();
                newPalt.Colors = new Color[256];
                ref var colors = ref newPalt.Colors;
                for (int i = 0; i < 256; i++)
                {
                    colors[i] = new Color(i, i, i, 255);
                }
                return newPalt;
            }
        }

        /// <summary>
        /// Reads a PALT chunk from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream object holding a PALT chunk.</param>
        public override void Read(IffFile iff, Stream stream)
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

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteUInt32(0);
                io.WriteUInt32((uint)Colors.Length);
                io.WriteBytes(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
                foreach (var col in Colors)
                {
                    io.WriteByte(col.R);
                    io.WriteByte(col.G);
                    io.WriteByte(col.B);
                }
                return true;
            }
        }

        public bool PalMatch(Color[] data)
        {
            for (var i=0; i<Colors.Length; i++)
            {
                if (i >= data.Length) return true;
                if (data[i].A != 0 && data[i] != Colors[i]) return false;
            }
            return true;
        }
    }
}

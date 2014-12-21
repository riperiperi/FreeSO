/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace SimsLib.IFF
{
    /// <summary>
    /// This chunk type holds a color palette.
    /// </summary>
    public class PALT : AbstractIffChunk
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
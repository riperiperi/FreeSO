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

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// This chunk type holds the filename of a semi-global iff file used by this object.
    /// </summary>
    public class GLOB : IffChunk
    {
        public string Name;

        /// <summary>
        /// Reads a GLOB chunk from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream object holding a GLOB chunk.</param>
        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                StringBuilder temp = new StringBuilder();
                var num = io.ReadByte();
                if (num < 48)
                { //less than smallest ASCII value for valid filename character, so assume this is a pascal string
                    temp.Append(io.ReadCString(num));
                }
                else
                { //we're actually a null terminated string!
                    temp.Append((char)num);
                    while (stream.Position < stream.Length)
                    {
                        char read = (char)io.ReadByte();
                        if (read == 0) break;
                        else temp.Append(read);
                    }
                }
                Name = temp.ToString();
            }
        }
    }
}

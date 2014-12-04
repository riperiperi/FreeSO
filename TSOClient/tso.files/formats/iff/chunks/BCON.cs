/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSO.Files.utils;

namespace TSO.Files.formats.iff.chunks
{
    /// <summary>
    /// This chunk type holds a number of constants that behavior code can refer to. 
    /// Labels may be provided for them in a TRCN chunk with the same ID.
    /// </summary>
    public class BCON : IffChunk
    {
        public byte Flags;
        public ushort[] Constants;

        /// <summary>
        /// Reads a BCON chunk from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream instance holding a BCON.</param>
        public override void Read(Iff iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var num = io.ReadByte();
                Flags = io.ReadByte();

                Constants = new ushort[num];
                for (var i = 0; i < num; i++)
                {
                    Constants[i] = io.ReadUInt16();
                }
            }
        }
    }
}

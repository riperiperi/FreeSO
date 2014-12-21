/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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

        public override void Read(Iff iff, Stream stream){
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN)){
                var num = io.ReadByte();
                Flags = io.ReadByte();

                Constants = new ushort[num];
                for (var i = 0; i < num; i++){
                    Constants[i] = io.ReadUInt16();
                }
            }
        }
    }
}

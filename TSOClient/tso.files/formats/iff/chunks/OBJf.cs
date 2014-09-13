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
    public class OBJf : IffChunk
    {
        public OBJfFunctionEntry[] functions;
        public uint Version;

        public override void Read(Iff iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.ReadUInt32(); //pad
                Version = io.ReadUInt32();
                string magic = io.ReadCString(4);
                functions = new OBJfFunctionEntry[io.ReadUInt32()];
                for (int i=0; i<functions.Length; i++) {
                    var result = new OBJfFunctionEntry();
                    result.ConditionFunction = io.ReadUInt16();
                    result.ActionFunction = io.ReadUInt16();
                    functions[i] = result;
                }
            }
        }
    }

    public struct OBJfFunctionEntry {
        public ushort ConditionFunction;
        public ushort ActionFunction;
    }
}

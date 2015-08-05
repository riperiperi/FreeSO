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
    /// This chunk type assigns BHAV subroutines to a number of events that occur in 
    /// (or outside of?) the object, which are described in behavior.iff chunk 00F5.
    /// </summary>
    public class OBJf : IffChunk
    {
        public OBJfFunctionEntry[] functions;
        public uint Version;

        /// <summary>
        /// Reads a OBJf chunk from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream object holding a OBJf chunk.</param>
        public override void Read(IffFile iff, Stream stream)
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

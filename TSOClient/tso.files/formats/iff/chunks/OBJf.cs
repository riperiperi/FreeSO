using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using tso.files.utils;

namespace tso.files.formats.iff.chunks
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

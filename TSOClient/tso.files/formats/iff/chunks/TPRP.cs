using FSO.Files.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// Labels for BHAV local variables and parameters.
    /// </summary>
    public class TPRP : IffChunk
    {
        public string[] ParamNames;
        public string[] LocalNames;

        /// <summary>
        /// Reads a TPRP from a stream.
        /// </summary>
        /// <param name="iff">Iff instance.</param>
        /// <param name="stream">A Stream instance holding a TPRP chunk.</param>
        public override void Read(IffFile iff, System.IO.Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var zero = io.ReadInt32();
                var version = io.ReadInt32();
                var name = io.ReadCString(4); //"PRPT", or randomly 4 null characters for no good reason

                var pCount = io.ReadInt32();
                var lCount = io.ReadInt32();
                ParamNames = new string[pCount];
                LocalNames = new string[lCount];
                for (int i = 0; i < pCount; i++)
                {
                    ParamNames[i] = (version == 5) ? io.ReadPascalString() : io.ReadNullTerminatedString();
                }
                for (int i = 0; i < lCount; i++)
                {
                    LocalNames[i] = (version == 5) ? io.ReadPascalString() : io.ReadNullTerminatedString();
                }

                //TODO: flags and unknowns
            }
        }
    }
}

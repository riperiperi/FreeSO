using FSO.Files.Utils;
using System.IO;

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

                for (int i = 0; i < pCount; i++)
                {
                    //flags for parameters. probably disabled, unused, etc.
                    var flag = io.ReadByte();
                }

                //what are these?
                if (version >= 3)
                    io.ReadInt32();
                if (version >= 4)
                    io.ReadInt32();
            }
        }

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteInt32(0);
                io.WriteInt32(5); //version
                io.WriteCString("PRPT", 4);
                io.WriteInt32(ParamNames.Length);
                io.WriteInt32(LocalNames.Length);
                foreach (var param in ParamNames)
                    io.WritePascalString(param);
                foreach (var local in LocalNames)
                    io.WritePascalString(local);

                for (int i=0; i<ParamNames.Length; i++)
                {
                    io.WriteByte(0);
                }

                io.WriteInt32(0);
                io.WriteInt32(0);
            }
            return true;
        }
    }
}

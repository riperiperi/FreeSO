using FSO.Files.RC;
using FSO.Files.Utils;
using System.IO;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// Metadata for an object's mesh reconstruction. Currently only supports file-wise parameters.
    /// </summary>
    public class FSOR : IffChunk
    {
        public static int CURRENT_VERSION = 1;
        public int Version = CURRENT_VERSION;
        public DGRPRCParams Params = new DGRPRCParams();

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                Version = io.ReadInt32();
                Params = new DGRPRCParams(io, Version);
            }
        }

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteInt32(Version);
                Params.Save(io);
            }
            return true;
        }
    }
}

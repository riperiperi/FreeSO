using FSO.Files.Utils;
using System.IO;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// A simple container for FSOV data within an iff. If this exists, normal TS1 iff loading is subverted.
    /// </summary>
    public class FSOV : IffChunk
    {
        public static int CURRENT_VERSION = 1;
        public int Version = CURRENT_VERSION;
        public byte[] Data;

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                Version = io.ReadInt32();
                var length = io.ReadInt32();
                Data = io.ReadBytes(length);
            }
        }

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteInt32(Version);
                io.WriteInt32(Data.Length);
                io.WriteBytes(Data);
            }
            return true;
        }
    }
}

using FSO.Files.Utils;
using System.IO;

namespace FSO.Files.Formats.IFF.Chunks
{
    public class THMB : IffChunk
    {
        public int Width;
        public int Height;
        public int BaseYOff;
        public int XOff;
        public int AddYOff; //accounts for difference between roofed and unroofed. relative to the base.

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                Width = io.ReadInt32();
                Height = io.ReadInt32();
                BaseYOff = io.ReadInt32();
                XOff = io.ReadInt32(); //0 in all cases i've found, pretty much?
                AddYOff = io.ReadInt32();
            }
        }
    }
}

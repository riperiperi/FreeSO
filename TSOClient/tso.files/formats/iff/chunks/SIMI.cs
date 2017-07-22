using FSO.Files.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Files.Formats.IFF.Chunks
{
    public class SIMI : IffChunk
    {
        public uint Version;
        public short[] GlobalData;

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.ReadUInt32(); //pad
                Version = io.ReadUInt32();
                string magic = io.ReadCString(4);
                var items = (Version > 0x3E) ? 0x80 : 0x40;

                GlobalData = new short[38];

                for (int i=0; i<items; i++)
                {
                    var dat = io.ReadInt16();
                    if (i < GlobalData.Length)
                        GlobalData[i] = dat;
                }

                //something comes after this, but i'm not sure what it is.
            }
        }
    }
}

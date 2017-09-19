using FSO.Files.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Files.Formats.IFF.Chunks
{
    public class TATT : IffChunk
    {
        public Dictionary<uint, short[]> TypeAttributesByGUID = new Dictionary<uint, short[]>();

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.ReadUInt32(); //pad
                var version = io.ReadUInt32(); //zero

                var TTAT = io.ReadUInt32();

                var compressionCode = io.ReadByte();
                if (compressionCode != 1) throw new Exception("hey what!!");

                var iop = new IffFieldEncode(io);

                var total = iop.ReadInt32();
                for (int i=0; i<total; i++)
                {
                    var guid = (uint)iop.ReadInt32();
                    var count = iop.ReadInt32();
                    var tatts = new short[count];
                    for (int j=0; j<count; j++)
                    {
                        tatts[j] = iop.ReadInt16();
                    }
                    TypeAttributesByGUID[guid] = tatts;
                }
            }
        }
    }
}

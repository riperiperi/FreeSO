using FSO.Files.Utils;
using System.Collections.Generic;
using System.IO;

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

                var TTAT = io.ReadCString(4);

                IOProxy iop;
                var compressionCode = io.ReadByte();
                //HACK: for freeso we don't run the field encoding coompression
                //since fso neighbourhoods are not compatible with ts1, it does not matter too much
                if (compressionCode != 1) iop = new TTABNormal(io);
                else iop = new IffFieldEncode(io);

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

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteInt32(0);
                io.WriteInt32(0); //version

                io.WriteCString("TTAT", 4);

                io.WriteByte(0); //compression code
                io.WriteInt32(TypeAttributesByGUID.Count);
                foreach (var tatt in TypeAttributesByGUID)
                {
                    io.WriteUInt32(tatt.Key);
                    io.WriteInt32(tatt.Value.Length);
                    foreach (var value in tatt.Value)
                        io.WriteInt16(value);
                }

            }
            return true;
        }
    }
}

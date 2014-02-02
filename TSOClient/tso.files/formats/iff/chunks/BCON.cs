using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using tso.files.utils;

namespace tso.files.formats.iff.chunks
{
    public class BCON : AbstractIffChunk
    {
        public byte Flags;
        public ushort[] Constants;

        public override void Read(Iff iff, Stream stream){
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN)){
                var num = io.ReadByte();
                Flags = io.ReadByte();

                Constants = new ushort[num];
                for (var i = 0; i < num; i++){
                    Constants[i] = io.ReadUInt16();
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSO.Files.utils;

namespace TSO.Files.formats.iff.chunks
{
    /// <summary>
    /// This chunk type holds the filename of a semi-global iff file used by this object.
    /// </summary>
    public class GLOB : IffChunk
    {
        public string Name;

        public override void Read(Iff iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                StringBuilder temp = new StringBuilder();
                var num = io.ReadByte();
                if (num < 48)
                { //less than smallest ASCII value for valid filename character, so assume this is a pascal string
                    temp.Append(io.ReadCString(num));
                }
                else
                { //we're actually a null terminated string!
                    temp.Append((char)num);
                    while (stream.Position < stream.Length)
                    {
                        char read = (char)io.ReadByte();
                        if (read == 0) break;
                        else temp.Append(read);
                    }
                }
                Name = temp.ToString();
            }
        }
    }
}

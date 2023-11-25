using System.Text;
using System.IO;
using FSO.Files.Utils;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// This chunk type holds the filename of a semi-global iff file used by this object.
    /// </summary>
    public class GLOB : IffChunk
    {
        public string Name;

        /// <summary>
        /// Reads a GLOB chunk from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream object holding a GLOB chunk.</param>
        public override void Read(IffFile iff, Stream stream)
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

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteNullTerminatedString(Name);
            }
            return true;
        }
    }
}

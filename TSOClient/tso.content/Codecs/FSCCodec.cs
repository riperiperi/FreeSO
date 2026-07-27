using FSO.Content.Framework;
using FSO.Files.HIT;

namespace FSO.Content.Codecs
{
    internal class FSCCodec : IContentCodec<FSC>
    {
        public override object GenDecode(System.IO.Stream stream)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                var data = ms.ToArray();
                return new FSC(data);
            }
        }
    }
}

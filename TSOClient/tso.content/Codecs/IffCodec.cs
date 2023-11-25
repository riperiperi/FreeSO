using FSO.Content.Framework;
using FSO.Files.Formats.IFF;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for iffs (*.iff).
    /// </summary>
    public class IffCodec : IContentCodec<IffFile>
    {
        #region IContentCodec<Iff> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var result = new IffFile();
            result.Read(stream);
            return result;
        }

        #endregion
    }
}

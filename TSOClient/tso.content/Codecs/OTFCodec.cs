using FSO.Content.Framework;
using FSO.Files.Formats.OTF;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for object tuning files (*.otf).
    /// </summary>
    public class OTFCodec : IContentCodec<OTFFile>
    {
        #region IContentCodec<OTF> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var result = new OTFFile();
            result.Read(stream);
            return result;
        }

        #endregion
    }
}

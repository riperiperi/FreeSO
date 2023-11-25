using FSO.Content.Framework;
using FSO.Vitaboy;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for bcf files (*.bcf).
    /// </summary>
    public class BCFCodec : IContentCodec<BCF>
    {
        #region IContentCodec<OTF> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var result = new BCF(stream, false);
            return result;
        }

        #endregion
    }
}

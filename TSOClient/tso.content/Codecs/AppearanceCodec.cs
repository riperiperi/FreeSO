using FSO.Vitaboy;
using FSO.Content.Framework;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for appearances (*.apr).
    /// </summary>
    public class AppearanceCodec : IContentCodec<Appearance>
    {
        #region IContentCodec<Appearance> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var result = new Appearance();
            result.Read(stream);
            return result;
        }

        #endregion
    }
}

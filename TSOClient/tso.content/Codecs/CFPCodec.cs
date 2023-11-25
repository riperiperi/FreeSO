using FSO.Content.Framework;
using FSO.Vitaboy;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for cfp files (*.cfp).
    /// </summary>
    public class CFPCodec : IContentCodec<CFP>
    {
        #region IContentCodec<CFP> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var result = new CFP();
            result.Read(stream);
            return result;
        }

        #endregion
    }
}

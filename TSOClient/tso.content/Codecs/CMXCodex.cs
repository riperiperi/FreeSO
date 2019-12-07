using FSO.Content.Framework;
using FSO.Vitaboy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for cmx files (*.cmx). 
    /// for some reason, these are plaintext bcf.
    /// </summary>
    public class CMXCodec : IContentCodec<BCF>
    {
        #region IContentCodec<OTF> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var result = new BCF(stream, true);
            return result;
        }

        #endregion
    }
}

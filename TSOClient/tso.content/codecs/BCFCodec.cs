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

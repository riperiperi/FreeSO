using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content.framework;
using TSO.Files.formats.otf;

namespace TSO.Content.codecs
{
    /// <summary>
    /// Codec for object tuning files (*.otf).
    /// </summary>
    public class OTFCodec : IContentCodec<OTF>
    {
        #region IContentCodec<OTF> Members

        public OTF Decode(System.IO.Stream stream)
        {
            var result = new OTF();
            result.Read(stream);
            return result;
        }

        #endregion
    }
}

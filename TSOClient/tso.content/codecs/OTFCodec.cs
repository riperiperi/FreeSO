using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content.framework;
using tso.files.formats.otf;

namespace tso.content.codecs
{
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

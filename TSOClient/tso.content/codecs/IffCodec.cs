using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content.framework;
using tso.files.formats.iff;

namespace tso.content.codecs
{
    public class IffCodec : IContentCodec<Iff>
    {
        #region IContentCodec<Iff> Members

        public Iff Decode(System.IO.Stream stream)
        {
            var result = new Iff();
            result.Read(stream);
            return result;
        }

        #endregion
    }
}

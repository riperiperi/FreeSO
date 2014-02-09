using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.vitaboy;
using tso.content.framework;

namespace tso.content.codecs
{
    public class AppearanceCodec : IContentCodec<Appearance>
    {
        #region IContentCodec<Appearance> Members

        public Appearance Decode(System.IO.Stream stream)
        {
            var result = new Appearance();
            result.Read(stream);
            return result;
        }

        #endregion
    }
}

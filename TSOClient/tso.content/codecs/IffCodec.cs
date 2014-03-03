using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content.framework;
using TSO.Files.formats.iff;

namespace TSO.Content.codecs
{
    /// <summary>
    /// Codec for iffs (*.iff).
    /// </summary>
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

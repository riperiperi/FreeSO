using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content.framework;
using tso.vitaboy;

namespace tso.content.codecs
{
    /// <summary>
    /// Codec for outfits (*.oft).
    /// </summary>
    public class OutfitCodec : IContentCodec<Outfit>
    {
        #region IContentCodec<Outfit> Members

        public Outfit Decode(System.IO.Stream stream)
        {
            var outfit = new Outfit();
            outfit.Read(stream);
            return outfit;
        }

        #endregion
    }
}

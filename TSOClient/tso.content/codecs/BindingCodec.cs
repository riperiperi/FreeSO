using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Vitaboy;
using TSO.Content.framework;

namespace TSO.Content.codecs
{
    /// <summary>
    /// Codec for bindings (*.bnd).
    /// </summary>
    public class BindingCodec : IContentCodec<Binding>
    {

        #region IContentCodec<Binding> Members

        public Binding Decode(System.IO.Stream stream)
        {
            var binding = new Binding();
            binding.Read(stream);
            return binding;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.vitaboy;
using tso.content.framework;

namespace tso.content.codecs
{
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

using FSO.Vitaboy;
using FSO.Content.Framework;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for bindings (*.bnd).
    /// </summary>
    public class BindingCodec : IContentCodec<Binding>
    {

        #region IContentCodec<Binding> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var binding = new Binding();
            binding.Read(stream);
            return binding;
        }

        #endregion
    }
}

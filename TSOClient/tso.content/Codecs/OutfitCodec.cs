using FSO.Content.Framework;
using FSO.Vitaboy;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for outfits (*.oft).
    /// </summary>
    public class OutfitCodec : IContentCodec<Outfit>
    {
        #region IContentCodec<Outfit> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var outfit = new Outfit();
            outfit.Read(stream);
            return outfit;
        }

        #endregion
    }
}

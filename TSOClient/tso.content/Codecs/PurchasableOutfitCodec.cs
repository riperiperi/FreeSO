using FSO.Content.Framework;
using FSO.Vitaboy;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for purchasable outfits (*.po).
    /// </summary>
    public class PurchasableOutfitCodec : IContentCodec<PurchasableOutfit>
    {
        #region IContentCodec<Outfit> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var outfit = new PurchasableOutfit();
            outfit.Read(stream);
            return outfit;
        }

        #endregion
    }
}

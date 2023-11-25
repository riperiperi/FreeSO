using FSO.Content.Model;

namespace FSO.Common.Domain.RealestateDomain
{
    public interface IShardRealestateDomain
    {
        int GetPurchasePrice(ushort x, ushort y);
        bool IsPurchasable(ushort x, ushort y);
        int GetSlope(ushort x, ushort y);
        CityMap GetMap();
    }
}

using FSO.Content.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Domain.RealestateDomain
{
    public interface IShardRealestateDomain
    {
        int GetPurchasePrice(ushort x, ushort y);
        bool IsPurchasable(ushort x, ushort y);
        CityMap GetMap();
    }
}

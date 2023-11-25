using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using System.Collections.Immutable;

namespace FSO.Common.DataService.Providers.Client
{
    public class ClientCityProvider : ReceiveOnlyServiceProvider<uint, City>
    {
        protected override City CreateInstance(uint key)
        {
            var city = new City
            {
                City_NeighborhoodsVec = ImmutableList.Create<uint>(),
                City_OnlineLotVector = ImmutableList.Create<bool>(),
                City_ReservedLotInfo = ImmutableDictionary.Create<uint, bool>(),
                City_ReservedLotVector = ImmutableList.Create<bool>(),
                City_SpotlightsVector = ImmutableList.Create<uint>(),
                City_Top100ListIDs = ImmutableList.Create<uint>(),
                City_TopTenNeighborhoodsVector = ImmutableList.Create<uint>()
            };

            return city;
        }
    }
}

using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                City_ReservedLotInfo = new Dictionary<uint, bool>(),
                City_ReservedLotVector = ImmutableList.Create<bool>(),
                City_SpotlightsVector = ImmutableList.Create<uint>(),
                City_Top100ListIDs = ImmutableList.Create<uint>(),
                City_TopTenNeighborhoodsVector = ImmutableList.Create<uint>()
            };

            return city;
        }
    }
}

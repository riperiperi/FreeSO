using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using System;
using System.Collections.Generic;
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
                City_NeighborhoodsVec = new List<uint>(),
                City_OnlineLotVector = new List<bool>(),
                City_ReservedLotInfo = new Dictionary<uint, bool>(),
                City_ReservedLotVector = new List<bool>(),
                City_SpotlightsVector = new List<uint>(),
                City_Top100ListIDs = new List<uint>(),
                City_TopTenNeighborhoodsVector = new List<uint>()
            };

            return city;
        }
    }
}

using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using FSO.Common.Domain.Realestate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Providers.Client
{
    public class ClientLotProvider : ReceiveOnlyServiceProvider<uint, Lot>
    {
        protected override Lot CreateInstance(uint key)
        {
            var coords = MapCoordinates.Unpack(key);

            var lot = base.CreateInstance(key);
            lot.Id = key;
            lot.Lot_Location = new Location()
            {
                Location_X = coords.X,
                Location_Y = coords.Y
            };
            //TODO: Use the string tables
            lot.Lot_Name = "Retrieving...";
            return lot;
        }
    }
}

using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.DataService.Providers
{
    public class ServerCityProvider : AbstractDataServiceProvider<uint, City>
    {
        private ServerLotProvider Lots;

        public void BindLots(ServerLotProvider lots)
        {
            Lots = lots;
        }

        public override Task<object> Get(object key)
        {
            //The lot provider actually knows a lot about this anyways, so they provide out sole city object.
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(Lots.CityRepresentation);
            return tcs.Task;
        }
    }
}

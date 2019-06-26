using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Providers.Client
{
    public class ClientNeighProvider : ReceiveOnlyServiceProvider<uint, Neighborhood>
    {
        protected override Neighborhood CreateInstance(uint key)
        {
            var neigh = base.CreateInstance(key);
            neigh.Id = key;
            neigh.Neighborhood_Name = "Retrieving...";
            return neigh;
        }

    }
}

using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;

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

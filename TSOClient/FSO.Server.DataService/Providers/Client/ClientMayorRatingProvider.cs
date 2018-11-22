using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Providers.Client
{
    public class ClientMayorRatingProvider : ReceiveOnlyServiceProvider<uint, MayorRating>
    {
        protected override MayorRating CreateInstance(uint key)
        {
            var rating = base.CreateInstance(key);
            rating.Id = key;
            rating.MayorRating_Comment = "Retrieving...";
            return rating;
        }

    }
}

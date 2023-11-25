using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;

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

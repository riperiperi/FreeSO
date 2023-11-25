using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using FSO.Server.Database.DA;
using Ninject;
using NLog;

namespace FSO.Server.DataService.Providers
{
    public class ServerMayorRatingProvider : LazyDataServiceProvider<uint, MayorRating>
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private int ShardId;
        private IDAFactory DAFactory;

        public ServerMayorRatingProvider([Named("ShardId")] int shardId, IDAFactory factory)
        {
            this.ShardId = shardId;
            this.DAFactory = factory;
        }

        protected override MayorRating LazyLoad(uint key, MayorRating oldVal)
        {
            using (var db = DAFactory.Get())
            {
                var rating = db.Elections.GetRating(key);
                if (rating == null) { return null; }

                return new MayorRating()
                {
                    MayorRating_Comment = rating.comment,
                    MayorRating_Date = rating.date,
                    MayorRating_FromAvatar = (rating.anonymous > 0) ? 0 : rating.from_avatar_id,
                    MayorRating_ToAvatar = rating.to_avatar_id,
                    MayorRating_HalfStars = rating.rating,
                    MayorRating_Neighborhood = rating.neighborhood,
                    Id = key
                };
            }
        }
    }
}

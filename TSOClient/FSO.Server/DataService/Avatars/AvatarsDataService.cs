using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Database.DA.Shards;
using FSO.Server.Protocol.Voltron.DataService;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.DataService.Avatars
{
    public class AvatarsDataService : AbstractLoadingDataService<uint, Avatar>
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private Shard Shard;
        private IDAFactory DAFactory;
        
        public AvatarsDataService(Shard shard, IDAFactory factory) {
            this.Shard = shard;
            this.DAFactory = factory;
        }

        protected override Avatar LoadOne(uint id){
            using (var db = DAFactory.Get()){
                var avatar = db.Avatars.Get(id);
                if (avatar == null) { return null; }
                if (avatar.shard_id != Shard.shard_id) { return null; }
                return HydrateOne(avatar);
            }
        }
        
        private Avatar HydrateOne(DbAvatar dbAvatar)
        {
            var result = new Avatar();
            result.Avatar_Name = dbAvatar.name;
            result.Avatar_IsOnline = false;
            result.Avatar_Description = dbAvatar.description;
            result.Avatar_Appearance = new AvatarAppearance {
                AvatarAppearance_BodyOutfitID = dbAvatar.body,
                AvatarAppearance_HeadOutfitID = dbAvatar.head,
                AvatarAppearance_SkinTone = dbAvatar.skin_tone
            };
            result.Avatar_BookmarksVec = new List<Bookmark>()
            {
                { new Bookmark { Bookmark_TargetID = 0x02, Bookmark_Type = BookmarkType.BOOKMARK } }
            };
            return result;
        }

        protected override List<Avatar> LoadAll(){
            throw new NotImplementedException();
        }

        private List<Avatar> HydrateAll(List<DbAvatar> avatars){
            throw new NotImplementedException();
        }
    }
}

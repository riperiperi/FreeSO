using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using FSO.Common.Security;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.Shards;
using Ninject;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Providers.Server
{
    public class ServerAvatarProvider : LazyDataServiceProvider<uint, Avatar>
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private int ShardId;
        private IDAFactory DAFactory;

        public ServerAvatarProvider([Named("ShardId")] int shardId, IDAFactory factory)
        {
            this.ShardId = shardId;
            this.DAFactory = factory;
        }

        public override void PersistMutation(object entity, MutationType type, string path, object value)
        {
            var avatar = entity as Avatar;

            using (var db = DAFactory.Get())
            {
                switch (path)
                {
                    case "Avatar_Description":
                        db.Avatars.UpdateDescription(avatar.Avatar_Id, avatar.Avatar_Description);
                        break;
                }
            }
        }

        public override void DemandMutation(object entity, MutationType type, string path, object value, ISecurityContext context)
        {
            var avatar = entity as Avatar;

            switch (path)
            {
                case "Avatar_BookmarksVec":
                case "Avatar_Description":
                    context.DemandAvatar(avatar.Avatar_Id, AvatarPermissions.WRITE);
                    var desc = value as string;
                    if (desc != null && desc.Length > 500)
                        throw new Exception("Description too long!");
                    break;

                default:
                    throw new SecurityException("Field: " + path + " may not be mutated by users");
            }
        }

        protected override Avatar LazyLoad(uint key)
        {
            using (var db = DAFactory.Get())
            {
                var avatar = db.Avatars.Get(key);
                if (avatar == null) { return null; }
                if (avatar.shard_id != ShardId) { return null; }

                var lot = db.Lots.GetByOwner(avatar.avatar_id);

                return HydrateOne(avatar, lot);
            }
        }

        private Avatar HydrateOne(DbAvatar dbAvatar, DbLot dbLot)
        {
            var result = new Avatar();
            result.Avatar_Id = dbAvatar.avatar_id;
            result.Avatar_Name = dbAvatar.name;
            result.Avatar_IsOnline = false;
            result.Avatar_Description = dbAvatar.description;
            result.Avatar_Appearance = new AvatarAppearance
            {
                AvatarAppearance_BodyOutfitID = dbAvatar.body,
                AvatarAppearance_HeadOutfitID = dbAvatar.head,
                AvatarAppearance_SkinTone = dbAvatar.skin_tone
            };
            result.Avatar_Age = 100;
            result.Avatar_Skills = new AvatarSkills
            {
                AvatarSkills_Body = 400,
                AvatarSkills_LockLv_Body = 2
            };
            result.Avatar_SkillsLockPoints = 10;

            if (dbLot != null){
                result.Avatar_LotGridXY = dbLot.location;
            }

            result.Avatar_BookmarksVec = new List<Bookmark>()
            {
                { new Bookmark { Bookmark_TargetID = 0x02, Bookmark_Type = (byte)BookmarkType.BOOKMARK } }
            };
            return result;
        }
    }
}

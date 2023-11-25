using FSO.Common.DataService;
using FSO.Files.Formats.tsodata;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Bulletin;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Servers.City.Domain;
using Ninject;
using NLog;
using System;
using System.Linq;

namespace FSO.Server.Servers.City.Handlers
{
    public class BulletinHandler
    {
        private IDAFactory DA;
        private IDataService DataService;
        private CityServerContext Context;
        private IKernel Kernel;
        private Neighborhoods Nhoods;
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private int POST_FREQ_LIMIT = 60 * 60 * 24 * 3;
        private int POST_FREQ_LIMIT_MAYOR = 60 * 60 * 24;
        private int MOVE_LIMIT_PERIOD = 60 * 60 * 24 * 7;

        public BulletinHandler(CityServerContext context, IDAFactory da, IDataService dataService, IKernel kernel, Neighborhoods nhoods)
        {
            Context = context;
            DA = da;
            DataService = dataService;
            Kernel = kernel;
            Nhoods = nhoods;

            POST_FREQ_LIMIT = context.Config.Neighborhoods.Bulletin_Post_Frequency * 60 * 60 * 24;
            POST_FREQ_LIMIT_MAYOR = context.Config.Neighborhoods.Bulletin_Mayor_Frequency * 60 * 60 * 24;
            MOVE_LIMIT_PERIOD = context.Config.Neighborhoods.Bulletin_Move_Penalty * 60 * 60 * 24;
        }

        private BulletinItem ToItem(DbBulletinPost post)
        {
            return new BulletinItem()
            {
                ID = post.bulletin_id,
                Subject = post.title,
                Body = post.body,
                SenderID = post.avatar_id ?? 0,
                Time = post.date,
                Flags = (BulletinFlags)post.flags,
                LotID = (uint)(post.lot_id ?? 0),
                NhoodID = (uint)post.neighborhood_id,
                SenderName = "",
                Type = (BulletinType)post.type,
            };
        }

        private BulletinResponse Code(BulletinResponseType code)
        {
            return new BulletinResponse()
            {
                Type = code
            };
        }

        public void Handle(IVoltronSession session, BulletinRequest message)
        {
            if (session.IsAnonymous)  //CAS users can't do this.
                return;

            try
            {
                using (var da = DA.Get())
                {
                    switch (message.Type)
                    {
                        case BulletinRequestType.GET_MESSAGES:
                            //when a user logs in they will send this request to recieve all messages after their last recieved message.
                            {
                                var msgs = da.BulletinPosts.GetByNhoodId(message.TargetNHood, 0);
                                session.Write(new BulletinResponse()
                                {
                                    Type = BulletinResponseType.MESSAGES,
                                    Messages = msgs.Select(x => ToItem(x)).ToArray()
                                });
                                return;
                            }
                        case BulletinRequestType.DELETE_MESSAGE:
                            //delete bulletin message
                            {
                                //check if either we own this post or we're admin
                                var post = da.BulletinPosts.Get(message.Value);
                                if (post == null)
                                {
                                    session.Write(Code(BulletinResponseType.FAIL_MESSAGE_DOESNT_EXIST));
                                    return; //doesn't exist
                                }
                                if (post.avatar_id != session.AvatarId)
                                {
                                    //not the owner of this post. are we an admin?
                                    var myAva = da.Avatars.Get(session.AvatarId);
                                    if (myAva == null || myAva.moderation_level == 0)
                                    {
                                        session.Write(Code(BulletinResponseType.FAIL_CANT_DELETE));
                                        return;
                                    }
                                }

                                da.BulletinPosts.SoftDelete(message.Value);
                                session.Write(Code(BulletinResponseType.SUCCESS));
                                return;
                            }
                        case BulletinRequestType.PROMOTE_MESSAGE:
                            //promote message to the mayor channel
                            {
                                //check if either we're mayor of this post's nhood or we're admin
                                //also the post should like, exist.
                                var post = da.BulletinPosts.Get(message.Value);
                                if (post == null)
                                {
                                    session.Write(Code(BulletinResponseType.FAIL_MESSAGE_DOESNT_EXIST));
                                    return;
                                }
                                if ((post.flags & 1) > 0)
                                {
                                    session.Write(Code(BulletinResponseType.FAIL_ALREADY_PROMOTED));
                                    return; //already promoted.
                                }
                                if (post.type != DbBulletinType.community)
                                {
                                    session.Write(Code(BulletinResponseType.FAIL_BAD_PERMISSION));
                                    return;
                                }
                                var postNhood = da.Neighborhoods.Get((uint)post.neighborhood_id);
                                if (postNhood == null || postNhood.mayor_id != session.AvatarId)
                                {
                                    session.Write(Code(BulletinResponseType.FAIL_NOT_MAYOR));
                                    return; //no permission
                                }
                                post.flags |= 1;

                                da.BulletinPosts.SetTypeFlag(message.Value, DbBulletinType.mayor, (int)post.flags);
                                session.Write(Code(BulletinResponseType.SUCCESS));
                                return;
                            }
                        case BulletinRequestType.CAN_POST_MESSAGE:
                        case BulletinRequestType.CAN_POST_SYSTEM_MESSAGE:
                        case BulletinRequestType.POST_SYSTEM_MESSAGE:
                        case BulletinRequestType.POST_MESSAGE:
                            {
                                var type = (message.Type == BulletinRequestType.POST_SYSTEM_MESSAGE) ? DbBulletinType.system : DbBulletinType.community;
                                var myAva = da.Avatars.Get(session.AvatarId);
                                if (myAva == null)
                                {
                                    session.Write(Code(BulletinResponseType.FAIL_UNKNOWN));
                                    return;
                                }

                                var now = Epoch.Now;
                                if (now - myAva.move_date < MOVE_LIMIT_PERIOD && myAva.moderation_level == 0)
                                {
                                    session.Write(Code(BulletinResponseType.SEND_FAIL_JUST_MOVED)); return;
                                }

                                var myLotID = da.Roommates.GetAvatarsLots(session.AvatarId)?.FirstOrDefault();
                                DbLot myLot = (myLotID != null) ? da.Lots.Get(myLotID.lot_id) : null;

                                if (myLot == null || myLot.neighborhood_id != message.TargetNHood || 
                                    message.Type == BulletinRequestType.POST_SYSTEM_MESSAGE || message.Type == BulletinRequestType.CAN_POST_SYSTEM_MESSAGE)
                                {
                                    //need to live in this nhood to post
                                    //if we're an admin we can ignore this
                                    if (myAva.moderation_level == 0)
                                    {
                                        session.Write(Code(BulletinResponseType.SEND_FAIL_NON_RESIDENT));
                                        return;
                                    }
                                }

                                var postNhood = da.Neighborhoods.Get((uint)message.TargetNHood);
                                if (postNhood == null)
                                {
                                    session.Write(Code(BulletinResponseType.FAIL_UNKNOWN));
                                    return;
                                }
                                if (session.AvatarId == postNhood.mayor_id && type == DbBulletinType.community) type = DbBulletinType.mayor;

                                //are we nhood gameplay banned?

                                var ban = da.Neighborhoods.GetNhoodBan(myAva.user_id);
                                if (ban != null)
                                {
                                    session.Write(new BulletinResponse
                                    {
                                        Type = BulletinResponseType.SEND_FAIL_GAMEPLAY_BAN,
                                        Message = ban.ban_reason,
                                        BanEndDate = ban.end_date
                                    });
                                    return;
                                }

                                //verify post frequency
                                var last = da.BulletinPosts.LastUserPost(myAva.user_id, message.TargetNHood);
                                int frequency = 0;
                                switch (type)
                                {
                                    case DbBulletinType.mayor:
                                        frequency = POST_FREQ_LIMIT_MAYOR; break;
                                    case DbBulletinType.community:
                                        frequency = POST_FREQ_LIMIT; break;
                                }
                                if (Epoch.Now - (last?.date ?? 0) < frequency)
                                {
                                    session.Write(Code(BulletinResponseType.SEND_FAIL_TOO_FREQUENT));
                                    return;
                                }

                                if (message.Type == BulletinRequestType.CAN_POST_MESSAGE || message.Type == BulletinRequestType.CAN_POST_SYSTEM_MESSAGE)
                                {
                                    session.Write(Code(BulletinResponseType.SUCCESS));
                                    return;
                                }

                                int? lotID = null;
                                if (message.LotID != 0)
                                {
                                    //verify the lot ID if one is included
                                    var lot = da.Lots.GetByLocation(Context.ShardId, message.LotID);
                                    if (lot == null)
                                    {
                                        session.Write(Code(BulletinResponseType.SEND_FAIL_INVALID_LOT));
                                        return;
                                    }
                                    lotID = (int)message.LotID;
                                }

                                if (message.Message.Length == 0 || message.Message.Length > 1000)
                                {
                                    session.Write(Code(BulletinResponseType.SEND_FAIL_INVALID_MESSAGE));
                                    return;
                                }
                                if (message.Title.Length == 0 || message.Title.Length > 64)
                                {
                                    session.Write(Code(BulletinResponseType.SEND_FAIL_INVALID_TITLE));
                                    return;
                                }

                                var db = new DbBulletinPost()
                                {
                                    avatar_id = session.AvatarId,
                                    date = Epoch.Now,
                                    title = message.Title,
                                    body = message.Message,
                                    flags = 0,
                                    lot_id = lotID,
                                    neighborhood_id = (int)message.TargetNHood,
                                    type = type
                                };
                                try
                                {
                                    db.bulletin_id = da.BulletinPosts.Create(db);
                                }
                                catch (Exception e)
                                {
                                    LOG.Error(e.ToString());
                                    session.Write(Code(BulletinResponseType.FAIL_UNKNOWN));
                                    return;
                                }

                                session.Write(new BulletinResponse()
                                {
                                    Type = BulletinResponseType.SEND_SUCCESS,
                                    Messages = new BulletinItem[] { ToItem(db) }
                                });
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                LOG.Error(e.ToString());
                session.Write(Code(BulletinResponseType.FAIL_UNKNOWN));
                return;
            }
        }
    }
}

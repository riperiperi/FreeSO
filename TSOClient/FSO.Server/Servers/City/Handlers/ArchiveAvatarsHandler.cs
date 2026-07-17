using FSO.Common;
using FSO.Common.DataService;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.CitySelector;
using FSO.Server.Protocol.Electron.Packets;
using Ninject;
using NLog;

namespace FSO.Server.Servers.City.Handlers
{
    internal class ArchiveAvatarsHandler
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private IDAFactory DA;
        private CityServerContext Context;
        private IKernel Kernel;

        private Lock SharedAvatarsCacheLock = new();
        private Task<ArchiveAvatar[]> SharedAvatarsCache;

        public ArchiveAvatarsHandler(CityServerContext context, IDAFactory da, IDataService dataService, IKernel kernel)
        {
            Context = context;
            DA = da;
            Kernel = kernel;
        }

        private ArchiveAvatar[] GetSharedAvatars(IDA da)
        {
            Task<ArchiveAvatar[]> task;

            lock (SharedAvatarsCacheLock)
            {
                if (SharedAvatarsCache == null)
                {
                    SharedAvatarsCache = Task.Run(() =>
                    {
                        var shared = da.Avatars.GetSummaryByUserId(1);
                        return shared.Select(ToArchiveAvatar).ToArray();
                    });
                }

                task = SharedAvatarsCache;
            }

            return task.Result;
        }

        private static ArchiveAvatar ToArchiveAvatar(DbAvatarSummary ava)
        {
            return new ArchiveAvatar()
            {
                AvatarId = ava.avatar_id,
                UserId = ava.user_id,
                LotId = ava.lot_location ?? 0,
                Name = ava.name,
                LotName = ava.lot_name,
                Type = (AvatarAppearanceType)ava.skin_tone,
                Head = ava.head,
                Body = ava.body
            };
        }

        public async void Handle(IVoltronSession session, ArchiveAvatarsRequest _packet)
        {
            if (Context.Config.Archive == null)
                return;

            if (session.UserId == 0)
                return;

            try
            {
                if (session is VoltronSession vSession && vSession.Unverified)
                {
                    // User must be verified first.
                    session.Write(new ArchiveAvatarsResponse()
                    {
                        IsVerified = false,
                        CasEnabled = false,
                        RecentAvatars = [],
                        UserAvatars = [],
                        SharedAvatars = [],
                    });

                    return;
                }

                using (var da = DA.Get())
                {
                    var forUser = da.Avatars.GetSummaryByUserId(session.UserId);

                    var userAvatars = forUser.Select(ToArchiveAvatar).ToArray();

                    // TODO: cache?

                    var archiveFlags = Context.Config.Archive.Flags;

                    bool canUseArchive = !archiveFlags.HasFlag(ArchiveConfigFlags.LockArchivedSims) || session.HasModerationLevel(1);

                    ArchiveAvatar[] sharedAvatars;

                    if (canUseArchive)
                    {
                        sharedAvatars = GetSharedAvatars(da);
                    }
                    else
                    {
                        sharedAvatars = [];
                    }

                    // Can't cache this obviously
                    var mostRecent = da.ArchiveRecents.AvatarsByUser((int)session.UserId, 5);
                    var recentAvatars = mostRecent.Where(x => userAvatars.Any(y => y.AvatarId == x) || sharedAvatars.Any(y => y.AvatarId == x)).Select(x => (uint)x).ToArray();

                    session.Write(new ArchiveAvatarsResponse()
                    {
                        IsVerified = true,
                        CasEnabled = archiveFlags.HasFlag(ArchiveConfigFlags.AllowSimCreation) || session.HasModerationLevel(1),
                        UserAvatars = userAvatars,
                        SharedAvatars = sharedAvatars,
                        RecentAvatars = recentAvatars
                    });
                }
            }
            catch
            {

            }
        }
      }
}

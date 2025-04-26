using FSO.Common.DataService;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.CitySelector;
using FSO.Server.Protocol.Electron.Packets;
using Ninject;
using NLog;
using System.Linq;

namespace FSO.Server.Servers.City.Handlers
{
    internal class ArchiveAvatarsHandler
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private IDAFactory DA;
        private CityServerContext Context;
        private IKernel Kernel;

        public ArchiveAvatarsHandler(CityServerContext context, IDAFactory da, IDataService dataService, IKernel kernel)
        {
            Context = context;
            DA = da;
            Kernel = kernel;
        }

        private static ArchiveAvatar ToArchiveAvatar(DbAvatar ava)
        {
            return new ArchiveAvatar()
            {
                AvatarId = ava.avatar_id,
                UserId = ava.user_id,
                Name = ava.name,
                Type = (AvatarAppearanceType)ava.skin_tone,
                Head = ava.head,
                Body = ava.body
            };
        }

        public async void Handle(IVoltronSession session, ArchiveAvatarsRequest _packet)
        {
            if (Context.Config.ArchiveGUID == null)
                return;

            if (session.UserId == 0)
                return;

            try
            {
                using (var da = DA.Get())
                {
                    var forUser = da.Avatars.GetByUserId(session.UserId);

                    var userAvatars = forUser.Select(ToArchiveAvatar).ToArray();

                    // TODO: cache?

                    var shared = da.Avatars.GetByUserId(1);
                    var sharedAvatars = shared.Select(ToArchiveAvatar).ToArray();

                    session.Write(new ArchiveAvatarsResponse()
                    {
                        UserAvatars = userAvatars,
                        SharedAvatars = sharedAvatars
                    });
                }
            }
            catch
            {

            }
        }
      }
}

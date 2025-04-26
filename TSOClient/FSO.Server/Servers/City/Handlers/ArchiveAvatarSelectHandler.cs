using FSO.Common.DataService;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.AvatarClaims;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using Ninject;
using NLog;
using System.Threading;

namespace FSO.Server.Servers.City.Handlers
{
    internal static class ArchiveAvatarSelectExtensions
    {
        public static void Response(this IVoltronSession session, ArchiveAvatarSelectCode code)
        {
            session.Write(new ArchiveAvatarSelectResponse()
            {
                Code = code
            });
        }
    }

    internal class ArchiveAvatarSelectHandler
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private IDAFactory DA;
        private CityServerContext Context;
        private IKernel Kernel;

        public ArchiveAvatarSelectHandler(CityServerContext context, IDAFactory da, IDataService dataService, IKernel kernel)
        {
            Context = context;
            DA = da;
            Kernel = kernel;
        }

        public async void Handle(IVoltronSession session, ArchiveAvatarSelectRequest packet)
        {
            var config = Context.Config;

            if (config.ArchiveGUID == null)
                return;

            if (session.UserId == 0 || session.AvatarId != 0)
                return;

            uint avatarId = packet.AvatarId;
            uint userId = session.UserId;

            try
            {
                using (var da = DA.Get())
                {
                    var ava = da.Avatars.Get(avatarId);

                    if (ava == null)
                    {
                        session.Response(ArchiveAvatarSelectCode.NotFound);
                        return;
                    }

                    // permissions check - currently supports shared and owned avatars but pretty fixed

                    if (ava.user_id != session.UserId && ava.user_id != 1)
                    {
                        session.Response(ArchiveAvatarSelectCode.NoPermission);
                        return;
                    }

                    // Try to claim the avatar for this session.

                    int? claim = da.AvatarClaims.TryCreate(new DbAvatarClaim
                    {
                        avatar_id = avatarId,
                        location = 0,
                        owner = config.Call_Sign
                    });

                    if (!claim.HasValue)
                    {
                        //Try and disconnect this user, if we still can't get a claim out of luck
                        //The voltron session close should handle removing any lot tickets and disconnecting them from the target servers
                        //then it will remove the avatar claim. This takes time but it should be less than 5 seconds.
                        var existingSession = Context.Sessions.GetByAvatarId(avatarId);
                        if (existingSession != null)
                        {
                            // If the session is owned by another user, we can't close it.
                            // TODO: allow if the user is admin?

                            if (existingSession.UserId != userId)
                            {
                                session.Response(ArchiveAvatarSelectCode.InUse);
                                return;
                            }

                            existingSession.Close();
                        }
                        else
                        {
                            //check if there really is an old claim
                            var oldClaim = da.AvatarClaims.GetByAvatarID(avatarId);
                            if (oldClaim != null)
                            {
                                da.AvatarClaims.Delete(oldClaim.avatar_claim_id, config.Call_Sign);
                                LOG.Debug("Zombie Avatar claim removed: Avatar ID " + avatarId);
                            }
                            else
                            {
                                LOG.Debug("Unknown claim error occurred. Connection will likely time out. Avatar ID " + avatarId);
                            }
                        }

                        // Wait for the claim to disappear.

                        int i = 0;
                        while (i < 10)
                        {
                            claim = da.AvatarClaims.TryCreate(new DbAvatarClaim
                            {
                                avatar_id = avatarId,
                                location = 0,
                                owner = config.Call_Sign
                            });

                            if (claim.HasValue)
                            {
                                break;
                            }

                            Thread.Sleep(500);
                            i++;
                        }

                        if (!claim.HasValue)
                        {
                            //No luck
                            session.Response(ArchiveAvatarSelectCode.InUseSelf);
                            session.Close();
                            return;
                        }
                    }

                    if (session is VoltronSession vSession)
                    {
                        vSession.AvatarId = avatarId;
                        vSession.AvatarClaimId = claim.Value;
                    }

                    // TODO: Try and update the avatar's moderation level to match the user

                    session.Response(ArchiveAvatarSelectCode.Success);
                    return;
                }
            }
            catch
            {

            }


            session.Response(ArchiveAvatarSelectCode.UnknownError);
        }
    }
}

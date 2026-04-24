using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.RealestateDomain;
using FSO.Content.Model;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using NLog;

namespace FSO.Server.Servers.City.Handlers
{
    internal class CityUpdateHandler
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private CityServerContext Context;
        private IRealestateDomain Realestate;

        public CityUpdateHandler(CityServerContext context, IRealestateDomain realestate)
        {
            Context = context;
            Realestate = realestate;
        }

        private IShardRealestateDomain GetShard(IVoltronSession session)
        {
            if (session.IsAnonymous)
                return null;

            if (!session.HasModerationLevel(1))
                return null;

            if (!(Context.Config.Archive?.Flags.HasFlag(FSO.Common.ArchiveConfigFlags.CityEditor) ?? false))
                return null;

            var shard = Realestate.GetByShard(Context.ShardId);

            return shard.Dynamic ? shard : null;
        }

        public async void Handle(IVoltronSession session, CityUpdateCommand packet)
        {
            var shard = GetShard(session);

            if (shard == null)
                return;

            if (session.AvatarId != packet.AvatarID)
                return;

            // TODO: proper ordering, thread safety
            if (shard.HandleUserCommand(packet))
            {
                Context.Broadcast(packet);
            }
        }

        public async void Handle(IVoltronSession session, CityUpdateRequest packet)
        {
            var shard = GetShard(session);

            if (shard == null)
                return;

            var cmd = packet.Command.Command;
            cmd.AvatarId = session.AvatarId;

            if (cmd.IsTemp)
            {
                // Temp commands aren't reflected in the city - they are forwarded to everyone else though.

                return;
            }

            // TODO: Add reserved locations from the server (any open lots)

            //lock (shard.UpdateLock)
            {
                int id = shard.AppendCommand(cmd);

                // TODO: proper ordering
                if (id != -1)
                {
                    Context.Broadcast(new CityUpdateResponse()
                    {
                        StartIndex = id,
                        Commands = [new(cmd)]
                    });
                }
            }
        }
    }
}

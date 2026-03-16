using FSO.Common.Domain.Realestate;
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

        public async void Handle(IVoltronSession session, CityUpdateRequest packet)
        {
            if (session.IsAnonymous)
                return;

            if (!session.HasModerationLevel(1))
                return;

            var shard = Realestate.GetByShard(Context.ShardId);

            if (!shard.Dynamic)
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

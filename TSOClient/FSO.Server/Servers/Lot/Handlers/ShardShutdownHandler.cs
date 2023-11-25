using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Servers.Lot.Domain;
using Ninject;

namespace FSO.Server.Servers.Lot.Handlers
{
    public class ShardShutdownHandler
    {
        private LotHost Lots;
        private IKernel Kernel;

        public ShardShutdownHandler(LotHost lots, IKernel kernel)
        {
            this.Lots = lots;
            this.Kernel = kernel;
        }

        public void Handle(IGluonSession session, ShardShutdownRequest request)
        {
            //todo: how to handle partial shard shutdown?
            //if there are two separate cities, sure they can both shut down separately and the lot server will not need a "full" shutdown.
            //...assuming they dont want to upgrade the software! in this case, reconnecting to the upgraded city server without upgrading ourselves
            //could prove to be an issue!

            //just assume for now that lot servers are for a single shard each.
            var server = Kernel.Get<LotServer>();
            Lots.Shutdown().ContinueWith(x =>
            {
                if (x.Result)
                {
                    //shutdown complete. Send a response!
                    session.Write(new ShardShutdownCompleteResponse
                    {
                        ShardId = request.ShardId
                    });
                    server.Shutdown(); //actually shut down the server
                    server.SignalInternalShutdown(request.Type);
                } else
                {
                    //shutdown already started likely.
                }
            });

        }
    }
}

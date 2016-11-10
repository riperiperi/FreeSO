using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Servers.Lot.Domain;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot.Handlers
{
    public class LotNegotiationHandler
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private LotHost Lots;

        public LotNegotiationHandler(LotHost lots)
        {
            this.Lots = lots;
        }

        public void Handle(IGluonSession session, TransferClaim request)
        {
            LOG.Info("Recieved lot host request... ");

            if (request.Type != Protocol.Gluon.Model.ClaimType.LOT)
            {
                session.Write(new TransferClaimResponse {
                    Status = TransferClaimResponseStatus.REJECTED,
                    Type = request.Type,
                    ClaimId = request.ClaimId,
                    EntityId = request.EntityId
                });
                return;
            }

            var lot = Lots.TryHost(request.EntityId, session);
            if(lot == null)
            {
                session.Write(new TransferClaimResponse
                {
                    Status = TransferClaimResponseStatus.REJECTED,
                    Type = request.Type,
                    ClaimId = request.ClaimId,
                    EntityId = request.EntityId
                });
                return;
            }

            if(Lots.TryAcceptClaim((int)request.EntityId, request.ClaimId, request.SpecialId, request.FromOwner))
            {
                session.Write(new TransferClaimResponse
                {
                    Status = TransferClaimResponseStatus.ACCEPTED,
                    Type = request.Type,
                    ClaimId = request.ClaimId,
                    EntityId = request.EntityId
                });
            }
            else
            {
                session.Write(new TransferClaimResponse
                {
                    Status = TransferClaimResponseStatus.CLAIM_NOT_FOUND,
                    Type = request.Type,
                    ClaimId = request.ClaimId,
                    EntityId = request.EntityId
                });
            }
        }

        public void Handle(IGluonSession session, RequestLotClientTermination request)
        {
            Lots.TryDisconnectClient(request.LotId, request.AvatarId);
        }
    }
}

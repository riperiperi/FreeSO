using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Servers.Lot.Domain;
using NLog;

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

            if(Lots.TryAcceptClaim((int)request.EntityId, request.ClaimId, request.SpecialId, request.FromOwner, request.Action))
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

        public void Handle(IGluonSession session, NotifyLotRoommateChange request)
        {
            Lots.NotifyRoommateChange(request.LotId, request.AvatarId, request.ReplaceId, request.Change);
        }

        public void Handle(IGluonSession session, TuningChanged request)
        {
            Lots.UpdateTuning(request.UpdateInstantly);
        }

        public void Handle(IGluonSession session, SetLotObjectLimitBonus request)
        {
            // No-op for any lot this server isn't currently hosting; UpdateObjectLimitBonus
            // returns false in that case. The DB write was already done by the API.
            Lots.UpdateObjectLimitBonus((int)request.LotId, request.TargetBonus);
        }

        public void Handle(IGluonSession session, SetAvatarSkillLockLimit request)
        {
            // Walks every hosted lot and updates the matching avatar's SkillLocks. The
            // ones not hosting that avatar early-return; the one that is hosting them
            // forwards a server-only VM command to bring the live PersonData[70] in
            // sync. DB was already updated by the API.
            Lots.UpdateAvatarSkillLockLimit(request.AvatarId, request.NewLimit);
        }
    }
}

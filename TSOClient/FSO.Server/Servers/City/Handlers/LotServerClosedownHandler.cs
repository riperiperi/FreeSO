using FSO.Server.Database.DA;
using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Servers.City.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class LotServerClosedownHandler
    {
        private LotAllocations Lots;
        private IDAFactory DAFactory;

        public LotServerClosedownHandler(LotAllocations lots, IDAFactory daFactory)
        {
            this.Lots = lots;
            this.DAFactory = daFactory;
        }

        public void Handle(IGluonSession session, TransferClaim request)
        {
            if (request.Type != Protocol.Gluon.Model.ClaimType.LOT)
            {
                //what?
                session.Write(new TransferClaimResponse
                {
                    Status = TransferClaimResponseStatus.REJECTED,
                    Type = request.Type,
                    ClaimId = request.ClaimId,
                    EntityId = request.EntityId
                });
                return;
            }

            Lots.TryClose(request.EntityId, request.ClaimId);
            try
            {
                using (var db = DAFactory.Get())
                {
                    db.LotClaims.Delete(request.ClaimId, request.FromOwner);
                }
            }
            catch (Exception e)
            {
                //probably already unclaimed. do nothing.
            }
        }
    }
}

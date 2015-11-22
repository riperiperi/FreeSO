using FSO.Server.Framework.Gluon;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Servers.City.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class JoinLotHandler
    {
        private LotServerPicker PickingEngine;
        private LotAllocations Lots;

        public JoinLotHandler(LotAllocations lots, LotServerPicker pickingEngine)
        {
            this.Lots = lots;
            this.PickingEngine = pickingEngine;
        }

        public async void Handle(IVoltronSession session, FindLotRequest packet)
        {
            var find = await Lots.TryFindOrOpen(packet.LotId, session);
            
            if(find.Status == Protocol.Electron.Model.FindLotResponseStatus.FOUND){
                session.Write(new FindLotResponse {
                    Status = find.Status,
                    LotId = packet.LotId
                });
            }
            else
            {
                //I need a shard ticket so I can connect to the lot server
                session.Write(new FindLotResponse {
                    Status = find.Status,
                    LotId = packet.LotId
                });
            }
        }

        public void Handle(IGluonSession session, TransferClaimResponse claimResponse)
        {
            if(claimResponse.Type == Protocol.Gluon.Model.ClaimType.LOT)
            {
                Lots.OnTransferClaimResponse(claimResponse);
            }
        }
    }
}

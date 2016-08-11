using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.RealestateDomain;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class PurchaseLotHandler
    {
        private IRealestateDomain GlobalRealestate;
        private IShardRealestateDomain Realestate;
        private IDAFactory DA;
        private IDataService DataService;
        private CityServerContext Context;
        
        public PurchaseLotHandler(CityServerContext context, IRealestateDomain realestate, IDAFactory da, IDataService dataService)
        {
            Context = context;
            GlobalRealestate = realestate;
            Realestate = realestate.GetByShard(context.ShardId);
            DA = da;
            DataService = dataService;
        }

        public async void Handle(IVoltronSession session, PurchaseLotRequest packet)
        {
            if (session.IsAnonymous){
                return;
            }

            var isPurchasable = Realestate.IsPurchasable(packet.LotLocation_X, packet.LotLocation_Y);

            if (!isPurchasable){
                session.Write(new PurchaseLotResponse(){
                    Status = PurchaseLotStatus.FAILED,
                    Reason = PurchaseLotFailureReason.LOT_NOT_PURCHASABLE
                });
                return;
            }

            var name = packet.Name;
            if(!GlobalRealestate.ValidateLotName(name))
            {
                session.Write(new PurchaseLotResponse(){
                    Status = PurchaseLotStatus.FAILED,
                    Reason = PurchaseLotFailureReason.NAME_VALIDATION_ERROR
                });
                return;
            }


            var packedLocation = MapCoordinates.Pack(packet.LotLocation_X, packet.LotLocation_Y);
            var price = Realestate.GetPurchasePrice(packet.LotLocation_X, packet.LotLocation_Y);

            //TODO: ESCROW money
            uint lotId = 0;

            using (var db = DA.Get())
            {
                //TODO: If I already own a lot, move out. For now I'm just making it impossible to do
                if(db.Lots.GetByOwner(session.AvatarId) != null)
                {
                    session.Write(new PurchaseLotResponse()
                    {
                        Status = PurchaseLotStatus.FAILED,
                        Reason = PurchaseLotFailureReason.UNKNOWN
                    });
                    return;
                }

                try
                {
                    lotId = db.Lots.Create(new DbLot {
                        name = name,
                        shard_id = Context.ShardId,

                        location = packedLocation,
                        owner_id = session.AvatarId,
                        created_date = Epoch.Now,
                        category_change_date = Epoch.Default,
                        category = DbLotCategory.none,

                        buildable_area = 1,
                        description = ""
                    });

                    DataService.Invalidate<FSO.Common.DataService.Model.Lot>(packedLocation);
                }catch(Exception ex){
                    //Name taken
                    session.Write(new PurchaseLotResponse()
                    {
                        Status = PurchaseLotStatus.FAILED,
                        Reason = PurchaseLotFailureReason.NAME_TAKEN
                    });
                    return;
                }
            }

            //TODO: Init lot with default thumbnail / blueprint?

            //TODO: Broadcast to the world a new lot exists

            //Update my sim's lot
            var avatar = await DataService.Get<Avatar>(session.AvatarId);
            avatar.Avatar_LotGridXY = packedLocation;
            
            session.Write(new PurchaseLotResponse()
            {
                Status = PurchaseLotStatus.SUCCESS,
                NewLotId = lotId
            });
        }
    }
}

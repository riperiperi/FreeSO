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
        private IShardRealestateDomain Realestate;
        private IDAFactory DA;
        private CityServerContext Context;

        private Regex VALIDATE_NUMERIC = new Regex(".*[0-9]+.*");
        private Regex VALIDATE_SPECIAL_CHARS = new Regex("[a-z|A-Z|-| |']*");


        public PurchaseLotHandler(CityServerContext context, IRealestateDomain realestate, IDAFactory da){
            Context = context;
            Realestate = realestate.GetByShard(context.ShardId);
            DA = da;
        }

        public void Handle(IVoltronSession session, PurchaseLotRequest packet)
        {
            var isPurchasable = Realestate.IsPurchasable(packet.LotLocation_X, packet.LotLocation_Y);

            if (!isPurchasable){
                session.Write(new PurchaseLotResponse(){
                    Status = PurchaseLotStatus.FAILED,
                    Reason = PurchaseLotFailureReason.LOT_NOT_PURCHASABLE
                });
                return;
            }

            var name = packet.Name;
            if(string.IsNullOrEmpty(name) || 
                name.Length < 3 || 
                name.Length > 24 ||
                VALIDATE_NUMERIC.IsMatch(name) || 
                !VALIDATE_SPECIAL_CHARS.IsMatch(name) ||
                name.Split(new char[] { '\'' }).Length > 1 ||
                name.Split(new char[] { '-' }).Length > 1)
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

            session.Write(new PurchaseLotResponse()
            {
                Status = PurchaseLotStatus.SUCCESS,
                NewLotId = lotId
            });
        }
    }
}

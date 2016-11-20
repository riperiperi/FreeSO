using FSO.Common.Serialization;
using FSO.Content.Model;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.EODs.Utils;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.SimAntics.Engine.TSOGlobalLink.Model;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Engine.Utils;
using System.Text.RegularExpressions;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODRackOwnerPlugin : VMEODHandler
    {
        private EODLobby<VMEODPaperChaseSlot> Lobby;
        private RackType RackType;

        //TODO: Read this from tuning variables?
        public const int MAX_OUTFITS = 20;
        public static Regex PRICE_VALIDATION = new Regex("^([1-9]){1}([0-9]){0,5}$");


        public VMEODRackOwnerPlugin(VMEODServer server) : base(server)
        {
            Lobby = new EODLobby<VMEODPaperChaseSlot>(server, 1)
                    .OnFailedToJoinDisconnect();

            PlaintextHandlers["close"] = Lobby.Close;
            PlaintextHandlers["rackowner_stock"] = Stock;
            PlaintextHandlers["rackowner_delete"] = DeleteStock;
            PlaintextHandlers["rackowner_update_price"] = UpdatePrice;
        }

        private void BroadcastStock(VM vm, bool updateNumOutfits)
        {
            vm.GlobalLink.GetOutfits(vm, VMGLOutfitOwner.OBJECT, Server.Object.PersistID, x =>
            {
                var packet = new VMEODRackOwnerBrowseResponse() {
                    Outfits = x
                };
                Lobby.Broadcast("rackowner_browse", packet);

                if (updateNumOutfits)
                {
                    var controller = Controller;
                    if(controller != null){
                        controller.SendOBJEvent(new VMEODEvent((short)VMEODRackOwnerEvent.SetOutfitCount, (short)x.Length));
                    }
                }
            });
        }

        private void UpdatePrice(string evt, string data, VMEODClient client)
        {
            if (Lobby.GetPlayerSlot(client) != 0){
                return;
            }

            var split = data.Split(',');
            uint outfitId = 0;
            int newSalePrice = 0;
            
            if(!uint.TryParse(split[0], out outfitId) || 
                !int.TryParse(split[1], out newSalePrice)){
                return;
            }

            if (!PRICE_VALIDATION.IsMatch(newSalePrice.ToString())){
                return;
            }

            var VM = client.vm;
            
            VM.GlobalLink.UpdateOutfitSalePrice(VM, outfitId, Server.Object.PersistID,newSalePrice, success => {
                BroadcastStock(VM, false);
            });
        }

        private void DeleteStock(string evt, string data, VMEODClient client)
        {
            if (Lobby.GetPlayerSlot(client) != 0)
            {
                return;
            }

            var outfitId = uint.Parse(data);
            var VM = client.vm;

            //TODO: Some kind of refund?
            VM.GlobalLink.DeleteOutfit(VM, outfitId, VMGLOutfitOwner.OBJECT, Server.Object.PersistID, success => {
                BroadcastStock(VM, true);
            });
        }

        private void Stock(string evt, string data, VMEODClient client){
            if (Lobby.GetPlayerSlot(client) != 0){
                return;
            }

            var outfitAssetId = ulong.Parse(data);
            var outfit = Content.Content.Get().RackOutfits.GetByRackType(RackType).Outfits.FirstOrDefault(x => x.AssetID == outfitAssetId);
            if (outfit == null) { return; }

            var VM = client.vm;

            //TODO: Do stores get bulk discounts on clothes?
            VM.GlobalLink.PerformTransaction(VM, false, client.Avatar.PersistID, uint.MaxValue, (int)outfit.Price,
                        
                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                    //TODO: Make this part of global link
                    VM.SendCommand(new VMNetAsyncResponseCmd(0, new VMTransferFundsState
                    {
                        Responded = true,
                        Success = success,
                        TransferAmount = transferAmount,
                        UID1 = uid1,
                        Budget1 = budget1,
                        UID2 = uid2,
                        Budget2 = budget2
                    }));


                    if (success)
                    {
                        //Create the outfit
                        VM.GlobalLink.StockOutfit(VM, Server.Object.PersistID, outfit.AssetID, outfit.Price, (bool created, uint outfitId) => {
                            client.SendOBJEvent(new VMEODEvent((short)VMEODRackOwnerEvent.StockOutfit, 0));
                            BroadcastStock(VM, true);
                        });
                    }else{
                        client.SendOBJEvent(new VMEODEvent((short)VMEODRackOwnerEvent.StockOutfit, 0));
                    }
            });
        }

        public override void OnConnection(VMEODClient client)
        {
            var param = client.Invoker.Thread.TempRegisters;
            if (client.Avatar != null){
                var rackType = param[0];
                if(!Lobby.Join(client, 0)){
                    return;
                }
                RackType = (RackType)rackType;
                client.Send("rackowner_show", ((short)RackType).ToString());
                client.SendOBJEvent(new VMEODEvent((short)VMEODRackOwnerEvent.SetRackType, (short)RackType));
                BroadcastStock(client.vm, false);
            }
        }

        public override void OnDisconnection(VMEODClient client)
        {
            Lobby.Leave(client);
        }

        public VMEODClient Controller
        {
            get
            {
                return Lobby.Players[0];
            }
        }
    }

    public enum VMEODRackOwnerEvent : short
    {
        SetRackType = 1,
        PurchaseOutfit = 2,
        TryOnOutfit = 3,
        StockOutfit = 4,
        SetOutfitCount = 8,
    }


    /// <summary>
    /// Packets
    /// </summary>
    


    public class VMEODRackOwnerBrowseResponse : IoBufferSerializable, IoBufferDeserializable
    {
        public VMGLOutfit[] Outfits;

        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            var length = input.GetInt32();
            Outfits = new VMGLOutfit[length];
            for(var i=0; i < length; i++)
            {
                Outfits[i] = new VMGLOutfit();
                Outfits[i].Deserialize(input, context);
            }
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutInt32(Outfits.Length);
            foreach(var outfit in Outfits){
                outfit.Serialize(output, context);
            }
        }
    }
}

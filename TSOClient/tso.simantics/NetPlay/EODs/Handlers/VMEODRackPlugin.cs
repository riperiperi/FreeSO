using FSO.Content.Model;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Engine.TSOGlobalLink.Model;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODRackPlugin : VMAbstractEODRackPlugin
    {
        public VMEODRackPlugin(VMEODServer server) : base(server)
        {
            PlaintextHandlers["rack_try_outfit_on"] = TryOutfitOn;
            PlaintextHandlers["rack_purchase"] = Purchase;
        }

        private void Purchase(string evt, string data, VMEODClient client)
        {
            var split = data.Split(',');
            if (split.Length != 2) { return; }

            uint outfitId = 0;
            if (!uint.TryParse(split[0], out outfitId)){
                return;
            }

            var putOnNow = false;
            if(!bool.TryParse(split[1], out putOnNow)){
                return;
            }

            var VM = client.vm;

            GetOutfit(VM, outfitId, outfit =>
            {
                if (outfit == null) { return; }

                //Take payment
                VM.GlobalLink.PerformTransaction(client.vm, false, client.Avatar.PersistID, Server.Object.PersistID, (int)outfit.sale_price,

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

                    if (success){
                        //Transfer outfit to my avatar
                        VM.GlobalLink.PurchaseOutfit(VM, outfit.outfit_id, Server.Object.PersistID, client.Avatar.PersistID, purchaseSuccess => {
                            if(purchaseSuccess && putOnNow){
                                PutOnNow(outfit, client);
                            }

                            BroadcastStock(VM, true);
                        });
                    }
                });
            });
        }

        private void PutOnNow(VMGLOutfit outfit, VMEODClient client)
        {
            var slot = GetSuitSlot(false);
            client.vm.SendCommand(new VMNetSetOutfitCmd
            {
                UID = client.Avatar.PersistID,
                Scope = slot,
                Outfit = RackOutfit.GetOutfitID(outfit.asset_id)
            });
            client.SendOBJEvent(new VMEODEvent((short)VMEODRackEvent.PutOnNow, (short)RackType));
        }

        private void TryOutfitOn(string evt, string data, VMEODClient client)
        {
            uint outfitId = 0;
            if(!uint.TryParse(data, out outfitId)){
                return;
            }

            GetOutfit(client.vm, outfitId, outfit => {
                if (outfit == null) { return; }

                var slot = GetSuitSlot(true);

                //store the outfit under dynamic costume
                client.vm.SendCommand(new VMNetSetOutfitCmd {
                    UID = client.Avatar.PersistID,
                    Scope = slot,
                    Outfit = RackOutfit.GetOutfitID(outfit.asset_id)
                });
                //3 uses dynamic costume, by using this we avoid updating default outfits without a good reason to
                client.SendOBJEvent(new VMEODEvent((short)VMEODRackEvent.TryOnOutfit, (short)RackType));
            });
        }

        private VMPersonSuits GetSuitSlot(bool tryOn)
        {
            var slot = VMPersonSuits.DynamicCostume;

            if (!tryOn)
            {
                switch (RackType)
                {
                    case RackType.Formalwear:
                    case RackType.Daywear:
                        slot = VMPersonSuits.DefaultDaywear;
                        break;
                    case RackType.Sleepwear:
                        slot = VMPersonSuits.DefaultSleepwear;
                        break;
                    case RackType.Swimwear:
                        slot = VMPersonSuits.DefaultSwimwear;
                        break;
                }
            }

            switch (RackType)
            {
                case RackType.Decor_Head:
                    slot = VMPersonSuits.DecorationHead;
                    break;
                case RackType.Decor_Back:
                    slot = VMPersonSuits.DecorationBack;
                    break;
                case RackType.Decor_Shoe:
                    slot = VMPersonSuits.DecorationShoes;
                    break;
                case RackType.Decor_Tail:
                    slot = VMPersonSuits.DecorationTail;
                    break;
            }

            return slot;
        }

        public override void OnDisconnection(VMEODClient client)
        {
            client.SendOBJEvent(new VMEODEvent((short)VMEODRackEvent.PutClothesBack, (short)RackType));
            base.OnDisconnection(client);
        }
    }
}
